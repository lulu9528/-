using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using APS168_W32;
using APS_Define_W32;
using BasicClassLibrary.RexHelps;
using csLTDMC;
using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Frm;
using lctdevice;
using Jabil.Module.Permission;

namespace Jabil.Module.Motion.Lib
{
    /// <summary>
    /// 凌臣运控控制器实现类
    /// 实现了IMotionController接口，封装了凌臣运动控制卡的所有操作
    /// </summary>
    public class EcatMotion : IMotionController
    {
        #region 常量定义
        // 状态位掩码常量 - 用于解析伺服轴的状态信息
        private const int ServoAlarmMask = 0x02;         // 伺服报警状态掩码 (bit 1)
        private const int ServoEnabledMask = 0x200;      // 伺服使能状态掩码 (bit 9)
        private const int PositiveLimitMask = 0x20;      // 正极限状态掩码 (bit 5)
        private const int OriginSignalMask = 0x100000;   // 原点信号状态掩码 (bit 20)
        private const int NegativeLimitMask = 0x40;      // 负极限状态掩码 (bit 6)
        private const int MotionMask = 0x400;            // 运动中状态掩码 (bit 10)
        private const int InPositionMask = 0x800;        // 到位信号状态掩码 (bit 11)
        private const int OfflineMask = 0x1000000;       // 掉线状态掩码 (bit 24)
        private const int SoftStopMask = 0x80;           // 缓停触发状态掩码 (bit 7)

        // 错误信息常量 - 统一的错误提示信息
        private const string ErrorNotBound = "Error--->未绑定轴！";  // 轴未正确绑定的错误提示
        private const string ErrorOccurred = "Error--->报错！";      // 通用错误提示
        private const string NoError = "NoError";                    // 无错误状态提示

        // 运动参数常量 - 运动控制相关的固定参数
        private const short HomingMode = 6;              // 回零模式：6表示使用特定的回零方式（具体参考凌臣手册）
        private const ushort ProbeFunctionReserved = 0;  // 预留探测功能：0表示未使用该功能
        #endregion

        #region 私有字段
        private bool _useFastConnectMode = true;        // 快速连接模式：是否启用EtherCAT快速连接
        private short _option = 0;                       // 断线输出保持参数：0-不保持输出；1-断线时保持输出状态
        private short _axisCount = 0;                    // 伺服数量：系统中配置的伺服轴总数
        private int _servoStatus = 0;                    // 伺服轴运行状态：存储最近读取的伺服状态字
        private int targetPulses;
        private int speedPulse;

        #endregion

        /// <summary>
        /// 初始化轴卡
        /// 执行轴卡的打开、配置和连接等初始化操作
        /// </summary>
        /// <param name="motionCard">轴卡参数对象，包含轴卡的配置信息</param>
        /// <returns>是否初始化成功：true-成功，false-失败</returns>
        public bool Open(MotionCardDto motionCard)
        {
            if (motionCard == null)
                throw new ArgumentNullException(nameof(motionCard));  // 验证参数不为空

            try
            {
                // 打开轴卡设备
                short returnCode = ecat_motion.M_Open(motionCard.轴卡卡号, 0);
                if (returnCode != 0)
                    return false;  // 打开失败

                // 配置急停参数（极性、动作模式等）
                if (!ConfigureEmergencyStop(motionCard))
                    return false;  // 急停配置失败

                // 加载ENI文件（EtherCAT网络信息文件）
                returnCode = ecat_motion.M_LoadEni(
                    "C:\\Program Files (x86)\\LCT\\PCIe-M60\\Eni\\eni.xml",
                    motionCard.轴卡卡号);

                // 处理连接模式（快速连接或常规连接）
                if (!HandleConnectionMode(motionCard))
                    return false;  // 连接模式处理失败

                // 连接EtherCAT总线
                returnCode = ecat_motion.M_ConnectECAT(_option, motionCard.轴卡卡号);
                if (returnCode != 0)
                {
                    MessageBox.Show($"ecat_motion.M_ConnectECAT 执行失败，返回值为：{returnCode}");
                    return false;  // 总线连接失败
                }

                Thread.Sleep(500); // 必要延时，等待总线稳定

                // 加载参数文件（轴卡配置参数）
                returnCode = ecat_motion.M_LoadParamFromFile(
                    "C:\\Program Files (x86)\\LCT\\PCIe-M60\\Motion_Assistant\\AxisParam\\ParamCard0.ini",
                    motionCard.轴卡卡号);

                //if (returnCode == 0)
                //    MessageBox.Show("初始化轴卡成功！");  // 初始化成功提示

                // 验证从站资源（检查EtherCAT从站是否正常）
                return VerifySlaveResources(motionCard.轴卡卡号);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);  // 记录异常日志
                return false;
            }
        }

