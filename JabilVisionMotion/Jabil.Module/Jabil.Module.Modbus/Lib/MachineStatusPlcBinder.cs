using System;
using Jabil.Lib; // 包含MachineRunStatus枚举定义
using Jabil.Module.Log.Control; // 包含LogView
using System.Drawing;
using Jabil.Module.Permission; // 包含MenuProxy类
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Log.Lib;

namespace Jabil.Module.Modbus.Lib
{
    /// <summary>
    /// 机器运行状态与PLC寄存器绑定器
    /// 负责监听状态变化并写入对应PLC寄存器值
    /// 支持带Flags特性的MachineRunStatus枚举（可组合多种状态）
    /// </summary>
    public class MachineStatusPlcBinder : IDisposable
    {
        // PLC寄存器地址常量 - 根据实际需求修改
        private const string STATUS_REGISTER_ADDRESS = "1607";

        // 上一次的状态，用于检测变化
        private MachineRunStatus _lastStatus;

        // PLC通信客户端
        private readonly PLC_Modbus _plcClient;

        // 是否已经初始化
        private bool _isInitialized;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="plcClient">PLC Modbus客户端实例</param>
        public MachineStatusPlcBinder(PLC_Modbus plcClient)
        {
            _plcClient = plcClient ?? throw new ArgumentNullException(nameof(plcClient));
            _lastStatus = MenuProxy.Instance.MachineRunStatus;
        }

        /// <summary>
        /// 初始化绑定
        /// </summary>
        public void Initialize()
        {
            if (!_isInitialized)
            {
                // 注册状态变化事件监听
                MenuProxy.Instance.MachineRunStatusChanged += OnMachineRunStatusChanged;
                _isInitialized = true;

                // 初始状态写入一次
                WriteStatusToPlc(_lastStatus);
            }
        }

        /// <summary>
        /// 机器运行状态变化事件处理
        /// </summary>
        private void OnMachineRunStatusChanged(object sender, MachineRunStatus e)
        {
            // 状态实际发生变化才处理（对于Flags枚举，任何位的变化都应被检测到）
            if (e != _lastStatus)
            {
                _lastStatus = e;
                WriteStatusToPlc(e);
            }
        }

        /// <summary>
        /// 将状态值写入PLC寄存器
        /// 处理带Flags特性的枚举，支持多种状态组合
        /// </summary>
        /// <param name="status">当前机器运行状态（可能是多种状态的组合）</param>
        private void WriteStatusToPlc(MachineRunStatus status)
        {
            try
            {
                // 对于带Flags的枚举，直接使用其 underlying value 作为寄存器值
                // 由于枚举值定义为2的幂次，组合状态的值将是各状态值的总和
                short valueToWrite = (short)status;

                // 写入PLC寄存器，使用异步方法避免阻塞UI
                _plcClient.WriteInt16(valueToWrite, STATUS_REGISTER_ADDRESS);
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, $"处理PLC状态写入时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源，取消事件订阅
        /// </summary>
        public void Dispose()
        {
            if (_isInitialized)
            {
                MenuProxy.Instance.MachineRunStatusChanged -= OnMachineRunStatusChanged;
                _isInitialized = false;
            }
        }
    }
}
