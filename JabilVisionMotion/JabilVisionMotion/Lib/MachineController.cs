using System;
using System.Linq;
using Jabil.Module.Modbus;
using Jabil.Module.Modbus.Lib;
using JabilVisionMotion.Edit;

public static class MachineController
{
    // PLC客户端实例
    private static PLC_Modbus _plcClient;

    // 状态绑定器实例
    private static MachineStatusPlcBinder _statusBinder;

    // CT值绑定器实例
    private static CtValuePlcBinder _ctValueBinder;

    // CT值变化阈值
    private const decimal CT_CHANGE_THRESHOLD = 0.1m;

    // 静态构造函数
    static MachineController()
    {
        try
        {
            // 初始化PLC客户端
            _plcClient = GetPlcDevice("H5u-2") ??
                throw new InvalidOperationException("无法获取PLC设备实例，请检查设备名称是否正确");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MachineController静态初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化控制器，需要在使用前调用
    /// </summary>
    public static void Initialize(decimal initialCtValue)
    {
        if (_plcClient == null)
        {
            _plcClient = GetPlcDevice("PLC_80_H5u") ??
                throw new InvalidOperationException("无法获取PLC设备实例，请检查设备名称是否正确");
        }

        // 创建状态绑定器实例
        _statusBinder = new MachineStatusPlcBinder(_plcClient ??
            throw new ArgumentNullException(nameof(_plcClient), "PLC客户端不能为null"));

        // 创建CT值绑定器实例
        _ctValueBinder = new CtValuePlcBinder(_plcClient, initialCtValue, CT_CHANGE_THRESHOLD);
    }

    /// <summary>
    /// 获取PLC设备实例
    /// </summary>
    private static PLC_Modbus GetPlcDevice(string DeviceName)
    {
        if (string.IsNullOrWhiteSpace(DeviceName))
            throw new ArgumentException("设备名称不能为空或空白", nameof(DeviceName));

        if (ModbusSystem.Instance == null)
            throw new InvalidOperationException("ModbusSystem实例未初始化");

        return ModbusSystem.Instance.PlcModbusList?.FirstOrDefault(p =>
            p != null && p.DeviceName == DeviceName);
    }

    /// <summary>
    /// 启动所有监控绑定过程
    /// </summary>
    public static void StartMonitoring()
    {
        try
        {
            // 初始化状态绑定器
            if (_statusBinder == null)
            {
                _statusBinder = new MachineStatusPlcBinder(_plcClient);
            }
            _statusBinder.Initialize();

            // 初始化CT值绑定器
            if (_ctValueBinder == null)
            {
                // 如果需要重新创建，这里需要获取当前CT值作为初始值
                decimal currentCtValue = GetCurrentCtValue();
                _ctValueBinder = new CtValuePlcBinder(_plcClient, currentCtValue, CT_CHANGE_THRESHOLD);
            }
            _ctValueBinder.Initialize();

            Console.WriteLine("机器状态与CT值监控已启动");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"启动监控失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新CT值（由外部数据源调用）
    /// </summary>
    public static void UpdateCurrentCtValue(decimal newValue)
    {
        if (_ctValueBinder == null)
        {
            throw new InvalidOperationException("CT值绑定器未初始化，请先调用StartMonitoring");
        }

        _ctValueBinder.UpdateCtValue(newValue);
    }

    private static decimal GetCurrentCtValue()
    {
        // 这里应该是从实际的CT值数据源获取当前值的逻辑
        // 例如从传感器、数据库或其他系统组件
        return (decimal)Controller.Instance.StationInfo.CtTime; // 使用显式转换
    }

    /// <summary>
    /// 停止所有监控并释放资源
    /// </summary>
    public static void StopMonitoring()
    {
        // 释放状态绑定器资源
        if (_statusBinder != null)
        {
            _statusBinder.Dispose();
            _statusBinder = null;
        }

        // 释放CT值绑定器资源
        if (_ctValueBinder != null)
        {
            _ctValueBinder.Dispose();
            _ctValueBinder = null;
        }

        // 关闭PLC客户端连接
        if (_plcClient != null)
        {
            try
            {
                _plcClient.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"断开PLC连接时发生错误: {ex.Message}");
            }
        }

        Console.WriteLine("机器状态与CT值监控已停止");
    }
}