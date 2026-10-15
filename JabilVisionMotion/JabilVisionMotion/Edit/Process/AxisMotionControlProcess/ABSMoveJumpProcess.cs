using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion;
using Jabil.Module.Motion.Common.Dtos;
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
    /// 绝对运动流程类（Z轴优先运动，XYUR四轴同步运动）
    /// 核心逻辑：Z轴先运动到安全高度 → 移动XYUR轴 → Z轴再运动到目标高度
    /// 支持机台状态联动的暂停/继续功能和位置稳定检查
    /// 指令格式：ABSMoveJump|点位类型|点位定义|点位名称|安全高度|超时
    /// </summary>
    public class ABSMoveJumpProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>
        /// 位置误差允许值，用于判断是否到达目标位置
        /// 单位：与轴位置单位一致（如毫米）
        /// </summary>
        private const double PositionTolerance = 0.1;

        /// <summary>
        /// 运动检查延迟时间(毫秒)
        /// 用于控制运动状态检查的频率，避免CPU占用过高
        /// </summary>
        private const int MotionDelayMs = 100;

        /// <summary>
        /// 位置稳定判定时间(毫秒)
        /// 位置需在此时间内保持稳定才认为运动完成
        /// </summary>
        private const int PositionStableTimeMs = 100;

        /// <summary>
        /// 位置稳定允许的最大变化量
        /// 用于判断位置是否真正稳定
        /// </summary>
        private const double StabilityChangeTolerance = 0.05;
        #endregion

        #region 私有字段
        private readonly string _process;          // 运动指令字符串
        private readonly string _processTip;       // 运动指令提示信息
        private static readonly object _motionLock = new object(); // 运动控制的线程锁，确保线程安全

        // 指令解析结果
        protected internal string _commandName;       // 指令名称（固定为"ABSMoveJump"）
        protected internal string _pointType;         // 点位类型
        protected internal string _pointDefinition;   // 点位定义
        protected internal string _pointName;         // 点位名称
        protected internal double _safeHeight;        // 安全高度
        protected internal int _timeoutSeconds;       // 超时时间（秒）

        // 运动参数
        private MotionPointDto _motionPoint;  // 运动点位数据传输对象
        private MotionAxisDto _axisX;         // X轴数据传输对象
        private MotionAxisDto _axisY;         // Y轴数据传输对象
        private MotionAxisDto _axisZ;         // Z轴数据传输对象
        private MotionAxisDto _axisU;         // U轴数据传输对象
        private MotionAxisDto _axisR;         // R轴数据传输对象

        // 位置反馈
        private double _posX;  // X轴实际位置
        private double _posY;  // Y轴实际位置
        private double _posZ;  // Z轴实际位置
        private double _posU;  // U轴实际位置
        private double _posR;  // R轴实际位置

        // 位置稳定跟踪变量
        private Dictionary<string, double> _lastPositions = new Dictionary<string, double>(); // 上一次位置记录
        private Dictionary<string, DateTime?> _stableStartTimes = new Dictionary<string, DateTime?>(); // 稳定开始时间

        private readonly List<AxisMotionParam> _syncAxisParams = new List<AxisMotionParam>(); // 同步运动轴参数列表
        private CancellationToken _cancellationToken;  // 取消令牌，用于外部取消运动

        // 暂停/继续相关字段（线程安全设计）
        private volatile bool _isPaused = false; // 标记是否处于暂停状态（volatile确保多线程可见性）
        private readonly object _statusCheckLock = new object(); // 状态检查与切换的锁
        private readonly Dictionary<string, double> _breakPointPositions = new Dictionary<string, double>(); // 轴暂停时的位置记录
        private MotionPhase _currentPhase = MotionPhase.NotStarted; // 当前运动阶段
        private double _originalZTarget; // Z轴原始目标位置（用于暂停后恢复）
        #endregion

        /// <summary>
        /// 运动阶段枚举
        /// 用于跟踪当前运动处于哪个阶段，支持分阶段暂停/继续
        /// </summary>
        private enum MotionPhase
        {
            NotStarted,       // 未开始
            ZAxisToSafe,      // Z轴运动到安全高度
            XYURMoving,       // XYUR轴同步运动
            ZAxisToTarget     // Z轴运动到目标位置
        }

        /// <summary>
        /// 同步运动轴参数封装类
        /// 封装了单个轴的运动参数和状态信息
        /// </summary>
        private class AxisMotionParam
        {
            public MotionAxisDto Axis { get; }               // 轴信息对象
            public Func<double> GetTargetPos { get; }        // 获取目标位置的委托
            public Func<double> GetSpeed { get; }            // 获取运动速度的委托
            public double CurrentPos { get; set; }           // 当前位置
            public double LastPos { get; set; }              // 上一次位置
            public DateTime? StableStartTime { get; set; }   // 稳定开始时间
            public bool IsCompleted { get; set; }            // 运动是否完成（包括稳定）
            public string ErrorMessage { get; set; }         // 错误信息（如有）
            public double OriginalTargetPos { get; set; }    // 原始目标位置，用于暂停后校验是否有变更

            /// <summary>
            /// 构造函数，初始化轴运动参数
            /// </summary>
            public AxisMotionParam(MotionAxisDto axis, Func<double> getTargetPos, Func<double> getSpeed)
            {
                Axis = axis ?? throw new ArgumentNullException(nameof(axis));
                GetTargetPos = getTargetPos ?? throw new ArgumentNullException(nameof(getTargetPos));
                GetSpeed = getSpeed ?? throw new ArgumentNullException(nameof(getSpeed));
                OriginalTargetPos = getTargetPos(); // 记录初始目标位置
            }
        }

        /// <summary>
        /// 构造函数，初始化绝对运动流程实例
        /// </summary>
        /// <param name="process">运动指令字符串</param>
        /// <param name="processTip">运动指令提示信息</param>
        public ABSMoveJumpProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait;  // 初始状态设为等待
                Message = "Wait";

                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);       // 解析运动指令
                BindMotionParameters();      // 绑定运动参数
            }
            catch (ArgumentException ex)
            {
                Message = $"参数错误: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return;
            }
            catch (InvalidOperationException ex)
            {
                Message = $"系统错误: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return;
            }
            catch (Exception ex)
            {
                Message = $"初始化失败: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return;
            }
        }

        /// <summary>
        /// 设置取消令牌，用于外部取消运动
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// 克隆方法，创建当前对象的副本
        /// 用于在需要重复执行相同运动时创建新实例
        /// </summary>
        public override object Clone()
        {
            var clone = new ABSMoveJumpProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointType = this._pointType,
                _pointDefinition = this._pointDefinition,
                _pointName = this._pointName,
                _safeHeight = this._safeHeight,
                _timeoutSeconds = this._timeoutSeconds
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

                if (clone._axisU != null)
                    clone._syncAxisParams.Add(new AxisMotionParam(clone._axisU, () => _motionPoint.U轴目标位置, () => _motionPoint.U轴运行速度));

                if (clone._axisR != null)
                    clone._syncAxisParams.Add(new AxisMotionParam(clone._axisR, () => _motionPoint.R轴目标位置, () => _motionPoint.R轴运行速度));
            }

            return clone;
        }

        /// <summary>
        /// 解析运动指令字符串
        /// 指令格式：ABSMoveJump|点位类型|点位定义|点位名称|安全高度|超时
        /// </summary>
        private void ParseCommand(string process)
        {
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (commandParts.Length < 6)
            {
                HandleMotionError($"指令格式错误: {process}，需要6个部分，格式：ABSMoveJump|点位类型|点位定义|点位名称|安全高度|超时");
                return;
            }

            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _pointDefinition = commandParts[2];
            _pointName = commandParts[3];

            // 解析安全高度
            if (!double.TryParse(commandParts[4], out _safeHeight))
            {
                HandleMotionError($"安全高度参数无效: {commandParts[4]}，必须是数字");
                return;
            }

            // 解析超时时间
            if (!int.TryParse(commandParts[5], out _timeoutSeconds) || _timeoutSeconds <= 0)
            {
                HandleMotionError($"超时参数无效: {commandParts[5]}，必须是正整数");
                return;
            }
        }

        /// <summary>
        /// 绑定运动参数
        /// 从运动系统中获取与指令匹配的点位信息和轴信息
        /// </summary>
        private void BindMotionParameters()
        {
            if (MotionSystem.Instance == null)
            {
                CurrentStatus = GeneralStatus.Error;
                HandleMotionError("运动系统未初始化");
                return;
            }

            // 查找匹配的运动点位
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

            // 绑定并验证各轴信息
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

            // Z轴是必须的，因为需要运动到安全高度
            if (_axisZ == null)
            {
                HandleMotionError("Z轴未绑定，无法执行安全高度运动");
                return;
            }

            // 为需要同步运动的轴添加参数
            if (_axisX != null)
                _syncAxisParams.Add(new AxisMotionParam(
                    _axisX,
                    () => _motionPoint.X轴目标位置,
                    () => _motionPoint.X轴运行速度));

            if (_axisY != null)
                _syncAxisParams.Add(new AxisMotionParam(
                    _axisY,
                    () => _motionPoint.Y轴目标位置,
                    () => _motionPoint.Y轴运行速度));

            if (_axisU != null)
                _syncAxisParams.Add(new AxisMotionParam(
                    _axisU,
                    () => _motionPoint.U轴目标位置,
                    () => _motionPoint.U轴运行速度));

            if (_axisR != null)
                _syncAxisParams.Add(new AxisMotionParam(
                    _axisR,
                    () => _motionPoint.R轴目标位置,
                    () => _motionPoint.R轴运行速度));

            // 初始化稳定检查所需的字典
            InitializeStabilityTracking();

            // 记录运动绑定信息日志
            LogMotionBindingInfo();
        }

        /// <summary>
        /// 初始化位置稳定跟踪所需的数据结构
        /// </summary>
        private void InitializeStabilityTracking()
        {
            _lastPositions.Clear();
            _stableStartTimes.Clear();

            if (_axisX != null)
            {
                _lastPositions[_axisX.轴名称] = 0;
                _stableStartTimes[_axisX.轴名称] = null;
            }
            if (_axisY != null)
            {
                _lastPositions[_axisY.轴名称] = 0;
                _stableStartTimes[_axisY.轴名称] = null;
            }
            if (_axisZ != null)
            {
                _lastPositions[_axisZ.轴名称] = 0;
                _stableStartTimes[_axisZ.轴名称] = null;
            }
            if (_axisU != null)
            {
                _lastPositions[_axisU.轴名称] = 0;
                _stableStartTimes[_axisU.轴名称] = null;
            }
            if (_axisR != null)
            {
                _lastPositions[_axisR.轴名称] = 0;
                _stableStartTimes[_axisR.轴名称] = null;
            }
        }

        /// <summary>
        /// 绑定并验证轴信息
        /// </summary>
        /// <param name="axisName">轴名称</param>
        /// <param name="axisLabel">轴标签（如"X"、"Y"）</param>
        /// <returns>轴数据传输对象，如果未绑定则返回null</returns>
        private MotionAxisDto BindAndValidateAxis(string axisName, string axisLabel)
        {
            // 如果轴未绑定，返回null
            if (string.IsNullOrWhiteSpace(axisName) || axisName.Contains("未绑定"))
                return null;

            // 查找轴信息
            var axis = MotionSystem.Instance.MotionAxes
                .FirstOrDefault(a => a.轴名称 == axisName);

            if (axis == null)
            {
                HandleMotionError($"未找到{axisLabel}轴绑定信息: {axisName}");
                return axis;
            }

            return axis;
        }

        /// <summary>
        /// 记录运动绑定信息日志
        /// 用于调试和跟踪运动参数配置
        /// </summary>
        private void LogMotionBindingInfo()
        {
            var logMessage = new StringBuilder();
            logMessage.AppendLine($"运动点位绑定信息 - {_pointName}");
            logMessage.AppendLine($"安全高度: {_safeHeight}");
            logMessage.AppendLine($"位置稳定判定时间: {PositionStableTimeMs}ms");
            logMessage.AppendLine($"位置稳定允许误差: {StabilityChangeTolerance}");
            logMessage.AppendLine($"X轴: {(_axisX?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.X轴目标位置 ?? 0}");
            logMessage.AppendLine($"Y轴: {(_axisY?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.Y轴目标位置 ?? 0}");
            logMessage.AppendLine($"Z轴: {(_axisZ?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.Z轴目标位置 ?? 0}");
            logMessage.AppendLine($"U轴: {(_axisU?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.U轴目标位置 ?? 0}");
            logMessage.AppendLine($"R轴: {(_axisR?.轴名称 ?? "未绑定")} 目标位置: {_motionPoint?.R轴目标位置 ?? 0}");
            logMessage.AppendLine($"超时设置: {_timeoutSeconds}秒");

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
        /// 记录错误日志并将机台状态设为紧急停止
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
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
        /// 核心流程：Z轴先到安全高度 → XYUR轴同步运动 → Z轴到目标位置
        /// 支持各阶段的暂停/继续功能和位置稳定检查
        /// </summary>
        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";

                // 测试模式下直接返回成功
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 检查是否有需要运动的轴
                if (NoAxesToMove())
                {
                    Message = $"错误的指令：{_process}\r\n报错原因：未绑定任何轴";
                    CurrentStatus = GeneralStatus.Error;
                    return;
                }

                // 保存Z轴的目标位置，用于后续运动
                _originalZTarget = _motionPoint.Z轴目标位置;

                // 第一阶段：Z轴运动到安全高度
                _currentPhase = MotionPhase.ZAxisToSafe;
                if (_axisZ != null)
                {
                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Blue,
                        "安全高度运动",
                        _axisZ.轴名称,
                        $"开始运动到安全高度: {_safeHeight}，并将进行{PositionStableTimeMs}ms稳定检查"
                    );

                    var zSafeStopwatch = Stopwatch.StartNew();
                    // 执行带暂停/继续支持的单轴运动，包含稳定检查
                    if (!ExecutePausableSingleAxisMotion(
                        _axisZ,
                        () => _safeHeight,
                        () => _motionPoint.Z轴运行速度,
                        ref _posZ,
                        zSafeStopwatch))
                    {
                        return; // 运动失败或被取消
                    }

                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Green,
                        "安全高度到达并稳定",
                        _axisZ.轴名称,
                        $"已到达安全高度并稳定{PositionStableTimeMs}ms，耗时: {zSafeStopwatch.Elapsed.TotalSeconds:F3}秒"
                    );
                }

                // 第二阶段：XYUR四轴同步运动
                _currentPhase = MotionPhase.XYURMoving;
                if (_syncAxisParams.Any())
                {
                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Blue,
                        "同步运动开始",
                        "XYUR轴",
                        $"开始多轴同步运动，完成后将进行{PositionStableTimeMs}ms稳定检查"
                    );

                    var syncStopwatch = Stopwatch.StartNew();

                    // 发送同步运动指令
                    if (!SendSyncAxisMoveCommands())
                    {
                        StopSyncAxes();
                        return;
                    }
                    Thread.Sleep(100);
                    // 监控同步运动状态，包含稳定检查
                    MonitorSyncAxisMotion(syncStopwatch);

                    // 检查是否有错误
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
                        "同步运动完成并稳定",
                        "XYUR轴",
                        $"所有轴已到达目标位置并稳定，耗时: {syncStopwatch.Elapsed.TotalSeconds:F3}秒"
                    );
                }

                // 第三阶段：Z轴从安全高度运动到目标位置
                _currentPhase = MotionPhase.ZAxisToTarget;
                if (_axisZ != null)
                {
                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Blue,
                        "目标位置运动",
                        _axisZ.轴名称,
                        $"从安全高度运动到目标位置: {_originalZTarget}，完成后将进行{PositionStableTimeMs}ms稳定检查"
                    );

                    var zTargetStopwatch = Stopwatch.StartNew();
                    // 执行带暂停/继续支持的单轴运动，包含稳定检查
                    if (!ExecutePausableSingleAxisMotion(
                        _axisZ,
                        () => _originalZTarget,
                        () => _motionPoint.Z轴运行速度,
                        ref _posZ,
                        zTargetStopwatch))
                    {
                        return; // 运动失败或被取消
                    }

                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Green,
                        "目标位置到达并稳定",
                        _axisZ.轴名称,
                        $"已到达目标位置并稳定{PositionStableTimeMs}ms，耗时: {zTargetStopwatch.Elapsed.TotalSeconds:F3}秒"
                    );
                }

                // 更新运动完成信息
                UpdateCompletionMessage();
            }
            catch (OperationCanceledException)
            {
                Message = "运动已被取消";
                CurrentStatus = GeneralStatus.Error;
                StopAllAxes();
                return;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                StopAllAxes();
                return;
            }
        }

        /// <summary>
        /// 执行支持暂停/继续的单轴运动，包含位置稳定检查
        /// </summary>
        /// <param name="axis">运动轴</param>
        /// <param name="getTargetPos">获取目标位置的委托</param>
        /// <param name="getSpeed">获取运动速度的委托</param>
        /// <param name="currentPos">当前位置引用</param>
        /// <param name="stopwatch">计时器，用于计算运动时间</param>
        /// <returns>如果运动成功完成则返回true，否则返回false</returns>
        private bool ExecutePausableSingleAxisMotion(MotionAxisDto axis, Func<double> getTargetPos,
                                                   Func<double> getSpeed, ref double currentPos,
                                                   Stopwatch stopwatch)
        {
            CurrentStatus = GeneralStatus.Running;
            // 重置稳定检查变量
            double lastPos = 0;
            DateTime? stableStartTime = null;

            // 发送初始运动指令
            MoveAxis(axis, getTargetPos, getSpeed, ref currentPos);
            if (CurrentStatus == GeneralStatus.Error) return false;

            // 短暂延迟，确保指令已被处理
            Thread.Sleep(100);

            // 等待运动完成并稳定，期间支持暂停/继续
            if (!WaitPausableMoveAxisCompletionWithStability(
                axis, getTargetPos, getSpeed,
                ref currentPos, ref lastPos, ref stableStartTime,
                stopwatch))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否没有需要运动的轴
        /// </summary>
        /// <returns>如果没有轴需要运动则返回true，否则返回false</returns>
        private bool NoAxesToMove()
        {
            return (_axisZ == null) && !_syncAxisParams.Any();
        }

        /// <summary>
        /// 向XYUR四轴同时发送运动指令
        /// 确保所有轴同时开始运动，实现同步效果
        /// </summary>
        /// <returns>如果所有指令发送成功则返回true，否则返回false</returns>
        private bool SendSyncAxisMoveCommands()
        {
            lock (_motionLock) // 确保指令发送的原子性
            {
                foreach (var param in _syncAxisParams)
                {
                    try
                    {
                        param.IsCompleted = false;
                        param.ErrorMessage = null;

                        ProcessLog.Instance.SaveLog(
                            _processTip,
                            Color.Blue,
                            "轴运动指令",
                            param.Axis.轴名称,
                            $"目标位置: {param.GetTargetPos()}, 速度: {param.GetSpeed()}"
                        );

                        // 发送绝对运动指令
                        string moveResult = MotionSystem.Instance.MotionController
                            .AbsMove(param.Axis, param.GetTargetPos(), param.GetSpeed());

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
                        param.ErrorMessage = $"发送指令异常: {ex.Message}\r\n目标位置: {param.GetTargetPos()}";
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 发送继续运动指令（从暂停位置恢复）
        /// </summary>
        /// <returns>如果所有指令发送成功则返回true，否则返回false</returns>
        private bool SendResumeMotionCommands()
        {
            lock (_motionLock)
            {
                foreach (var param in _syncAxisParams)
                {
                    if (param.IsCompleted)
                    {
                        param.IsCompleted = false;
                        param.ErrorMessage = null;
                        continue;
                    }

                    try
                    {
                        double currentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                        double targetPos = param.GetTargetPos();

                        // 检查目标位置是否有变更（与暂停前对比）
                        if (Math.Abs(targetPos - param.OriginalTargetPos) > PositionTolerance)
                        {
                            ProcessLog.Instance.SaveLog(
                                _processTip,
                                Color.Orange,
                                "目标位置已更新",
                                param.Axis.轴名称,
                                $"暂停期间目标位置变更: 原{param.OriginalTargetPos:F4} → 新{targetPos:F4}"
                            );
                            param.OriginalTargetPos = targetPos; // 更新原始目标位置记录
                            param.StableStartTime = null; // 重置稳定计时器
                        }

                        ProcessLog.Instance.SaveLog(
                            _processTip,
                            Color.Blue,
                            "轴继续运动指令",
                            param.Axis.轴名称,
                            $"从当前位置 {currentPos:F4} 继续运动到目标 {targetPos:F4}, 速度: {param.GetSpeed():F2}"
                        );

                        // 发送继续运动指令
                        string moveResult = MotionSystem.Instance.MotionController
                            .AbsMove(param.Axis, targetPos, param.GetSpeed());

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
        /// 监控XYUR四轴的同步运动状态（支持暂停/继续和稳定检查）
        /// 循环检查各轴运动状态，处理暂停/继续请求和超时
        /// </summary>
        /// <param name="stopwatch">计时器，用于计算运动时间</param>
        private void MonitorSyncAxisMotion(Stopwatch stopwatch)
        {
            // 使用ManualResetEventSlim实现可取消的等待
            using (var waitHandle = new ManualResetEventSlim(false))
            {
                while (true)
                {
                    // 响应外部取消请求
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

                    // 处理暂停/继续状态切换（加锁确保线程安全）
                    lock (_statusCheckLock)
                    {
                        var currentMachineStatus = MenuProxy.Instance.MachineRunStatus;
                        var currentMotionStatus = CurrentStatus;

                        // 处理继续运动
                        if (IsMachineResumeStatus(currentMachineStatus)
                            && _isPaused
                            && currentMotionStatus == GeneralStatus.Wait)
                        {
                            HandleMotionResume(currentMachineStatus, stopwatch);
                        }
                        // 处理暂停运动
                        else if (IsMachinePauseStatus(currentMachineStatus)
                                 && !_isPaused
                                 && currentMotionStatus == GeneralStatus.Running)
                        {
                            HandleMotionPause(currentMachineStatus, stopwatch);
                        }
                        if (CurrentStatus == GeneralStatus.Error) break;

                    }

                    // 检查全局异常状态（紧急停止或报警）
                    if (CheckGlobalMachineStatus())
                        break;

                    // 检查超时（暂停期间不计时）
                    if (!_isPaused && stopwatch.Elapsed.TotalSeconds > _timeoutSeconds)
                    {
                        HandleMotionTimeout(stopwatch, _timeoutSeconds);
                        break;
                    }

                    // 更新轴运动状态并判断是否完成（包括稳定检查）
                    lock (_syncAxisParams)
                    {
                        UpdateSyncAxisStatusWithStability();
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

                    // 等待延迟（可被取消信号唤醒）
                    waitHandle.Wait(MotionDelayMs, _cancellationToken);
                }
            }
        }

        /// <summary>
        /// 更新XYUR四轴的运动状态，包含位置稳定检查
        /// 检查各轴是否已到达目标位置并保持稳定
        /// </summary>
        private void UpdateSyncAxisStatusWithStability()
        {
            foreach (var param in _syncAxisParams)
            {
                if (param.IsCompleted) continue;

                // 获取当前位置
                param.CurrentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);

                // 检查运动是否完成
                if (MotionSystem.Instance.MotionController.CheckMoveDone(param.Axis))
                {
                    // 检查位置误差是否在允许范围内
                    if (Math.Abs(param.GetTargetPos() - param.CurrentPos) < PositionTolerance)
                    {
                        // 创建临时变量存储属性值，解决CS0206错误
                        double tempLastPos = param.LastPos;
                        DateTime? tempStableStartTime = param.StableStartTime;

                        // 检查位置是否稳定
                        if (IsPositionStable(
                            param.CurrentPos,
                            ref tempLastPos,
                            param.GetTargetPos(),
                            ref tempStableStartTime))
                        {
                            param.IsCompleted = true;
                            param.ErrorMessage = null;
                            // 将临时变量的值回写到属性
                            param.LastPos = tempLastPos;
                            param.StableStartTime = tempStableStartTime;

                            ProcessLog.Instance.SaveLog(
                                _processTip,
                                Color.Green,
                                "轴运动完成并稳定",
                                param.Axis.轴名称,
                                $"目标: {param.GetTargetPos()}, 实际: {param.CurrentPos}, " +
                                $"误差: {Math.Abs(param.GetTargetPos() - param.CurrentPos):F4}, " +
                                $"稳定时间: {PositionStableTimeMs}ms"
                            );

                            // 更新对应轴的位置反馈
                            if (param.Axis == _axisX) _posX = param.CurrentPos;
                            else if (param.Axis == _axisY) _posY = param.CurrentPos;
                            else if (param.Axis == _axisU) _posU = param.CurrentPos;
                            else if (param.Axis == _axisR) _posR = param.CurrentPos;
                        }
                        else
                        {
                            // 将临时变量的值回写到属性
                            param.LastPos = tempLastPos;
                            param.StableStartTime = tempStableStartTime;

                            // 位置在目标范围内但尚未稳定
                            var stableDuration = param.StableStartTime.HasValue
                                ? DateTime.Now - param.StableStartTime.Value
                                : TimeSpan.Zero;

                            ProcessLog.Instance.SaveLog(
                                _processTip,
                                Color.LightBlue,
                                "轴位置稳定中",
                                param.Axis.轴名称,
                                $"目标: {param.GetTargetPos()}, 实际: {param.CurrentPos}, " +
                                $"已稳定: {stableDuration.TotalMilliseconds:F0}ms/{PositionStableTimeMs}ms"
                            );
                        }
                    }
                    else
                    {
                        param.ErrorMessage = $"未达到目标位置（误差超限）\r\n目标: {param.GetTargetPos()}, 实际: {param.CurrentPos}, 误差: {Math.Abs(param.GetTargetPos() - param.CurrentPos):F4}";
                        return;
                    }
                }
                else
                {
                    // 运动尚未完成，重置稳定计时器
                    param.StableStartTime = null;
                }

                // 检查是否发生伺服报警
                if (MotionSystem.Instance.MotionController.CheckServoAlarm(param.Axis))
                {
                    CurrentStatus = GeneralStatus.Error;
                    param.CurrentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                    param.ErrorMessage = $"运动中发生伺服报警\r\n当前位置: {param.CurrentPos}";
                    return;
                }
            }
        }

        /// <summary>
        /// 等待单轴运动完成（支持暂停/继续和稳定检查）
        /// </summary>
        /// <param name="axis">运动轴</param>
        /// <param name="getTargetPos">获取目标位置的委托</param>
        /// <param name="getSpeed">获取运动速度的委托</param>
        /// <param name="currentPos">当前位置引用</param>
        /// <param name="lastPos">上一次位置引用</param>
        /// <param name="stableStartTime">稳定开始时间引用</param>
        /// <param name="stopwatch">计时器</param>
        /// <returns>如果运动成功完成则返回true，否则返回false</returns>
        private bool WaitPausableMoveAxisCompletionWithStability(MotionAxisDto axis, Func<double> getTargetPos,
                                                               Func<double> getSpeed,
                                                               ref double currentPos,
                                                               ref double lastPos,
                                                               ref DateTime? stableStartTime,
                                                               Stopwatch stopwatch)
        {
            using (var waitHandle = new ManualResetEventSlim(false))
            {
                try
                {
                    while (true)
                    {
                        // 响应外部取消请求
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            CurrentStatus = GeneralStatus.Error;
                            Message = "运动被外部请求取消";
                            throw new OperationCanceledException("运动被外部请求取消");
                        }

                        // 处理暂停/继续状态切换
                        lock (_statusCheckLock)
                        {
                            var currentMachineStatus = MenuProxy.Instance.MachineRunStatus;
                            var currentMotionStatus = CurrentStatus;

                            // 处理继续运动
                            if (IsMachineResumeStatus(currentMachineStatus)
                                && _isPaused
                                && currentMotionStatus == GeneralStatus.Wait)
                            {
                                // 发送继续运动指令
                                string moveResult;
                                lock (_motionLock)
                                {
                                    double targetPos = getTargetPos();
                                    double speed = getSpeed();
                                    moveResult = MotionSystem.Instance.MotionController
                                        .AbsMove(axis, targetPos, speed);
                                }

                                if (moveResult != "NoError")
                                {
                                    CurrentStatus = GeneralStatus.Error;
                                    Message = $"轴[{axis.轴名称}]继续运动指令失败: {moveResult}";
                                    return false;
                                }

                                _isPaused = false;
                                CurrentStatus = GeneralStatus.Running;
                                Message = $"Running（从{currentMachineStatus}恢复）";
                                stopwatch.Start();

                                // 重置稳定计时器，因为位置可能已变化
                                stableStartTime = null;

                                ProcessLog.Instance.SaveLog(
                                    _processTip,
                                    Color.Green,
                                    "继续运动成功",
                                    axis.轴名称,
                                    $"已从断点恢复运动，累计耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒"
                                );
                            }
                            // 处理暂停运动
                            else if (IsMachinePauseStatus(currentMachineStatus)
                                     && !_isPaused
                                     && currentMotionStatus == GeneralStatus.Running)
                            {
                                // 停止轴运动并记录断点
                                MotionSystem.Instance.MotionController.SmtStop(axis, 0);
                                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                                _breakPointPositions[axis.轴名称] = currentPos;

                                _isPaused = true;
                                CurrentStatus = GeneralStatus.Wait;
                                Message = $"Wait（因{currentMachineStatus}暂停）";
                                stopwatch.Stop();

                                // 重置稳定计时器，暂停后需要重新检查稳定
                                stableStartTime = null;

                                ProcessLog.Instance.SaveLog(
                                    _processTip,
                                    Color.Orange,
                                    "轴已暂停",
                                    axis.轴名称,
                                    $"暂停位置: {currentPos:F4}, 剩余距离: {Math.Abs(getTargetPos() - currentPos):F4}"
                                );
                            }
                        }

                        // 检查机台状态和超时
                        if (CheckMachineStatus(axis, getTargetPos, ref currentPos) ||
                            (!_isPaused && CheckTimeout(axis, getTargetPos, ref currentPos, stopwatch)))
                        {
                            return false;
                        }

                        // 获取当前位置
                        currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);

                        // 检查运动是否完成且位置稳定
                        if (!_isPaused && MotionSystem.Instance.MotionController.CheckMoveDone(axis))
                        {
                            // 检查位置是否在目标范围内
                            if (Math.Abs(getTargetPos() - currentPos) < PositionTolerance)
                            {
                                // 检查位置是否稳定
                                if (IsPositionStable(currentPos, ref lastPos, getTargetPos(), ref stableStartTime))
                                {
                                    CurrentStatus = GeneralStatus.NoError;
                                    break;
                                }
                                else
                                {
                                    // 位置在目标范围内但尚未稳定
                                    var stableDuration = stableStartTime.HasValue
                                        ? DateTime.Now - stableStartTime.Value
                                        : TimeSpan.Zero;

                                    Message = $"轴[{axis.轴名称}]位置稳定中... " +
                                              $"已稳定: {stableDuration.TotalMilliseconds:F0}ms/{PositionStableTimeMs}ms";

                                    ProcessLog.Instance.SaveLog(
                                        _processTip,
                                        Color.LightBlue,
                                        "位置稳定中",
                                        axis.轴名称,
                                        Message
                                    );
                                }
                            }
                            else
                            {
                                Message = $"轴[{axis.轴名称}]位置误差超限\r\n目标: {getTargetPos()}, 实际: {currentPos}, 误差: {Math.Abs(getTargetPos() - currentPos):F4}";
                            }
                        }
                        else
                        {
                            // 运动尚未完成，重置稳定计时器
                            stableStartTime = null;
                        }

                        // 等待延迟（可被取消信号唤醒）
                        waitHandle.Wait(MotionDelayMs, _cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                    CurrentStatus = GeneralStatus.Error;
                    Message = $"轴[{axis.轴名称}]运动异常: {ex.Message}\r\n目标位置: {getTargetPos()}\r\n实际位置: {currentPos}";
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查位置是否稳定
        /// </summary>
        /// <param name="currentPos">当前位置</param>
        /// <param name="lastPos">上一次位置引用</param>
        /// <param name="targetPos">目标位置</param>
        /// <param name="stableStartTime">稳定开始时间引用</param>
        /// <returns>true表示位置已稳定，false表示未稳定</returns>
        private bool IsPositionStable(double currentPos, ref double lastPos, double targetPos, ref DateTime? stableStartTime)
        {
            // 1. 检查当前位置与目标位置的偏差
            bool isWithinTargetTolerance = Math.Abs(currentPos - targetPos) < PositionTolerance;

            // 2. 检查当前位置与上一次位置的变化量
            bool isWithinChangeTolerance = Math.Abs(currentPos - lastPos) < StabilityChangeTolerance;

            // 更新上一次位置记录
            lastPos = currentPos;

            // 位置不稳定，重置计时器
            if (!isWithinTargetTolerance || !isWithinChangeTolerance)
            {
                stableStartTime = null;
                return false;
            }

            // 开始计时
            if (!stableStartTime.HasValue)
            {
                stableStartTime = DateTime.Now;
                return false;
            }

            // 检查是否达到稳定时间要求
            var stableDuration = DateTime.Now - stableStartTime.Value;
            return stableDuration.TotalMilliseconds >= PositionStableTimeMs;
        }

        #region 暂停/继续辅助方法
        /// <summary>
        /// 判断机台是否处于「可恢复运动」的状态（Running/Homing）
        /// </summary>
        private bool IsMachineResumeStatus(MachineRunStatus status)
        {
            return status == MachineRunStatus.Running || status == MachineRunStatus.Homing;
        }

        /// <summary>
        /// 判断机台是否处于「需暂停运动」的状态（Paused/Alarming）
        /// </summary>
        private bool IsMachinePauseStatus(MachineRunStatus status)
        {
            return status == MachineRunStatus.Paused || status == MachineRunStatus.Alarming;
        }

        /// <summary>
        /// 处理「继续运动」逻辑
        /// </summary>
        private void HandleMotionResume(MachineRunStatus machineStatus, Stopwatch stopwatch)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Orange,
                "状态触发继续",
                "所有轴",
                $"机台状态从暂停切换为【{machineStatus}】，准备恢复运动（阶段：{_currentPhase}）"
            );

            // 发送继续指令并校验结果
            bool resumeSuccess = SendResumeMotionCommands();
            if (resumeSuccess)
            {
                _isPaused = false;
                CurrentStatus = GeneralStatus.Running;
                Message = $"Running（从{machineStatus}恢复，阶段：{_currentPhase}）";
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

        /// <summary>
        /// 处理「暂停运动」逻辑
        /// </summary>
        private void HandleMotionPause(MachineRunStatus machineStatus, Stopwatch stopwatch)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Orange,
                "状态触发暂停",
                "所有轴",
                $"机台状态从运行切换为【{machineStatus}】，准备停止运动（阶段：{_currentPhase}）"
            );

            // 停止所有轴并记录断点位置
            StopSyncAxes();
            _breakPointPositions.Clear();
            foreach (var param in _syncAxisParams.Where(p => !p.IsCompleted))
            {
                double currentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                _breakPointPositions[param.Axis.轴名称] = currentPos;

                // 重置稳定计时器
                param.StableStartTime = null;

                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Orange,
                    "轴断点记录",
                    param.Axis.轴名称,
                    $"暂停位置: {currentPos:F4}, 剩余距离: {Math.Abs(param.GetTargetPos() - currentPos):F4}"
                );
            }

            _isPaused = true;
            CurrentStatus = GeneralStatus.Wait;
            Message = $"Wait（因{machineStatus}暂停，阶段：{_currentPhase}）";
            stopwatch.Stop();

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Orange,
                "暂停运动成功",
                "所有轴",
                $"所有轴已停止，累计耗时: {stopwatch.Elapsed.TotalSeconds:F2}秒"
            );
        }

        /// <summary>
        /// 处理「运动超时」逻辑
        /// </summary>
        private void HandleMotionTimeout(Stopwatch stopwatch, int timeoutSeconds)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "运动超时触发",
                "所有轴",
                $"运动耗时超过{timeoutSeconds}秒，准备停止所有轴（阶段：{_currentPhase}）"
            );

            StopSyncAxes();

            foreach (var param in _syncAxisParams.Where(p => !p.IsCompleted))
            {
                CurrentStatus = GeneralStatus.Error;
                param.CurrentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                param.ErrorMessage = $"运动超时（设定{timeoutSeconds}秒，实际{stopwatch.Elapsed.TotalSeconds:F1}秒）\r\n目标: {param.GetTargetPos():F4}, 实际: {param.CurrentPos:F4}";
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
        /// 检查全局机台状态
        /// 处理紧急停止和报警状态
        /// </summary>
        /// <returns>如果发生紧急情况则返回true，否则返回false</returns>
        private bool CheckGlobalMachineStatus()
        {
            var isEmergency = MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped;
            var isAlarming = MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming;

            if (isEmergency || isAlarming)
            {
                StopSyncAxes();
                var errorType = isEmergency ? "紧急停止报警" : "机台报警";

                foreach (var param in _syncAxisParams)
                {
                    CurrentStatus = GeneralStatus.Error;
                    param.CurrentPos = MotionSystem.Instance.MotionController.GetEncPos(param.Axis);
                    param.ErrorMessage = $"{errorType}\r\n目标: {param.GetTargetPos()}, 实际: {param.CurrentPos}";
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 单轴运动方法
        /// 发送单轴绝对运动指令
        /// </summary>
        private void MoveAxis(MotionAxisDto axis, Func<double> getTargetPos,
                             Func<double> getSpeed, ref double currentPos)
        {
            try
            {
                string moveResult;
                lock (_motionLock)
                {
                    moveResult = MotionSystem.Instance.MotionController
                        .AbsMove(axis, getTargetPos(), getSpeed());
                }

                if (moveResult != "NoError")
                {
                    CurrentStatus = GeneralStatus.Error;
                    Message = $"轴[{axis.轴名称}]运动指令失败: {moveResult}";
                    return;
                }
            }
            catch (Exception ex)
            {
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"轴[{axis.轴名称}]运动异常: {ex.Message}\r\n目标位置: {getTargetPos()}\r\n实际位置: {currentPos}";
                return;
            }
        }

        /// <summary>
        /// 停止所有同步轴（XYUR）
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
        /// 停止所有轴（包括Z轴和XYUR轴）
        /// </summary>
        private void StopAllAxes()
        {
            if (_axisZ != null)
            {
                try
                {
                    MotionSystem.Instance.MotionController.SmtStop(_axisZ, 0);
                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Orange,
                        "轴已停止",
                        _axisZ.轴名称,
                        "因异常情况停止运动"
                    );
                }
                catch (Exception ex)
                {
                    RLog.SaveError(new Exception($"停止Z轴失败", ex));
                }
            }

            StopSyncAxes();
        }

        /// <summary>
        /// 检查机台状态
        /// 处理紧急停止和报警情况
        /// </summary>
        private bool CheckMachineStatus(MotionAxisDto axis, Func<double> getTargetPos, ref double currentPos)
        {
            double targetPos = getTargetPos();

            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台紧急停止报警\r\n{axis.轴名称}轴目标点位：{targetPos}\r\n{axis.轴名称}轴实际点位：{currentPos}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(axis, 0);
                return true;
            }

            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming)
            {
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：机台报警\r\n{axis.轴名称}轴目标点位：{targetPos}\r\n{axis.轴名称}轴实际点位：{currentPos}\r\n";
                MotionSystem.Instance.MotionController.SmtStop(axis, 0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查运动是否超时
        /// </summary>
        private bool CheckTimeout(MotionAxisDto axis, Func<double> getTargetPos, ref double currentPos, Stopwatch stopwatch)
        {
            double targetPos = getTargetPos();

            if (stopwatch.Elapsed.TotalSeconds > _timeoutSeconds)
            {
                currentPos = MotionSystem.Instance.MotionController.GetEncPos(axis);
                CurrentStatus = GeneralStatus.Error;
                Message = $"\r\n报错原因：运动超时报警\r\n{axis.轴名称}轴目标点位：{targetPos}\r\n{axis.轴名称}轴实际点位：{currentPos}\r\n耗时：{stopwatch.Elapsed.TotalSeconds:F3}秒";
                MotionSystem.Instance.MotionController.SmtStop(axis, 0);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新运动完成信息
        /// 汇总各轴实际位置并设置完成状态
        /// </summary>
        private void UpdateCompletionMessage()
        {
            var message = new StringBuilder();
            message.AppendLine("NoError");
            message.AppendLine($"安全高度: {_safeHeight}");
            message.AppendLine($"位置稳定时间: {PositionStableTimeMs}ms");
            message.AppendLine($"X轴实际点位：  {_posX:F4}");
            message.AppendLine($"Y轴实际点位：  {_posY:F4}");
            message.AppendLine($"Z轴实际点位：  {_posZ:F4}");
            message.AppendLine($"U轴实际点位：  {_posU:F4}");
            message.AppendLine($"R轴实际点位：  {_posR:F4}");

            Message = message.ToString();
            CurrentStatus = GeneralStatus.NoError;
        }
    }
}
