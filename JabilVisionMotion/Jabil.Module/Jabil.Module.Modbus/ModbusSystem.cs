using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Modbus.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.Modbus
{
    /// <summary>
    /// Modbus系统核心管理器
    /// 采用单例模式设计，确保整个应用程序中只有一个实例
    /// 主要功能：
    /// 1. 管理Modbus设备和DIO点位的配置信息
    /// 2. 处理设备和点位的增删改查(CRUD)操作
    /// 3. 协调Modbus通信服务与UI层的数据交互
    /// 4. 维护设备连接状态和DIO信号状态
    /// 继承自BindableBase，支持属性变更通知，用于UI数据绑定
    /// </summary>
    public class ModbusSystem : BindableBase
    {
        #region 单例模式（线程安全实现）
        /// <summary>
        /// 私有静态实例变量，存储ModbusSystem的唯一实例
        /// </summary>
        private static ModbusSystem _instance;

        /// <summary>
        /// 线程同步锁对象，用于在多线程环境下保证单例初始化的线程安全
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// 公共静态属性，提供单例实例的访问点
        /// 采用双重检查锁定(Double-Checked Locking)模式，兼顾性能和线程安全
        /// </summary>
        public static ModbusSystem Instance
        {
            get
            {
                // 第一次检查，避免每次访问都进入同步块
                if (_instance == null)
                {
                    // 加锁，确保只有一个线程进入实例初始化代码
                    lock (_lock)
                    {
                        // 第二次检查，防止多个线程同时通过第一次检查后重复创建实例
                        if (_instance == null)
                        {
                            _instance = new ModbusSystem();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 私有字段
        /// <summary>
        /// 存储Modbus通信设备的配置信息列表
        /// 包含设备的基本信息（如IP地址、端口、DeviceType等）
        /// </summary>
        private List<ModbusCommunicationDto> _modbusCommunications;

        /// <summary>
        /// 存储Modbus DIO（数字输入输出）点位的配置信息列表
        /// 包含点位地址、DataType、所属设备等信息
        /// </summary>
        private List<ModbusCommunicationDioDto> _modbusCommunicationDios;

        /// <summary>
        /// Modbus设备通信服务实例
        /// 负责与数据源（如数据库或远程服务）交互，处理设备信息的持久化操作
        /// 只读字段确保初始化后不可修改，保证线程安全
        /// </summary>
        private readonly ModbusCommunicationService _modbusService;

        /// <summary>
        /// Modbus DIO点位服务实例
        /// 负责DIO点位信息的持久化操作（增删改查）
        /// 只读字段确保初始化后不可修改，保证线程安全
        /// </summary>
        private readonly ModbusCommunicationDIOService _modbusDioService;

        /// <summary>
        /// PLC设备通信实例集合
        /// 存储已创建的PLC设备通信对象，用于实时通信和状态监控
        /// 初始化为空集合，避免null引用异常
        /// </summary>
        public List<PLC_Modbus> PlcModbusList { get; } = new List<PLC_Modbus>();

        /// <summary>
        /// 机器人设备通信实例集合
        /// 存储已创建的机器人设备通信对象，用于实时通信和状态监控
        /// 初始化为空集合，避免null引用异常
        /// </summary>
        public List<Robot_Modbus> RobotModbusList { get; } = new List<Robot_Modbus>();

        #endregion

        #region 公共属性
        /// <summary>
        /// Modbus通信设备列表的公开访问接口
        /// 提供设备配置信息的读取访问，并在数据变更时通知UI层更新
        /// 使用null-coalescing运算符(??=)确保不会返回null，避免外部调用时的null检查
        /// </summary>
        public List<ModbusCommunicationDto> ModbusCommunications
        {
            get => _modbusCommunications ??= new List<ModbusCommunicationDto>();
            private set
            {
                _modbusCommunications = value;
                // 触发属性变更通知，UI层绑定该属性的控件会自动刷新
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Modbus DIO点位列表的公开访问接口
        /// 提供DIO点位配置信息的读取访问，并在数据变更时通知UI层更新
        /// 使用null-coalescing运算符(??=)确保不会返回null，避免外部调用时的null检查
        /// </summary>
        public List<ModbusCommunicationDioDto> ModbusCommunicationDios
        {
            get => _modbusCommunicationDios ??= new List<ModbusCommunicationDioDto>();
            private set
            {
                _modbusCommunicationDios = value;
                // 触发属性变更通知，UI层绑定该属性的控件会自动刷新
                RaisePropertyChanged();
            }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 私有构造函数，确保只能通过Instance属性创建实例（单例模式要求）
        /// 初始化服务实例并加载初始数据
        /// </summary>
        private ModbusSystem()
        {
            try
            {
                // 初始化设备通信服务并进行有效性检查
                // 若服务实例创建失败，抛出明确的异常信息
                _modbusService = new ModbusCommunicationService()
                    ?? throw new InvalidOperationException("Modbus通信服务初始化失败：服务实例为null");

                // 初始化DIO点位服务并进行有效性检查
                _modbusDioService = new ModbusCommunicationDIOService()
                    ?? throw new InvalidOperationException("Modbus DIO服务初始化失败：服务实例为null");

                // 异步加载初始数据，使用_ = 忽略未等待的任务，避免编译器警告
                // 不使用await，防止阻塞构造函数执行
                _ = RefreshDataAsync();
            }
            catch (Exception ex)
            {
                // 记录初始化过程中的异常到日志系统
                RLog.SaveError(ex);
                // 重新抛出异常，确保调用者能够感知到初始化失败
                throw;
            }
        }
        #endregion

        #region 核心数据操作方法
        /// <summary>
        /// 异步刷新所有Modbus数据，包括设备列表和DIO点位列表
        /// 采用并行加载策略，提高数据加载效率
        /// </summary>
        /// <returns>表示异步操作的Task对象，可用于等待操作完成</returns>
        public async Task RefreshDataAsync()
        {
            try
            {
                // 并行发起设备和DIO点位的数据加载请求
                // 同时执行两个异步操作，减少总体等待时间
                var deviceTask = _modbusService.GetAllAsync();
                var dioTask = _modbusDioService.GetAllAsync();

                // 等待两个异步操作都完成
                await Task.WhenAll(deviceTask, dioTask);

                // 处理设备数据结果
                var deviceResult = await deviceTask;
                ModbusCommunications = deviceResult?.Status == true
                    // 操作成功时使用返回结果，若结果为null则初始化空集合
                    ? deviceResult.Result ?? new List<ModbusCommunicationDto>()
                    // 操作失败时使用空集合
                    : new List<ModbusCommunicationDto>();

                // 处理DIO点位数据结果
                var dioResult = await dioTask;
                ModbusCommunicationDios = dioResult?.Status == true
                    // 操作成功时使用返回结果，若结果为null则初始化空集合
                    ? dioResult.Result ?? new List<ModbusCommunicationDioDto>()
                    // 操作失败时使用空集合
                    : new List<ModbusCommunicationDioDto>();
            }
            catch (Exception ex)
            {
                // 记录数据刷新过程中的异常
                RLog.SaveError(ex);
                // 重新抛出异常，允许调用者捕获并处理（如显示错误提示）
                throw;
            }
        }
        #endregion
    }

    /// <summary>
    /// Modbus DIO状态容器类
    /// 用于存储单个设备的所有数字量输入输出信号的实时状态和值
    /// 作为设备与UI界面之间的状态数据传输载体
    /// </summary>
    public class Status_Modbus_DIO
    {
        /// <summary>
        /// EquipmentName
        /// 用于关联对应的Modbus设备，确保状态数据与设备正确匹配
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 数字输入状态集合
        /// 存储每个输入点位的激活状态（true表示激活，false表示未激活）
        /// 与DigitalInputValues按索引位置一一对应
        /// </summary>
        public List<bool> DigitalInputStatus { get; } = new List<bool>();

        /// <summary>
        /// 数字输出状态集合
        /// 存储每个输出点位的激活状态（true表示激活，false表示未激活）
        /// 与DigitalOutputValues按索引位置一一对应
        /// </summary>
        public List<bool> DigitalOutputStatus { get; } = new List<bool>();

        /// <summary>
        /// 数字输入值集合
        /// 存储每个输入点位的原始值（可能是bool、int16等类型）
        /// 与DigitalInputStatus按索引位置一一对应
        /// </summary>
        public List<object> DigitalInputValues { get; } = new List<object>();

        /// <summary>
        /// 数字输出值集合
        /// 存储每个输出点位的原始值（可能是bool、int16等类型）
        /// 与DigitalOutputStatus按索引位置一一对应
        /// </summary>
        public List<object> DigitalOutputValues { get; } = new List<object>();
    }
}
