using HslCommunication;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Permission;
using Jabil.Module.ProductData.Frm;
using Jabil.Module.ProductData.Lib;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace JabilVisionMotion.Edit.Process.ModbusControlProcess
{
    /// <summary>
    /// ModbusRobot通信流程处理类，继承自ProcessBase并实现ICloneable接口
    /// 核心功能：通过"缓存+直接通信"双源数据机制与工业机器人设备进行通信
    /// 支持输入（读取机器人状态）和输出（写入控制指令）两种操作模式
    /// 适配品牌：Epson、Fanuc、Efort等主流工业机器人
    /// 
    /// 设计特点：
    /// 1. 双源数据获取：优先使用ModbusDioRealTimeManager缓存数据，缓存无效时直接与设备通信
    /// 2. 工业级可靠性：包含连接重试、超时控制、连续失败阈值等容错机制
    /// 3. 线程安全：关键操作加锁，支持多线程环境下的并发执行
    /// 4. 松耦合设计：不强制依赖缓存管理器，可降级为直接通信模式
    /// </summary>
    public class ModbusRobotCommunicationProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>
        /// 连接重试延迟时间（毫秒）
        /// 设计目的：机器人连接失败后，等待一段时间再重试，避免短时间内频繁连接导致的设备/网络压力
        /// 取值依据：工业场景中设备恢复响应的典型时间，1000ms为经验值
        /// </summary>
        private const int ConnectionRetryDelayMs = 1000;

        /// <summary>
        /// 读取检查间隔（毫秒）
        /// 设计目的：输入操作循环读取时的等待间隔，平衡实时性与系统资源占用
        /// 取值依据：15ms既能保证较高的实时性，又不会过度消耗CPU资源
        /// </summary>
        private const int ReadCheckIntervalMs = 15;

        /// <summary>Epson机器人类型标识</summary>
        private const string EpsonDeviceType = "Epson";

        /// <summary>Fanuc机器人类型标识</summary>
        private const string FanucDeviceType = "Fanuc";

        /// <summary>Efort机器人类型标识</summary>
        private const string EfortDeviceType = "Efort";

        /// <summary>
        /// 最大直接读取失败阈值
        /// 设计目的：限制连续读取失败次数，避免无限重试导致的流程阻塞
        /// 取值依据：工业环境中短暂通信波动的容忍次数，5次为经验值
        /// </summary>
        private const int MaxDirectReadFailedThreshold = 5;
        #endregion

        #region 私有字段
        /// <summary>原始Modbus指令字符串（完整指令内容，格式：指令名称|点位类型|...）</summary>
        private readonly string _process;

        /// <summary>指令提示信息（用于日志记录，提高可追溯性）</summary>
        private readonly string _processTip;

        /// <summary>
        /// 静态线程锁对象
        /// 作用：确保多线程环境下对机器人设备的读写操作互斥，避免并发冲突导致的数据不一致
        /// 注意：使用静态锁确保所有实例共享同一把锁，严格控制设备访问顺序
        /// </summary>
        private static readonly object _lock = new object();

        // 指令解析结果（从原始指令中提取的关键参数）
        /// <summary>指令名称（固定为"ModbusRobot"，用于指令类型校验）</summary>
        private string _commandName;
        /// <summary>点位类型："输入"（读取机器人状态）或"输出"（控制机器人动作）</summary>
        private string _pointType;
        /// <summary>点位定义：标识机器人的功能区域（如"C030"对应特定控制模块）</summary>
        private string _pointDefinition;
        /// <summary>点位名称：具体信号的语义标识（如"机器人就绪"、"抓取完成"）</summary>
        private string _pointName;
        /// <summary>点位号值：目标值（读取时为期望状态值，写入时为控制指令值）</summary>
        private int _pointValue;
        /// <summary>超时时间（秒）：操作最长允许时间，超时未完成则触发错误处理</summary>
        private int _timeoutSeconds;

        // Modbus参数字段（与机器人通信的核心配置）
        /// <summary>
        /// 点位配置DTO（数据传输对象）
        /// 包含：数据类型（int16/bool）、设备名称、点位索引等详细配置
        /// </summary>
        private ModbusCommunicationDioDto _modbusDio;
        /// <summary>
        /// 设备配置DTO
        /// 包含：机器人IP地址、端口号、设备名称等网络通信必要信息
        /// </summary>
        private ModbusCommunicationDto _modbusDevice;
        /// <summary>流程开始时间：用于计算操作是否超时（管理器不可用时）</summary>
        private DateTime _processStartTime;
        /// <summary>
        /// 管理器中的点位唯一键
        /// 格式："设备名称_点位索引"，用于从实时数据管理器快速定位点位数据
        /// </summary>
        private string _dataKey;

        // 直接通信相关字段
        /// <summary>
        /// 直接读取失败计数器
        /// 作用：累计连续读取失败次数，达到阈值时触发错误处理
        /// 重置时机：从管理器或直接读取成功时重置为0
        /// </summary>
        private int _directReadFailedCount = 0;
        #endregion

        /// <summary>
        /// 初始化ModbusRobot通信流程实例
        /// </summary>
        /// <param name="process">原始Modbus机器人指令字符串（格式：指令名称|点位类型|点位定义|点位名称|点位号值|超时）</param>
        /// <param name="processTip">指令提示信息（用于日志记录的附加说明）</param>
        public ModbusRobotCommunicationProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                // 验证输入参数非空，为空则抛出参数异常（快速失败原则）
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                // 不强制依赖缓存管理器，未初始化时仅警告并切换到直接通信模式
                // 设计思路：提高系统容错性，避免因管理器故障导致整个通信流程失效
                if (!ModbusDioRealTimeManager.IsInitialized)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                        "Modbus实时数据管理器尚未初始化，将使用直接通信模式");
                }

                // 解析指令字符串，提取关键参数（点位类型、定义等）
                ParseCommand(process);

                // 绑定机器人设备和点位配置（从管理器或系统配置获取）
                BindModbusParameters();

                // 初始化流程状态为"等待"，等待执行信号
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";
            }
            catch (Exception ex)
            {
                // 初始化失败时设置错误状态，记录异常日志（便于问题排查）
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 克隆当前实例，实现ICloneable接口
        /// 设计目的：在多线程并发执行时，确保每个线程使用独立的实例，避免状态干扰
        /// 应用场景：主从流程同步、并行控制多个机器人时使用独立实例
        /// </summary>
        /// <returns>当前实例的克隆副本（深拷贝关键配置字段）</returns>
        public override object Clone()
        {
            // 创建新实例并复制所有关键配置字段（确保状态独立）
            var clone = new ModbusRobotCommunicationProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointType = this._pointType,
                _pointDefinition = this._pointDefinition,
                _pointName = this._pointName,
                _pointValue = this._pointValue,
                _timeoutSeconds = this._timeoutSeconds,
                _modbusDio = this._modbusDio,
                _modbusDevice = this._modbusDevice,
                _dataKey = this._dataKey
            };
            // 克隆实例初始状态为"等待"
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";

            return clone;
        }

        /// <summary>
        /// 解析Modbus机器人指令参数
        /// 功能：将原始指令字符串按格式分割，提取并验证各部分参数的有效性
        /// 指令格式：指令名称|点位类型|点位定义|点位名称|点位号值|超时（秒）
        /// </summary>
        /// <param name="process">待解析的原始指令字符串</param>
        private void ParseCommand(string process)
        {
            // 按竖线"|"分割指令，移除空条目（处理可能的格式错误，如连续竖线）
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式是否正确（至少包含6个部分）
            if (commandParts.Length < 6)
            {
                HandleModbusError($"指令格式错误: {process}，至少需要6个部分（指令名称|点位类型|点位定义|点位名称|点位号值|超时）");
                return;
            }

            // 提取各部分参数并赋值
            _commandName = commandParts[0];
            _pointType = commandParts[1];         // 点位类型（"输入"/"输出"）
            _pointDefinition = commandParts[2];   // 点位定义（如"C030"）
            _pointName = commandParts[3];         // 点位名称（如"机器人就绪信号"）

            // 解析点位号值（必须为整数）
            if (!int.TryParse(commandParts[4], out _pointValue))
                HandleModbusError($"点位号值格式错误: {commandParts[4]}（应为整数）");

            // 解析超时时间（必须为整数，-1表示无限等待）
            if (!int.TryParse(commandParts[5], out _timeoutSeconds))
                HandleModbusError($"超时值格式错误: {commandParts[5]}（应为整数）");
        }

        /// <summary>
        /// 绑定Modbus参数，支持从管理器和系统配置双重来源获取
        /// 设计思路：采用"缓存优先，配置兜底"策略，提高配置获取的可靠性
        /// </summary>
        private void BindModbusParameters()
        {
            // 优先从管理器获取配置（缓存机制提高效率）
            IEnumerable<ModbusCommunicationDioDto> allDios = new List<ModbusCommunicationDioDto>();

            if (ModbusDioRealTimeManager.IsInitialized)
            {
                allDios = ModbusDioRealTimeManager
                    .GetDeviceRealTimeData(string.Empty)  // 空字符串表示获取所有设备配置
                    .Select(d => d.DioConfig)             // 提取点位配置信息
                    .Where(d => d != null);               // 过滤空配置，避免空引用异常
            }

            // 管理器无数据时，直接从系统配置获取（兜底机制）
            if (!allDios.Any())
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                    "从管理器获取配置失败，尝试直接从系统配置获取");
                allDios = ModbusSystem.Instance?.ModbusCommunicationDios ?? new List<ModbusCommunicationDioDto>();
            }

            // 检查是否存在任何点位配置（无配置则无法进行通信）
            if (!allDios.Any())
            {
                HandleModbusError("未找到任何点位配置，请检查系统配置");
                return;
            }

            // 根据指令参数（点位类型、定义、名称）查找匹配的点位配置
            _modbusDio = allDios
                .FirstOrDefault(dio =>
                    dio.点位类型 == _pointType &&
                    dio.点位定义 == _pointDefinition &&
                    dio.点位名称 == _pointName);

            // 未找到匹配的点位配置时处理错误
            if (_modbusDio == null)
            {
                HandleModbusError($"未找到匹配的Modbus DIO配置 [类型:{_pointType}, 定义:{_pointDefinition}, 名称:{_pointName}]");
                return;
            }

            // 绑定流程ID（用于日志跟踪和流程管理，与点位配置关联）
            ProcessId = _modbusDio.绑定代码;

            // 生成管理器中的点位唯一键（用于快速查询实时数据）
            _dataKey = ModbusDioRealTimeManager.GetKey(_modbusDio.设备名称, _modbusDio.点位索引);

            // 根据点位配置中的设备名称，查找对应的机器人设备配置（IP、端口等）
            _modbusDevice = ModbusSystem.Instance.ModbusCommunications
                .FirstOrDefault(dev => dev.设备名称 == _modbusDio.设备名称);

            // 未找到设备配置时处理错误
            if (_modbusDevice == null)
                HandleModbusError($"未找到Modbus设备配置 [设备名称:{_modbusDio.设备名称}]");
        }

        /// <summary>
        /// 处理Modbus配置错误
        /// 设计目的：工业环境下，配置错误可能导致设备异常动作，需立即触发紧急停止确保安全
        /// </summary>
        /// <param name="errorMessage">错误描述信息（用于日志和状态提示）</param>
        private void HandleModbusError(string errorMessage)
        {
            // 触发机台紧急停止，确保设备和人员安全（工业场景核心优先级）
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

            // 记录错误日志（包含提示信息、指令内容和错误详情，便于追溯）
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,          // 日志颜色标识（红色表示错误）
                "紧急停止",         // 日志类型
                _process,           // 原始指令
                $"--->>错误：{errorMessage}！！！！"  // 错误详情
            );

            // 更新当前状态为错误，并抛出异常终止流程执行
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
            throw new InvalidOperationException(errorMessage);
        }

        /// <summary>
        /// 执行ModbusRobot通信操作（核心入口方法）
        /// 流程：初始化状态 → 验证模块 → 确保设备连接 → 分发到输入/输出处理
        /// </summary>
        public override void Run()
        {
            try
            {
                // 测试模式：直接返回成功，不进行实际通信（用于调试和模拟场景）
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 初始化运行状态
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";
                _processStartTime = DateTime.Now;  // 记录流程开始时间，用于超时计算

                // 验证Robot模块是否加载（防止模块缺失导致的空引用异常）
                if (ModbusSystem.Instance.RobotModbusList == null ||
                    ModbusSystem.Instance.RobotModbusList.Count == 0)
                {
                    SetErrorStatus("未加载Robot_Modbus模块！！！");
                    return;
                }

                // 获取机器人设备实例并确保连接正常
                var robotDevice = GetRobotDevice();
                if (robotDevice == null || !EnsureDeviceConnection(robotDevice))
                    return;  // 设备获取失败或连接失败，终止流程

                // 根据点位类型执行对应的操作（输入/输出）
                switch (_pointType)
                {
                    case "输入":
                        ProcessInputOperation(robotDevice);  // 处理读取机器人状态操作
                        break;
                    case "输出":
                        ProcessOutputOperation(robotDevice); // 处理控制机器人动作操作
                        break;
                    default:
                        SetErrorStatus($"未知点位类型--{_pointType}（应为\"输入\"或\"输出\"）");
                        return;
                }
            }
            catch (Exception ex)
            {
                // 捕获并处理执行过程中的异常（记录日志并更新状态）
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);  // 记录异常日志，便于问题排查
            }
        }

        /// <summary>
        /// 获取机器人设备实例
        /// 功能：从系统机器人设备列表中查找与当前配置匹配的设备
        /// </summary>
        /// <returns>匹配的Robot_Modbus实例，若未找到则返回null</returns>
        private Robot_Modbus GetRobotDevice()
        {
            return ModbusSystem.Instance.RobotModbusList
                ?.FirstOrDefault(p => p.DeviceName == _modbusDevice.设备名称);
        }

        /// <summary>
        /// 确保机器人设备连接正常
        /// 流程：检查连接状态 → 未连接则断开旧连接 → 延迟后重试连接 → 验证连接结果
        /// </summary>
        /// <param name="robotDevice">机器人设备实例</param>
        /// <returns>true：连接正常；false：连接失败</returns>
        private bool EnsureDeviceConnection(Robot_Modbus robotDevice)
        {
            // 检查设备当前连接状态
            if (!robotDevice.IsConnected)
            {
                // 先断开现有连接（防止残留连接影响重连）
                robotDevice.Disconnect();

                // 等待重试延迟（避免频繁重试导致的网络/设备压力）
                Thread.Sleep(ConnectionRetryDelayMs);

                // 执行连接操作（根据机器人类型选择对应协议）
                var connectResult = ConnectRobot(robotDevice);
                if (!connectResult.IsSuccess)
                {
                    SetErrorStatus($"设备：{_modbusDevice.设备名称}--通讯连接失败，请检查IP、端口及设备状态！！");
                    return false;
                }
            }
            return true;  // 连接正常
        }

        /// <summary>
        /// 根据机器人类型建立连接
        /// 功能：适配不同品牌机器人的Modbus通信协议（Epson/Fanuc/Efort）
        /// </summary>
        /// <param name="robotDevice">机器人设备实例</param>
        /// <returns>连接操作结果（成功/失败及错误信息）</returns>
        private OperateResult ConnectRobot(Robot_Modbus robotDevice)
        {
            // 根据设备名称识别机器人类型，选择对应连接方式
            if (robotDevice.DeviceName.Contains(EpsonDeviceType))
            {
                return robotDevice.Connect(Robot_Modbus.RobotType.Epson, _modbusDevice.设备IP, _modbusDevice.设备Port);
            }
            if (robotDevice.DeviceName.Contains(FanucDeviceType))
            {
                return robotDevice.Connect(Robot_Modbus.RobotType.Fanuc, _modbusDevice.设备IP, _modbusDevice.设备Port);
            }
            if (robotDevice.DeviceName.Contains(EfortDeviceType))
            {
                return robotDevice.Connect(Robot_Modbus.RobotType.Efort, _modbusDevice.设备IP, _modbusDevice.设备Port);
            }

            // 未知机器人类型时返回失败
            return new OperateResult("未知机器人类型，不支持连接");
        }

        /// <summary>
        /// 处理输入操作（读取机器人状态）
        /// 核心逻辑："缓存优先，直接读取兜底"的双源数据获取机制
        /// 流程：检查急停/超时 → 尝试从管理器获取数据 → 缓存无效则直接读取 → 验证数据 → 成功则退出循环
        /// </summary>
        /// <param name="robotDevice">机器人设备实例（备用，直接读取时使用）</param>
        private void ProcessInputOperation(Robot_Modbus robotDevice)
        {
            // 验证点位定义是否支持（当前仅支持"C030"区域，可根据需求扩展）
            //if (_pointDefinition != "Efort")
            //{
            //    SetErrorStatus($"未知点位定义--{_pointDefinition}（当前仅支持\"C030\"）");
            //    return;
            //}

            // 循环读取并验证数据，直到满足条件或超时/急停
            while (true)
            {
                // 检查急停状态（急停时终止操作）
                if (CheckEmergencyStop()) return;

                // 检查是否超时（超时则终止操作）
                if (CheckTimeout()) return;

                object currentValue = null;
                bool isDataValid = false;

                // 1. 尝试从管理器获取有效数据（优先使用缓存，减少设备通信）
                if (ModbusDioRealTimeManager.IsInitialized)
                {
                    var realTimeData = ModbusDioRealTimeManager.GetRealTimeData(_modbusDio.设备名称, _modbusDio.点位索引);
                    if (realTimeData != null && realTimeData.IsValid)
                    {
                        currentValue = realTimeData.RealTimeValue;
                        isDataValid = true;
                        _directReadFailedCount = 0; // 重置失败计数器
                    }
                }

                // 2. 管理器数据无效时，直接读取机器人（兜底机制）
                if (true) //!isDataValid
                {
                    Message = "管理器数据无效，尝试直接读取机器人...";
                    var directReadResult = ReadFromRobotDirectly(robotDevice);

                    // 直接读取失败处理
                    if (!directReadResult.IsSuccess)
                    {
                        _directReadFailedCount++;
                        // 达到最大失败阈值，触发错误
                        if (_directReadFailedCount >= MaxDirectReadFailedThreshold)
                        {
                            SetErrorStatus($"连续{MaxDirectReadFailedThreshold}次直接读取机器人失败: {directReadResult.Message}");
                            return;
                        }
                        // 未达阈值，等待后重试
                        Thread.Sleep(ReadCheckIntervalMs);
                        continue;
                    }

                    // 直接读取成功
                    currentValue = directReadResult.Content;
                    isDataValid = true;

                    // 3. 更新管理器缓存（如果管理器可用），保证数据一致性
                    if (ModbusDioRealTimeManager.IsInitialized)
                    {
                        ModbusDioRealTimeManager.UpdateRealTimeValue(
                            _modbusDio.设备名称,
                            _modbusDio.点位索引,
                            currentValue,
                            true); // 强制更新缓存
                    }
                }

                // 4. 验证数据是否符合目标值
                if (VerifyInputValue(currentValue))
                {
                    Message = CreateSuccessMessage(
                                            _modbusDio.点位名称,
                                            _modbusDio.点位索引,
                                            _pointValue,
                                            currentValue.ToString()
                                        );
                    CurrentStatus = GeneralStatus.NoError;  // 验证通过，设置成功状态
                    break;
                }

                // 未满足条件，间隔等待后重试
                Thread.Sleep(ReadCheckIntervalMs);
            }
        }

        /// <summary>
        /// 直接从机器人读取数据（双源机制的兜底实现）
        /// 支持类型：bool（开关量）、int16（16位整数）
        /// 线程安全：通过lock确保并发安全
        /// </summary>
        /// <param name="robotDevice">机器人设备实例</param>
        /// <returns>读取结果（包含是否成功和数据内容）</returns>
        private OperateResult<object> ReadFromRobotDirectly(Robot_Modbus robotDevice)
        {
            try
            {
                // 加锁确保读取操作的线程安全（避免并发读取导致的通信冲突）
                lock (_lock)
                {
                    // 根据数据类型执行对应的读取方法
                    switch (_modbusDio.数据类型)
                    {
                        case "bool":
                            var boolResult = robotDevice.ReadEpsonIoPoint(robotDevice,_modbusDio);
                            return boolResult.IsSuccess
                                ? OperateResult.CreateSuccessResult<object>(boolResult.Content)
                                : OperateResult.CreateFailedResult<object>(boolResult);

                        case "int16":
                            var intResult = robotDevice.ReadInt16(_modbusDio.点位索引);
                            return intResult.IsSuccess
                                ? OperateResult.CreateSuccessResult<object>(intResult.Content)
                                : OperateResult.CreateFailedResult<object>(intResult);

                        default:
                            return OperateResult.CreateFailedResult<object>(
                                new OperateResult($"不支持的读取类型: {_modbusDio.数据类型}"));
                    }
                }
            }
            catch (Exception ex)
            {
                return OperateResult.CreateFailedResult<object>(
                    new OperateResult($"直接读取异常: {ex.Message}"));
            }
        }












        /// <summary>
        /// 验证输入值（兼容缓存和直接读取的数据）
        /// 功能：检查当前值是否与目标值匹配，支持int16和bool两种类型
        /// </summary>
        /// <param name="value">待验证的值（来自缓存或直接读取）</param>
        /// <returns>true：验证通过；false：未通过</returns>
        private bool VerifyInputValue(object value)
        {
            if (value == null) return false;  // 数据为空时返回无效

            try
            {
                // 根据数据类型执行验证
                switch (_modbusDio.数据类型)
                {
                    case "int16":
                        // 将object类型转换为short（处理主要的数值比较场景）
                        if (!short.TryParse(value.ToString(), out short int16Value))
                        {
                            SetErrorStatus($"数据格式错误，无法转换为整数: {value}");
                            return false;
                        }

                        switch (_modbusDio.点位名称)
                        {
                            case "Efort呼叫拍照":
                                switch (int16Value)
                                {
                                    case 0:
                                    case 1:
                                    case 2:
                                    case 3:
                                    case 4:
                                        if (int16Value == _pointValue)
                                        {
                                            //Controller.Instance.StationInfo.Efort呼叫拍照索引 = int16Value;
                                            //Message = CreateSuccessMessage(
                                            //     _modbusDio.点位名称,
                                            //     _modbusDio.点位索引,
                                            //     _pointValue,
                                            //     int16Value.ToString()
                                            // );
                                            return true;
                                        }
                                        else
                                        {
                                            return false;
                                        }
                                    default:
                                        return false;
                                }
                            default:
                                // 验证16位整数是否匹配目标值
                                return value is short currentInt && currentInt == _pointValue;
                        }
                    case "bool":
                        // 布尔值转换为整数（1/0）后验证（适配工业场景中开关量的表示习惯）
                        //int currentBoolAsInt = (value is bool b && b) ? 1 : 0;
                        //if (currentBoolAsInt == _pointValue)
                        //{
                        //    Message = CreateSuccessMessage(
                        //        _modbusDio.点位名称,
                        //        _modbusDio.点位索引,
                        //        _pointValue,
                        //        (value is bool).ToString());
                        //    return true;
                        //}
                        //return false;



                        int currentBoolAsInt = (value is bool b && b) ? 1 : 0;
                        if (currentBoolAsInt == _pointValue)
                        {
                            Message = CreateSuccessMessage(
                                _modbusDio.点位名称,
                                _modbusDio.点位索引,
                                _pointValue,
                                (value is bool).ToString());
                            return true;
                        }
                        return false;

                    default:
                        SetErrorStatus($"未知数据类型--{_modbusDio.数据类型}（支持：int16、bool）");
                        return false;
                }
            }
            catch (Exception ex)
            {
                Message = $"数据验证错误: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 处理输出操作（向机器人写入控制指令）
        /// 流程：验证点位定义 → 转换目标值类型 → 写入机器人 → 更新缓存 → 读取验证 → 最终确认
        /// </summary>
        /// <param name="robotDevice">机器人设备实例（用于执行写入操作）</param>
        private void ProcessOutputOperation(Robot_Modbus robotDevice)
        {
            // 验证点位定义是否支持（当前仅支持"C030"区域）
            //if (_pointDefinition != "Efort")
            //{
            //    SetErrorStatus($"未知点位定义--{_pointDefinition}（当前仅支持\"C030\"）");
            //    return;
            //}

            try
            {
                //switch (_pointName)
                //{
                //    case "扫码Gap结果":
                        
                //        break;
                //    default:
                //        break;
                //}

                // 根据数据类型转换目标值（int转short/bool，匹配机器人接收格式）
                object targetValue = ConvertTargetValue(_modbusDio.数据类型, _pointValue);

                // 写入机器人并检查是否成功
                var writeSuccess = WriteToRobot(robotDevice, targetValue);
                if (writeSuccess)
                {
                    // 写入成功后，更新管理器缓存（如果可用），确保其他模块获取最新数据
                    if (ModbusDioRealTimeManager.IsInitialized)
                    {
                        ModbusDioRealTimeManager.UpdateRealTimeValue(
                            _modbusDio.设备名称,
                            _modbusDio.点位索引,
                            targetValue,
                            true);  // 强制更新缓存
                    }

                    // 读取确认：写入后立即读取，验证是否写入成功
                    var confirmResult = ReadFromRobotDirectly(robotDevice);
                    if (!confirmResult.IsSuccess)
                    {
                        SetErrorStatus($"写入成功但验证读取失败: {confirmResult.Message}");
                        return;
                    }

                    // 最终验证：确认读取值与目标值一致
                    if (VerifyInputValue(confirmResult.Content))
                    {
                        Message = CreateSuccessMessage(
                                              _modbusDio.点位名称,
                                              _modbusDio.点位索引,
                                              _pointValue,
                                              confirmResult.Content.ToString()
                                          );
                        CurrentStatus = GeneralStatus.NoError;
                    }
                    else
                    {
                        SetErrorStatus($"写入值与读取验证不匹配: 目标值={targetValue}, 实际值={confirmResult.Content}");
                    }
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus($"输出操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 向机器人写入数据
        /// 功能：根据数据类型执行对应的写入方法，确保线程安全
        /// </summary>
        /// <param name="robotDevice">机器人设备实例</param>
        /// <param name="value">待写入的值（已转换为对应类型）</param>
        /// <returns>true：写入成功；false：写入失败</returns>
        private bool WriteToRobot(Robot_Modbus robotDevice, object value)
        {
            try
            {
                // 加锁确保写入操作的线程安全（避免并发写入导致的数据冲突）
                lock (_lock)
                {
                    // 根据数据类型执行对应的写入方法
                    OperateResult result = _modbusDio.数据类型 switch
                    {
                        "bool" => robotDevice.WriteBool(Convert.ToBoolean(value), _modbusDio.点位索引),
                        "int16" => robotDevice.WriteInt16(Convert.ToInt16(value), _modbusDio.点位索引),
                        _ => new OperateResult($"不支持的数据类型: {_modbusDio.数据类型}")
                    };

                    // 处理写入失败的情况
                    if (!result.IsSuccess)
                    {
                        SetErrorStatus($"写入失败: {result.Message} (错误代码: {result.ErrorCode})");
                        return false;
                    }
                    return true;  // 写入成功
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus($"写入操作异常: {ex.Message}");
                return false;
            }
        }

        #region 辅助方法
        /// <summary>
        /// 转换目标值到对应的数据类型
        /// 功能：将整数类型的指令值转换为机器人要求的实际类型（bool/int16）
        /// </summary>
        /// <param name="dataType">目标数据类型</param>
        /// <param name="value">原始整数值</param>
        /// <returns>转换后的目标值</returns>
        /// <exception cref="ArgumentException">不支持的数据类型时抛出</exception>
        private object ConvertTargetValue(string dataType, int value)
        {
            return dataType switch
            {
                "bool" => value != 0,  // 非0值转换为true，0转换为false（工业开关量标准）
                "int16" => (short)value,  // 转换为16位整数（避免数据溢出）
                _ => throw new ArgumentException($"不支持的数据类型转换: {dataType}")
            };
        }

        /// <summary>
        /// 检查紧急停止状态
        /// 功能：在操作过程中实时监测机台是否处于急停状态，确保安全
        /// </summary>
        /// <returns>true：急停状态；false：正常状态</returns>
        private bool CheckEmergencyStop()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                SetErrorStatus("机台急停，终止操作");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查操作是否超时
        /// 设计：支持两种超时检测模式，适配管理器可用/不可用场景
        /// </summary>
        /// <returns>true：已超时；false：未超时</returns>
        private bool CheckTimeout()
        {
            if (_timeoutSeconds == -1)
                return false;  // -1表示无限等待，不检查超时

            // 1. 管理器可用时，优先使用管理器超时检查（基于数据更新时间）
            if (ModbusDioRealTimeManager.IsInitialized)
            {
                var timeoutData = ModbusDioRealTimeManager.GetTimeoutData(TimeSpan.FromSeconds(_timeoutSeconds));
                if (timeoutData.Any(d => d.DeviceName == _modbusDio.设备名称 && d.PointIndex == _modbusDio.点位索引))
                {
                    SetErrorStatus($"通讯超时！超过 {_timeoutSeconds} 秒未更新机器人数据");
                    return true;
                }
            }
            // 2. 管理器不可用时，使用直接计时检查（基于流程开始时间）
            else
            {
                var elapsed = DateTime.Now - _processStartTime;
                if (elapsed.TotalSeconds > _timeoutSeconds)
                {
                    SetErrorStatus($"操作超时！超过 {_timeoutSeconds} 秒未完成");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 创建成功状态消息
        /// 功能：格式化成功信息，包含关键参数，便于日志查看和状态跟踪
        /// </summary>
        /// <param name="pointName">点位名称</param>
        /// <param name="pointIndex">点位号</param>
        /// <param name="targetValue">目标值</param>
        /// <param name="currentValue">当前值</param>
        /// <returns>格式化的成功消息字符串</returns>
        private string CreateSuccessMessage(string pointName, string pointIndex, object targetValue, string currentValue)
        {
            return $"\r\nNoError" +
                   $"\r\n点位名称：{pointName}" +
                   $"\r\n点位号：{pointIndex}" +
                   $"\r\n目标值：{targetValue}" +
                   $"\r\n当前值：{currentValue}\r\n";
        }

        /// <summary>
        /// 设置错误状态
        /// 功能：统一更新当前流程状态为错误，并记录错误消息
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }
        #endregion
    }
}
