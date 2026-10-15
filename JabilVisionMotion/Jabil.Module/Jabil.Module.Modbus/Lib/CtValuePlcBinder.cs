using System;
using Jabil.Lib;
using Jabil.Module.Log.Control;
using System.Drawing;
using Jabil.Module.Permission;
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Log.Lib;

namespace Jabil.Module.Modbus.Lib
{
    /// <summary>
    /// CT值与PLC寄存器绑定器
    /// 负责监听外部CT值变化并写入对应PLC寄存器
    /// </summary>
    public class CtValuePlcBinder : IDisposable
    {
        // PLC寄存器地址常量
        private const string CT_REGISTER_ADDRESS = "1627";

        // 上一次的CT值，用于检测变化
        private decimal _lastCtValue;

        // 变化阈值
        private readonly decimal _changeThreshold;

        // PLC通信客户端
        private readonly PLC_Modbus _plcClient;

        // 是否已经初始化
        private bool _isInitialized;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="plcClient">PLC Modbus客户端实例</param>
        /// <param name="initialCtValue">初始CT值</param>
        /// <param name="changeThreshold">变化阈值，默认0.1</param>
        public CtValuePlcBinder(PLC_Modbus plcClient, decimal initialCtValue, decimal changeThreshold = 0.1m)
        {
            _plcClient = plcClient ?? throw new ArgumentNullException(nameof(plcClient));
            _changeThreshold = changeThreshold;
            _lastCtValue = initialCtValue;
        }

        /// <summary>
        /// 初始化绑定
        /// </summary>
        public void Initialize()
        {
            if (!_isInitialized)
            {
                // 初始值写入一次
                WriteCtValueToPlc(_lastCtValue);
                _isInitialized = true;
            }
        }

        /// <summary>
        /// 外部调用此方法更新CT值
        /// </summary>
        /// <param name="newValue">新的CT值</param>
        public void UpdateCtValue(decimal newValue)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("请先调用Initialize方法初始化绑定器");

            decimal changeAmount = Math.Abs(newValue - _lastCtValue);

            if (changeAmount > _changeThreshold)
            {
                _lastCtValue = newValue;
                WriteCtValueToPlc(newValue);
            }
        }

        /// <summary>
        /// 将CT值写入PLC寄存器
        /// </summary>
        private void WriteCtValueToPlc(decimal ctValue)
        {
            try
            {
                // 数值转换（根据实际需求调整）
                short valueToWrite = (short)Math.Round(ctValue * 10);
                _plcClient.WriteInt16(valueToWrite, CT_REGISTER_ADDRESS);

                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                    $"CT值已同步到PLC: {ctValue} -> 寄存器值: {valueToWrite}");
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Error, Color.Red,
                    $"处理CT值写入PLC时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _isInitialized = false;
        }
    }
}