        /// <summary>
        /// 释放所有轴卡资源
        /// 执行轴卡的关闭和资源释放操作
        /// </summary>
        /// <param name="motionCard">轴卡参数对象</param>
        /// <returns>是否释放成功：true-成功，false-失败</returns>
        public bool Close(MotionCardDto motionCard)
        {
            if (motionCard == null)
                throw new ArgumentNullException(nameof(motionCard));  // 验证参数不为空

            try
            {
                // 尝试使所有轴去使能
                DisableAllAxes();

                // 尝试断开EtherCAT总线连接
                try
                {
                    //ecat_motion.M_DisconnectECAT(motionCard.轴卡卡号);
                }
                catch { /* 忽略断开总线时的错误 */ }

                // 关闭轴卡设备
                short returnCode = ecat_motion.M_Close(motionCard.轴卡卡号);
                return returnCode == 0;  // 返回关闭结果
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);  // 记录异常日志
                return false;
            }
        }

        /// <summary>
        /// 伺服使能控制
        /// 控制指定轴的伺服使能状态（使能或禁用）
        /// </summary>
        /// <param name="axisParam">轴参数对象，包含轴的配置信息</param>
        /// <param name="enable">是否使能：1-使能，0-禁用</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        public string ServoSet(MotionAxisDto axisParam, ushort enable)
        {
            return ExecuteAxisOperation(axisParam, () =>
            {
                // 检查伺服报警状态，有报警则不执行操作
                if (CheckServoAlarm(axisParam))
                    return GetErrorCodeMessage(ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _, 1, axisParam.轴卡卡号索引));

                //设置到位范围，稳定时间
                if (ecat_motion.M_SetAxisBand(axisParam.轴卡轴号索引, axisParam.到位信号范围, axisParam.到位信号稳定时间, axisParam.轴卡卡号索引) != 0)
                { return "设置轴到位范围失败!"; }

                // 执行使能/禁用操作
                short returnCode = enable == 1
                    ? ecat_motion.M_Servo_On(axisParam.轴卡轴号索引, axisParam.轴卡卡号索引)  // 伺服使能
                    : ecat_motion.M_Servo_Off(axisParam.轴卡轴号索引, axisParam.轴卡卡号索引); // 伺服禁用

                return returnCode == 0 ? NoError : GetErrorCodeMessage(returnCode);
            });
        }

        /// <summary>
        /// 报警复位
        /// 清除指定轴的伺服报警状态
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <param name="reset">是否复位：1-复位</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        public string ERCSet(MotionAxisDto axisParam, ushort reset)
        {
            return ExecuteAxisOperation(axisParam, () =>
            {
                // 执行报警复位命令
                short returnCode = ecat_motion.M_ClrSts(axisParam.轴卡轴号索引, 1, axisParam.轴卡卡号索引);
                return returnCode == 0 ? NoError : GetErrorCodeMessage(returnCode);
            });
        }

        /// <summary>
        /// 读取伺服使能状态
        /// 获取指定轴当前的伺服使能状态
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>状态值：1-已使能，0-未使能，-1-错误</returns>
        public short ServoRead(MotionAxisDto axisParam)
        {
            if (!IsAxisValid(axisParam))
                return -1;  // 轴参数无效

            try
            {
                // 读取伺服状态
                ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _servoStatus, 1, axisParam.轴卡卡号索引);
                // 解析使能状态位
                return ((_servoStatus & ServoEnabledMask) == ServoEnabledMask) ? (short)1 : (short)0;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);  // 记录异常日志
                return -1;  // 操作错误
            }
        }

        /// <summary>
        /// 点动操作
        /// 控制指定轴进行点动（连续运动）
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <param name="isNegative">是否负方向：0-负方向，1-正方向</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        public string JogMove(MotionAxisDto axisParam, ushort isNegative)
        {
            return ExecuteAxisOperation(axisParam, () =>
            {
                // 停止当前运动
                short returnCode = ecat_motion.M_StopSingleAxis(axisParam.轴卡轴号索引, 0, axisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 停止失败

                // 检查伺服报警状态
                if (CheckServoAlarm(axisParam))
                    return GetErrorCodeMessage(ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _, 1, axisParam.轴卡卡号索引));

                // 检查限位状态，在限位状态下不执行点动
                if (IsAxisAtLimit(axisParam))
                    return NoError;  // 已在限位，不执行操作

                // 计算点动速度（方向由isNegative决定）
                double speed = isNegative == 1
                    ? axisParam.单轴运行速度
                    : -axisParam.单轴运行速度;

                // 执行点动命令
                returnCode = ecat_motion.M_Jog(axisParam.轴卡轴号索引, speed, axisParam.轴卡卡号索引);
                return returnCode == 0 ? NoError : GetErrorCodeMessage(returnCode);
            });
        }

        /// <summary>
        /// 相对运动操作
        /// 控制指定轴进行相对距离运动
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <param name="distance">相对距离（单位：mm）</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        public string RelMove(MotionAxisDto axisParam, double distance)
        {
            return ExecuteAxisOperation(axisParam, () =>
            {
                // 停止当前运动
                short returnCode = ecat_motion.M_StopSingleAxis(axisParam.轴卡轴号索引, 0, axisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 停止失败

                // 检查报警状态
                if (CheckServoAlarm(axisParam))
                    return GetErrorCodeMessage(ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _, 1, axisParam.轴卡卡号索引));

                // 计算脉冲数（距离 * 脉冲比 = 脉冲数）
                int pulses = Convert.ToInt32(distance * axisParam.脉冲比);
                // 执行相对运动命令
                returnCode = ecat_motion.M_RelMove(axisParam.轴卡轴号索引, pulses, axisParam.单轴运行速度, axisParam.轴卡卡号索引);

                if (returnCode == 0)
                {
                    // 启动运动完成检测任务
                    StartMotionCompletionCheck(axisParam);
                }

                return returnCode == 0 ? NoError : GetErrorCodeMessage(returnCode);
            });
        }

        /// <summary>
        /// 绝对运动操作
        /// 控制指定轴运动到绝对位置，首次运动使用低速，后续运动自动切换为高速
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，包含轴卡信息、运动参数等配置</param>
        /// <param name="pose">目标位置（单位：mm）</param>
        /// <param name="speed">基准运动速度（高速模式下使用）</param>
        /// <returns>操作结果："NoError"表示成功，其他字符串表示具体错误信息</returns>
        public string AbsMove(MotionAxisDto currentAxisParam, double pose, double speed)
        {
            // 为每个轴创建唯一标识键，使用"轴卡卡号索引_轴卡轴号索引"组合
            // 用于跟踪不同轴的运动次数，实现各自独立的速度切换逻辑
            string axisKey = $"{currentAxisParam.轴卡卡号索引}_{currentAxisParam.轴卡轴号索引}";

            // 调用轴操作执行器，封装轴操作的通用逻辑（如异常处理等）
            return ExecuteAxisOperation(currentAxisParam, () =>
            {
                // 1. 停止当前轴的所有运动，确保运动前处于静止状态
                // 参数说明：轴号索引、停止方式（0=立即停止）、轴卡卡号索引
                short returnCode = ecat_motion.M_StopSingleAxis(
                    currentAxisParam.轴卡轴号索引,
                    0,
                    currentAxisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 停止失败时返回错误信息

                // 2. 检查伺服是否处于报警状态
                if (CheckServoAlarm(currentAxisParam))
                {
                    // 获取具体报警状态码并返回错误信息
                    // 参数说明：轴号索引、输出状态（占位）、状态类型（1=报警状态）、轴卡卡号索引
                    return GetErrorCodeMessage(ecat_motion.M_GetSts(
                        currentAxisParam.轴卡轴号索引,
                        out _,
                        1,
                        currentAxisParam.轴卡卡号索引));
                }

                // 3. 使能伺服电机（上电准备）
                returnCode = ecat_motion.M_Servo_On(
                    currentAxisParam.轴卡轴号索引,
                    currentAxisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 伺服使能失败时返回错误信息

                // 4. 读取当前位置并校准（回零后确保位置准确性）
                // 4.1 读取编码器当前位置
                ecat_motion.M_GetEncPos(
                    currentAxisParam.轴卡轴号索引,
                    out double currentPos,
                    1,  // 读取模式（1=实时位置）
                    currentAxisParam.轴卡卡号索引);

                // 4.2 设置当前位置为基准位置（消除累计误差）
                returnCode = ecat_motion.M_SetCurrentPos(
                    currentAxisParam.轴卡轴号索引,
                    Convert.ToInt32(currentPos),  // 转换为整数脉冲数
                    currentAxisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 位置校准失败时返回错误信息

                // 5. 配置运动参数（加减速等）
                var cmdParams = new ecat_motion.CmdPrm
                {
                    acc = currentAxisParam.单轴加速度,    // 加速度参数
                    dec = currentAxisParam.单轴减速度,    // 减速度参数
                    sTime = currentAxisParam.加加速时间   // 加加速时间（柔性启动参数）
                };

                // 6. 将运动参数设置到硬件
                returnCode = ecat_motion.M_SetMove(
                    currentAxisParam.轴卡轴号索引,
                    ref cmdParams,  // 运动参数引用
                    currentAxisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 参数设置失败时返回错误信息

                // 7. 计算目标位置对应的脉冲数
                // 公式：目标位置(mm) * 脉冲比(脉冲/mm) = 目标脉冲数
                int targetPulses = Convert.ToInt32(pose * currentAxisParam.脉冲比);

                // 8. 确定当前应使用的速度（核心逻辑）
                // 首次运动使用低速，后续运动自动切换为高速
                double currentSpeed = GetCurrentSpeed(currentAxisParam, speed, axisKey);

                // 9. 根据运行模式计算最终速度脉冲数
                int speedPulse;
                switch (MenuProxy.Instance.MachineRunModel)
                {
                    case MachineMode.Manual:
                        // 手动模式：速度 = 当前速度 * 脉冲比 * 速度倍率(%)
                        speedPulse = Convert.ToInt32(
                            currentSpeed * currentAxisParam.脉冲比 *
                            Project.Instance.StationInfo.RunningSpeedRatio / 100);
                        break;
                    case MachineMode.Auto:
                        // 自动模式：速度 = 当前速度 * 脉冲比（无倍率）
                        speedPulse = Convert.ToInt32(currentSpeed * currentAxisParam.脉冲比);
                        break;
                    default:
                        // 其他模式默认使用自动模式的速度计算方式
                        speedPulse = Convert.ToInt32(currentSpeed * currentAxisParam.脉冲比);
                        break;
                }

                // 10. 执行绝对运动命令
                returnCode = ecat_motion.M_AbsMove(
                    currentAxisParam.轴卡轴号索引,    // 轴号索引
                    targetPulses,                     // 目标位置脉冲数
                    speedPulse,                       // 速度脉冲数
                    currentAxisParam.轴卡卡号索引);   // 轴卡卡号索引

                // 11. 如果运动成功，更新该轴的运动计数器
                if (returnCode == 0)
                {
                    UpdateMotionCount(axisKey);
                }

                // 12. 返回运动结果（成功返回"NoError"，失败返回错误信息）
                return returnCode == 0 ? NoError : GetErrorCodeMessage(returnCode);
            });
        }

        /// <summary>
        /// 轴回原动作
        /// 控制指定轴执行回零（回原点）操作
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        public string GoHome(MotionAxisDto axisParam)
        {
            return ExecuteAxisOperation(axisParam, () =>
            {
                short returnCode = 0;
                // 停止当前运动
                //short returnCode = ecat_motion.M_StopSingleAxis(axisParam.轴卡轴号索引, 0, axisParam.轴卡卡号索引);
                //if (returnCode != 0)
                //    return GetErrorCodeMessage(returnCode);  // 停止失败

                // 检查轴状态
                returnCode = ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _servoStatus, 1, axisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 获取状态失败

                // 检查报警状态
                if (CheckServoAlarm(axisParam))
                    return GetErrorCodeMessage(returnCode);  // 存在报警，不执行回零

                // 配置回零模式
                returnCode = ecat_motion.M_SetHomingMode(axisParam.轴卡轴号索引, HomingMode, axisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 设置回零模式失败

                // 配置回零参数
                returnCode = ecat_motion.M_SetHomingPrm(
                    axisParam.轴卡轴号索引,
                    axisParam.回零方法,
                    axisParam.回零偏移,
                    (uint)axisParam.回零速度1,
                    (uint)axisParam.回零速度2,
                    (uint)axisParam.回零加速度,
                    ProbeFunctionReserved,
                    axisParam.轴卡卡号索引);

                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 设置回零参数失败

                Thread.Sleep(50); // 必要延时，等待参数生效

                // 启动回零操作
                returnCode = ecat_motion.M_HomingStart(axisParam.轴卡轴号索引, axisParam.轴卡卡号索引);
                return returnCode == 0 ? NoError : GetErrorCodeMessage(returnCode);
            });
        }

        /// <summary>
        /// 轴减速停止
        /// 控制指定轴以减速方式停止运动
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <param name="stopMode">停止模式：0-平滑减速，1-急停</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        public string SmtStop(MotionAxisDto axisParam, ushort stopMode)
        {
            return ExecuteAxisOperation(axisParam, () =>
            {
                // 检查报警状态
                if (CheckServoAlarm(axisParam))
                    return GetErrorCodeMessage(ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _, 1, axisParam.轴卡卡号索引));
                ResetMotionCount(axisParam);
                // 执行停止命令
                short returnCode = ecat_motion.M_StopSingleAxis(axisParam.轴卡轴号索引, stopMode, axisParam.轴卡卡号索引);
                return returnCode == 0 ? NoError : GetErrorCodeMessage(returnCode);
            });
        }

        /// <summary>
        /// 轴紧急停止
        /// 控制指定轴执行紧急停止操作（调用SmtStop并指定模式1）
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        public string EmgStop(MotionAxisDto axisParam)
        {
            ResetMotionCount(axisParam);
            // 紧急停止使用模式1（急停模式）
            return SmtStop(axisParam, 1);
        }

        /// <summary>
        /// 获取轴实时状态
        /// 读取指定轴的各种状态信息（报警、使能、限位等）
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>状态数组(32位)：每个元素代表一种状态的布尔值</returns>
        public bool[] GetMotionSts(MotionAxisDto axisParam)
        {
            try
            {
                if (!IsAxisValid(axisParam))
                    return CreateDefaultStatusArray();  // 轴参数无效，返回默认状态

                // 同步轴状态（锁定当前状态值）
                ecat_motion.M_SetAxisStsSync(axisParam.轴卡轴号索引, axisParam.限位信号锁定, axisParam.轴卡卡号索引);
                // 读取伺服状态字
                ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _servoStatus, 1, axisParam.轴卡卡号索引);

                // 解析状态位并返回状态数组
                return new[]
                {
                    (_servoStatus & ServoAlarmMask) == ServoAlarmMask,         // 0: 伺服报警状态
                    (_servoStatus & ServoEnabledMask) == ServoEnabledMask,     // 1: 伺服使能状态
                    (_servoStatus & PositiveLimitMask) == PositiveLimitMask,   // 2: 正极限状态
                    (_servoStatus & OriginSignalMask) == OriginSignalMask,     // 3: 原点信号状态
                    (_servoStatus & NegativeLimitMask) == NegativeLimitMask,   // 4: 负极限状态
                    (_servoStatus & MotionMask) == MotionMask,                 // 5: 运动中状态
                    (_servoStatus & InPositionMask) == InPositionMask,         // 6: 到位状态
                    (_servoStatus & OfflineMask) == OfflineMask,               // 7: 掉线状态
                    (_servoStatus & SoftStopMask) == SoftStopMask              // 8: 缓停触发状态
                    // 后续可根据需要添加更多状态位解析
                };
            }
            catch (Exception)
            {
                return CreateDefaultStatusArray();  // 发生异常，返回默认状态
            }
        }

        /// <summary>
        /// 设置指令脉冲值
        /// 手动设置指定轴的当前位置（用于校准等场景）
        /// </summary>
        /// <param name="position">位置值（单位：mm）</param>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        public string SetPos(double position, MotionAxisDto axisParam)
        {
            return ExecuteAxisOperation(axisParam, () =>
            {
                // 读取当前伺服状态
                short returnCode = ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _servoStatus, 1, axisParam.轴卡卡号索引);
                if (returnCode != 0)
                    return GetErrorCodeMessage(returnCode);  // 获取状态失败

                // 检查报警状态
                if (CheckServoAlarm(axisParam))
                    return GetErrorCodeMessage(returnCode);  // 存在报警，不执行操作

                // 计算脉冲数并设置当前位置
                int pulses = Convert.ToInt32(position * axisParam.脉冲比);
                returnCode = ecat_motion.M_SetCurrentPos(axisParam.轴卡轴号索引, pulses, axisParam.轴卡卡号索引);
                return returnCode == 0 ? NoError : GetErrorCodeMessage(returnCode);
            });
        }

        /// <summary>
        /// 判定轴停止状态
        /// 检查指定轴是否已经停止运动
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>true-已停止，false-运行中</returns>
        public bool CheckMoveDone(MotionAxisDto axisParam)
        {
            if (!IsAxisValid(axisParam))
                return false;  // 轴参数无效

            try
            {
                // 读取当前状态
                ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out int currentStatus, 1, axisParam.轴卡卡号索引);
                // 同时判断motion和inp信号：运动中信号未置位且到位信号置位表示已停止
                return !((currentStatus & MotionMask) == MotionMask) && (currentStatus & InPositionMask) == InPositionMask;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);  // 记录异常日志
                return false;  // 发生异常，返回false
            }
        }

        /// <summary>
        /// 检查伺服报警状态
        /// 检查指定轴是否处于报警状态
        /// </summary>
        public bool CheckServoAlarm(MotionAxisDto axisParam)
        {
            // 读取伺服状态
            ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _servoStatus, 1, axisParam.轴卡卡号索引);
            // 检查报警位
            return (_servoStatus & ServoAlarmMask) == ServoAlarmMask;
        }
        /// <summary>
        /// 检查回零动作是否完成，并进行后续的回零模式配置
        /// </summary>
        /// <param name="axisParam">运动轴参数对象，包含轴卡卡号索引和轴卡轴号索引等信息</param>
        /// <returns>操作是否成功：true表示成功，false表示失败（包括参数无效、操作失败或发生异常）</returns>
        public bool CheckMoveHomeDone(MotionAxisDto axisParam)
        {
            // 首先检查轴参数是否有效，如果无效直接返回false
            if (!IsAxisValid(axisParam))
                return false;  // 轴参数无效

            try
            {
                // 等待回零完成，超时时间为100000毫秒（100秒）
                // 调用底层函数M_WaitHomingFinished，传入轴号索引、超时时间和卡号索引
                short returnCode = ecat_motion.M_WaitHomingFinished(axisParam.轴卡轴号索引, 100000, axisParam.轴卡卡号索引);

                // 如果返回码不为0，表示等待回零完成失败
                if (returnCode != 0)
                    return false;  // 获取状态失败

                // 如果返回码为0，表示回零已完成，执行后续操作
                if (returnCode == 0)
                {
                    // 取消单个轴的回零动作
                    returnCode = ecat_motion.M_HomeCancelSingleAxis(axisParam.轴卡轴号索引, axisParam.轴卡卡号索引);

                    // 如果取消回零失败，返回false
                    if (returnCode != 0)
                        return false;  // 获取状态失败

                    // 配置回零模式为8（具体模式含义取决于底层驱动定义）
                    int count = 0;
                    // 最多尝试10次配置回零模式，每次间隔500毫秒
                    do
                    {
                        // 等待500毫秒后重试
                        Thread.Sleep(500);
                        // 调用底层函数设置回零模式
                        returnCode = ecat_motion.M_SetHomingMode(axisParam.轴卡轴号索引, 8, axisParam.轴卡卡号索引);
                        count++;  // 累计尝试次数
                    } while (returnCode != 0 && count < 10);  // 当配置失败且未达到最大尝试次数时继续循环

                    // 如果最终配置失败（返回码不为0），返回false
                    if (returnCode != 0)
                        return false;  // 获取状态失败
                }

                // 所有操作都成功完成，返回true
                return true;
            }
            catch (Exception ex)
            {
                // 捕获所有异常并记录到日志
                RLog.SaveError(ex);  // 记录异常日志
                return false;  // 发生异常，返回false
            }
        }
        /// <summary>
        /// 获取当前轴的指令位置
        /// 读取指定轴的目标位置（指令位置）
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>指令位置(mm)：返回-999表示操作失败</returns>
        public double GetCmd(MotionAxisDto axisParam)
        {
            if (!IsAxisValid(axisParam))
                return -999;  // 轴参数无效

            // 读取指令位置（脉冲数）
            ecat_motion.M_GetCmd(axisParam.轴卡轴号索引, out double actualPos, 1, axisParam.轴卡卡号索引);
            // 转换为毫米并四舍五入保留3位小数
            return Math.Round(actualPos / axisParam.脉冲比, 3);
        }

        /// <summary>
        /// 获取当前轴的指令速度
        /// 读取指定轴的目标速度（指令速度）
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>指令速度：返回-999表示操作失败</returns>
        public double GetCmdVel(MotionAxisDto axisParam)
        {
            if (!IsAxisValid(axisParam))
                return -999;  // 轴参数无效

            // 读取指令速度（脉冲单位）
            ecat_motion.M_GetCmdVel(axisParam.轴卡轴号索引, out double actualPos, 1, axisParam.轴卡卡号索引);
            // 转换为实际速度并四舍五入保留3位小数
            return Math.Round(actualPos / axisParam.脉冲比, 3);
        }

        /// <summary>
        /// 获取当前轴的编码器位置
        /// 读取指定轴的编码器反馈位置
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>编码器位置(mm)：返回-999表示操作失败</returns>
        public double GetEncPos(MotionAxisDto axisParam)
        {
            if (!IsAxisValid(axisParam))
                return -999;  // 轴参数无效

            // 读取编码器位置（脉冲数）
            ecat_motion.M_GetEncPos(axisParam.轴卡轴号索引, out double actualPos, 1, axisParam.轴卡卡号索引);
            // 转换为毫米并四舍五入保留3位小数
            return Math.Round(actualPos / axisParam.脉冲比, 3);
        }

        /// <summary>
        /// 获取当前轴的编码器速度
        /// 读取指定轴的编码器反馈速度
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>编码器速度：返回-999表示操作失败</returns>
        public double GetEncVel(MotionAxisDto axisParam)
        {
            if (!IsAxisValid(axisParam))
                return -999;  // 轴参数无效

            // 读取编码器速度（脉冲单位）
            ecat_motion.M_GetEncVel(axisParam.轴卡轴号索引, out double actualPos, 1, axisParam.轴卡卡号索引);
            // 转换为实际速度并四舍五入保留3位小数
            return Math.Round(actualPos / axisParam.脉冲比, 3);
        }

        /// <summary>
        /// 获取当前轴的驱动器实际位置
        /// 读取指定轴的驱动器反馈的实际位置
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>实际位置(mm)：返回-999表示操作失败</returns>
        public double ReadActualPosition(MotionAxisDto axisParam)
        {
            try
            {
                if (!IsAxisValid(axisParam))
                    return -999;  // 轴参数无效

                // 读取驱动器实际位置（脉冲数）
                ecat_motion.M_ReadActualPosition(axisParam.轴卡轴号索引, out int actualPos, 1, axisParam.轴卡卡号索引);
                // 转换为毫米并四舍五入保留3位小数
                return Math.Round((double)actualPos / axisParam.脉冲比, 3);
            }
            catch (Exception)
            {
                return -999;  // 发生异常，返回-999
            }
        }

        /// <summary>
        /// 获取当前轴的目标位置
        /// 读取指定轴的运动目标位置
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <returns>目标位置(mm)：返回-999表示操作失败</returns>
        public double GetTargetPos(MotionAxisDto axisParam)
        {
            try
            {
                if (!IsAxisValid(axisParam))
                    return -999;  // 轴参数无效

                // 读取目标位置（脉冲数）
                int targetPos = 0;
                ecat_motion.M_GetTargetPos(axisParam.轴卡轴号索引, ref targetPos, axisParam.轴卡卡号索引);
                // 转换为毫米并四舍五入保留3位小数
                return Math.Round((double)targetPos / axisParam.脉冲比, 3);
            }
            catch (Exception)
            {
                return -999;  // 发生异常，返回-999
            }
        }
        #region 私有辅助方法
        /// <summary>
        /// 配置急停参数
        /// 配置轴卡的急停极性、动作模式等参数
        /// </summary>
        private bool ConfigureEmergencyStop(MotionCardDto motionCard)
        {
            // 设置急停信号极性
            short returnCode = ecat_motion.M_SetEmgInv(motionCard.急停极性, motionCard.轴卡卡号);
            if (returnCode != 0)
            {
                MessageBox.Show($"ecat_motion.M_SetEmgInv 执行失败，返回值为：{returnCode}");
                return false;  // 配置失败
            }

            // 设置急停动作模式（0x00表示急停时所有轴停止）
            returnCode = ecat_motion.M_SetEmgAction(0x00, motionCard.轴卡卡号);
            if (returnCode != 0)
            {
                MessageBox.Show($"ecat_motion.M_SetEmgAction 执行失败，返回值为：{returnCode}");
                return false;  // 配置失败
            }

            // 清除急停状态
            returnCode = ecat_motion.M_ClrEmg(motionCard.轴卡卡号);
            if (returnCode != 0)
            {
                MessageBox.Show($"ecat_motion.M_ClrEmg 执行失败，返回值为：{returnCode}");
                return false;  // 清除失败
            }

            return true;  // 配置成功
        }

        /// <summary>
        /// 处理连接模式
        /// 根据配置处理快速连接模式或常规连接模式
        /// </summary>
        private bool HandleConnectionMode(MotionCardDto motionCard)
        {
            if (_useFastConnectMode)
            {
                short isOp = -1;          // 操作状态：1-正常操作，0-未就绪
                short isSlaveChange = -1; // 从站变化：1-从站配置变化，0-无变化
                short isOffline = -1;     // 离线状态：1-离线，0-在线

                // 获取连接状态
                short returnCode = ecat_motion.M_GetConnectStatus(ref isOp, ref isSlaveChange, ref isOffline, motionCard.轴卡卡号);
                if (returnCode != 0)
                {
                    MessageBox.Show($"ecat_motion.M_GetConnectStatus 执行失败，返回值为：{returnCode}");
                    return false;  // 获取状态失败
                }

                // 检查是否满足快速连接条件
                if (isOp == 0 || isSlaveChange == 1 || isOffline == 1)
                {
                    //MessageBox.Show("当前状态不满足使用快速连接功能，将自动进行常规连接操作！！！");
                    return ResetFpga(motionCard.轴卡卡号);  // 执行FPGA重置（常规连接）
                }
            }
            else
            {
                // 非快速连接模式，直接执行FPGA重置
                return ResetFpga(motionCard.轴卡卡号);
            }

            return true;  // 连接模式处理成功
        }

        /// <summary>
        /// 重置FPGA芯片
        /// 重置轴卡上的FPGA芯片，初始化硬件
        /// </summary>
        private bool ResetFpga(short cardNumber)
        {
            // 执行FPGA重置
            short returnCode = ecat_motion.M_ResetFpga(cardNumber);
            if (returnCode != 0)
            {
                MessageBox.Show($"ecat_motion.M_ResetFpga 执行失败，返回值为：{returnCode}");
                return false;  // 重置失败
            }

            Thread.Sleep(500); // 必要延时，等待FPGA稳定
            return true;  // 重置成功
        }

        /// <summary>
        /// 验证从站资源
        /// 检查EtherCAT从站设备是否正常连接
        /// </summary>
        private bool VerifySlaveResources(short cardNumber)
        {
            var slaveResource = new ecat_motion.SL_RES();  // 从站资源信息结构体
            // 获取从站资源信息
            short returnCode = ecat_motion.M_GetSlaveResource(out slaveResource, cardNumber);

            if (returnCode == 0)
                return true;  // 验证成功

            MessageBox.Show($"卡{cardNumber}获取从站资源失败！");
            return false;  // 验证失败
        }

        /// <summary>
        /// 禁用所有轴
        /// 对所有轴执行伺服禁用操作
        /// </summary>
        private void DisableAllAxes()
        {
            try
            {
                // 遍历所有轴并禁用
                for (short i = 1; i < _axisCount + 1; i++)
                    ecat_motion.M_Servo_Off(i, 0);

                Thread.Sleep(100);  // 等待操作生效
            }
            catch { /* 忽略禁用轴时的错误 */ }
        }

        /// <summary>
        /// 检查轴是否有效
        /// 验证轴参数对象是否有效（非空且轴名称不为空）
        /// </summary>
        private bool IsAxisValid(MotionAxisDto axisParam)
        {
            return axisParam != null && !string.IsNullOrEmpty(axisParam.轴名称);
        }

        /// <summary>
        /// 执行轴操作的通用方法
        /// 封装轴操作的通用流程：参数验证、异常处理等
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <param name="operation">具体的轴操作函数</param>
        /// <returns>操作结果："NoError"表示成功，其他表示错误信息</returns>
        private string ExecuteAxisOperation(MotionAxisDto axisParam, Func<string> operation)
        {
            if (!IsAxisValid(axisParam))
                return ErrorNotBound;  // 轴参数无效

            try
            {
                return operation();  // 执行具体操作
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);  // 记录异常日志
                return ErrorOccurred;  // 操作发生错误
            }
        }
        /// <summary>
        /// 检查轴是否处于限位状态
        /// 检查指定轴是否处于正极限或负极限状态
        /// </summary>
        private bool IsAxisAtLimit(MotionAxisDto axisParam)
        {
            // 读取伺服状态
            ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out _servoStatus, 1, axisParam.轴卡卡号索引);
            // 检查正极限或负极限位
            return (_servoStatus & PositiveLimitMask) == PositiveLimitMask
                || (_servoStatus & NegativeLimitMask) == NegativeLimitMask;
        }

        /// <summary>
        /// 启动运动完成检测任务
        /// 启动异步任务监测运动是否完成
        /// </summary>
        private void StartMotionCompletionCheck(MotionAxisDto axisParam)
        {
            // 使用Task.Run启动异步任务，避免阻塞主线程
            var task = Task.Run(() =>
            {
                int currentStatus;
                //循环检查运动状态，直到运动结束
                do
                {
                    Thread.Sleep(100);  // 100ms检查一次
                    ecat_motion.M_GetSts(axisParam.轴卡轴号索引, out currentStatus, 1, axisParam.轴卡卡号索引);
                } while ((currentStatus & MotionMask) == MotionMask);  // 运动中标志位为1则继续等待
            });
            task.Wait();
        }

        /// <summary>
        /// 创建默认状态数组
        /// 创建一个长度为32，所有元素为false的布尔数组
        /// </summary>
        private bool[] CreateDefaultStatusArray()
        {
            var statusArray = new bool[32];
            // 兼容旧版本.NET Framework，使用for循环初始化
            for (int i = 0; i < statusArray.Length; i++)
            {
                statusArray[i] = false;
            }
            return statusArray;
        }

        /// <summary>
        /// 获取错误代码信息
        /// 根据错误代码生成错误信息
        /// </summary>
        private string GetErrorCodeMessage(short errorCode)
        {
            return $"设置参数Error--->参考凌臣错误代码：{errorCode}";
        }
        // 用于跟踪每个轴的运动次数（键：轴唯一标识，值：运动次数）
        private Dictionary<string, int> _axisMotionCounts = new Dictionary<string, int>();
        // 线程安全锁，确保多线程环境下计数操作的原子性
        private object _motionCountLock = new object();

        /// <summary>
        /// 获取当前应使用的速度（内部辅助方法）
        /// </summary>
        /// <param name="axisParam">轴参数对象</param>
        /// <param name="normalSpeed">正常速度（高速）</param>
        /// <param name="axisKey">轴唯一标识</param>
        /// <returns>当前应使用的速度值</returns>
        private double GetCurrentSpeed(MotionAxisDto axisParam, double normalSpeed, string axisKey)
        {
            // 加锁确保线程安全
            lock (_motionCountLock)
            {
                // 逻辑：如果是首次运动（计数不存在或为0），使用低速；否则使用正常速度
                // 低速值取轴参数中配置的"首次运动速度"，确保灵活性
                if (!_axisMotionCounts.ContainsKey(axisKey) || _axisMotionCounts[axisKey] == 0)
                {
                    return normalSpeed * 0.3;  // 首次运动使用低速
                }
                return normalSpeed;  // 非首次运动使用高速
            }
        }

        /// <summary>
        /// 更新轴的运动次数（内部辅助方法）
        /// </summary>
        /// <param name="axisKey">轴唯一标识</param>
        private void UpdateMotionCount(string axisKey)
        {
            // 加锁确保线程安全
            lock (_motionCountLock)
            {
                if (_axisMotionCounts.ContainsKey(axisKey))
                {
                    // 已存在计数，累加1
                    _axisMotionCounts[axisKey]++;
                }
                else
                {
                    // 不存在计数，初始化为1（表示已完成首次运动）
                    _axisMotionCounts[axisKey] = 1;
                }
            }
        }
        /// <summary>
        /// 重置轴的运动计数（需要时可调用，例如回零后）
        /// </summary>
        private void ResetMotionCount(MotionAxisDto axisParam)
        {
            string axisKey = $"{axisParam.轴卡卡号索引}_{axisParam.轴卡轴号索引}";
            lock (_motionCountLock)
            {
                if (_axisMotionCounts.ContainsKey(axisKey))
                {
                    _axisMotionCounts[axisKey] = 0;
                }
            }
        }

        public string LinearInterpolation2D(MotionAxisDto axis1, MotionAxisDto axis2, double targetPos1, double targetPos2, double speed, double accel, double decel)
        {
            throw new NotImplementedException();
        }

        public string CircularInterpolation2D(MotionAxisDto xAxis, MotionAxisDto yAxis, double centerX, double centerY, double endX, double endY, bool isClockwise, double speed, double accel, double decel)
        {
            throw new NotImplementedException();
        }

        public string LinearInterpolation3D(MotionAxisDto axis1, MotionAxisDto axis2, MotionAxisDto axis3, double targetPos1, double targetPos2, double targetPos3, double speed, double accel, double decel)
        {
            throw new NotImplementedException();
        }

        public string Interpolation4D(MotionAxisDto[] axes, double[] targetPositions, double speed, double accel, double decel)
        {
            throw new NotImplementedException();
        }

        public string Interpolation5D(MotionAxisDto[] axes, double[] targetPositions, double speed, double accel, double decel, double toolLength = 0)
        {
            throw new NotImplementedException();
        }

        public bool CheckInterpolationDone(MotionAxisDto[] axes)
        {
            throw new NotImplementedException();
        }

        public string StopInterpolation(MotionAxisDto[] axes, ushort stopMode)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
