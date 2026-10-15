using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Lib;
using Jabil.Module.Permission;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process.AxisMotionControlProcess
{
    /// <summary>
    /// 绝对运动流程类（XYZUR五轴同步运动）
    /// 核心逻辑：支持机台状态联动的暂停/继续，确保运动安全可控，增加位置稳定检查
    /// 支持通过全局变量获取补偿值
    /// 指令格式：ABSMoveCompensation|点位类型|点位定义|点位名称|超时|补偿变量前缀(可选)
    /// </summary>
    public class ABSMoveCompensationProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>位置误差允许值（单位：与轴位置单位一致）</summary>
        private const double PositionTolerance = 0.1;

        /// <summary>运动检查延迟时间(毫秒)</summary>
        private const int MotionDelayMs = 10;

        /// <summary>位置稳定判定时间(毫秒)，需保持稳定的最小持续时间</summary>
        private const int PositionStableTimeMs = 10;

        /// <summary>位置变化允许值，稳定期间的最大位置波动</summary>
        private const double StabilityChangeTolerance = 0.05;

        /// <summary>全局变量补偿值默认前缀</summary>
        private const string DefaultCompensationPrefix = "AxisCompensation_";
        #endregion

        #region 私有字段
        private readonly string _process;
        private readonly string _processTip;
        private static readonly object _motionLock = new object();

        // 指令解析结果
        protected internal string _commandName;
        protected internal string _pointType;
        protected internal string _pointDefinition;
        protected internal string _pointName;
        protected internal int _timeoutSeconds;
        protected internal string _compensationPrefix; // 补偿变量前缀

        // 运动参数
        private MotionPointDto _motionPoint;
        private MotionAxisDto _axisX;
        private MotionAxisDto _axisY;
        private MotionAxisDto _axisZ;
        private MotionAxisDto _axisU;
        private MotionAxisDto _axisR;

        // 暂停/继续相关字段
        private volatile bool _isPaused = false;
        private readonly object _statusCheckLock = new object();
        private readonly Dictionary<string, double> _breakPointPositions = new Dictionary<string, double>();

        // 位置反馈
        private double _posX;
        private double _posY;
        private double _posZ;
        private double _posU;
        private double _posR;

        /// <summary>同步运动轴参数列表</summary>
        private readonly List<AxisMotionParam> _syncAxisParams = new List<AxisMotionParam>();

        private CancellationToken _cancellationToken;
        #endregion

        /// <summary>
        /// 同步运动轴参数封装类（扩展稳定检查功能）
        /// </summary>
        private class AxisMotionParam
        {
            /// <summary>轴对象</summary>
            public MotionAxisDto Axis { get; }

            /// <summary>目标位置获取委托</summary>
            public Func<double> GetTargetPos { get; }

            /// <summary>补偿值获取委托</summary>
            public Func<double> GetCompensation { get; set; }

            /// <summary>速度获取委托</summary>
            public Func<double> GetSpeed { get; }

            /// <summary>当前位置</summary>
            public double CurrentPos { get; set; }

            /// <summary>是否完成运动（包括稳定检查）</summary>
            public bool IsCompleted { get; set; }

            /// <summary>是否到达目标位置（未考虑稳定）</summary>
            public bool IsAtTarget { get; set; }

            /// <summary>错误信息</summary>
            public string ErrorMessage { get; set; }

            /// <summary>原始目标位置</summary>
            public double OriginalTargetPos { get; set; }

            // 稳定检查相关字段
            /// <summary>位置稳定计时开始时间</summary>
            public DateTime? StabilityStartTime { get; set; }

            /// <summary>上一次检查的位置</summary>
            public double LastCheckedPos { get; set; }

            /// <summary>稳定状态标记</summary>
            public bool IsStable { get; set; }

            public AxisMotionParam(MotionAxisDto axis, Func<double> getTargetPos, Func<double> getSpeed)
            {
                Axis = axis ?? throw new ArgumentNullException(nameof(axis));
                GetTargetPos = getTargetPos ?? throw new ArgumentNullException(nameof(getTargetPos));
                GetSpeed = getSpeed ?? throw new ArgumentNullException(nameof(getSpeed));
                OriginalTargetPos = getTargetPos();
                GetCompensation = () => 0; // 默认无补偿
            }

            /// <summary>
            /// 获取带补偿的目标位置
            /// </summary>
            public double GetTargetPosWithCompensation()
            {
                return GetTargetPos() + GetCompensation();
            }
        }

        /// <summary>
        /// 构造函数，初始化绝对运动流程实例
        /// </summary>
        public ABSMoveCompensationProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";

                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);
                BindMotionParameters();
            }
            catch (ArgumentException ex)
            {
                Message = $"参数错误: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
            }
            catch (InvalidOperationException ex)
            {
                Message = $"系统错误: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
            }
            catch (Exception ex)
            {
                Message = $"初始化失败: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 设置取消令牌
        /// </summary>
        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// 克隆方法
        /// </summary>
        public override object Clone()
        {
            var clone = new ABSMoveCompensationProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointType = this._pointType,
                _pointDefinition = this._pointDefinition,
                _pointName = this._pointName,
                _timeoutSeconds = this._timeoutSeconds,
                _compensationPrefix = this._compensationPrefix
            };

            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";

            if (_motionPoint != null)
            {
                clone._syncAxisParams.Clear();
                if (clone._axisX != null)
                    clone._syncAxisParams.Add(new AxisMotionParam(clone._axisX, () => _motionPoint.X轴目标位置, () => _motionPoint.X轴运行速度));

                if (clone._axisY != null)
                    clone._syncAxisParams.Add(new AxisMotionParam(clone._axisY, () => _motionPoint.Y轴目标位置, () => _motionPoint.Y轴运行速度));

                if (clone._axisZ != null)
                    clone._syncAxisParams.Add(new AxisMotionParam(clone._axisZ, () => _motionPoint.Z轴目标位置, () => _motionPoint.Z轴运行速度));

                if (clone._axisU != null)
                    clone._syncAxisParams.Add(new AxisMotionParam(clone._axisU, () => _motionPoint.U轴目标位置, () => _motionPoint.U轴运行速度));

                if (clone._axisR != null)
                    clone._syncAxisParams.Add(new AxisMotionParam(clone._axisR, () => _motionPoint.R轴目标位置, () => _motionPoint.R轴运行速度));
            }

            return clone;
        }

        /// <summary>
        /// 解析运动指令字符串
        /// </summary>
        private void ParseCommand(string process)
        {
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 支持6个部分，最后一个是补偿变量前缀（可选）
            if (commandParts.Length < 5)
            {
                HandleMotionError($"指令格式错误: {process}，至少需要5个部分，格式：ABSMove|点位类型|点位定义|点位名称|超时|补偿变量前缀(可选)");
                return;
            }

            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _pointDefinition = commandParts[2];
            _pointName = commandParts[3];

            if (!int.TryParse(commandParts[4], out _timeoutSeconds) || _timeoutSeconds <= 0)
            {
                HandleMotionError($"超时参数无效: {commandParts[4]}，必须是正整数");
                return;
            }

            // 解析可选的补偿变量前缀
            _compensationPrefix = commandParts.Length >= 6 ? commandParts[5] : DefaultCompensationPrefix;
        }

        /// <summary>
        /// 绑定运动参数，包括补偿值获取逻辑
        /// </summary>
        private void BindMotionParameters()
        {
            if (MotionSystem.Instance == null)
            {
                CurrentStatus = GeneralStatus.Error;
                HandleMotionError("运动系统未初始化");
                return;
            }

            _motionPoint = MotionSystem.Instance.MotionPoints
                .FirstOrDefault(p => p.点位名称 == _pointName
                                 && p.点位类型 == _pointType
                                 && p.点位定义 == _pointDefinition);

            if (_motionPoint == null)
            {
                HandleMotionError("未找到匹配的运动点位信息");
                return;
            }

            ProcessId = _motionPoint.绑定代码;

            _axisX = BindAndValidateAxis(_motionPoint.X轴绑定轴, "X");
            if (CurrentStatus == GeneralStatus.Error) return;

            _axisY = BindAndValidateAxis(_motionPoint.Y轴绑定轴, "Y");
            if (CurrentStatus == GeneralStatus.Error) return;

            _axisZ = BindAndValidateAxis(_motionPoint.Z轴绑定轴, "Z");
            if (CurrentStatus == GeneralStatus.Error) return;

            _axisU = BindAndValidateAxis(_motionPoint.U轴绑定轴, "U");
            if (CurrentStatus == GeneralStatus.Error) return;

            _axisR = BindAndValidateAxis(_motionPoint.R轴绑定轴, "R");
            if (CurrentStatus == GeneralStatus.Error) return;

            // 添加轴参数并配置补偿值获取逻辑
            if (_axisX != null)
            {
                var param = new AxisMotionParam(
                    _axisX,
                    () => _motionPoint.X轴目标位置,
                    () => _motionPoint.X轴运行速度);
                param.GetCompensation = () => GetAxisCompensation("X");
                _syncAxisParams.Add(param);
            }

            if (_axisY != null)
            {
                var param = new AxisMotionParam(
                    _axisY,
                    () => _motionPoint.Y轴目标位置,
                    () => _motionPoint.Y轴运行速度);
                param.GetCompensation = () => GetAxisCompensation("Y");
                _syncAxisParams.Add(param);
            }

            if (_axisZ != null)
            {
                var param = new AxisMotionParam(
                    _axisZ,
                    () => _motionPoint.Z轴目标位置,
                    () => _motionPoint.Z轴运行速度);
                param.GetCompensation = () => GetAxisCompensation("Z");
                _syncAxisParams.Add(param);
            }

            if (_axisU != null)
            {
                var param = new AxisMotionParam(
                    _axisU,
                    () => _motionPoint.U轴目标位置,
                    () => _motionPoint.U轴运行速度);
                param.GetCompensation = () => GetAxisCompensation("U");
                _syncAxisParams.Add(param);
            }

            if (_axisR != null)
            {
                var param = new AxisMotionParam(
                    _axisR,
                    () => _motionPoint.R轴目标位置,
                    () => _motionPoint.R轴运行速度);
                param.GetCompensation = () => GetAxisCompensation("R");
                _syncAxisParams.Add(param);
            }

            LogMotionBindingInfo();
        }

        /// <summary>
        /// 从全局变量获取轴补偿值
        /// </summary>
        private double GetAxisCompensation(string axisLabel)
        {
            try
            {
                // 构建全局变量名，格式：[补偿前缀][轴标签]，例如：AxisCompensation_X
                string variableName = $"{_compensationPrefix}{axisLabel}";

                if (_motionPoint.点位定义 == "组装" && _motionPoint.点位类型 == "组装模组")
                {
                    // 定义各轴的允许误差范围
                    double xYTolerance = 0.3;
                    double rTolerance = 1.0;

                    switch (_motionPoint.点位名称)
                    {
                        #region 前滑台
                        case "前组装过渡点1":
                        case "前组装基准点1":
                            var frontPoint1 = Controller.Instance.StationInfo.coordinateData_前滑台_穴位一;
                            return GetCompensationWithLogging(frontPoint1, axisLabel, xYTolerance, rTolerance, "前滑台_穴位一");

                        case "前组装过渡点2":
                        case "前组装基准点2":
                            var frontPoint2 = Controller.Instance.StationInfo.coordinateData_前滑台_穴位二;
                            return GetCompensationWithLogging(frontPoint2, axisLabel, xYTolerance, rTolerance, "前滑台_穴位二");

                        case "前组装过渡点3":
                        case "前组装基准点3":
                            var frontPoint3 = Controller.Instance.StationInfo.coordinateData_前滑台_穴位三;
                            return GetCompensationWithLogging(frontPoint3, axisLabel, xYTolerance, rTolerance, "前滑台_穴位三");

                        case "前组装过渡点4":
                        case "前组装基准点4":
                            var frontPoint4 = Controller.Instance.StationInfo.coordinateData_前滑台_穴位四;
                            return GetCompensationWithLogging(frontPoint4, axisLabel, xYTolerance, rTolerance, "前滑台_穴位四");
                        #endregion

                        #region 后滑台
                        case "后组装过渡点1":
                        case "后组装基准点1":
                            var backPoint1 = Controller.Instance.StationInfo.coordinateData_后滑台_穴位一;
                            return GetCompensationWithLogging(backPoint1, axisLabel, xYTolerance, rTolerance, "后滑台_穴位一");

                        case "后组装过渡点2":
                        case "后组装基准点2":
                            var backPoint2 = Controller.Instance.StationInfo.coordinateData_后滑台_穴位二;
                            return GetCompensationWithLogging(backPoint2, axisLabel, xYTolerance, rTolerance, "后滑台_穴位二");

                        case "后组装过渡点3":
                        case "后组装基准点3":
                            var backPoint3 = Controller.Instance.StationInfo.coordinateData_后滑台_穴位三;
                            return GetCompensationWithLogging(backPoint3, axisLabel, xYTolerance, rTolerance, "后滑台_穴位三");

                        case "后组装过渡点4":
                        case "后组装基准点4":
                            var backPoint4 = Controller.Instance.StationInfo.coordinateData_后滑台_穴位四;
                            return GetCompensationWithLogging(backPoint4, axisLabel, xYTolerance, rTolerance, "后滑台_穴位四");
                        #endregion

                        default:
                            return 0;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception($"获取{axisLabel}轴补偿值失败", ex));
                return 0;
            }
        }

        /// <summary>
        /// 获取补偿值并在超限时记录日志，超过设定值则按设定值补偿
        /// </summary>
        private double GetCompensationWithLogging(dynamic coordinateData, string axisLabel,
                                                 double xYTolerance, double rTolerance, string pointDescription)
        {
            double compensationValue = 0;
            double tolerance = axisLabel == "R" ? rTolerance : xYTolerance;

            // 根据轴标签获取对应补偿值
            switch (axisLabel)
            {
                case "X":
                    compensationValue = coordinateData.X;
                    break;
                case "Y":
                    compensationValue = coordinateData.Y;
                    break;
                case "R":
                    compensationValue = coordinateData.R;
                    break;
                default:
                    return 0;
            }

            // 检查是否超限
            if (Math.Abs(compensationValue) > tolerance)
            {
                // 计算限制后的补偿值（保留符号，值为设定的最大补偿值）
                double limitedValue = Math.Sign(compensationValue) * tolerance;

                // 记录超限日志，包含详细信息
                string logMessage = $"轴[{axisLabel}]补偿值超限！已自动调整为设定最大值" +
                                   $"\r\n点位: {_motionPoint.点位名称}({pointDescription})" +
                                   $"\r\n原始补偿值: {compensationValue}" +
                                   $"\r\n设定最大值: {tolerance}" +
                                   $"\r\n调整后补偿值: {limitedValue}" +
                                   $"\r\n超限值: {Math.Abs(compensationValue) - tolerance}";

                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Orange,  // 使用橙色标识警告级别
                    "补偿值超限",
                    axisLabel,
                    logMessage
                );

                // 超限返回设定的最大补偿值
                return limitedValue;
            }

            // 未超限返回实际补偿值
            return compensationValue;
        }

        /// <summary>
        /// 绑定并验证轴信息
        /// </summary>
        private MotionAxisDto BindAndValidateAxis(string axisName, string axisLabel)
        {
            if (string.IsNullOrWhiteSpace(axisName) || axisName.Contains("未绑定"))
                return null;

            var axis = MotionSystem.Instance.MotionAxes
                .FirstOrDefault(a => a.轴名称 == axisName);

            if (axis == null)
            {
                HandleMotionError($"未找到{axisLabel}轴绑定信息: {axisName}");
                return null;
            }

            return axis;
        }

        /// <summary>
        /// 记录运动绑定信息日志
        /// </summary>
        private void LogMotionBindingInfo()
        {
            var logMessage = new StringBuilder();
            logMessage.AppendLine($"运动点位绑定信息 - {_pointName}");
            logMessage.AppendLine($"X轴: {(_axisX?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.X轴目标位置 ?? 0} 补偿变量: {_compensationPrefix}X");
            logMessage.AppendLine($"Y轴: {(_axisY?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.Y轴目标位置 ?? 0} 补偿变量: {_compensationPrefix}Y");
            logMessage.AppendLine($"Z轴: {(_axisZ?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.Z轴目标位置 ?? 0} 补偿变量: {_compensationPrefix}Z");
            logMessage.AppendLine($"U轴: {(_axisU?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.U轴目标位置 ?? 0} 补偿变量: {_compensationPrefix}U");
            logMessage.AppendLine($"R轴: {(_axisR?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.R轴目标位置 ?? 0} 补偿变量: {_compensationPrefix}R");
            logMessage.AppendLine($"超时设置: {_timeoutSeconds}秒");
            logMessage.AppendLine($"位置稳定要求: {PositionStableTimeMs}ms内波动≤{StabilityChangeTolerance}");

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Gray,
                "运动绑定信息",
                _pointName,
                logMessage.ToString()
            );
        }

        /// <summary>
        /// 处理运动错误
        /// </summary>
        private void HandleMotionError(string errorMessage)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _process,
                $"--->>错误：{errorMessage}！！！！"
            );
            CurrentStatus = GeneralStatus.Error;
            throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// 执行绝对运动的主方法
        /// </summary>
        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";

                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }
                //防呆逻辑！！！重要 
                switch (_pointDefinition)
                {
                    case "上料":
                        if (!SafetyInterlock.IsFeedingYZMovementAllowed(_motionPoint.Y轴目标位置, out string feedingError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"上料模组-安全互锁不满足，无法移动！原因：{feedingError}");
                            Message = "上料模组-安全互锁不满足，无法移动！";
                            return;
                        }
                        break;

                    case "检测X/旋转轴":
                    case "检测X轴":
                        if (!SafetyInterlock.IsInspectionAxisMovementAllowed(out string inspectionError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"检测轴-安全互锁不满足，无法移动！原因：{inspectionError}");
                            Message = "检测轴-安全互锁不满足，无法移动！";
                            return;
                        }
                        break;
                    case "分料":
                        if (!SafetyInterlock.IsSortingMovementAllowed(_motionPoint.Y轴目标位置, out string sortingError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"分料模组-安全互锁不满足，无法移动！原因：{sortingError}");
                            Message = "分料轴-安全互锁不满足，无法移动！";
                            return;
                        }
                        break;
                    case "缓冲取放":
                    case "缓冲轴":
                        if (!SafetyInterlock.IsBufferXAxisMovementAllowed(out string bufferError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"缓冲轴-安全互锁不满足，无法移动！原因：{bufferError}");
                            Message = "缓冲轴-安全互锁不满足，无法移动！";
                            return;
                        }
                        break;
                    default:
                        break;
                }

                if (NoAxesToMove())
                {
                    Message = $"错误的指令：{_process}\r\n报错原因：未绑定任何轴";
                    CurrentStatus = GeneralStatus.Error;
                    return;
                }

                if (_syncAxisParams.Any())
                {
                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Blue,
                        "同步运动开始",
                        "XYUR轴",
                        "开始多轴同步运动"
                    );

                    var syncStopwatch = Stopwatch.StartNew();

                    if (!SendSyncAxisMoveCommands())
                    {
                        StopSyncAxes();
                        return;
                    }
                    Thread.Sleep(100);
                    MonitorSyncAxisMotion(syncStopwatch);

                    var errorParams = _syncAxisParams.Where(p => !string.IsNullOrEmpty(p.ErrorMessage)).ToList();
                    if (errorParams.Any())
                    {
                        CurrentStatus = GeneralStatus.Error;
                        Message = string.Join("\r\n", errorParams.Select(p =>
                            $"轴[{p.Axis.轴名称}]错误: {p.ErrorMessage}"));

                        ProcessLog.Instance.SaveLog(
                            _processTip,
                            Color.Red,
                            "同步运动错误",
                            "XYUR轴",
                            Message
                        );
                        return;
                    }

                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Green,
                        "同步运动完成",
                        "XYUR轴",
                        $"所有轴已到达目标位置并稳定，总耗时: {syncStopwatch.Elapsed.TotalSeconds:F3}秒"
                    );
                }

                UpdateCompletionMessage();
            }
            catch (OperationCanceledException)
            {
                Message = "运动已被取消";
                CurrentStatus = GeneralStatus.Error;
                StopAllAxes();
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                StopAllAxes();
            }
        }

        /// <summary>
        /// 检查是否没有需要运动的轴
        /// </summary>
        private bool NoAxesToMove()
        {
            return (_axisZ == null) && !_syncAxisParams.Any();
        }

        /// <summary>
        /// 向XYZUR五轴同时发送运动指令（包含补偿值）
        /// </summary>
        private bool SendSyncAxisMoveCommands()
        {
            lock (_motionLock)
            {
                foreach (var param in _syncAxisParams)
                {
                    try
                    {
                        param.ErrorMessage = null;
                        // 获取带补偿的目标位置
                        double targetPosWithComp = param.GetTargetPosWithCompensation();

                        ProcessLog.Instance.SaveLog(
                            _processTip,
                            Color.Blue,
                            "轴运动指令",
                            param.Axis.轴名称,
                            $"目标位置: {param.GetTargetPos()}，补偿值: {param.GetCompensation()}，实际目标: {targetPosWithComp}, 速度: {param.GetSpeed()}"
                        );

                        string moveResult = MotionSystem.Instance.MotionController
                            .AbsMove(param.Axis, targetPosWithComp, param.GetSpeed());

                        if (moveResult != "NoError")
                        {
                            CurrentStatus = GeneralStatus.Error;
                            param.ErrorMessage = $"运动指令失败: {moveResult}";
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        CurrentStatus = GeneralStatus.Error;
                        param.CurrentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                        param.ErrorMessage = $"发送指令异常: {ex.Message}\r\n目标位置: {param.GetTargetPosWithCompensation()}";
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 发送继续运动指令（包含补偿值）
        /// </summary>
        private bool SendResumeMotionCommands()
        {
            lock (_motionLock)
            {
                foreach (var param in _syncAxisParams)
                {
                    if (param.IsCompleted)
                    {
                        param.ErrorMessage = null;
                        continue;
                    }

                    try
                    {
                        double currentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                        double targetPosWithComp = param.GetTargetPosWithCompensation();
                        double originalTargetWithComp = param.OriginalTargetPos + param.GetCompensation();

                        // 重置稳定检查状态
                        param.StabilityStartTime = null;
                        param.IsStable = false;
                        param.IsAtTarget = false;
                        param.LastCheckedPos = currentPos;

                        if (Math.Abs(targetPosWithComp - originalTargetWithComp) > PositionTolerance)
                        {
                            ProcessLog.Instance.SaveLog(
                                _processTip,
                                Color.Yellow,
                                "目标位置已更新",
                                param.Axis.轴名称,
                                $"暂停期间目标位置变更: 原{originalTargetWithComp:F4} → 新{targetPosWithComp:F4} (含补偿)"
                            );
                            param.OriginalTargetPos = param.GetTargetPos();
                        }

                        ProcessLog.Instance.SaveLog(
                            _processTip,
                            Color.Blue,
                            "轴继续运动指令",
                            param.Axis.轴名称,
                            $"从当前位置 {currentPos:F4} 继续运动到目标 {targetPosWithComp:F4} (含补偿), 速度: {param.GetSpeed():F2}"
                        );

                        string moveResult = MotionSystem.Instance.MotionController
                            .AbsMove(param.Axis, targetPosWithComp, param.GetSpeed());

                        if (moveResult != "NoError")
                        {
                            CurrentStatus = GeneralStatus.Error;
                            param.ErrorMessage = $"继续运动指令失败: {moveResult}";
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        CurrentStatus = GeneralStatus.Error;
                        param.ErrorMessage = $"发送继续指令异常: {ex.Message}";
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 监控XYZUR五轴同步运动状态（包含稳定检查）
        /// </summary>
        private void MonitorSyncAxisMotion(Stopwatch stopwatch)
        {
            using (var waitHandle = new ManualResetEventSlim(false))
            {
                while (true)
                {
                    #region 1. 响应外部取消请求
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        ProcessLog.Instance.SaveLog(
                            _processTip,
                            Color.Orange,
                            "外部取消触发",
                            "所有轴",
                            "收到外部取消请求，准备终止运动"
                        );
                        throw new OperationCanceledException("运动被外部请求取消");
                    }
                    #endregion

                    #region 2. 处理机台状态切换
                    lock (_statusCheckLock)
                    {
                        var currentMachineStatus = MenuProxy.Instance.MachineRunStatus;
                        var currentMotionStatus = CurrentStatus;

                        if (IsMachineResumeStatus(currentMachineStatus)
                            && _isPaused
                            && currentMotionStatus == GeneralStatus.Wait)
                        {
                            HandleMotionResume(currentMachineStatus, stopwatch);
                        }
                        else if (IsMachinePauseStatus(currentMachineStatus)
                                 && !_isPaused
                                 && currentMotionStatus == GeneralStatus.Running)
                        {
                            HandleMotionPause(currentMachineStatus, stopwatch);
                        }
                        if (CurrentStatus == GeneralStatus.Error) break;

                    }
                    #endregion

                    #region 3. 检查全局异常状态
                    if (CheckGlobalMachineStatus())
                        break;
                    #endregion

                    #region 4. 检查运动超时
                    if (!_isPaused && stopwatch.Elapsed.TotalSeconds > _timeoutSeconds)
                    {
                        HandleMotionTimeout(stopwatch, _timeoutSeconds);
                        break;
                    }
                    #endregion

                    #region 5. 更新轴运动状态（包含稳定检查）
                    lock (_syncAxisParams)
                    {
                        UpdateSyncAxisStatusWithStabilityCheck();

                        // 所有轴完成运动且稳定后退出
                        if (_syncAxisParams.All(p => p.IsCompleted))
                        {
                            ProcessLog.Instance.SaveLog(
                                _processTip,
                                Color.Green,
                                "运动监控完成",
                                "所有轴",
                                $"所有轴已到达目标位置并稳定{PositionStableTimeMs}ms，监控循环终止"
                            );
                            break;
                        }
                    }
                    #endregion

                    waitHandle.Wait(MotionDelayMs, _cancellationToken);
                }
            }
        }

        #region 辅助方法
        private bool IsMachineResumeStatus(MachineRunStatus status)
        {
            return status == MachineRunStatus.Running || status == MachineRunStatus.Homing;
        }

        private bool IsMachinePauseStatus(MachineRunStatus status)
        {
            return status == MachineRunStatus.Paused || status == MachineRunStatus.Alarming;
        }

        private void HandleMotionResume(MachineRunStatus machineStatus, Stopwatch stopwatch)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Orange,
                "状态触发继续",
                "所有轴",
                $"机台状态从暂停切换为【{machineStatus}】，准备恢复运动"
            );

            bool resumeSuccess = SendResumeMotionCommands();
            if (resumeSuccess)
            {
                _isPaused = false;
                CurrentStatus = GeneralStatus.Running;
                Message = $"Running（从{machineStatus}恢复）";
                stopwatch.Start();

                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Green,
                    "继续运动成功",
                    "所有轴",
                    $"已从断点恢复运动，累计耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒"
                );
            }
            else
            {
                CurrentStatus = GeneralStatus.Error;
                Message = $"从{machineStatus}恢复运动失败，指令发送错误";
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Red,
                    "继续运动失败",
                    "所有轴",
                    $"{Message}，运动终止"
                );
            }
        }

        private void HandleMotionPause(MachineRunStatus machineStatus, Stopwatch stopwatch)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Orange,
                "状态触发暂停",
                "所有轴",
                $"机台状态从运行切换为【{machineStatus}】，准备停止运动"
            );

            StopAllAxes();
            _breakPointPositions.Clear();
            foreach (var param in _syncAxisParams.Where(p => !p.IsCompleted))
            {
                double currentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                _breakPointPositions[param.Axis.轴名称] = currentPos;

                // 暂停时记录稳定检查状态
                string stabilityStatus = param.IsAtTarget
                    ? $"已到达目标位置，稳定计时: {param.StabilityStartTime?.ToString("mm:ss.fff") ?? "未开始"}"
                    : "未到达目标位置";

                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Orange,
                    "轴断点记录",
                    param.Axis.轴名称,
                    $"暂停位置: {currentPos:F4}, 剩余距离: {Math.Abs(param.GetTargetPosWithCompensation() - currentPos):F4}, 稳定状态: {stabilityStatus}"
                );
            }

            _isPaused = true;
            CurrentStatus = GeneralStatus.Wait;
            Message = $"Wait（因{machineStatus}暂停）";
            stopwatch.Stop();

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Orange,
                "暂停运动成功",
                "所有轴",
                $"所有轴已停止，累计耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒"
            );
        }

        private void HandleMotionTimeout(Stopwatch stopwatch, int timeoutSeconds)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "运动超时触发",
                "所有轴",
                $"运动耗时超过{timeoutSeconds}秒，准备停止所有轴"
            );

            StopSyncAxes();

            foreach (var param in _syncAxisParams.Where(p => !p.IsCompleted))
            {
                CurrentStatus = GeneralStatus.Error;
                param.CurrentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);

                // 超时原因细分：未到达目标/未稳定
                string timeoutReason = param.IsAtTarget
                    ? $"已到达目标但未稳定（需稳定{PositionStableTimeMs}ms）"
                    : "未到达目标位置";

                param.ErrorMessage = $"运动超时（设定{timeoutSeconds}秒，实际{stopwatch.Elapsed.TotalSeconds:F1}秒）\r\n" +
                                    $"原因: {timeoutReason}\r\n" +
                                    $"目标(含补偿): {param.GetTargetPosWithCompensation():F4}, 实际: {param.CurrentPos:F4}";
            }

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "运动超时终止",
                "所有轴",
                $"已停止所有未完成轴，超时详情见各轴错误日志"
            );
        }
        #endregion

        /// <summary>
        /// 更新XYZUR五轴的运动状态（包含稳定检查）
        /// </summary>
        private void UpdateSyncAxisStatusWithStabilityCheck()
        {
            foreach (var param in _syncAxisParams)
            {
                if (param.IsCompleted) continue;

                // 1. 检查是否到达带补偿的目标位置
                param.CurrentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                double targetPosWithComp = param.GetTargetPosWithCompensation();
                bool isAtTargetNow = Math.Abs(targetPosWithComp - param.CurrentPos) < PositionTolerance;

                // 2. 首次到达目标位置时记录日志
                if (isAtTargetNow && !param.IsAtTarget)
                {
                    param.IsAtTarget = true;
                    param.LastCheckedPos = param.CurrentPos;
                    param.StabilityStartTime = DateTime.Now;

                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Blue,
                        "到达目标位置",
                        param.Axis.轴名称,
                        $"已到达目标位置(含补偿)，开始稳定检查（需保持{PositionStableTimeMs}ms）\r\n" +
                        $"目标(含补偿): {targetPosWithComp:F4}, 当前: {param.CurrentPos:F4}, 误差: {Math.Abs(targetPosWithComp - param.CurrentPos):F4}"
                    );
                }
                // 3. 若已离开目标位置，重置稳定检查状态
                else if (!isAtTargetNow && param.IsAtTarget)
                {
                    param.IsAtTarget = false;
                    param.IsStable = false;
                    param.StabilityStartTime = null;

                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Yellow,
                        "离开目标位置",
                        param.Axis.轴名称,
                        $"已离开目标位置(含补偿)，重置稳定检查\r\n" +
                        $"目标(含补偿): {targetPosWithComp:F4}, 当前: {param.CurrentPos:F4}, 误差: {Math.Abs(targetPosWithComp - param.CurrentPos):F4}"
                    );
                }

                // 4. 已到达目标位置，进行稳定检查
                if (param.IsAtTarget && !param.IsStable)
                {
                    // 检查位置变化是否在允许范围内
                    bool positionStable = Math.Abs(param.CurrentPos - param.LastCheckedPos) < StabilityChangeTolerance;
                    param.LastCheckedPos = param.CurrentPos;

                    if (!positionStable)
                    {
                        // 位置波动超限，重置稳定计时
                        param.StabilityStartTime = DateTime.Now;
                        ProcessLog.Instance.SaveLog(
                            _processTip,
                            Color.Yellow,
                            "位置波动",
                            param.Axis.轴名称,
                            $"位置波动超限，重置稳定计时\r\n" +
                            $"当前: {param.CurrentPos:F4}, 上次: {param.LastCheckedPos:F4}, 变化: {Math.Abs(param.CurrentPos - param.LastCheckedPos):F4}"
                        );
                    }
                    else
                    {
                        // 计算已稳定时间
                        var stableDuration = DateTime.Now - param.StabilityStartTime.Value;
                        if (stableDuration.TotalMilliseconds >= PositionStableTimeMs)
                        {
                            // 达到稳定时间要求
                            param.IsStable = true;
                            param.IsCompleted = true;

                            ProcessLog.Instance.SaveLog(
                                _processTip,
                                Color.Green,
                                "位置稳定完成",
                                param.Axis.轴名称,
                                $"已在目标位置(含补偿)稳定{PositionStableTimeMs}ms，运动完成\r\n" +
                                $"目标(含补偿): {targetPosWithComp:F4}, 最终位置: {param.CurrentPos:F4}, 稳定时长: {stableDuration.TotalMilliseconds:F0}ms"
                            );

                            // 更新最终位置记录
                            if (param.Axis == _axisX) _posX = param.CurrentPos;
                            else if (param.Axis == _axisY) _posY = param.CurrentPos;
                            else if (param.Axis == _axisZ) _posZ = param.CurrentPos;
                            else if (param.Axis == _axisU) _posU = param.CurrentPos;
                            else if (param.Axis == _axisR) _posR = param.CurrentPos;
                        }
                        else if (stableDuration.TotalMilliseconds % 1000 < MotionDelayMs) // 每1秒记录一次稳定进度
                        {
                            ProcessLog.Instance.SaveLog(
                                _processTip,
                                Color.Gray,
                                "稳定检查中",
                                param.Axis.轴名称,
                                $"位置稳定中... 已稳定: {stableDuration.TotalMilliseconds:F0}ms / 需{PositionStableTimeMs}ms"
                            );
                        }
                    }
                }

                // 5. 检查伺服报警
                if (MotionSystem.Instance.MotionController.CheckServoAlarm(param.Axis))
                {
                    CurrentStatus = GeneralStatus.Error;
                    param.ErrorMessage = $"运动中发生伺服报警\r\n当前位置: {param.CurrentPos}";
                    return;
                }
            }
        }

        /// <summary>
        /// 检查全局机台状态
        /// </summary>
        private bool CheckGlobalMachineStatus()
        {
            var isEmergency = MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped;

            if (isEmergency)
            {
                StopSyncAxes();
                var errorType = "紧急停止报警";

                foreach (var param in _syncAxisParams)
                {
                    CurrentStatus = GeneralStatus.Error;
                    param.CurrentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);

                    // 包含稳定状态信息
                    string stabilityInfo = param.IsAtTarget
                        ? $"已到达目标，稳定计时: {(DateTime.Now - param.StabilityStartTime.Value).TotalMilliseconds:F0}ms"
                        : "未到达目标位置";

                    param.ErrorMessage = $"{errorType}\r\n{stabilityInfo}\r\n" +
                                        $"目标(含补偿): {param.GetTargetPosWithCompensation()}, 实际: {param.CurrentPos}";
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 停止所有同步轴
        /// </summary>
        private void StopSyncAxes()
        {
            lock (_syncAxisParams)
            {
                foreach (var param in _syncAxisParams.Where(p => !p.IsCompleted && p.Axis != null))
                {
                    try
                    {
                        MotionSystem.Instance.MotionController.SmtStop(param.Axis, 0);
                        ProcessLog.Instance.SaveLog(
                            _processTip,
                            Color.Orange,
                            "轴已停止",
                            param.Axis.轴名称,
                            "因异常情况或暂停请求停止运动"
                        );
                    }
                    catch (Exception ex)
                    {
                        RLog.SaveError(new Exception($"停止轴[{param.Axis.轴名称}]失败", ex));
                    }
                }
            }
        }

        /// <summary>
        /// 停止所有轴
        /// </summary>
        private void StopAllAxes()
        {
            StopSyncAxes();
        }

        /// <summary>
        /// 更新运动完成信息
        /// </summary>
        private void UpdateCompletionMessage()
        {
            var message = new StringBuilder();
            message.AppendLine("NoError");
            message.AppendLine($"各轴已到达目标位置并稳定{PositionStableTimeMs}ms：");
            message.AppendLine($"X轴实际点位：  {_posX:F4} (补偿值: {(_axisX != null ? GetAxisCompensation("X") : 0):F4})");
            message.AppendLine($"Y轴实际点位：  {_posY:F4} (补偿值: {(_axisY != null ? GetAxisCompensation("Y") : 0):F4})");
            message.AppendLine($"Z轴实际点位：  {_posZ:F4} (补偿值: {(_axisZ != null ? GetAxisCompensation("Z") : 0):F4})");
            message.AppendLine($"U轴实际点位：  {_posU:F4} (补偿值: {(_axisU != null ? GetAxisCompensation("U") : 0):F4})");
            message.AppendLine($"R轴实际点位：  {_posR:F4} (补偿值: {(_axisR != null ? GetAxisCompensation("R") : 0):F4})");

            Message = message.ToString();
            CurrentStatus = GeneralStatus.NoError;
        }
    }
}
