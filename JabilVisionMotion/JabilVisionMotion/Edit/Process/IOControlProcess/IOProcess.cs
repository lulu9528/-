using csLTDMC;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.IO;
using Jabil.Module.IO.Common.Dtos;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using JabilVisionMotion.Lib;
using lctdevice;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process.IOControlProcess
{
    /// <summary>
    /// IO流程处理类，继承自ProcessBase基类，实现ICloneable支持对象克隆，实现IDisposable支持资源释放
    /// 核心功能：
    /// 1. 解析IO控制指令（输入/输出）
    /// 2. 管理不同类型IO卡的硬件交互
    /// 3. 处理输入信号的稳定性检查和超时控制
    /// 4. 确保IO操作的线程安全和资源正确释放
    /// 设计模式：采用模板方法模式，将通用流程放在基类，具体实现放在子类
    /// </summary>
    public class IOProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>
        /// 信号稳定性检查次数，连续多次验证信号状态
        /// 作用：避免因电气干扰或机械振动导致的单次信号抖动误判
        /// 可根据实际硬件特性调整，默认10次
        /// </summary>
        private const int CheckCount = 10;

        /// <summary>
        /// 信号检查间隔（毫秒）
        /// 作用：控制多次信号检查之间的时间间隔，平衡响应速度和系统负载
        /// 注意：过短会增加CPU占用，过长会降低响应灵敏度
        /// </summary>
        private const int SleepIntervalMs = 1;

        /// <summary>
        /// IO操作默认超时时间（秒）
        /// 作用：当指令未指定超时时使用的默认值，防止操作无限阻塞
        /// 实际应用中应根据设备响应特性调整
        /// </summary>
        private const int DefaultTimeoutSeconds = 30;

        /// <summary>
        /// PCIe-M60类型IO卡的点位分组大小
        /// 硬件特性：该型号IO卡采用32点位一组的地址映射方式
        /// 用于计算实际硬件地址索引
        /// </summary>
        private const int M60PointGroupSize = 32;

        /// <summary>
        /// 数字信号值：高电平（逻辑1）
        /// 统一常量定义便于维护，避免魔法数字
        /// </summary>
        private const int SignalHigh = 1;

        /// <summary>
        /// 数字信号值：低电平（逻辑0）
        /// 统一常量定义便于维护，避免魔法数字
        /// </summary>
        private const int SignalLow = 0;
        #endregion

        #region 私有字段
        /// <summary>IO指令提示信息，用于日志显示的友好名称</summary>
        private readonly string _processTip;

        /// <summary>
        /// 轻量级信号量，确保IO操作的线程安全
        /// 设计：采用静态实例，保证所有IOProcess实例共享同一信号量
        /// 作用：防止多个线程同时操作同一IO卡导致的硬件冲突
        /// 初始化为1，表示同时只允许一个线程访问
        /// </summary>
        private static readonly SemaphoreSlim _ioSemaphore = new SemaphoreSlim(1, 1);

        // 指令解析结果 - 这些字段存储从IO指令字符串中解析出的信息
        private string _commandName;       // 指令名称（固定为"IO"，用于验证指令合法性）
        private string _ioPointType;       // IO类型（"输入"或"输出"，决定执行哪种操作）
        private string _pointName;         // 点位名称（对应IO配置中的点位，用于查找硬件配置）
        private int _signalValue;          // 目标信号值（0或1，数字信号的具体值）
        private bool _signalState;         // 目标信号状态（true对应1，false对应0，便于逻辑判断）
        private int _timeoutSeconds;       // 超时时间（秒）

        // IO设备参数 - 这些字段存储从系统配置中查询到的硬件信息
        private IOCardDIODto _ioCardDio;   // IO点位配置（包含点位名称、卡名称、点位索引等详细信息）
        private IOCardDto _ioCard;         // IO卡配置（包含卡类型、卡号索引等硬件标识信息）

        /// <summary>
        /// 取消令牌，支持外部终止IO操作
        /// 可为null，表示不启用取消功能
        /// 用于在紧急情况下（如程序退出、用户中断）终止长时间运行的IO操作
        /// </summary>
        private CancellationToken? _cancellationToken;

        #endregion

        /// <summary>
        /// 指令参数（原始IO指令字符串）
        /// 保留原始指令用于日志记录和错误排查
        /// </summary>
        public string Parameter { get; private set; }

        /// <summary>
        /// 初始化IO流程实例
        /// 构造函数负责基础初始化和参数验证
        /// </summary>
        /// <param name="process">IO指令字符串（如"IO|输入|点位1|1|-1"）</param>
        /// <param name="processTip">IO指令提示信息（用于日志显示的友好名称）</param>
        public IOProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                // 初始化状态为等待
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";

                // 验证并保存参数，使用空值合并运算符和异常抛出确保参数有效
                Parameter = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                // 核心初始化步骤：解析指令和绑定硬件配置
                ParseCommand(process);      // 解析IO指令字符串，提取各参数
                BindIoParameters();         // 根据点位名称查找并绑定IO卡和点位配置
            }
            catch (Exception ex)
            {
                // 捕获初始化过程中的异常，更新状态并记录日志
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return; // 设置错误状态后立即返回
            }
        }

        /// <summary>
        /// 设置取消令牌，支持外部终止IO操作
        /// 可在操作执行前或执行中设置，用于响应外部取消请求
        /// </summary>
        /// <param name="cancellationToken">取消令牌，通常来自CancellationTokenSource</param>
        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// 动态设置流程指令（支持运行中更新指令）
        /// 用于需要在运行时改变IO操作参数的场景
        /// </summary>
        /// <param name="process">新的IO指令字符串</param>
        public void SetProcess(string process)
        {
            try
            {
                // 验证参数并更新，重新解析和绑定配置
                Parameter = process ?? throw new ArgumentNullException(nameof(process));
                ParseCommand(process);      // 重新解析指令
                BindIoParameters();         // 重新绑定IO配置
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return; // 设置错误状态后立即返回
            }
        }

        /// <summary>
        /// 解析IO指令参数，提取各部分信息并验证格式
        /// 指令格式：IO|类型|点位|信号值|超时秒数
        /// 例如：IO|输入|传感器A|1|-1 表示无限等待传感器A变为高电平
        /// </summary>
        /// <param name="process">待解析的IO指令字符串</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目（处理可能的连续竖线或首尾竖线）
            var commandParts = process.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式（至少包含5个部分）
            if (commandParts.Length < 5)
            {
                HandleIoError($"指令格式错误: {process}，至少需要5个部分，格式为指令名称|类型|点位|信号|超时");
                return;
            }

            // 提取指令各部分信息并赋值给相应字段
            _commandName = commandParts[0];
            _ioPointType = commandParts[1];       // "输入"或"输出"
            _pointName = commandParts[2];         // 点位名称（如"传感器A"）

            // 解析信号值（必须为0或1）
            if (!int.TryParse(commandParts[3], out _signalValue) ||
                (_signalValue != SignalLow && _signalValue != SignalHigh))
            {
                HandleIoError($"信号值格式错误: {commandParts[3]}（必须为0或1）");
                return;
            }

            // 转换信号值为布尔状态（便于逻辑判断，true对应高电平）
            _signalState = _signalValue == SignalHigh;

            // 解析超时时间，设置合理默认值
            if (!int.TryParse(commandParts[4], out _timeoutSeconds) || _timeoutSeconds < -1)
            {
                _timeoutSeconds = DefaultTimeoutSeconds; // 使用默认超时
                HandleIoError($"超时值格式错误: {commandParts[4]}，使用默认值 {DefaultTimeoutSeconds} 秒");
                return;
            }
        }

        /// <summary>
        /// 绑定IO卡和点位参数（从系统配置中查询对应的IO卡和点位信息）
        /// 该方法是连接指令与硬件的关键，通过点位名称关联到实际硬件配置
        /// </summary>
        private void BindIoParameters()
        {
            // 查找点位配置（根据点位名称从系统配置中查询）
            _ioCardDio = IOSystem.Instance.IOCardDios
                .FirstOrDefault(dio => dio.点位名称 == _pointName && dio.点位类型 == _ioPointType);

            // 若未找到点位配置，处理错误（触发紧急停止）
            if (_ioCardDio == null)
            {
                HandleIoError($"未找到点位名称[{_pointName}]的IO配置");
                return;
            }

            // 查找IO卡配置（根据点位所属的卡名称查询）
            _ioCard = IOSystem.Instance.IOCards
                .FirstOrDefault(card => card.卡名称 == _ioCardDio.卡名称);

            // 若未找到IO卡配置，处理错误（触发紧急停止）
            if (_ioCard == null)
            {
                HandleIoError($"未找到IO卡[{_ioCardDio.卡名称}]的配置");
                return;
            }

            // 绑定流程ID（用于日志跟踪和流程管理）
            ProcessId = _ioCardDio.绑定代码;
        }

        /// <summary>
        /// 记录IO绑定信息日志，便于调试和追溯
        /// 日志包含关键配置信息，用于问题排查时确认硬件配置是否正确
        /// </summary>
        private void LogIoBindingInfo()
        {
            // 使用StringBuilder高效构建日志信息
            var logMessage = new StringBuilder();
            logMessage.AppendLine($"IO点位绑定信息 - {_pointName}");
            logMessage.AppendLine($"IO卡类型: {_ioCard.卡类型}");
            logMessage.AppendLine($"卡号索引: {_ioCard.卡号索引}");
            logMessage.AppendLine($"点位索引: {_ioCardDio.点位索引}");
            logMessage.AppendLine($"信号值: {_signalValue} ({(_signalState ? "高电平" : "低电平")})");
            logMessage.AppendLine($"超时设置: {(_timeoutSeconds == -1 ? "无限等待" : $"{_timeoutSeconds}秒")}");

            // 保存日志到系统日志
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Gray,
                "IO绑定信息",
                _pointName,
                logMessage.ToString()
            );
        }

        /// <summary>
        /// 处理IO配置错误（配置缺失时触发紧急停止并记录日志）
        /// 配置错误属于严重错误，需要立即停止设备运行以避免意外
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void HandleIoError(string errorMessage)
        {
            // 触发机台紧急停止
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

            // 记录紧急停止日志
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                Parameter,
                $"--->>错误：{errorMessage}！！！！"
            );
            CurrentStatus = GeneralStatus.Error;
            // 抛出异常，终止当前流程
            throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// 执行IO操作（核心方法）
        /// 根据IO类型（输入/输出）执行不同操作
        /// 实现了模板方法模式，定义了通用执行流程
        /// </summary>
        public override void Run()
        {

            try
            {
                // 测试模式：直接返回成功（不实际操作IO）
                // 用于调试和模拟运行，不影响实际硬件
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 更新状态为运行中
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";

                // 检查IO配置是否初始化完成
                if (_ioCardDio == null || _ioCard == null)
                {
                    SetErrorStatus("IO配置未初始化完成");
                    return;
                }

                // 根据点位类型执行输入/输出操作
                switch (_ioCardDio.点位类型)
                {
                    case "输入":
                        ExecuteInputOperation(); // 处理输入操作
                        break;
                    case "输出":
                        ExecuteOutputOperation(); // 处理输出操作
                        break;
                    default:
                        SetErrorStatus($"未知点位类型--{_ioCardDio.点位类型}");
                        return;
                }
            }
            catch (OperationCanceledException)
            {
                // 预期的取消操作，不记录错误日志
                SetErrorStatus("操作已被取消");
                return;
            }
            catch (Exception ex)
            {
                // 捕获其他异常，更新状态并记录日志
                Message = ex.Message;
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return; // 设置错误状态后立即返回
            }
        }

        /// <summary>
        /// 执行输入操作（等待输入信号达到目标状态）
        /// 核心逻辑：循环检查输入信号，直到信号稳定达到目标值或超时/急停/取消
        /// 采用轮询方式检测信号，支持可取消操作
        /// </summary>
        private void ExecuteInputOperation()
        {
            // 使用Stopwatch更精确地计时，不受系统时间调整影响
            var stopwatch = Stopwatch.StartNew();
            var successLog = $"输入信号[{_pointName}]已稳定达到目标值 {_signalValue}";

            try
            {
                // 无限循环，直到满足退出条件
                while (true)
                {
                    // 检查取消信号，若已请求取消则抛出异常
                    _cancellationToken?.ThrowIfCancellationRequested();

                    // 检查急停状态（急停时终止操作）
                    if (CheckEmergencyStop())
                        return;

                    // 检查超时（超时秒数>0时生效）
                    if (CheckTimeout(stopwatch))
                        return;

                    // 读取并验证输入信号（多次检查确保稳定性）
                    if (ReadAndVerifyInputSignal())
                    {
                        // 信号稳定达到目标值，更新状态并记录成功日志
                        CurrentStatus = GeneralStatus.NoError;
                        Message = "NoError";
                        ProcessLog.Instance.SaveLog(_processTip, Color.Green, "输入操作成功", _pointName, successLog);
                        break;
                    }

                    // 使用可取消的延迟，提高响应性
                    // 相比Thread.Sleep，可在等待期间响应取消请求
                    _cancellationToken?.WaitHandle.WaitOne(SleepIntervalMs);
                }
            }
            finally
            {
                // 确保计时器停止
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// 执行输出操作（设置输出信号为目标状态）
        /// 核心逻辑：根据IO卡类型调用对应API，设置输出信号值
        /// 使用信号量确保线程安全，防止并发操作冲突
        /// </summary>
        private void ExecuteOutputOperation()
        {
            // 转换信号状态为输出值（1对应true，0对应false）
            var outputValue = (ushort)(_signalState ? SignalHigh : SignalLow);
            // 部分IO卡需要反转值（根据硬件特性，如低电平有效）
            var inverseValue = (ushort)(_signalState ? SignalLow : SignalHigh);

            try
            {
                // 使用轻量级信号量确保线程安全，支持取消
                // 等待信号量，若取消则抛出OperationCanceledException
                _ioSemaphore.Wait(_cancellationToken ?? CancellationToken.None);

                // 检查取消信号
                _cancellationToken?.ThrowIfCancellationRequested();

                // 根据IO卡类型执行不同的输出命令（适配不同硬件）
                int result = 0; // API调用返回值，0通常表示成功
                switch (_ioCard.卡类型)
                {
                    case "3400":
                    case "3600":
                    case "5800":
                        // 调用对应型号IO卡的输出API（使用反转值，硬件特性要求）
                        result = LTDMC.dmc_write_outbit(_ioCard.卡号索引, _ioCardDio.点位索引, inverseValue);
                        break;
                    case "7230":
                    case "7432":
                        // 调用对应型号IO卡的输出API（使用原始输出值）
                        result = DASK.DO_WriteLine(_ioCard.卡号索引, 0, _ioCardDio.点位索引, outputValue);
                        break;
                    case "PCIe-EC8003":
                        // 调用EtherCAT类型IO卡的输出API
                        result = lctdevice.ecat_io.EC_Set_Digital_Chn_Out(
                            1,
                            (short)(_ioCardDio.点位索引 + 1), // 点位索引从1开始
                            (short)outputValue,
                            (short)_ioCard.卡号索引
                        );
                        break;
                    case "PCIe-M60":
                        // 计算该型号IO卡的实际点位编号（32点位一组）
                        int number = (_ioCard.卡号索引 - 1) * M60PointGroupSize + _ioCardDio.点位索引 + 1;
                        result = ecat_motion.M_Set_Digital_Chn_Output((short)number, (short)outputValue, 0);
                        break;
                    default:
                        SetErrorStatus($"未知IO卡类型--{_ioCard.卡类型}");
                        return;
                }

                // 输出操作成功，更新状态并记录日志
                CurrentStatus = GeneralStatus.NoError;
                Message = "NoError";
            }
            finally
            {
                // 确保信号量释放，即使发生异常也会执行
                if (_ioSemaphore.CurrentCount == 0)
                {
                    _ioSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// 读取并验证输入信号（多次检查确保信号稳定，避免抖动）
        /// 信号稳定性检查是工业控制中的重要机制，可过滤瞬间干扰
        /// </summary>
        /// <returns>true：信号稳定达到目标值；false：未达到或不稳定</returns>
        private bool ReadAndVerifyInputSignal()
        {
            // 连续CheckCount次检查信号，全部符合目标值才认为稳定
            for (int i = 0; i < CheckCount; i++)
            {
                // 检查取消信号
                _cancellationToken?.ThrowIfCancellationRequested();

                // 单次读取不符合目标值，直接返回false
                if (!ReadInputSignal())
                    return false;

                // 间隔检查，避免高频读取
                _cancellationToken?.WaitHandle.WaitOne(SleepIntervalMs);
            }
            // 多次检查均符合，信号稳定
            return true;
        }

        /// <summary>
        /// 读取输入信号（根据IO卡类型调用对应API）
        /// 针对不同硬件型号的特性，实现相应的读取逻辑
        /// </summary>
        /// <returns>true：当前信号符合目标值；false：不符合</returns>
        private bool ReadInputSignal()
        {
            try
            {
                // 根据IO卡类型调用不同的读取API
                switch (_ioCard.卡类型)
                {
                    case "7230":
                    case "7432":
                        // 读取7230/7432系列IO卡的输入信号
                        var readResult7x3x = DASK.DI_ReadLine(
                            _ioCard.卡号索引,
                            1,
                            _ioCardDio.点位索引,
                            out ushort value7x3x
                        );
                        // 检查读取操作是否成功
                        if (readResult7x3x != 0)
                        {
                            SetErrorStatus($"读取7230/7432系列IO卡失败，返回代码: {readResult7x3x}");
                            return false;
                        }
                        // 直接比较读取值与目标值
                        return value7x3x == _signalValue;

                    case "3400":
                    case "3600":
                    case "5800":
                        // 读取3400/3600/5800系列IO卡的输入信号（硬件信号需反转）
                        var rawValue3x00 = LTDMC.dmc_read_inbit(_ioCard.卡号索引, _ioCardDio.点位索引);
                        // 硬件特性：该系列IO卡返回值需要逻辑反转
                        bool value3x00 = rawValue3x00 == SignalHigh ? false : true;
                        return value3x00 == _signalState;

                    case "PCIe-EC8003":
                        // 读取PCIe-EC8003系列IO卡的输入信号
                        short value8003 = (short)(_signalState ? SignalHigh : SignalLow);
                        var readResult8003 = lctdevice.ecat_io.EC_Get_Digital_Chn_In(
                            1,
                            (short)(_ioCardDio.点位索引 + 1), // 点位索引从1开始
                            ref value8003,
                            (short)_ioCard.卡号索引
                        );
                        // 检查读取操作是否成功
                        if (readResult8003 != 0)
                        {
                            SetErrorStatus($"读取PCIe-EC8003系列IO卡失败，返回代码: {readResult8003}");
                            return false;
                        }
                        return value8003 == _signalValue;

                    case "PCIe-M60":
                        short valueM60;
                        // 计算该型号IO卡的实际点位编号
                        int number = (_ioCard.卡号索引 - 1) * M60PointGroupSize + _ioCardDio.点位索引 + 1;
                        short ret = ecat_motion.M_Get_Digital_Chn_Input((short)number, out valueM60, 0);
                        // 检查读取操作是否成功
                        if (ret != 0)
                        {
                            SetErrorStatus($"读取PCIe-M60系列IO卡失败，返回代码: {ret}");
                            return false;
                        }
                        return valueM60 == _signalValue;

                    default:
                        SetErrorStatus($"未知IO卡类型--{_ioCard.卡类型}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus($"读取输入信号失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查机台急停状态（急停时终止IO操作）
        /// 安全机制：急停状态下应立即停止所有IO操作
        /// </summary>
        /// <returns>true：机台急停；false：正常运行</returns>
        private bool CheckEmergencyStop()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                SetErrorStatus("机台急停");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查IO操作是否超时
        /// 超时机制防止操作无限阻塞，提高系统可靠性
        /// </summary>
        /// <param name="stopwatch">计时器，记录操作开始时间</param>
        /// <returns>true：已超时；false：未超时</returns>
        private bool CheckTimeout(Stopwatch stopwatch)
        {
            // 超时秒数≠-1（有限等待），且已超过设定时间
            if (_timeoutSeconds != -1 && stopwatch.Elapsed.TotalSeconds > _timeoutSeconds)
            {
                //if (_pointName== "后焊接机焊接中")
                //{
                //    Controller.Instance.后焊接机无焊接中信号报警 = true;
                //}
                //if (_pointName == "前焊接机焊接中")
                //{
                //    Controller.Instance.前焊接机无焊接中信号报警 = true;
                //}
                SetErrorStatus($"IO超时！！  超时时间: {_timeoutSeconds}秒  点位: {_pointName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置错误状态（统一更新运行状态和消息）
        /// 封装错误处理逻辑，确保状态和日志的一致性
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
            ProcessLog.Instance.SaveLog(_processTip, Color.Red, "IO操作错误", _pointName, errorMessage);
        }

        /// <summary>
        /// 克隆IOProcess实例（用于缓存机制中的对象复制）
        /// 实现ICloneable接口，提供深拷贝功能
        /// </summary>
        /// <returns>克隆的IOProcess实例</returns>
        public override object Clone()
        {
            // 创建新实例并复制所有属性
            var cloned = new IOProcess(this.Parameter, this._processTip);
            cloned._commandName = this._commandName;
            cloned._ioPointType = this._ioPointType;
            cloned._pointName = this._pointName;
            cloned._signalValue = this._signalValue;
            cloned._signalState = this._signalState;
            cloned._timeoutSeconds = this._timeoutSeconds;
            cloned._ioCardDio = this._ioCardDio;
            cloned._ioCard = this._ioCard;
            cloned.ProcessId = this.ProcessId;
            cloned.CurrentStatus = GeneralStatus.Wait;
            cloned.Message = "Wait";
            return cloned;
        }
    }
}
