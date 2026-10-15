using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Permission;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process.AxisMotionControlProcess
{
    /// <summary>
    /// 非等待式绝对运动处理类
    /// 功能：发送绝对运动指令后立即返回，不等待运动完成
    /// 特性：支持机台状态联动的暂停/继续功能，实现运动过程的灵活控制
    /// 实现接口：ProcessBase（基础流程）、ICloneable（支持克隆）、IDisposable（资源释放）
    /// </summary>
    public class ABSMoveNoWaitProcess : ProcessBase, ICloneable
    {
        #region 私有字段
        /// <summary>原始运动指令字符串（如"ABSMoveNoWait|...|..."）</summary>
        private readonly string _process;

        /// <summary>运动指令提示信息，用于日志显示的友好名称</summary>
        private readonly string _processTip;

        /// <summary>
        /// 运动控制的线程锁
        /// 确保多线程环境下运动指令（启动/停止/继续）的原子性操作
        /// </summary>
        private static readonly object _motionLock = new object();

        /// <summary>
        /// 状态控制的线程锁
        /// 确保状态检查和修改的线程安全，避免并发冲突
        /// </summary>
        private static readonly object _statusLock = new object();

        // 指令解析结果 - 从原始指令字符串中解析出的各部分信息
        /// <summary>指令名称（固定为"ABSMoveNoWait"）</summary>
        private string _commandName;

        /// <summary>点位类型（如"用户点位"、"系统点位"等分类）</summary>
        private string _pointType;

        /// <summary>点位定义（点位的具体配置信息标识）</summary>
        private string _pointDefinition;

        /// <summary>点位名称（点位的唯一标识名称）</summary>
        private string _pointName;

        /// <summary>超时时间（秒）- 运动的最大允许时间</summary>
        private int _timeoutSeconds;

        // 运动参数 - 从系统中获取的实际运动配置
        /// <summary>运动点位数据传输对象，包含各轴目标位置等信息</summary>
        private MotionPointDto _motionPoint;

        /// <summary>X轴数据传输对象，包含轴的配置信息</summary>
        private MotionAxisDto _axisX;

        /// <summary>Y轴数据传输对象，包含轴的配置信息</summary>
        private MotionAxisDto _axisY;

        /// <summary>Z轴数据传输对象，包含轴的配置信息</summary>
        private MotionAxisDto _axisZ;

        /// <summary>U轴数据传输对象，包含轴的配置信息</summary>
        private MotionAxisDto _axisU;

        /// <summary>R轴数据传输对象，包含轴的配置信息</summary>
        private MotionAxisDto _axisR;

        // 暂停/继续相关字段 - 实现暂停/继续功能的核心变量
        /// <summary>
        /// 标记是否处于暂停状态
        /// volatile关键字确保多线程环境下的可见性，避免缓存导致的状态不一致
        /// </summary>
        private volatile bool _isPaused = false;

        /// <summary>暂停时各轴位置记录字典，key为轴标识（如"X"），value为位置值</summary>
        private readonly Dictionary<string, double> _pausePositions = new Dictionary<string, double>();

        /// <summary>用于取消监控线程的令牌源，支持安全终止监控任务</summary>
        private CancellationTokenSource _monitorCts;

        /// <summary>状态监控任务，独立线程运行以响应机台状态变化</summary>
        private Task _monitorTask;

        /// <summary>资源释放标志，防止重复释放资源</summary>
        private bool _isDisposed = false;
        #endregion

        /// <summary>
        /// 初始化绝对运动不等待流程实例
        /// </summary>
        /// <param name="process">原始运动指令字符串</param>
        /// <param name="processTip">运动指令提示信息（用于日志显示）</param>
        public ABSMoveNoWaitProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                // 初始化状态为等待
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";

                // 验证输入参数非空
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                // 解析指令并绑定运动参数
                ParseCommand(process);
                BindMotionParameters();
            }
            catch (Exception ex)
            {
                // 捕获初始化过程中的异常，设置错误状态并记录日志
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 克隆方法 - 支持流程实例的复制
        /// 用于副流程缓存机制，确保多实例并发时的状态独立性
        /// </summary>
        /// <returns>克隆的ABSMoveNoWaitProcess实例</returns>
        public override object Clone()
        {
            // 创建新实例并复制核心属性
            var clone = new ABSMoveNoWaitProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointType = this._pointType,
                _pointDefinition = this._pointDefinition,
                _pointName = this._pointName,
                _timeoutSeconds = this._timeoutSeconds,
                _motionPoint = this._motionPoint,
                _axisX = this._axisX,
                _axisY = this._axisY,
                _axisZ = this._axisZ,
                _axisU = this._axisU,
                _axisR = this._axisR
            };

            // 重置克隆实例的状态，确保初始状态一致
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";

            return clone;
        }

        /// <summary>
        /// 解析运动指令字符串，提取指令各组成部分
        /// 指令格式：ABSMoveNoWait|点位类型|点位定义|点位名称|超时时间
        /// </summary>
        /// <param name="process">待解析的运动指令字符串</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式至少包含5个部分
            if (commandParts.Length < 5)
            {
                HandleMotionError($"指令格式错误: {process}，至少需要5个部分");
                return;
            }

            // 提取指令各部分信息
            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _pointDefinition = commandParts[2];
            _pointName = commandParts[3];

            // 解析并验证超时参数（必须为正整数）
            if (!int.TryParse(commandParts[4], out _timeoutSeconds) || _timeoutSeconds <= 0)
            {
                HandleMotionError($"超时参数无效: {commandParts[4]}，必须是正整数");
                return;
            }
        }

        /// <summary>
        /// 绑定运动参数和轴信息
        /// 从运动系统中获取点位的详细配置和各轴的绑定信息
        /// </summary>
        private void BindMotionParameters()
        {
            // 验证运动系统是否已初始化
            if (MotionSystem.Instance == null)
            {
                HandleMotionError("运动系统未初始化");
                return;
            }

            // 从运动系统中查找匹配的点位信息
            // 匹配条件：点位名称、点位类型、点位定义三者一致
            _motionPoint = MotionSystem.Instance.MotionPoints
                .FirstOrDefault(p => p.点位名称 == _pointName
                                 && p.点位类型 == _pointType
                                 && p.点位定义 == _pointDefinition);

            // 如果未找到匹配点位，处理错误
            if (_motionPoint == null)
            {
                HandleMotionError("未找到匹配的运动点位信息");
                return;
            }

            // 设置流程ID为点位的绑定代码
            ProcessId = _motionPoint.绑定代码;

            // 依次绑定各轴信息，并在任何一轴绑定失败时终止
            _axisX = BindAxis(_motionPoint.X轴绑定轴, "X");
            if (CurrentStatus == GeneralStatus.Error) return;

            _axisY = BindAxis(_motionPoint.Y轴绑定轴, "Y");
            if (CurrentStatus == GeneralStatus.Error) return;

            _axisZ = BindAxis(_motionPoint.Z轴绑定轴, "Z");
            if (CurrentStatus == GeneralStatus.Error) return;

            _axisU = BindAxis(_motionPoint.U轴绑定轴, "U");
            if (CurrentStatus == GeneralStatus.Error) return;

            _axisR = BindAxis(_motionPoint.R轴绑定轴, "R");
            if (CurrentStatus == GeneralStatus.Error) return;
        }

        /// <summary>
        /// 绑定单个轴信息并验证其有效性
        /// </summary>
        /// <param name="axisName">轴名称（从点位信息中获取的绑定轴名称）</param>
        /// <param name="axisLabel">轴标签（如"X"、"Y"，用于错误信息描述）</param>
        /// <returns>有效的轴数据传输对象，未绑定或无效则返回null</returns>
        private MotionAxisDto BindAxis(string axisName, string axisLabel)
        {
            // 处理未绑定情况（轴名称为空或包含"未绑定"标识）
            if (string.IsNullOrWhiteSpace(axisName) || axisName.Contains("未绑定"))
                return null;

            // 从运动系统中查找匹配的轴信息
            var axis = MotionSystem.Instance.MotionAxes
                .FirstOrDefault(a => a.轴名称 == axisName);

            // 如果未找到轴，处理错误
            if (axis == null)
            {
                HandleMotionError($"未找到{axisLabel}轴绑定信息: {axisName}");
                return null;
            }

            return axis;
        }

        /// <summary>
        /// 处理运动错误的统一方法
        /// 功能：设置机台紧急停止状态、记录错误日志、更新流程状态并抛出异常
        /// </summary>
        /// <param name="errorMessage">错误信息描述</param>
        private void HandleMotionError(string errorMessage)
        {
            // 更新机台状态为紧急停止，确保系统安全
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

            // 记录错误日志，包含详细错误信息
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _process,
                $"--->>错误：{errorMessage}！！！！"
            );

            // 更新流程状态为错误
            CurrentStatus = GeneralStatus.Error;

            // 抛出异常以终止当前流程
            throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// 执行绝对运动（不等待完成）的主方法
        /// 核心逻辑：发送运动指令后立即返回，同时启动状态监控任务以响应暂停/继续命令
        /// </summary>
        public override void Run()
        {
            try
            {
                // 停止任何已存在的监控任务，确保状态干净
                StopMonitorTask();

                // 更新流程状态为运行中
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";

                // 测试模式处理：直接返回成功，不实际执行运动
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 检查是否有需要运动的轴，无轴绑定则标记错误
                if (NoAxesToMove())
                {
                    Message = $"错误的指令：{_process}\r\n报错原因：未绑定任何轴";
                    CurrentStatus = GeneralStatus.Error;
                    return;
                }

                // 启动状态监控任务，用于处理暂停/继续/紧急停止
                StartMonitorTask();

                // 依次发送各轴运动指令（不等待完成）
                SendMoveCommand(_axisX, () => _motionPoint.X轴目标位置, () => _motionPoint.X轴运行速度);
                if (CurrentStatus == GeneralStatus.Error) return;

                SendMoveCommand(_axisY, () => _motionPoint.Y轴目标位置, () => _motionPoint.Y轴运行速度);
                if (CurrentStatus == GeneralStatus.Error) return;

                SendMoveCommand(_axisZ, () => _motionPoint.Z轴目标位置, () => _motionPoint.Z轴运行速度);
                if (CurrentStatus == GeneralStatus.Error) return;

                SendMoveCommand(_axisU, () => _motionPoint.U轴目标位置, () => _motionPoint.U轴运行速度);
                if (CurrentStatus == GeneralStatus.Error) return;

                SendMoveCommand(_axisR, () => _motionPoint.R轴目标位置, () => _motionPoint.R轴运行速度);
                if (CurrentStatus == GeneralStatus.Error) return;

                // 所有轴运动指令发送完成
                Message = "所有轴运动指令已发送";
            }
            catch (Exception ex)
            {
                // 捕获执行过程中的异常，设置错误状态并记录日志
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                StopMonitorTask(); // 发生错误时停止监控任务
            }
        }

        /// <summary>
        /// 启动状态监控任务
        /// 功能：在独立线程中定期检查机台状态，响应暂停/继续/紧急停止命令
        /// 设计：使用长运行任务（LongRunning）优化线程池资源使用
        /// </summary>
        private void StartMonitorTask()
        {
            // 创建新的取消令牌源
            _monitorCts = new CancellationTokenSource();
            var token = _monitorCts.Token;

            // 启动监控任务
            _monitorTask = Task.Factory.StartNew(() =>
            {
                // 使用ManualResetEventSlim实现高效等待，替代Thread.Sleep
                using (var waitHandle = new ManualResetEventSlim(false))
                {
                    // 循环监控，直到收到取消信号
                    while (!token.IsCancellationRequested)
                    {
                        // 加锁确保状态检查和修改的原子性
                        lock (_statusLock)
                        {
                            // 获取当前机台状态
                            var machineStatus = MenuProxy.Instance.MachineRunStatus;

                            // 处理暂停：机台状态为暂停，且当前流程处于运行中
                            if (machineStatus == MachineRunStatus.Paused && !_isPaused &&
                                CurrentStatus == GeneralStatus.Running)
                            {
                                HandlePause();
                            }
                            // 处理继续：机台状态为运行，且当前流程处于暂停中
                            else if (machineStatus == MachineRunStatus.Running && _isPaused &&
                                     CurrentStatus == GeneralStatus.Wait)
                            {
                                HandleResume();
                            }
                            // 处理紧急停止：立即终止运动
                            else if (machineStatus == MachineRunStatus.EmergencyStopped)
                            {
                                HandleEmergencyStop();
                                break; // 紧急停止后退出监控循环
                            }
                        }

                        // 等待100ms或直到收到取消信号，平衡实时性和资源占用
                        waitHandle.Wait(100, token);
                    }
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// 停止监控任务
        /// 功能：安全取消并释放监控任务相关资源，避免线程泄漏
        /// </summary>
        private void StopMonitorTask()
        {
            // 取消并释放取消令牌源
            if (_monitorCts != null)
            {
                _monitorCts.Cancel(); // 发送取消信号
                _monitorCts.Dispose(); // 释放资源
                _monitorCts = null;
            }

            // 等待监控任务结束
            if (_monitorTask != null)
            {
                try
                {
                    // 最多等待1秒，避免无限阻塞
                    _monitorTask.Wait(1000);
                }
                catch (AggregateException)
                {
                    // 忽略任务取消导致的异常
                }
                _monitorTask = null;
            }
        }

        /// <summary>
        /// 处理暂停逻辑
        /// 流程：记录当前位置 → 停止所有轴 → 更新状态 → 记录日志
        /// </summary>
        private void HandlePause()
        {
            try
            {
                // 记录暂停前各轴位置，用于后续恢复运动
                _pausePositions.Clear();
                RecordAxisPositions();

                // 停止所有轴运动
                StopAllAxes();

                // 更新暂停状态和流程状态
                _isPaused = true;
                CurrentStatus = GeneralStatus.Wait;
                Message = "运动已暂停";

                // 记录暂停日志
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Orange,
                    "运动暂停",
                    _pointName,
                    "收到暂停指令，已停止所有轴运动"
                );
            }
            catch (Exception ex)
            {
                // 记录暂停失败的错误信息
                RLog.SaveError(new Exception("暂停运动失败", ex));
                Message = $"暂停失败: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
            }
        }

        /// <summary>
        /// 处理继续逻辑
        /// 流程：从暂停位置恢复运动 → 更新状态 → 记录日志
        /// </summary>
        private void HandleResume()
        {
            try
            {
                // 从暂停位置继续运动到原始目标位置
                ResumeAllAxes();

                // 更新状态，取消暂停标记
                _isPaused = false;
                CurrentStatus = GeneralStatus.Running;
                Message = "运动已继续";

                // 记录继续日志
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Green,
                    "运动继续",
                    _pointName,
                    "收到继续指令，已恢复所有轴运动"
                );
            }
            catch (Exception ex)
            {
                // 记录继续失败的错误信息
                RLog.SaveError(new Exception("继续运动失败", ex));
                Message = $"继续失败: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
            }
        }

        /// <summary>
        /// 处理紧急停止
        /// 流程：立即停止所有轴 → 更新错误状态 → 记录日志
        /// </summary>
        private void HandleEmergencyStop()
        {
            // 停止所有轴运动
            StopAllAxes();

            // 更新状态为错误
            CurrentStatus = GeneralStatus.Error;
            Message = "运动已因紧急停止中断";

            // 记录紧急停止日志
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _pointName,
                "收到紧急停止指令，已停止所有轴运动"
            );
        }

        /// <summary>
        /// 记录当前各轴位置
        /// 用于暂停时保存位置信息，为后续恢复运动提供基准
        /// </summary>
        private void RecordAxisPositions()
        {
            if (_axisX != null)
                _pausePositions["X"] = MotionSystem.Instance.MotionController.GetEncPos(_axisX);

            if (_axisY != null)
                _pausePositions["Y"] = MotionSystem.Instance.MotionController.GetEncPos(_axisY);

            if (_axisZ != null)
                _pausePositions["Z"] = MotionSystem.Instance.MotionController.GetEncPos(_axisZ);

            if (_axisU != null)
                _pausePositions["U"] = MotionSystem.Instance.MotionController.GetEncPos(_axisU);

            if (_axisR != null)
                _pausePositions["R"] = MotionSystem.Instance.MotionController.GetEncPos(_axisR);
        }

        /// <summary>
        /// 停止所有轴运动
        /// 加锁确保所有轴停止操作的原子性，避免部分轴未停止的情况
        /// </summary>
        private void StopAllAxes()
        {
            lock (_motionLock)
            {
                StopAxis(_axisX);
                StopAxis(_axisY);
                StopAxis(_axisZ);
                StopAxis(_axisU);
                StopAxis(_axisR);
            }
        }

        /// <summary>
        /// 停止单个轴运动
        /// </summary>
        /// <param name="axis">要停止的轴</param>
        private void StopAxis(MotionAxisDto axis)
        {
            // 轴未绑定则直接返回
            if (axis == null) return;

            try
            {
                // 发送停止指令（SmtStop：平滑停止，0表示立即执行）
                MotionSystem.Instance.MotionController.SmtStop(axis, 0);

                // 记录轴停止日志
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Orange,
                    "轴已停止",
                    axis.轴名称,
                    "因暂停请求停止运动"
                );
            }
            catch (Exception ex)
            {
                // 记录停止轴失败的错误
                RLog.SaveError(new Exception($"停止轴[{axis.轴名称}]失败", ex));
            }
        }

        /// <summary>
        /// 从暂停位置继续所有轴运动
        /// 加锁确保所有轴继续操作的原子性，保证运动同步性
        /// </summary>
        private void ResumeAllAxes()
        {
            lock (_motionLock)
            {
                // 恢复X轴运动（从暂停位置到目标位置）
                if (_axisX != null && _pausePositions.TryGetValue("X", out double xPos))
                    MotionSystem.Instance.MotionController.AbsMove(_axisX, _motionPoint.X轴目标位置, _motionPoint.X轴运行速度);

                // 恢复Y轴运动
                if (_axisY != null && _pausePositions.TryGetValue("Y", out double yPos))
                    MotionSystem.Instance.MotionController.AbsMove(_axisY, _motionPoint.Y轴目标位置, _motionPoint.Y轴运行速度);

                // 恢复Z轴运动
                if (_axisZ != null && _pausePositions.TryGetValue("Z", out double zPos))
                    MotionSystem.Instance.MotionController.AbsMove(_axisZ, _motionPoint.Z轴目标位置, _motionPoint.Z轴运行速度);

                // 恢复U轴运动
                if (_axisU != null && _pausePositions.TryGetValue("U", out double uPos))
                    MotionSystem.Instance.MotionController.AbsMove(_axisU, _motionPoint.U轴目标位置, _motionPoint.U轴运行速度);

                // 恢复R轴运动
                if (_axisR != null && _pausePositions.TryGetValue("R", out double rPos))
                    MotionSystem.Instance.MotionController.AbsMove(_axisR, _motionPoint.R轴目标位置, _motionPoint.R轴运行速度);
            }
        }

        /// <summary>
        /// 检查是否没有需要运动的轴（所有轴都未绑定）
        /// </summary>
        /// <returns>true表示没有轴需要运动，false表示有轴需要运动</returns>
        private bool NoAxesToMove()
        {
            return _axisX == null && _axisY == null && _axisZ == null &&
                   _axisU == null && _axisR == null;
        }

        /// <summary>
        /// 发送轴运动指令（不等待完成）
        /// 这是"不等待"特性的核心实现：仅发送指令，不检查运动是否完成
        /// </summary>
        /// <param name="axis">运动轴</param>
        /// <param name="getTargetPos">获取目标位置的委托（支持动态获取最新值）</param>
        /// <param name="getSpeed">获取运动速度的委托（支持动态获取最新值）</param>
        private void SendMoveCommand(MotionAxisDto axis, Func<double> getTargetPos, Func<double> getSpeed)
        {
            // 轴未绑定则直接返回
            if (axis == null) return;

            try
            {
                string moveResult;
                // 加锁确保运动指令发送的原子性
                lock (_motionLock)
                {
                    // 发送绝对运动指令（AbsMove），不等待完成
                    moveResult = MotionSystem.Instance.MotionController
                        .AbsMove(axis, getTargetPos(), getSpeed());
                }

                // 检查指令发送结果（仅验证指令是否成功发送，不检查运动是否完成）
                if (moveResult != "NoError")
                {
                    CurrentStatus = GeneralStatus.Error;
                    Message = $"轴[{axis.轴名称}]运动指令发送失败: {moveResult}";
                    return;
                }

                // 记录指令发送成功的日志
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Blue,
                    "轴运动指令已发送",
                    axis.轴名称,
                    $"目标位置: {getTargetPos()}, 速度: {getSpeed()}"
                );
            }
            catch (Exception ex)
            {
                // 记录指令发送异常
                CurrentStatus = GeneralStatus.Error;
                Message = $"轴[{axis.轴名称}]运动指令异常: {ex.Message}";
                return;
            }
        }
    }
}
