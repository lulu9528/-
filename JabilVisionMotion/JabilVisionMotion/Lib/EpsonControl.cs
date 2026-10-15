using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus;
using Jabil.Module.Modbus.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Lib
{
    /// <summary>
    /// Epson机器人控制类
    /// 核心职责：通过Modbus协议与Epson机器人进行通信，封装启动、停止、暂停等控制命令的执行逻辑
    /// 设计特点：
    /// - 提供统一的命令发送入口，简化外部调用
    /// - 自动处理命令位重置，避免残留命令导致的异常行为
    /// - 包含参数校验和错误处理，提升系统稳定性
    /// </summary>
    public class EpsonControl
    {
        /// <summary>
        /// 命令执行延迟时间（毫秒）
        /// 作用：确保机器人有足够时间识别并响应命令（硬件响应需要一定时间）
        /// 统一配置便于后期根据机器人型号或工况调整
        /// </summary>
        private const int CommandDelayMs = 100;

        /// <summary>
        /// 自动控制Epson机器人执行指定命令
        /// 核心流程：验证参数→定位机器人→重置命令位→执行命令，确保操作的完整性和安全性
        /// </summary>
        /// <param name="output">要执行的输出命令（如启动/停止/暂停等，取值见<see cref="EpsonOutput"/>枚举）</param>
        /// <param name="epsonName">机器人设备名称（用于在Modbus设备列表中精准定位目标机器人，需与配置一致）</param>
        public void EpsonAutoControl(EpsonOutput output, string epsonName)
        {
            // 校验机器人名称有效性：为空时记录错误并返回（避免空引用异常）
            if (string.IsNullOrEmpty(epsonName))
            {
                RLog.SaveError(new ArgumentNullException(nameof(epsonName), "机器人名称不能为空"));
                return;
            }

            try
            {
                // 查找目标机器人设备：从Modbus连接的PLC列表中筛选符合条件的机器人
                // 条件1：PLC类型为H5u（与Epson机器人通信的PLC型号）
                // 条件2：设备名称与目标名称一致（确保操作正确的机器人）
                var targetRobot = ModbusSystem.Instance.PlcModbusList
                    .FirstOrDefault(robot =>
                        robot.PlcType == PLC_Modbus.PLCType.H5u &&  // 匹配PLC类型
                        robot.DeviceName == epsonName);         // 匹配设备名称

                // 未找到目标机器人时的容错处理
                if (targetRobot == null)
                {
                    // 原代码注释的日志逻辑：记录未找到设备的错误（实际场景中建议启用）
                    // RLog.SaveError(new InvalidOperationException($"未找到名称为 {epsonName} 的Epson机器人"));
                    return; // 终止执行，避免后续空引用
                }

                // 重置所有命令位：清除之前可能残留的命令（如上次操作未正常复位的信号）
                ResetAllCommands(targetRobot);

                // 执行目标命令：向机器人发送指定控制信号
                ExecuteCommand(targetRobot, output);
            }
            catch (Exception ex)
            {
                // 记录命令执行过程中的所有异常（如Modbus通信失败、机器人无响应等）
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 重置机器人所有命令位
        /// 作用：清除所有可能的残留命令，避免多个命令叠加导致机器人行为异常
        /// 实现方式：遍历所有命令枚举值，将对应的Modbus地址设置为false（关闭状态）
        /// </summary>
        /// <param name="robot">目标机器人的Modbus连接对象（通过该对象发送重置命令）</param>
        private void ResetAllCommands(PLC_Modbus robot)
        {
            // 定义所有命令的数组：与EpsonOutput枚举的所有值对应，确保无遗漏
            var allCommands = new[]
            {
            EpsonOutput.Pause,    // 暂停命令位
            EpsonOutput.Stop,     // 停止命令位
            EpsonOutput.Continue, // 继续命令位
            EpsonOutput.Reset,    // 重置命令位
            EpsonOutput.Start     // 启动命令位
        };

            // 循环重置每个命令位：将所有命令对应的Modbus地址设置为false
            foreach (var command in allCommands)
            {
                robot.WriteBool(false, command.ToString()); // (int)command对应Modbus地址
            }
        }

        /// <summary>
        /// 执行指定的机器人命令
        /// 标准流程：触发命令（置为true）→等待响应（延迟）→复位命令（置为false）
        /// 确保机器人仅在指定时间内接收命令，避免持续触发
        /// </summary>
        /// <param name="robot">目标机器人的Modbus连接对象</param>
        /// <param name="command">要执行的命令（来自<see cref="EpsonOutput"/>枚举）</param>
        private void ExecuteCommand(PLC_Modbus robot, EpsonOutput command)
        {
            // 步骤1：发送命令→将对应Modbus地址置为true（触发机器人执行命令）
            robot.WriteBool(true, command.ToString());

            // 步骤2：等待延迟→确保机器人有足够时间识别命令（时长由CommandDelayMs定义）
            System.Threading.Thread.Sleep(CommandDelayMs);

            // 步骤3：复位命令→将对应Modbus地址置为false（避免命令持续生效）
            robot.WriteBool(false, command.ToString());
        }
    }

    /// <summary>
    /// Epson机器人输入信号常量
    /// 定义机器人向外部系统反馈的状态信号，每个常量对应一个Modbus输入地址
    /// 外部系统可通过读取这些地址获取机器人当前状态
    /// </summary>
    public class EpsonInput
    {
        /// <summary>
        /// 机器人就绪状态（可接收命令）
        /// 说明：当机器人初始化完成、无错误且处于待命状态时为true
        /// 对应Modbus地址：512.1（输入寄存器地址.位索引）
        /// </summary>
        public const string Ready = "512.1";

        /// <summary>
        /// 机器人运行中状态
        /// 说明：机器人正在执行任务时为true，与"就绪"状态互斥
        /// 对应Modbus地址：513（输入寄存器地址）
        /// </summary>
        public const string Running = "513";

        /// <summary>
        /// 机器人暂停状态
        /// 说明：机器人因外部命令或异常暂停时为true，可通过"继续"命令恢复
        /// 对应Modbus地址：514（输入寄存器地址）
        /// </summary>
        public const string Pause = "514";

        /// <summary>
        /// 机器人急停状态
        /// 说明：急停按钮被按下或触发安全联锁时为true，需手动复位
        /// 对应Modbus地址：515（输入寄存器地址）
        /// </summary>
        public const string EStopOn = "515";

        /// <summary>
        /// 机器人电机开启状态
        /// 说明：机器人伺服电机上电并使能时为true，是执行运动的前提
        /// 对应Modbus地址：516（输入寄存器地址）
        /// </summary>
        public const string MotorsOn = "516";
    }

    /// <summary>
    /// Epson机器人输出命令常量
    /// 定义外部系统向机器人发送的控制命令，每个常量对应一个Modbus输出地址
    /// 外部系统通过写入这些地址控制机器人行为
    /// </summary>
    public class EpsonOutput
    {
        /// <summary>
        /// 启动机器人命令
        /// 说明：触发机器人开始执行预设程序，仅在"就绪"状态下有效
        /// 对应Modbus地址：512.1（输出寄存器地址.位索引）
        /// </summary>
        public const string Start = "512.1";

        /// <summary>
        /// 停止机器人命令
        /// 说明：立即终止机器人当前任务，需要重新启动才能恢复
        /// 对应Modbus地址：513（输出寄存器地址）
        /// </summary>
        public const string Stop = "513";

        /// <summary>
        /// 暂停机器人命令
        /// 说明：暂停当前任务，可通过"继续"命令恢复执行
        /// 对应Modbus地址：514（输出寄存器地址）
        /// </summary>
        public const string Pause = "514";

        /// <summary>
        /// 继续运行命令（从暂停状态恢复）
        /// 说明：仅在"暂停"状态下有效，恢复机器人被中断的任务
        /// 对应Modbus地址：515（输出寄存器地址）
        /// </summary>
        public const string Continue = "515";

        /// <summary>
        /// 重置机器人命令（清除报警/错误状态）
        /// 说明：用于清除机器人的错误或报警状态，使其恢复到就绪状态
        /// 对应Modbus地址：516（输出寄存器地址）
        /// </summary>
        public const string Reset = "516";
    }

}
