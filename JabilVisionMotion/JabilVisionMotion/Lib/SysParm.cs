using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using Jabil.Lib;
using JabilVisionMotion.Frm;
using JabilVisionMotion.Frm.UserPage;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion;
using Jabil.Module.Permission;
using Jabil.Module.TCPIP.Lib;
using Jabil.Module.Vision;
using Jabil.Module.Vision.Lib;
using Jabil.Module.TCPIP;
using Jabil.Module.Modbus;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Modbus.Lib;
using BasicClassLibrary.RexHelps;
using JabilVisionMotion.Edit;
using Jabil.Module.IO;
using csLTDMC;
using System.Threading;
using Jabil.Module.Serialport;
using Jabil.Module.Serialport.Lib;
using System.IO.Ports;
using Jabil.Module.Vision.Entity;
using Jabil.Lib.SQLHelper;
using HslCommunication;
using Jabil.Module.IO.Common.Dtos;
using lctdevice;
using Jabil.Module.Vision.Common.Dtos;
using Jabil.Module.Setting.Lib.JVS;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.TCPIP.Common.Dtos;
using System.Collections;
using JabilVisionMotion.Frm.Dialog;
using System.Reflection;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Jabil.Module.Motion.Lib;

namespace JabilVisionMotion.Lib
{
    /// <summary>
    /// 系统参数核心类（采用单例模式）<br/>
    /// 职责：
    /// - 集中管理系统级状态（如当前窗体模式、硬件状态）
    /// - 统一控制后台线程的生命周期（启动/停止）
    /// - 管理硬件设备初始化与状态监控
    /// - 处理系统级警告与提示（蜂鸣器/LED指示灯）
    /// 设计目的：提供全局唯一的系统控制中枢，确保状态一致性和资源统一管理
    /// </summary>
    public class SysParm
    {
        // 线程安全的单例实现（使用Lazy<T>延迟初始化）
        // Lazy<T>默认使用ExecutionAndPublication模式，自动保证多线程环境下的线程安全
        // 确保仅在首次访问时初始化实例，避免不必要的资源消耗
        //初始化实例时调用委托    对整个类进行出
        private static readonly Lazy<SysParm> _instance = new Lazy<SysParm>(() => new SysParm());

        /// <summary>
        /// 全局唯一实例访问点<br/>
        /// 通过静态属性提供单例实例，外部代码无法直接创建新实例
        /// </summary>
        public static SysParm Instance => _instance.Value;

        /// <summary>
        /// 当前窗体运行模式<br/>
        /// 用于控制界面行为（如编辑模式/查看模式/配置模式等）
        /// </summary>
        public FormPage CurrentFormMode = FormPage.None;

        /// <summary>
        /// 蜂鸣器与LED指示灯控制器<br/>
        /// 用于系统警告、提示的硬件输出（如报警时蜂鸣+红灯闪烁）
        /// </summary>
        public BuzzerLED BuzzerLed = new BuzzerLED();

        /// <summary>
        /// 线程管理器（内部使用，限制外部直接修改）<br/>
        /// 负责：
        /// - 注册所有后台工作线程（如数据采集线程、通讯线程）
        /// - 统一启动/停止线程，避免线程管理分散
        /// - 处理线程异常与资源释放
        /// </summary>
        internal readonly ThreadManager _threadManager = new ThreadManager();

        /// <summary>
        /// 硬件初始化管理器（内部使用）<br/>
        /// 负责：
        /// - 初始化所有硬件设备（如传感器、PLC、运动控制器）
        /// - 检查硬件连接状态与故障诊断
        /// - 统一释放硬件资源
        /// </summary>
        internal readonly HardwareInitializer _hardwareInitializer = new HardwareInitializer();


        /// <summary>
        /// 私有构造函数（防止外部实例化，保证单例唯一性）<br/>
        /// 在此处可添加系统初始化逻辑（如加载配置文件、初始化默认状态）
        /// </summary>
        private SysParm() { }

        /// <summary>
        /// 启动所有注册的后台线程<br/>
        /// 调用线程管理器的批量启动方法，确保线程按预定顺序启动
        /// </summary>
        public void StartThreads() => _threadManager.StartAllThreads();

        /// <summary>
        /// 停止所有后台线程（优雅退出）<br/>
        /// 特点：
        /// - 不会强制终止线程，而是发送终止信号等待线程自行退出
        /// - 确保线程资源（如网络连接、文件句柄）正确释放
        /// - 适用于程序关闭或系统暂停场景
        /// </summary>
        public void StopThreads() => _threadManager.StopAllThreads();



        /// <summary>
        /// 前焊接机连接
        /// </summary>
        public HttpClient _httpClient1 = new HttpClient();


        /// <summary>
        /// 后焊接机连接
        /// </summary>
        public HttpClient _httpClient2 = new HttpClient();
    }

    /// <summary>
    /// 硬件初始化管理器
    /// 核心作用：统一协调各类硬件设备的初始化流程，包括运动控制卡、IO卡、视觉工具、Modbus设备等
    /// 设计思路：按硬件类型拆分初始化方法，通过内部工具类封装通用逻辑（如卡号解析、路径构建），降低代码冗余
    /// </summary>
    internal class HardwareInitializer
    {
        /// <summary>
        /// 硬件初始化通用工具类
        /// 作用：封装硬件初始化过程中的重复操作（卡号解析、路径拼接、空列表检查等），提升代码复用性
        /// </summary>
        internal class HardwareInitUtil
        {
            /// <summary>
            /// 解析卡号为ushort类型（通用方法）
            /// 用于将配置文件中的卡号（可能为字符串或数字）转换为硬件接口所需的ushort类型
            /// </summary>
            /// <param name="cardNumber">原始卡号（配置中读取的字符串/数字）</param>
            /// <param name="deviceType">设备类型（用于错误日志的上下文说明，如"运动卡"、"IO卡"）</param>
            /// <returns>解析成功返回ushort值，失败返回null</returns>
            public static ushort? ParseCardNumber(object cardNumber, string deviceType)
            {
                if (ushort.TryParse(cardNumber?.ToString(), out ushort parsedNum))
                    return parsedNum;

                // 解析失败时记录错误日志（原代码注释中预留的日志方法）
                // RLog.SaveError($"[{deviceType}] 卡号解析失败，原始值：{cardNumber}");
                return null;
            }

            /// <summary>
            /// 构建配置文件路径（自动拼接应用程序根目录）
            /// 避免硬编码路径，统一管理配置文件存储位置（如Config/Project下的各类配置）
            /// </summary>
            /// <param name="segments">路径片段（如["Config", "Project", "Motion"]）</param>
            /// <returns>完整的配置文件路径</returns>
            public static string BuildConfigPath(params string[] segments)
            {
                var fullSegments = new List<string> { Application.StartupPath }; // 基础路径为应用程序运行目录
                fullSegments.AddRange(segments); // 拼接传入的路径片段
                return Path.Combine(fullSegments.ToArray()); // 组合为完整路径
            }

            /// <summary>
            /// 检查设备配置列表是否为空
            /// 初始化前验证是否有有效配置，避免空引用异常，统一返回初始化结果对象
            /// </summary>
            /// <typeparam name="T">设备配置DTO类型（如IOCardDto、MotionAxisDto等）</typeparam>
            /// <param name="list">设备配置列表</param>
            /// <param name="deviceType">设备类型（用于错误信息，如"IO卡"、"运动轴"）</param>
            /// <returns>IniInfo对象（Success表示是否为空，Msg为提示信息）</returns>
            public static IniInfo CheckEmptyList<T>(IEnumerable<T> list, string deviceType)
            {
                return list == null || !list.Any()
                    ? new IniInfo(false, $"无{deviceType}配置（或系统不支持该类型）") // 空列表返回失败信息
                    : new IniInfo(true, string.Empty); // 非空列表返回成功
            }
        }

        #region 运动控制卡初始化
        /// <summary>
        /// 初始化运动控制卡
        /// 流程：读取运动卡配置列表 → 检查配置非空 → 解析卡号 → 构建配置路径 → 调用硬件接口打开运动卡
        /// </summary>
        /// <returns>IniInfo对象（初始化结果：成功/失败及描述信息）</returns>
        public IniInfo InitMotionCards()
        {
            try
            {
                // 从运动系统单例中获取配置列表（封装了配置读取逻辑）
                var motionCards = MotionSystem.Instance.MotionCards;
                // 检查配置列表是否为空
                var emptyCheck = HardwareInitUtil.CheckEmptyList(motionCards, "运动卡");
                if (!emptyCheck.Success) return emptyCheck;

                foreach (var card in motionCards)
                {
                    // 解析卡号（调用工具类方法，处理配置中的字符串/数字）
                    var cardNum = HardwareInitUtil.ParseCardNumber(card.轴卡卡号, "运动卡");
                    if (!cardNum.HasValue)
                        return new IniInfo(false, $"运动卡[{card.轴卡类型}]卡号解析失败：{card.轴卡卡号}");

                    // 构建配置文件路径：PCIe-M60类型无需配置文件，其他类型需指定参数文件
                    string configPath = card.轴卡类型 == "PCIe-M60"
                        ? string.Empty
                        : HardwareInitUtil.BuildConfigPath("Config", "Project", "Motion", card.轴卡参数文件);

                    // 调用运动控制器接口打开运动卡，失败则返回错误信息
                    if (!MotionSystem.Instance.MotionController.Open(card))
                        return new IniInfo(false, $"运动卡[{card.轴卡类型}]加载失败，卡号：{card.轴卡卡号}");
                }
                return new IniInfo(true, "运动卡初始化成功");
            }
            catch (Exception ex)
            {
                // 记录异常日志（全局日志工具）
                RLog.SaveError(ex);
                return new IniInfo(false, $"运动卡初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 轴伺服使能
        /// 为已初始化的运动轴执行伺服使能操作，确保轴可接收运动指令（仅支持指定型号的轴卡）
        /// 5秒内未完成使能则超时跳出，继续加载流程
        /// </summary>
        /// <returns>IniInfo对象（使能结果：成功/失败及描述信息）</returns>
        public IniInfo EnableAxisServo()
        {
            try
            {
                // 获取运动轴配置列表
                var motionAxes = MotionSystem.Instance.MotionAxes;
                // 检查配置列表是否为空
                var emptyCheck = HardwareInitUtil.CheckEmptyList(motionAxes, "运动轴");
                if (!emptyCheck.Success) return emptyCheck;

                foreach (var axis in motionAxes)
                {
                    // 仅支持指定类型的轴卡
                    if (!new[] { "3400", "3600", "3800", "5600", "5800", "PCIe-M60" }.Contains(axis.轴卡类型))
                    {
                        return new IniInfo(false, $"不支持的轴卡类型（伺服使能）：{axis.轴卡类型}");
                    }

                    // 为每个轴的ServoSet操作创建5秒超时任务
                    var servoTask = Task.Run(() =>
                    {
                        MotionSystem.Instance.MotionController.ServoSet(axis, 1);
                    });

                    // 等待任务完成，最多等待5秒
                    if (!servoTask.Wait(5000))
                    {
                        // 超时：记录警告日志并退出循环，继续后续流程
                        RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                                    $"轴[{axis.轴名称}]伺服使能操作超时，将退出循环并继续后续流程");
                        return new IniInfo(true, $"轴[{axis.轴名称}]伺服使能超时，已退出循环继续后续加载");
                    }
                }

                return new IniInfo(true, "所有轴伺服使能成功");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new IniInfo(false, $"轴伺服使能异常：{ex.Message}");
            }
        }
        #endregion


        #region IO卡初始化
        /// <summary>
        /// FlyTrigger
        /// </summary>
        /// <returns></returns>
        public IniInfo LoadFlyTrigger()
        {
            try
            {
                int Slavenum = 0;
                //初始化并连接总线，返回从站数量，0是不开启别名，1是开启别名
                MiniEcatLib.Mb_InitEcat(ref Slavenum, 0);

                if (Slavenum > 0)
                {
                    Thread.Sleep(100);
                    int a = MiniEcatLib.Mb_LoadParamFromFile(@"D:\JabilVisionMotion\JabilVisionMotion\bin\Debug\Config\Project\Motion\E4O4配置文件.INI");
                    return new IniInfo(true, "FlyTrigger成功");
                }
                else

                {
                    return new IniInfo(false, "FlyTrigger失败");
                }
            }
            catch
            {
                return new IniInfo(false, "FlyTrigger失败");
            }
        }

        /// <summary>
        /// 初始化IO卡
        /// 支持多种类型IO卡（3400/7230/EC8003等），按类型调用对应初始化逻辑（策略模式）
        /// </summary>
        /// <returns>IniInfo对象（初始化结果：成功/失败及描述信息）</returns>
        public IniInfo InitIOCards()
        {
            try
            {
                // 获取IO卡配置列表
                var ioCards = IOSystem.Instance.IOCards;
                // 检查配置列表是否为空
                var emptyCheck = HardwareInitUtil.CheckEmptyList(ioCards, "IO卡");
                if (!emptyCheck.Success) return emptyCheck;

                foreach (var card in ioCards)
                {
                    // 解析卡号（调用工具类方法）
                    var cardNum = HardwareInitUtil.ParseCardNumber(card.卡号索引, $"IO卡（类型：{card.卡类型}）");
                    if (!cardNum.HasValue)
                        return new IniInfo(false, $"IO卡[{card.卡类型}]卡号解析失败：{card.卡号索引}");

                    // 根据卡类型调用对应的初始化方法（策略模式：不同类型对应不同实现）
                    var initResult = card.卡类型 switch
                    {
                        "3400" => InitPassiveIOCard(card), // 依赖运动卡的IO，无需单独初始化
                        "3600" => InitPassiveIOCard(card),
                        "5800" => InitPassiveIOCard(card),
                        "PCIe-M60" => InitPassiveIOCard(card),
                        "7230" => InitPci7230Card(cardNum.Value), // PCI-7230独立初始化
                        "7432" => InitPci7432Card(cardNum.Value), // PCI-7432独立初始化
                        "PCIe-EC8003" => InitEc8003Card(cardNum.Value), // EtherCAT卡初始化
                        _ => (false, $"不支持的IO卡类型：{card.卡类型}") // 未知类型返回错误
                    };

                    if (!initResult.Item1)
                        return new IniInfo(false, initResult.Item2);
                }

                return new IniInfo(true, "IO卡初始化成功");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new IniInfo(false, $"IO卡初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 初始化PCI-7230卡（数字IO卡）
        /// 调用DASK库接口注册硬件，完成卡的初始化
        /// </summary>
        /// <param name="cardNum">卡号（硬件标识）</param>
        /// <returns>元组（是否成功，错误信息）</returns>
        private (bool Success, string ErrorMessage) InitPci7230Card(ushort cardNum)
        {
            // 调用DASK库注册PCI-7230卡，返回值<0表示失败
            if (DASK.Register_Card(DASK.PCI_7230, cardNum) < 0)
                return (false, $"PCI-7230卡注册失败（卡号：{cardNum}）");
            return (true, string.Empty);
        }

        /// <summary>
        /// 初始化PCI-7432卡（数字IO卡）
        /// 调用DASK库接口注册硬件，完成卡的初始化
        /// </summary>
        /// <param name="cardNum">卡号（硬件标识）</param>
        /// <returns>元组（是否成功，错误信息）</returns>
        private (bool Success, string ErrorMessage) InitPci7432Card(ushort cardNum)
        {
            // 调用DASK库注册PCI-7432卡，返回值<0表示失败
            if (DASK.Register_Card(DASK.PCI_7432, cardNum) < 0)
                return (false, $"PCI-7432卡注册失败（卡号：{cardNum}）");
            return (true, string.Empty);
        }

        /// <summary>
        /// 初始化PCIe-EC8003卡（EtherCAT总线IO卡）
        /// 流程：断开旧连接 → 打开卡 → 加载ENI配置 → 复位FPGA → 连接从站（EtherCAT从设备）
        /// </summary>
        /// <param name="cardNum">卡号（硬件标识）</param>
        /// <returns>元组（是否成功，错误信息）</returns>
        private (bool Success, string ErrorMessage) InitEc8003Card(ushort cardNum)
        {
            // ENI文件路径（EtherCAT网络配置文件，描述从站设备信息）
            const string eniPath = @"C:\Program Files (x86)\LCT\LCT 800X\ENI\eni.xml";
            short option = 1; // 断线重连模式：保持输出状态（避免重连时输出信号突变）
            short param = 0; // 预留参数

            // 断开现有连接（避免卡号被占用）
            short ret = lctdevice.ecat_io.EC_DisconnectECAT((short)cardNum);
            if (ret == 0) ret = lctdevice.ecat_io.EC_Close((short)cardNum);

            // 打开卡并加载配置
            ret = lctdevice.ecat_io.EC_Open((short)cardNum, param);
            if (ret != 0) return (false, $"EC8003打开失败（返回值：{ret}）");

            ret = lctdevice.ecat_io.EC_LoadEni(eniPath, (short)cardNum);
            if (ret != 0) return (false, $"EC8003加载ENI文件失败（返回值：{ret}）");

            ret = lctdevice.ecat_io.EC_ResetFpga((short)cardNum);
            Thread.Sleep(200); // 复位FPGA后必需延时，确保硬件稳定
            if (ret != 0) return (false, $"EC8003复位FPGA失败（返回值：{ret}）");

            ret = lctdevice.ecat_io.EC_ConnectECAT(option, (short)cardNum);
            if (ret != 0) return (false, $"EC8003连接从站失败（返回值：{ret}）");

            return (true, string.Empty);
        }

        /// <summary>
        /// 被动初始化（依赖运动卡的IO卡，无需单独处理）
        /// 如3400轴卡集成的IO功能，随运动卡初始化完成而可用
        /// </summary>
        /// <param name="card">IO卡配置DTO</param>
        /// <returns>元组（成功，空信息）</returns>
        private (bool Success, string ErrorMessage) InitPassiveIOCard(IOCardDto card)
        {
            return (true, string.Empty);
        }
        #endregion

        #region 视觉设备初始化
        /// <summary>JVS 视觉方案工具类型标识（进程内嵌入模式，不做 .vpp 加载）</summary>
        private const string JvsWorkspaceToolType = "JvsWorkspaceTool";

        /// <summary>JVS 取图工具类型标识（.jvt 相机工具，取图由 workspace 相机算子驱动，不做 .vpp 加载）</summary>
        private const string JvsCameraToolType = "JvsCameraTool";

        /// <summary>
        /// 初始化视觉工具（Cognex VisionPro相关工具）
        /// 流程：读取视觉工具配置 → JVS 工具走进程内装配分支 → 检查非空 → 跳过已初始化工具 → 构建工具路径 → 加载.vpp文件 → 按类型存储到对应数组
        /// 说明：类型为 JvsWorkspaceTool 的工具走 JVS 进程内分支——初始化 Python 引擎并装配 .jvws 方案（ToolBlockRunner）
        /// </summary>
        /// <param name="progressAction">进度回调（可选，用于UI显示加载进度）</param>
        /// <returns>IniInfo对象（初始化结果：成功/失败及描述信息）</returns>
        public IniInfo InitVisionTools(Action<int, string> progressAction = null)
        {
            try
            {
                // 获取视觉工具配置列表
                var visionTools = VisionSystem.Instance.ListVisionTool;
                // 检查配置列表是否为空
                var emptyCheck = HardwareInitUtil.CheckEmptyList(visionTools, "视觉工具");
                if (!emptyCheck.Success) return emptyCheck;

                foreach (var tool in visionTools)
                {
                    // ===== JVS 取图工具（.jvt 相机工具算子）：不做 .vpp 加载，校验并预加载取图工具 =====
                    if (tool.检测工具类型 == JvsCameraToolType)
                    {
                        progressAction?.Invoke(45, $"正在装配 JVS 取图工具：{tool.检测工具名称}（{tool.检测工具路径}）");
                        var jvsCamResult = InitJvsCameraTool(tool);
                        if (!jvsCamResult.Success)
                            return new IniInfo(false, jvsCamResult.ErrorMessage);
                        Thread.Sleep(100);
                        continue;
                    }

                    // ===== JVS 视觉方案工具（进程内嵌入）：不做 .vpp 加载，初始化引擎并装配 .jvws 方案 =====
                    if (tool.检测工具类型 == JvsWorkspaceToolType)
                    {
                        progressAction?.Invoke(45, $"正在装配 JVS 视觉方案：{tool.检测工具名称}（{tool.检测工具路径}）");
                        var jvsResult = InitJvsWorkspaceTool(tool);
                        if (!jvsResult.Success)
                            return new IniInfo(false, jvsResult.ErrorMessage);
                        Thread.Sleep(100);
                        continue;
                    }
                }

                return new IniInfo(true, "视觉工具初始化成功");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new IniInfo(false, $"视觉工具初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 初始化LMI3D设备（预留方法，用于3D视觉设备初始化）
        /// 当前为占位实现，实际项目中需补充Gocator等3D相机的连接逻辑
        /// </summary>
        /// <returns>IniInfo对象（预留成功信息）</returns>
        public IniInfo InitLmi3D()
        {
            try
            {
                // 示例：未来实现LMI3D连接逻辑
                // var gocator = new Gocator.GocatorAcquisition();
                // gocator.Connect("192.168.0.16", "");
                return new IniInfo(true, "LMI3D初始化成功（预留）");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new IniInfo(false, $"LMI3D初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 装配 JVS 取图工具（进程内嵌入：JvlDotNet + workspace 相机算子）
        /// 工具路径字段存 .jvt 绝对路径（如 D:\...\相机取图.jvt，JVS 导出的 CameraTool 工具实例）
        /// 流程：初始化 Python 引擎（幂等）→ 预加载取图工具（首次取图时直接复用，无需再反序列化）
        /// </summary>
        private (bool Success, string ErrorMessage) InitJvsCameraTool(VisionToolDto tool)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tool.检测工具路径))
                    return (false, $"JVS 取图工具 {tool.检测工具名称} 未配置 .jvt 路径（检测工具路径字段）");

                if (!JvsVisionBridge.Instance.InitializeEngine(out var engineError))
                    return (false, engineError);

                if (!JvsVisionBridge.Instance.Camera.Load(tool.检测工具路径))
                    return (false, $"JVS 取图工具加载失败：{JvsVisionBridge.Instance.Camera.LastError}（工具：{tool.检测工具路径}）");

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return (false, $"JVS 取图工具初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 装配 JVS 视觉方案（进程内嵌入：JvlDotNet）
        /// 工具路径字段存 .jvws 绝对路径（如 D:\...\筛选分拣程序.jvws）
        /// 流程：初始化 Python 引擎（幂等）→ 按工具名称装配 ToolBlockRunner（缓存，首次检测时直接复用）
        /// </summary>
        private (bool Success, string ErrorMessage) InitJvsWorkspaceTool(VisionToolDto tool)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tool.检测工具路径))
                    return (false, $"JVS 工具 {tool.检测工具名称} 未配置 .jvws 路径（检测工具路径字段）");

                if (!JvsVisionBridge.Instance.InitializeEngine(out var engineError))
                    return (false, engineError);

                var runner = JvsVisionBridge.Instance.GetRunner(tool.检测工具名称, tool.检测工具路径, out var runnerError);
                if (runner == null)
                    return (false, $"JVS 视觉方案装配失败：{runnerError}（方案：{tool.检测工具路径}）");

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return (false, $"JVS 视觉方案初始化异常：{ex.Message}");
            }
        }

        #endregion

        #region Modbus通讯设备初始化
        /// <summary>
        /// 初始化Modbus设备（PLC）
        /// 流程：读取Modbus配置 → 检查非空 → 按类型初始化PLC → 验证连接 → 添加到全局PLC列表
        /// </summary>
        /// <returns>IniInfo对象（初始化结果：成功/失败及描述信息）</returns>
        public IniInfo InitPLCModbusDevices()
        {
            try
            {
                // 获取Modbus设备配置列表（仅初始化"是否启用"的设备；开发/仿真环境无启用设备时跳过，不阻塞启动）
                var modbusDevices = ModbusSystem.Instance.ModbusCommunications
                    .Where(d => d.是否启用).ToList();
                if (modbusDevices.Count == 0)
                    return new IniInfo(true, "无启用的Modbus设备（跳过初始化）");

                foreach (var device in modbusDevices)
                {
                    // 根据设备类型初始化（工厂模式：不同类型创建不同设备实例）
                    var (deviceObj, isConnected) = device.设备类型 switch
                    {
                        "PLC" => InitPlcDevice(device), // PLC类型调用PLC初始化
                        _ => (null, false) // 非PLC类型忽略
                    };
                    // 连接失败且为PLC类型时返回错误
                    if (!isConnected && device.设备类型 == "PLC")
                        return new IniInfo(false, $"PLC[{device.设备名称}]连接失败");
                    // 添加到PLC列表（全局管理）
                    if (device.设备类型 == "PLC")
                        ModbusSystem.Instance.PlcModbusList.Add((PLC_Modbus)deviceObj);
                }

                MachineController.StartMonitoring();

                return new IniInfo(true, "Modbus设备初始化成功");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new IniInfo(false, $"Modbus初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 初始化Modbus设备（机器人）
        /// 流程：读取Modbus配置 → 检查非空 → 按类型初始化机器人 → 验证连接 → 添加到全局机器人列表
        /// </summary>
        /// <returns>IniInfo对象（初始化结果：成功/失败及描述信息）</returns>
        public IniInfo InitRobotModbusDevices()
        {
            try
            {
                // 获取Modbus设备配置列表（仅初始化"是否启用"的设备；开发/仿真环境无启用设备时跳过，不阻塞启动）
                var modbusDevices = ModbusSystem.Instance.ModbusCommunications
                    .Where(d => d.是否启用).ToList();
                if (modbusDevices.Count == 0)
                    return new IniInfo(true, "无启用的Modbus设备（跳过初始化）");

                foreach (var device in modbusDevices)
                {
                    // 根据设备类型初始化（工厂模式）
                    var (deviceObj, isConnected) = device.设备类型 switch
                    {
                        "Robot" => InitRobotDevice(device), // 机器人类型调用机器人初始化
                        _ => (null, false) // 非机器人类型忽略
                    };
                    // 连接失败且为机器人类型时返回错误
                    if (!isConnected && device.设备类型 == "Robot")
                        return new IniInfo(false, $"机器人[{device.设备名称}]连接失败");
                    // 添加到机器人列表（全局管理）
                    if (device.设备类型 == "Robot")
                        ModbusSystem.Instance.RobotModbusList.Add((Robot_Modbus)deviceObj);
                }

                return new IniInfo(true, "Modbus设备初始化成功");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new IniInfo(false, $"Modbus初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 初始化PLC设备（Modbus协议）
        /// 根据PLC型号（如H5u）调用对应连接方法，建立Modbus通信
        /// </summary>
        /// <param name="device">Modbus设备配置DTO</param>
        /// <returns>元组（PLC实例，是否连接成功）</returns>
        private (PLC_Modbus Device, bool IsConnected) InitPlcDevice(ModbusCommunicationDto device)
        {
            var plc = new PLC_Modbus { DeviceName = device.设备名称 }; // 创建PLC实例并设置名称
            bool isConnected = false;

            // 仅处理H5u类型PLC（可扩展其他类型如S7-1200等）
            if (device.设备名称.Contains("H5u"))
            {
                // 调用PLC连接方法（IP、端口、站号从配置中获取）
                isConnected = plc.Connect(PLC_Modbus.PLCType.H5u, device.设备IP, device.设备Port, device.站号).IsSuccess;

            }

            return (plc, isConnected);
        }

        /// <summary>
        /// 初始化机器人设备（Modbus协议）
        /// 根据机器人品牌（如Epson、Fanuc）调用对应连接方法，建立Modbus通信
        /// </summary>
        /// <param name="device">Modbus设备配置DTO</param>
        /// <returns>元组（机器人实例，是否连接成功）</returns>
        private (Robot_Modbus Device, bool IsConnected) InitRobotDevice(ModbusCommunicationDto device)
        {
            var robot = new Robot_Modbus { DeviceName = device.设备名称 }; // 创建机器人实例并设置名称
            bool isConnected = false;

            // 根据品牌连接机器人（可扩展其他品牌）
            if (device.设备名称.Contains("Epson"))
                isConnected = robot.Connect(Robot_Modbus.RobotType.Epson, device.设备IP, device.设备Port, device.站号).IsSuccess;
            else if (device.设备名称.Contains("Fanuc"))
                isConnected = robot.Connect(Robot_Modbus.RobotType.Fanuc, device.设备IP, device.设备Port, device.站号).IsSuccess;
            else if (device.设备名称.Contains("Efort"))
                isConnected = robot.Connect(Robot_Modbus.RobotType.Efort, device.设备IP, device.设备Port, device.站号).IsSuccess;

            return (robot, isConnected);
        }
        #endregion

        #region TCPIP通讯设备初始化
        /// <summary>
        /// 初始化TCPIP设备（Server/Client）
        /// 流程：读取TCPIP配置 → 检查非空 → 按类型创建Server/Client → 绑定事件 → 添加到全局列表
        /// </summary>
        /// <returns>IniInfo对象（初始化结果：成功/失败及描述信息）</returns>
        public IniInfo InitTcpipDevices()
        {
            try
            {
                // 获取TCPIP设备配置列表
                var tcpDevices = TcpIpSystem.Instance.TcpIpCommunications;
                // 检查配置列表是否为空
                var emptyCheck = HardwareInitUtil.CheckEmptyList(tcpDevices, "TCPIP设备");
                if (!emptyCheck.Success) return emptyCheck;

                foreach (var device in tcpDevices)
                {
                    // 根据设备类型（Server/Client）创建对应实例
                    switch (device.设备类型)
                    {
                        case "TCPServer":
                            var server = CreateTcpServer(device); // 创建TCP服务器
                            TcpIpSystem.Instance.TcpServers.Add(server); // 添加到服务器列表
                            break;
                        case "TCPClient":
                            var client = CreateTcpClient(device); // 创建TCP客户端
                            TcpIpSystem.Instance.TcpClients.Append(client); // 添加到客户端列表
                            break;
                        default:
                            return new IniInfo(false, $"不支持的TCPIP设备类型：{device.设备类型}");
                    }
                }

                return new IniInfo(true, "TCPIP设备初始化成功");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new IniInfo(false, $"TCPIP初始化异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 创建TCPServer并绑定事件
        /// 初始化服务器参数，为特殊设备（如Epson机器人）绑定通信事件（连接/断开/数据接收）
        /// </summary>
        /// <param name="device">TCPIP设备配置DTO</param>
        /// <returns>TCPServer实例</returns>
        private TCPServer CreateTcpServer(TcpIpCommunicationDto device)
        {
            // 创建服务器实例（IP、端口、最大连接数等参数从配置或常量获取）
            var server = new TCPServer(device.设备IP.ToString(), device.设备Port.ToString(), 20, 2014 * 2014, 1)
            {
                Name = device.设备名称 // 设置服务器名称（用于标识）
            };





            // 特殊设备（如Epson1机器人）绑定专属事件处理方法
            if (device.设备名称 == "Epson2")
            {
                server.ExceptionOccurred += Controller.Instance._Epson_1_TCPServerException; // 异常事件
                server.ClientClose += Controller.Instance._Epson_1_TCPServerClientClose; // 客户端断开事件
                server.ClientConnected += Controller.Instance._Epson_1_TCPServerClientConnected; // 客户端连接事件
                server.DataArrived += Controller.Instance._Epson_1_TCPServerDataArrived; // 数据接收事件
                server.StartServer(); // 启动服务器
            }
            else
            {
                // 其他服务器默认启动（可扩展事件绑定）
                server.StartServer();
            }

            return server;
        }

        /// <summary>
        /// 创建TCPClient（可扩展事件绑定）
        /// 初始化客户端参数，预留事件绑定接口（如数据接收/连接状态变化）
        /// </summary>
        /// <param name="device">TCPIP设备配置DTO</param>
        /// <returns>TCPClient实例</returns>
        private TCPClient CreateTcpClient(TcpIpCommunicationDto device)
        {
            // 创建客户端实例（名称、端口等参数从配置获取）
            var client = new TCPClient(device.设备名称, device.设备Port.ToString(), 8192)
            {
                Name = device.设备名称
            };

            // 可扩展Client事件绑定（如Client1的专属处理）
            return client;
        }
        #endregion

        #region 串口设备初始化
        /// <summary>
        /// 初始化串口设备
        /// 流程：读取串口配置 → 检查非空 → 配置串口参数（端口号、波特率等） → 打开串口 → 添加到全局列表
        /// </summary>
        /// <returns>IniInfo对象（初始化结果：成功/失败及描述信息）</returns>
        public IniInfo InitSerialPorts()
        {
            try
            {
                // 获取串口配置列表
                var serialPorts = SerialportSystem.Instance.SerialportList;
                // 检查配置列表是否为空
                var emptyCheck = HardwareInitUtil.CheckEmptyList(serialPorts, "串口");
                if (!emptyCheck.Success) return emptyCheck;

                foreach (var port in serialPorts)
                {
                    // 创建串口读写实例，并配置参数（从配置DTO中读取）
                    var serial = new SerialPortCommunicator(port.COM口, port.波特率, (Parity)port.校验位, port.数据位, (StopBits)port.停止位);
                    // 打开串口（参数1：端口名，参数2：重试次数，原代码固定为1）
                    serial.Open();
                    // 添加到串口列表（全局管理）
                    SerialportSystem.Instance.SerialPorts.Add(serial);
                }

                return new IniInfo(true, "串口初始化成功");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new IniInfo(false, $"串口初始化异常：{ex.Message}");
            }
        }
        #endregion

        #region 连接焊接机


        public IniInfo InitWeldingMachine()
        {
            if (!登录焊接机(SysParm.Instance._httpClient1, "https://192.168.10.105/Services/SystemLogin"))
            {
                Thread.Sleep(1000);
                if (!登出焊接机(SysParm.Instance._httpClient1, "https://192.168.10.105/Services/SystemLogout"))
                {
                    return new IniInfo(false, "焊接机登录失败");
                }
                Thread.Sleep(1000);
                if (!登录焊接机(SysParm.Instance._httpClient1, "https://192.168.10.105/Services/SystemLogin"))
                {
                    return new IniInfo(false, "焊接机登录失败");
                }

            }
            if (!登录焊接机(SysParm.Instance._httpClient2, "https://192.168.10.106/Services/SystemLogin"))
            {
                Thread.Sleep(1000);
                if (!登出焊接机(SysParm.Instance._httpClient2, "https://192.168.10.106/Services/SystemLogout"))
                {
                    return new IniInfo(false, "焊接机登录失败");
                }
                Thread.Sleep(1000);
                if (!登录焊接机(SysParm.Instance._httpClient2, "https://192.168.10.106/Services/SystemLogin"))
                {
                    return new IniInfo(false, "焊接机登录失败");
                }

            }
            Controller.Instance.StationInfo.前焊接机无动作时间记录 = DateTime.Now;
            Controller.Instance.StationInfo.后焊接机无动作时间记录 = DateTime.Now;
            return new IniInfo(true, "焊接机登录成功");
        }

        public  bool 登录焊接机(HttpClient httpClient, string uri)
        {
            Login login = new Login("ADMIN", "123456Aa#", "1234567890QWERTYUIOPASDFGHJKLZXC");
            string jsonData = JsonConvert.SerializeObject(login);
            ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(uri, content).Result;
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().Result;
            var loginResponse = JsonConvert.DeserializeObject<LoginBackup>(responseString);
            if (loginResponse.StatusCode != 0)
            {
                return false;
            }
            return true;
        }


        public bool 登出焊接机(HttpClient httpClient, string uri)
        {
            Post post = new Post(12345);
            string json = JsonConvert.SerializeObject(post);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(uri, content).Result;
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().Result;
            var loginResponse = JsonConvert.DeserializeObject<LogoutBackup>(responseString);
            if (loginResponse.StatusCode != 0)
            {
                return false;
            }
            return true;
        }


        //public bool 设置激活的参数组(string uri)
        //{
        //    Login login = new Login("ADMIN", "123456Aa#", "1234567890QWERTYUIOPASDFGHJKLZXC");
        //    string jsonData = JsonConvert.SerializeObject(login);
        //    ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
        //    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        //    var _httpClient = new HttpClient();
        //    var response =  _httpClient.PostAsync(uri, content).Result;
        //    response.EnsureSuccessStatusCode();
        //    var responseString =  response.Content.ReadAsStringAsync().Result;
        //    var setStore = JsonConvert.DeserializeObject<SetStore>(responseString);
        //    if (setStore.StatusCode != 0)
        //    {
        //        return false;
        //    }
        //    return true;
        //}


        #endregion

        #region 数据库初始化
        public IniInfo InitMesDb()
        {
            try
            {
                try
                {
                    string connString = "Host=192.168.1.13;Port=5432;Database=l15db;Username=postgres;Password=654321;";
                    using (var conn = new Npgsql.NpgsqlConnection(connString))
                    {
                        conn.Open();
                        return new IniInfo(true, "InitMesDb OK");
                    }
                }
                catch (TypeInitializationException ex)
                {
                    System.Text.StringBuilder exMsg = new System.Text.StringBuilder();
                    exMsg.AppendLine("【外层：TypeInitializationException】");
                    exMsg.AppendLine($"消息：{ex.Message}");
                    exMsg.AppendLine("----------------------------------------");

                    if (ex.InnerException is System.Reflection.ReflectionTypeLoadException reflectionEx)
                    {
                        exMsg.AppendLine("【内层：ReflectionTypeLoadException - LoaderExceptions 详情】");
                        int i = 1;
                        foreach (var loaderEx in reflectionEx.LoaderExceptions)
                        {
                            exMsg.AppendLine($"[{i}] 加载失败原因：");
                            exMsg.AppendLine($"  错误消息：{loaderEx.Message}");
                            exMsg.AppendLine($"  错误类型：{loaderEx.GetType().Name}");
                            exMsg.AppendLine($"  堆栈片段：{loaderEx.StackTrace?.Substring(0, 300) ?? "无"}");
                            exMsg.AppendLine("----------------------------------------");
                            i++;
                        }
                    }
                    else
                    {
                        exMsg.AppendLine("【内层异常不是 ReflectionTypeLoadException】");
                        exMsg.AppendLine($"内层消息：{ex.InnerException?.Message}");
                    }
                    return new IniInfo(false, $"InitMesDb error1：{exMsg}");
                }
                catch (Exception ex)
                {
                    return new IniInfo(false, $"InitMesDb error2：{ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return new IniInfo(false, $"InitMesDb error3：{ex.Message}");
            }
        }
        #endregion
    }

    /// <summary>
    /// 线程管理器
    /// 核心作用：统一管理所有后台监控线程的生命周期（启动、停止），确保线程安全与资源释放
    /// 设计思路：通过取消令牌（CancellationToken）控制线程退出，使用集中式锁对象避免资源竞争，提取通用线程启动模板
    /// </summary>
    internal class ThreadManager
    {
        // 线程取消令牌源：用于发送取消信号，通知所有线程优雅退出
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // 锁对象集中管理类：避免锁对象分散定义导致的混乱，提升线程安全可控性
        public static class Locks
        {
            public static readonly object IoLock = new object(); // IO线程锁（保护IO数据读写）
            public static readonly object MotionAxisLock = new object(); // 运动轴线程锁
            public static readonly object RobotLock = new object(); // 机器人线程锁
            public static readonly object PlcLock = new object(); // PLC线程锁
            public static readonly object StatusUpdateLock = new object(); // 状态更新线程锁
            public static readonly object BuzzerLedlock = new object(); // 蜂鸣器LED线程锁
            public static readonly object DrawerLedlock = new object(); // 抽屉LED线程锁
            public static readonly object MemoryCPUlock = new object(); // 内存CPU监控锁
        }

        /// <summary>
        /// 启动所有后台线程
        /// 按功能分类启动线程（IO监控、运动轴监控、机器人通信等），统一调度
        /// </summary>
        public void StartAllThreads()
        {
            //RunIOThread(); // IO卡监控线程
            //RunMotionAxisThread(); // 运动轴监控线程
            RunRobotThread(); // 机器人通信线程
            RunPlcThread(); // PLC通信线程
            RunStatusUpdateThread(); // 系统状态更新线程
            RunHeartbeatThreads(); // 设备心跳监控线程
            Frm_Main.Instance.StartPerformanceMonitoring();     // 启动内存/CPU监控线程（注释为预留）
            RunBuzzerLedThread(); // 预留蜂鸣器/LED控制线程
                                  //  RunDrawerLedThread();
                                  //VisionLib.Initialize(); // 初始化视觉库线程
        }

        /// <summary>
        /// 停止所有线程（优雅退出）
        /// 发送取消信号，释放令牌资源，避免线程泄露
        /// </summary>
        public void StopAllThreads()
        {
            _cts.Cancel(); // 发送取消信号
            _cts.Dispose(); // 释放资源
        }

        // 线程启动方法（按功能分类，调用通用模板）
        private void RunIOThread() => StartThread(IOProcessor.RunIO, Locks.IoLock, 10); // IO线程（10ms间隔）
        private void RunMotionAxisThread() => StartThread(MotionAxisProcessor.RunMotionAxis, Locks.MotionAxisLock, 10); // 运动轴线程（10ms间隔）
        private void RunRobotThread() => StartThread(RobotProcessor.RunRobot, Locks.RobotLock, 100); // 机器人线程（100ms间隔）
        private void RunPlcThread() => StartThread(PlcProcessor.RunPLC, Locks.PlcLock, 10); // PLC线程（10ms间隔）
        private void RunStatusUpdateThread() => StartThread(StatusUpdater.UpdateRunStatus, Locks.StatusUpdateLock, 300); // 状态更新线程（300ms间隔）
        private void RunBuzzerLedThread() => StartThread(SysParm.Instance.BuzzerLed.RunBuzzerLed, Locks.BuzzerLedlock, 1000); // 蜂鸣器LED线程（1s间隔）
        private void RunDrawerLedThread() => StartThread(SysParm.Instance.BuzzerLed.RunSafeLed, Locks.DrawerLedlock, 1000); // 抽屉LED线程（1s间隔）

        /// <summary>
        /// 启动心跳监控线程（设备在线状态检测）
        /// </summary>
        private void RunHeartbeatThreads()
        {
            var PlcHeartbeatLock = new SemaphoreSlim(2, 2);
            var RobotHeartbeatLock = new SemaphoreSlim(1, 1);
            var IoHeartbeatLock = new SemaphoreSlim(1, 1);

            _ = StartThreadAsync(PlcHeartbeatMonitor.RunPlcBeatAsync, PlcHeartbeatLock, 1000);
            //_ = StartThreadAsync(PlcHeartbeatMonitor.RunPlcBeatAsync1, PlcHeartbeatLock, 1000);
            //_ = StartThreadAsync(RobotHeartbeatMonitor.RunRobotBeatAsync, PlcHeartbeatLock, 1000);
            _ = StartThreadAsync(IoHeartbeatMonitor.RunIOBeatAsync, IoHeartbeatLock, 1000);
        }

        /// <summary>
        /// 线程启动模板（提取通用逻辑）
        /// 封装线程创建、延迟、锁保护、异常处理、取消逻辑，避免重复代码
        /// </summary>
        /// <param name="action">线程执行的核心逻辑（带取消令牌参数）</param>
        /// <param name="lockObj">锁对象（用于同步访问共享资源）</param>
        /// <param name="delayMs">线程循环间隔（毫秒）</param>
        private void StartThread(Action<CancellationToken> action, object lockObj, int delayMs)
        {
            // 使用Task.Run创建后台线程（线程池线程）
            _ = Task.Run(() =>
            {
                // 循环执行，直到收到取消信号
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // 带取消的延迟（避免线程空转，且可响应取消信号）
                        Task.Delay(delayMs, _cts.Token).Wait(_cts.Token);
                        // 锁保护：确保共享资源访问的线程安全
                        lock (lockObj)
                        {
                            // 检查程序退出或测试模式，提前退出循环
                            if (Project.Instance.GlobalVariables.IsTestMode || Project.Instance.GlobalVariables.IsProgramExiting)
                                break;
                            // 执行线程核心逻辑（传入取消令牌，支持内部响应取消）
                            action(_cts.Token);
                        }
                    }
                    catch (TaskCanceledException) { break; } // 取消异常：正常退出线程
                    catch (Exception ex)
                    {
                        RLog.SaveError(ex); // 记录其他异常
                    }
                }
            }, _cts.Token);
        }


        /// <summary>
        /// 异步线程启动模板（支持异步方法）
        /// </summary>
        /// <param name="asyncAction">异步执行的核心逻辑（返回Task）</param>
        /// <param name="semaphore">用于异步同步的信号量（替代lock）</param>
        /// <param name="delayMs">循环间隔（毫秒）</param>
        /// <returns>表示异步操作的Task</returns>
        private async Task StartThreadAsync(Func<CancellationToken, Task> asyncAction, SemaphoreSlim semaphore, int delayMs)
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // 异步延迟
                    await Task.Delay(delayMs, _cts.Token).ConfigureAwait(false);

                    // 使用SemaphoreSlim实现异步等待的"锁"
                    await semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);
                    try
                    {
                        if (Project.Instance.GlobalVariables.IsTestMode ||
                            Project.Instance.GlobalVariables.IsProgramExiting)
                            break;

                        // 现在可以安全地在异步上下文中等待
                        await asyncAction(_cts.Token).ConfigureAwait(false);
                    }
                    finally
                    {
                        // 确保释放信号量
                        semaphore.Release();
                    }
                }
                catch (TaskCanceledException)
                {
                    break; // 正常取消
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex);
                }
            }
        }

    }

    /// <summary>
    /// 运动轴监控处理器
    /// 核心作用：周期性监控运动轴状态（伺服状态、运动状态），异常时触发急停或停止轴运动
    /// 仅在ControlMode.PurePC模式下启用
    /// </summary>
    internal static class MotionAxisProcessor
    {
        // 运动状态索引定义（明确魔法数字含义）
        private static class MotionStatusIndex
        {
            public const int IsRunning = 5;    // 索引5：轴是否处于运行中
            public const int IsStopped = 8;    // 索引8：轴是否已停止运动
        }

        // 共享资源访问锁
        private static readonly object _axisDataLock = new object();

        // 新增静态字典用于缓存最近的日志记录，key为日志内容标识，value为最后记录时间
        private static readonly Dictionary<string, DateTime> _recentLogs = new Dictionary<string, DateTime>();
        // 日志记录的最小间隔（例如：5秒）
        private const int LOG_MIN_INTERVAL_MS = 5000;

        #region 模式检查辅助方法

        /// <summary>
        /// 检查当前是否为PurePC控制模式
        /// 所有监控操作前都需要调用此方法验证
        /// </summary>
        /// <returns>true如果是PurePC模式，否则false</returns>
        private static bool IsPurePCMode()
        {
            // 检查控制器和工位信息是否存在，避免空引用异常
            if (Controller.Instance?.StationInfo == null)
                return false;

            return Controller.Instance.StationInfo.currentMode == ControlMode.PurePC;
        }

        #endregion

        /// <summary>
        /// 运动轴监控核心逻辑
        /// 流程：检查模式 → 遍历运动轴 → 检查伺服状态（异常则急停） → 急停/报警状态下停止轴运动
        /// 仅在PurePC模式下执行
        /// </summary>
        /// <param name="token">取消令牌（用于线程退出）</param>
        public static void RunMotionAxis(CancellationToken token)
        {
            // 非PurePC模式下不执行任何监控操作
            if (!IsPurePCMode())
                return;

            // 缓存MotionSystem实例，减少重复调用
            var motionSystem = MotionSystem.Instance;
            var motionAxes = motionSystem.MotionAxes;
            if (motionAxes == null || !motionAxes.Any()) return;

            var motionController = motionSystem.MotionController;

            foreach (var axis in motionAxes)
            {
                // 检查取消信号，退出循环
                if (token.IsCancellationRequested) break;

                try
                {
                    // 更新轴位置（线程安全）
                    var encPos = motionController.GetEncPos(axis);
                    lock (_axisDataLock)
                    {
                        AllAxisPos.SetEncPos((axis.轴卡轴号索引 - 1), encPos);
                    }

                    // 更新轴运行状态（线程安全）
                    bool[] status = motionController.GetMotionSts(axis);
                    lock (_axisDataLock)
                    {
                        AllAxisPos.SetRunningStatus(axis.轴卡轴号索引 - 1, status[MotionStatusIndex.IsRunning]);
                    }

                    // 检查伺服状态（返回1表示异常，触发急停）
                    if (motionController.ServoRead(axis) == 0)
                    {
                        Controller.Instance.StationInfo._初始化状态 = false;

                        if (Controller.Instance.IsMachineEmergencyStopMonitored)
                        {
                            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
                        }
                        else
                        {
                            Controller.Instance.IsMachinePauseMonitored = true;
                        }

                        // 构建唯一日志标识
                        string logKey = $"ServoError_{axis.轴名称}";
                        string logMessage = $"轴 {axis.轴名称} 未处于使能状态，伺服状态异常。已暂停设备运行。";

                        // 检查是否需要记录日志（间隔时间内不重复记录）
                        if (ShouldLog(logKey))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, logMessage);
                        }
                    }

                    if (axis.轴名称 == "前滑台轴")
                    {
                        if (Controller.Instance.StationInfo.前滑台气缸动点M60_1 || Controller.Instance.StationInfo.前滑台气缸动点M60_2)
                        {


                            bool[] motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(axis);
                            if (motionStatus[5]) // 索引5表示"是否停止"，false=未停止
                            {
                                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止轴运动
                            }

                        }
                        if ((Controller.Instance.StationInfo.EncoderPosition_搬运轴 < 30) && (Controller.Instance.StationInfo.搬运轴气缸动点1 || Controller.Instance.StationInfo.搬运轴气缸动点2 || Controller.Instance.StationInfo.搬运轴气缸动点3 || Controller.Instance.StationInfo.搬运轴气缸动点4))
                        {


                            bool[] motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(axis);
                            if (motionStatus[5]) // 索引5表示"是否停止"，false=未停止
                            {
                                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止轴运动
                            }

                        }

                    }

                    if (axis.轴名称 == "后滑台轴")
                    {
                        if (Controller.Instance.StationInfo.后滑台气缸动点M60_1 || Controller.Instance.StationInfo.后滑台气缸动点M60_2)
                        {

                            bool[] motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(axis);
                            if (motionStatus[5]) // 索引5表示"是否停止"，false=未停止
                            {
                                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止轴运动
                            }

                        }
                        if ((Controller.Instance.StationInfo.EncoderPosition_搬运轴 > 175) && (Controller.Instance.StationInfo.搬运轴气缸动点1 || Controller.Instance.StationInfo.搬运轴气缸动点2 || Controller.Instance.StationInfo.搬运轴气缸动点3 || Controller.Instance.StationInfo.搬运轴气缸动点4))
                        {
                            bool[] motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(axis);
                            if (motionStatus[5]) // 索引5表示"是否停止"，false=未停止
                            {
                                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止轴运动
                            }
                        }

                    }

                    if (axis.轴名称 == "搬运轴")
                    {
                        Controller.Instance.StationInfo.EncoderPosition_搬运轴 = MotionSystem.Instance.MotionController.GetEncPos(axis);  // 编码器位置

                        if (Controller.Instance.StationInfo.搬运轴气缸动点1 || Controller.Instance.StationInfo.搬运轴气缸动点2 || Controller.Instance.StationInfo.搬运轴气缸动点3 || Controller.Instance.StationInfo.搬运轴气缸动点4)
                        {
                            bool[] motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(axis);
                            if (motionStatus[5]) // 索引5表示"是否停止"，false=未停止
                            {
                                MotionSystem.Instance.MotionController.SmtStop(axis, 0); // 停止轴运动
                            }
                        }

                    }


                    // 急停/报警状态下，停止所有未停止的轴
                    var machineStatus = MenuProxy.Instance.MachineRunStatus;
                    if (machineStatus == MachineRunStatus.EmergencyStopped
                        || machineStatus == MachineRunStatus.Alarming)
                    {
                        bool[] motionStatus = motionController.GetMotionSts(axis);
                        // 若轴未停止，则发送停止指令
                        if (!motionStatus[MotionStatusIndex.IsStopped])
                        {
                            motionController.SmtStop(axis, 0);

                            // 记录轴停止信息，同样增加间隔控制
                            string stopLogKey = $"AxisStop_{axis.轴名称}_{machineStatus}";
                            string stopMessage = $"轴 {axis.轴名称} 在{machineStatus}状态下已停止运动";

                            if (ShouldLog(stopLogKey))
                            {
                                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue, stopMessage);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 异常日志也增加间隔控制
                    string errorLogKey = $"AxisMonitorError_{axis.轴名称}";
                    if (ShouldLog(errorLogKey))
                    {
                        RLog.SaveError(new Exception($"监控轴[{axis.轴名称}]时发生错误", ex));
                    }
                }
            }

        }

        /// <summary>
        /// 检查是否应该记录日志（控制日志频率）
        /// </summary>
        /// <param name="logKey">日志唯一标识</param>
        /// <returns>是否应该记录</returns>
        private static bool ShouldLog(string logKey)
        {
            lock (_recentLogs)
            {
                DateTime lastLogTime;
                // 检查是否存在该日志的最近记录
                if (_recentLogs.TryGetValue(logKey, out lastLogTime))
                {
                    // 检查是否超过最小间隔时间
                    if ((DateTime.Now - lastLogTime).TotalMilliseconds < LOG_MIN_INTERVAL_MS)
                    {
                        return false; // 不记录
                    }
                }

                // 更新最近记录时间
                _recentLogs[logKey] = DateTime.Now;
                return true; // 记录日志
            }
        }


        // 报警计数器字典，用于跟踪每种报警的连续触发次数
        private static readonly Dictionary<string, int> _alarmCounters = new Dictionary<string, int>();

        // 触发报警并实现频率限制
        private static void TriggerAlarmWithThrottle(
            string alarmKey,
            MachineRunStatus status,
            string message,
            int currentCounter = 0,
            int requiredConfirms = 1,
            Action<int> updateCounter = null)
        {
            // 对于需要多次确认的报警
            if (requiredConfirms > 1)
            {
                int newCounter = currentCounter + 1;
                updateCounter?.Invoke(newCounter);

                // 未达到确认次数则不触发报警
                if (newCounter < requiredConfirms)
                    return;
            }

            // 检查是否在日志间隔内
            if (ShouldLog(alarmKey))
            {
                Controller.Instance.UpdateMachineRunStatus(status);
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, message);
            }
        }

        // 获取指定报警的计数器值
        private static int GetConfirmCounter(string alarmKey)
        {
            lock (_alarmCounters)
            {
                if (!_alarmCounters.ContainsKey(alarmKey))
                {
                    _alarmCounters[alarmKey] = 0;
                }
                return _alarmCounters[alarmKey];
            }
        }

        // 重置指定报警的计数器
        private static void ResetAlarmCounter(string alarmKey)
        {
            lock (_alarmCounters)
            {
                if (_alarmCounters.ContainsKey(alarmKey))
                {
                    _alarmCounters[alarmKey] = 0;
                }
            }
        }
    }

    #region IO卡数据处理处理器
    /// <summary>
    /// IO卡数据处理处理器
    /// 核心作用：周期性读取各类IO卡的输入/输出状态，处理核心业务逻辑（如急停、安全门、振动盘控制等）
    /// 设计思路：按IO卡类型拆分数据读取逻辑，通过辅助方法处理业务规则，提升代码可读性
    /// </summary>
    internal static class IOProcessor
    {
        /// <summary>
        /// IO点索引常量定义（替代魔法数字）
        /// 集中管理IO点的索引值，避免硬编码导致的维护困难（如急停对应索引0，安全门对应索引1等）
        /// </summary>
        private static class IoIndices
        {
            // 急停/STO相关（安全扭矩关闭）   前急停   Front EmergencyStop   后急停 Behind EmergencyStop

            public const int EmergencyStop1 = 0; // 前急停信号索引
            public const int EmergencyStop2 = 4; // 后急停信号索引

            public const int MachineInternalSto = 6; // 机台内部STO索引

            public const int 上下游Sto = 12;

            public const int 后门开关 = 5; // 后门开关索引

            public const int 安全门前1 = 8; // 安全门左索引
            public const int 安全门前2 = 9; // 安全门左索引
            public const int 安全门后1 = 10; // 安全门左索引
            public const int 安全门后2 = 11; // 安全门左索引

            public const int AutoManualSwitch = 7; // 手自动切换索引

            public const int MachineStart = 1; // 机台启动索引
            public const int MachineReset = 2; // 机台复位索引
            public const int MachinePause = 3; // 机台暂停索引

            public const int 前烟雾报警 = 15;
            public const int 后烟雾报警 = 14;

            //光纤检测膜的有无
            public const int 前膜缺料报警 = 10;
            public const int 后膜缺料报警 = 11;


            public const int 搬运模组真空1 = 24;
            public const int 搬运模组真空2 = 25;
            public const int 搬运模组真空3 = 26;
            public const int 搬运模组真空4 = 27;


            public const int 前治具X相推边气缸动点 = 17;
            public const int 前治具Y相夹紧气缸动点 = 19;
            public const int 后治具X相推边气缸动点 = 21;
            public const int 后治具Y相夹紧气缸动点 = 23;



            public const int 搬运模组升降气缸1动点 = 29;
            public const int 搬运模组升降气缸2动点 = 31;
            //M60-2
            public const int 搬运模组升降气缸3动点 = 1;
            public const int 搬运模组升降气缸4动点 = 3;

            public const int 前覆膜模组X相定位气缸1动点 = 13;
            public const int 前覆膜模组X相定位气缸2动点 = 15;
            public const int 后覆膜模组X相定位气缸1动点 = 21;
            public const int 后覆膜模组X相定位气缸2动点 = 23;

            //M60-3
            public const int 切换模组有无检测1 = 8;
            public const int 切换模组有无检测2 = 9;
            public const int 切换模组有无检测3 = 10;
            public const int 切换模组有无检测4 = 11;
            public const int 切换模组有无检测5 = 12;
            public const int 切换模组有无检测6 = 13;

            public const int 切换模组超高对射检测1 = 4;
            public const int 切换模组超高对射检测2 = 5;
            public const int 切换模组超高对射检测3 = 6;




            //M60-4
            public const int 前焊接机报警 = 14;
            public const int 后焊接机报警 = 10;


        }

        /// <summary>
        /// IO线程核心逻辑
        /// 遍历IO卡配置，按类型读取数据，并调用对应处理方法
        /// </summary>
        /// <param name="token">取消令牌（用于线程退出）</param>
        public static void RunIO(CancellationToken token)
        {
            var ioCards = IOSystem.Instance.IOCards;
            if (ioCards == null) return;

            foreach (var card in ioCards)
            {
                if (token.IsCancellationRequested) break; // 响应取消信号

                // 根据卡类型读取IO数据（策略模式：不同卡类型对应不同读取逻辑）
                var ioData = card.卡名称 switch
                {
                    "EC_8003" => ProcessEc8003Card(card),
                    "M60_1" => ProcessM60Card(card),
                    "M60_2" => ProcessM60Card(card),
                    "M60_3" => ProcessM60Card(card),
                    "M60_4" => ProcessM60Card(card),
                    _ => null // 未知卡类型忽略
                };

                // 根据卡类型处理IO数据（核心业务逻辑）
                //if (ioData != null && card.卡名称 == "EC_8003")
                //{
                //    ProcessEc8003IoData(ioData);
                //}
                if (ioData != null && card.卡名称 == "M60_1")
                {
                    ProcessM60_1IoData(ioData); // 预留M60_1卡处理逻辑
                }
                if (ioData != null && card.卡名称 == "M60_2")
                {
                    ProcessM60_2IoData(ioData); // 预留M60_1卡处理逻辑
                }
                if (ioData != null && card.卡名称 == "M60_3")
                {
                    ProcessM60_3IoData(ioData); // 预留M60_1卡处理逻辑
                }
                if (ioData != null && card.卡名称 == "M60_4")
                {
                    ProcessM60_4IoData(ioData); // 预留M60_1卡处理逻辑
                }

            }
        }

        #region 不同类型IO卡数据读取
        /// <summary>
        /// 处理EC8003卡数据（EtherCAT总线IO卡）
        /// 调用lctdevice库读取输入/输出字节数组，转换为bool列表
        /// </summary>
        /// <param name="card">IO卡配置DTO</param>
        /// <returns>IoCardStatus对象</returns>
        private static IoCardStatus ProcessEc8003Card(IOCardDto card)
        {
            int Get_DI_ByteCnt = 0; // 返回DI的通道个数（字节数）
            int Get_DO_ByteCnt = 0; // 返回DO的通道个数（字节数）

            var ioData = new IoCardStatus { CardName = card.卡名称 };
            // 读取输入：根据配置中的输入点数量创建缓冲区
            int diCount = IOSystem.Instance.IOCardDios
                .Count(d => d.点位类型 == "输入" && d.卡名称 == card.卡名称);
            byte[] diBuffer = new byte[diCount];
            ecat_io.EC_Get_Digital_In(ref diBuffer[0], ref Get_DI_ByteCnt, (short)card.卡号索引);
            // 转换为bool列表（每个字节表示一个IO点状态）
            ioData.DiStatus.AddRange(diBuffer.Select(b => Convert.ToBoolean(b)));

            // 读取输出：根据配置中的输出点数量创建缓冲区
            int doCount = IOSystem.Instance.IOCardDios
                .Count(d => d.点位类型 == "输出" && d.卡名称 == card.卡名称);
            byte[] doBuffer = new byte[doCount];
            ecat_io.EC_Get_Digital_Out(ref doBuffer[0], ref Get_DO_ByteCnt, (short)card.卡号索引);
            ioData.DoStatus.AddRange(doBuffer.Select(b => Convert.ToBoolean(b)));

            return ioData;
        }

        /// <summary>
        /// 处理M60卡数据（EtherCAT总线IO卡）
        /// 调用lctdevice库读取输入/输出字节数组，转换为bool列表
        /// </summary>
        /// <param name="card">IO卡配置DTO</param>
        /// <returns>IoCardStatus对象</returns>
        private static IoCardStatus ProcessM60Card(IOCardDto card)
        {
            var ioData = new IoCardStatus { CardName = card.卡名称 };
            // 读取输入端口值
            uint inValue = 0;
            uint outValue = 0;
            int chnBegin = (card.卡号索引 - 1) * 32 + 1;

            ecat_motion.M_Get_Digital_Port_Input((short)chnBegin, ref inValue, 0);
            ioData.DiStatus.AddRange(ConvertToBoolList(inValue));
            // 读取输出端口值
            ecat_motion.M_Get_Digital_Port_Output((short)chnBegin, ref outValue, 0);
            ioData.DoStatus.AddRange(ConvertToBoolList(outValue));
            return ioData;
        }
        #endregion

        #region EC8003卡数据处理（核心业务逻辑）
        /// <summary>
        /// 处理EC8003卡的IO数据（状态更新、报警等）
        /// 解析IO输入状态，更新机台运行状态（急停、STO、手自动模式等）
        /// </summary>
        /// <param name="ioData">IO数据对象（包含输入/输出状态）</param>
        private static void ProcessEc8003IoData(IoCardStatus ioData)
        {
        }
        #endregion

        #region M60卡数据处理（核心业务逻辑）
        /// <summary>
        /// 处理M60卡的IO数据（状态更新、报警等）
        /// 解析IO输入状态，更新机台运行状态（急停、STO、手自动模式等）
        /// </summary>
        /// <param name="ioData">IO数据对象（包含输入/输出状态）</param>
        private static void ProcessM60_1IoData(IoCardStatus ioData)
        {
            // 1. 机台急停信号处理
            if (!ioData.DiStatus[IoIndices.EmergencyStop1] || !ioData.DiStatus[IoIndices.EmergencyStop2]) // 急停信号为false表示触发急停
            {
                // 切换到UI线程更新状态（避免跨线程操作异常）
                WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                {
                    if (MenuProxy.Instance.MachineRunStatus != MachineRunStatus.EmergencyStopped)
                    {
                        Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
                    }
                });
            }

            // 2. STO监控处理（安全扭矩关闭：紧急情况下切断电机扭矩）
            Controller.Instance.IsMachineInternalStoMonitored = ioData.DiStatus[IoIndices.MachineInternalSto];
            // Controller.Instance.IsMachine上下游StoMonitored = ioData.DiStatus[IoIndices.上下游Sto];
            // Controller.Instance.IsMachine下料StoMonitored = ioData.DiStatus[IoIndices.上下游Sto];
            // 初始化状态下，若STO触发且非急停/复位状态，则切换到报警状态
            if (Controller.Instance.IsMachineInitializingMonitored &&
                (MenuProxy.Instance.MachineRunStatus != MachineRunStatus.EmergencyStopped || MenuProxy.Instance.MachineRunStatus != MachineRunStatus.Reseting))
            {
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.Alarming);
            }

            // 3. 手自动模式切换（根据IO点状态更新运行模式）
            MenuProxy.Instance.MachineRunModel = ioData.DiStatus[IoIndices.AutoManualSwitch]
                ? MachineMode.Auto // true=自动模式
                : MachineMode.Manual; // false=手动模式

            // 4. 启动/复位/暂停监控（更新控制器状态）
            Controller.Instance.IsMachineStartMonitored = ioData.DiStatus[IoIndices.MachineStart] && Controller.Instance.IsSafetyDoorStatusMonitored;

            //复位
            if (ioData.DiStatus[IoIndices.MachineReset]) // 复位信号触发
                Controller.Instance.IsMachineResetMonitored = true;



            switch (MenuProxy.Instance.MachineRunModel)
            {
                case MachineMode.Manual:
                    Controller.Instance.IsMachinePauseMonitored = !ioData.DiStatus[IoIndices.MachinePause];
                    break;
                case MachineMode.Auto:
                    if (!Controller.Instance.前站安全门状态监控 || !Controller.Instance.后站安全门状态监控)
                    {
                        Controller.Instance.IsMachinePauseMonitored = false;
                    }
                    else
                    {
                        Controller.Instance.IsMachinePauseMonitored = !ioData.DiStatus[IoIndices.MachinePause];
                    }
                    break;
            }


            // 5. 安全门与光栅状态（更新控制器状态）  安全门正常
            Controller.Instance.IsSafetyDoorStatusMonitored = (ioData.DiStatus[IoIndices.安全门前1] && ioData.DiStatus[IoIndices.安全门前2]) && (ioData.DiStatus[IoIndices.安全门后1] && ioData.DiStatus[IoIndices.安全门后2]);

            //6.安全门锁
            Controller.Instance.IsSafetyDoorLockMonitored = !ioData.DiStatus[IoIndices.后门开关];



            //烟雾报警
            Controller.Instance.烟雾报警 = (ioData.DiStatus[IoIndices.前烟雾报警] || ioData.DiStatus[IoIndices.后烟雾报警]);

            Controller.Instance.StationInfo.前滑台气缸动点M60_1 = ioData.DiStatus[IoIndices.前治具X相推边气缸动点] || ioData.DiStatus[IoIndices.前治具Y相夹紧气缸动点];


            Controller.Instance.StationInfo.后滑台气缸动点M60_1 = ioData.DiStatus[IoIndices.后治具X相推边气缸动点] || ioData.DiStatus[IoIndices.后治具Y相夹紧气缸动点];


            Controller.Instance.StationInfo.搬运轴气缸动点1 = ioData.DiStatus[IoIndices.搬运模组升降气缸1动点];

            Controller.Instance.StationInfo.搬运轴气缸动点2 = ioData.DiStatus[IoIndices.搬运模组升降气缸2动点];

        }
        private static void ProcessM60_2IoData(IoCardStatus ioData)
        {
            //膜缺料报警
            Controller.Instance.覆膜机缺料报警 = !(ioData.DiStatus[IoIndices.前膜缺料报警]) || !(ioData.DiStatus[IoIndices.后膜缺料报警]);


            Controller.Instance.StationInfo.前滑台气缸动点M60_2 = ioData.DiStatus[IoIndices.前覆膜模组X相定位气缸1动点] || ioData.DiStatus[IoIndices.前覆膜模组X相定位气缸2动点];


            Controller.Instance.StationInfo.后滑台气缸动点M60_2 = ioData.DiStatus[IoIndices.后覆膜模组X相定位气缸1动点] || ioData.DiStatus[IoIndices.后覆膜模组X相定位气缸2动点];


            Controller.Instance.StationInfo.搬运轴气缸动点3 = ioData.DiStatus[IoIndices.搬运模组升降气缸3动点];

            Controller.Instance.StationInfo.搬运轴气缸动点4 = ioData.DiStatus[IoIndices.搬运模组升降气缸4动点];

        }
        private static void ProcessM60_3IoData(IoCardStatus ioData)
        {

            ////判断治具哪个位置为空
            //if (!ioData.DiStatus[IoIndices.切换模组有无检测1] && !ioData.DiStatus[IoIndices.切换模组有无检测2])
            //{
            //    Controller.Instance.StationInfo.治具位空位 = 1;
            //}
            //else if (!ioData.DiStatus[IoIndices.切换模组有无检测3] && !ioData.DiStatus[IoIndices.切换模组有无检测4])
            //{
            //    Controller.Instance.StationInfo.治具位空位 = 2;
            //}
            //else if (!ioData.DiStatus[IoIndices.切换模组有无检测5] && !ioData.DiStatus[IoIndices.切换模组有无检测6])
            //{
            //    Controller.Instance.StationInfo.治具位空位 = 3;
            //}
            //else
            //{
            //    Controller.Instance.StationInfo.治具位空位 = 4;
            //}
        }

        private static void ProcessM60_4IoData(IoCardStatus ioData)
        {
            Controller.Instance.超声波焊接报警 = (ioData.DiStatus[IoIndices.前焊接机报警] || ioData.DiStatus[IoIndices.后焊接机报警]);
        }

        #endregion


        #region 辅助方法
        /// <summary>
        /// 将uint转换为bool列表（IO数据通用转换）
        /// 用于将硬件接口返回的整数值（表示多个IO点的状态）转换为bool列表（每个元素对应一个IO点）
        /// </summary>
        /// <param name="value">硬件接口返回的uint值</param>
        /// <param name="invert">是否取反（适应硬件电平逻辑，如低电平有效时需取反）</param>
        /// <returns>bool列表（每个元素对应一个IO点的状态）</returns>
        private static List<bool> ConvertToBoolList(uint value, bool invert = false)
        {
            byte[] bytes = BitConverter.GetBytes(value); // 转换为字节数组
            var bitArray = new BitArray(bytes); // 转换为位数组
            var result = bitArray.OfType<bool>().ToList(); // 转换为bool列表
            if (invert) result = result.Select(b => !b).ToList(); // 按需取反
            return result;
        }
        #endregion
    }

    /// <summary>
    /// IO心跳监控器静态类（异步版）
    /// 核心功能：
    /// 1. 周期性翻转指定IO点的状态（0→1→0循环），生成可识别的心跳信号
    /// 2. 监控安全继电器和Epson安全信号的通信链路完整性
    /// 3. 异常时记录错误信息，保障系统安全运行
    /// 设计模式：
    /// - 静态类设计：提供全局唯一的IO心跳监控功能
    /// - 翻转值机制：通过状态反转生成可验证的心跳信号
    /// - 异步操作：使用非阻塞IO操作提升系统响应性
    /// 仅在ControlMode.PurePC模式下启用
    /// </summary>
    internal static class IoHeartbeatMonitor
    {
        #region 私有成员变量
        // 心跳状态变量：记录上次发送的IO值
        private static bool _ioHeartbeat1 = false;
        private static bool _ioHeartbeat2 = false;

        // 异步同步对象：替代lock支持异步等待
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        #endregion

        #region 模式检查辅助方法

        /// <summary>
        /// 检查当前是否为PurePC控制模式
        /// 所有IO心跳操作前都需要调用此方法验证
        /// </summary>
        /// <returns>true如果是PurePC模式，否则false</returns>
        private static bool IsPurePCMode()
        {
            // 检查控制器和工位信息是否存在，避免空引用异常
            if (Controller.Instance?.StationInfo == null)
                return false;

            return Controller.Instance.StationInfo.currentMode == ControlMode.PurePC;
        }

        #endregion

        #region 公共方法
        /// <summary>
        /// 执行IO心跳检测逻辑（异步版）
        /// 功能：根据系统配置，对指定IO点执行心跳检测（状态翻转操作）
        /// 仅在PurePC模式下执行
        /// </summary>
        /// <param name="token">取消令牌，用于外部控制任务终止</param>
        public static async Task RunIOBeatAsync(CancellationToken token)
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            // 空引用保护：检查Controller实例及监控开关状态
            if (Controller.Instance?.IsMachineInternalStoMonitored ?? false)
            {
                // 对"安全继电器复位"点位执行异步心跳检查
                await CheckIOAsync("安全继电器复位", 1, token).ConfigureAwait(false);
            }

            // 可扩展：Epson安全复位信号的心跳检测
            // if (需要监控Epson的条件)
            // {
            //     await CheckIOAsync("Epson安全复位", 2, token).ConfigureAwait(false);
            // }
        }
        #endregion

        #region IO操作方法
        /// <summary>
        /// IO心跳检查通用模板方法（异步版）
        /// 执行流程：
        /// 1. 参数验证与空引用保护
        /// 2. 获取目标IO点及对应IO卡的配置信息
        /// 3. 计算翻转值并执行异步IO写操作
        /// 4. 线程安全地更新本地状态记录
        /// 仅在PurePC模式下执行
        /// </summary>
        /// <param name="pointName">IO点位名称</param>
        /// <param name="heartbeatId">心跳标识（1或2，用于区分不同心跳变量）</param>
        /// <param name="token">取消令牌</param>
        private static async Task CheckIOAsync(string pointName, int heartbeatId, CancellationToken token)
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            if (string.IsNullOrWhiteSpace(pointName) || IOSystem.Instance == null)
                return;

            // 获取IO点配置信息
            var ioCardDio = IOSystem.Instance.IOCardDios
                .FirstOrDefault(s => s.点位名称 == pointName);
            if (ioCardDio == null) return;

            // 获取IO卡配置信息
            var ioCard = IOSystem.Instance.IOCards
                .FirstOrDefault(s => s.卡名称 == ioCardDio.卡名称);
            if (ioCard == null) return;

            // 异步获取当前心跳状态（线程安全）
            bool currentValue;
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                currentValue = heartbeatId == 1 ? _ioHeartbeat1 : _ioHeartbeat2;
            }
            finally
            {
                _semaphore.Release();
            }

            // 计算需要写入的翻转值
            bool newValue = !currentValue;

            // 执行异步IO操作
            await ExecuteIOOperationAsync(ioCard, ioCardDio, newValue, token).ConfigureAwait(false);

            // 异步更新心跳状态（线程安全）
            await _semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (heartbeatId == 1)
                    _ioHeartbeat1 = newValue;
                else
                    _ioHeartbeat2 = newValue;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 执行具体的IO操作（异步版，按卡类型分发）
        /// 功能：调用对应硬件库的异步方法写入翻转值
        /// 仅在PurePC模式下执行
        /// </summary>
        private static async Task ExecuteIOOperationAsync(dynamic ioCard, dynamic ioCardDio, bool newValue, CancellationToken token)
        {
            // 非PurePC模式下不执行任何操作
            if (!IsPurePCMode())
                return;

            try
            {
                int writeValue = newValue ? 1 : 0;
                string cardType = ioCard.卡类型;

                // 根据不同卡类型执行对应的异步写操作
                switch (cardType)
                {
                    // 3400/3600/3800/5800系列：使用异步版本的LTDMC库函数
                    case "3400":
                    case "3600":
                    case "3800":
                    case "5800":
                        await Task.Run(() =>
                            LTDMC.dmc_write_outbit(
                                ioCard.卡号索引,
                                ioCardDio.点位索引,
                                (ushort)writeValue
                            ), token).ConfigureAwait(false);
                        break;

                    // 7230/7432系列：使用异步版本的DASK库函数
                    case "7230":
                    case "7432":
                        await Task.Run(() =>
                            DASK.DO_WriteLine(
                                ioCard.卡号索引,
                                0,
                                ioCardDio.点位索引,
                                (ushort)(1 - writeValue)
                            ), token).ConfigureAwait(false);
                        break;

                    // M60系列：使用异步版本的ecat_motion库函数
                    case "PCIe-M60":
                        short m60Chn = (short)(32 * (ioCard.卡号索引 - 1) + ioCardDio.点位索引 + 1);
                        await Task.Run(() =>
                            ecat_motion.M_Set_Digital_Chn_Output(
                                m60Chn,
                                (short)writeValue,
                                0
                            ), token).ConfigureAwait(false);
                        break;

                    // EC_8003系列：使用异步版本的ecat_io库函数
                    case "EC_8003":
                        await Task.Run(() =>
                            ecat_io.EC_Set_Digital_Chn_Out(
                                1,
                                (short)(ioCardDio.点位索引 + 1),
                                (short)writeValue,
                                (short)ioCard.卡号索引
                            ), token).ConfigureAwait(false);
                        break;

                    default:
                        break;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 正常取消，不记录
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"IO心跳操作异常: {ex.Message}");
            }
        }
        #endregion
    }

    #endregion

    #region PLC通信处理器
    /// <summary>
    /// PLC通信处理器
    /// 核心作用：周期性与PLC进行Modbus通信，双向处理信号（读取输入控制指令、写入输出状态反馈）
    /// 设计思路：
    /// 1. 采用分层设计，按设备→信号类型→数据类型三级处理，提高代码可维护性
    /// 2. 集成实时数据管理器(ModbusDioRealTimeManager)统一管理信号数据，确保系统数据一致性
    /// 3. 完善的异常处理机制，单个信号处理失败不影响整体流程
    /// 4. 与机器人通信模块保持架构一致，降低跨模块维护成本
    /// </summary>
    internal static class PlcProcessor
    {
        /// <summary>
        /// 日志记录委托
        /// 设计目的：解耦日志框架，支持外部注入自定义日志实现（如NLog、Serilog）
        /// 默认实现：控制台输出，包含时间戳便于调试
        /// </summary>
        public static Action<string> LogAction { get; set; } = message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

        /// <summary>
        /// 实时数据管理器初始化标记
        /// 实现方式：Lazy<T>延迟初始化，确保线程安全且仅在首次使用时加载
        /// 作用：避免系统启动时不必要的资源消耗，初始化失败时记录日志并返回false
        /// </summary>
        private static readonly Lazy<bool> _realtimeManagerInitialized = new Lazy<bool>(() =>
        {
            try
            {
                // 仅在未初始化时执行初始化（防止重复初始化）
                if (!ModbusDioRealTimeManager.IsInitialized)
                {
                    ModbusDioRealTimeManager.Initialize();
                }
                LogAction("实时数据管理器初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                LogAction($"实时数据管理器初始化失败：{ex.Message}");
                return false;
            }
        });


        /// <summary>
        /// PLC监控核心逻辑入口
        /// 执行流程：
        /// 1. 检查实时数据管理器初始化状态，失败则终止
        /// 2. 记录线程启动日志
        /// 3. 调用批量处理方法处理所有PLC设备
        /// 4. 捕获并记录批量处理中的异常
        /// 5. 线程结束时记录日志
        /// </summary>
        /// <param name="token">取消令牌，用于外部控制线程退出（如程序关闭时）</param>
        public static void RunPLC(CancellationToken token)
        {
            // 初始化检查：实时数据管理器是核心依赖，未初始化则无法运行
            if (!_realtimeManagerInitialized.Value)
            {
                LogAction("实时数据管理器未初始化，PLC监控无法启动");
                return;
            }

            LogAction("PLC监控线程启动");

            try
            {
                // 批量处理所有PLC设备（内部已实现异常隔离）
                ProcessAllPlcDevices();
            }
            catch (Exception ex)
            {
                // 捕获批量处理过程中的顶层异常（如配置获取失败等）
                LogAction($"PLC批量处理异常：{ex.Message}");
            }

            LogAction("PLC监控线程已停止");
        }

        /// <summary>
        /// 批量处理所有PLC设备
        /// 核心职责：
        /// 1. 从系统获取PLC设备配置列表
        /// 2. 过滤出类型为"PLC"的设备
        /// 3. 遍历设备并调用单设备处理方法
        /// 4. 对单个设备的处理异常进行隔离，不影响其他设备
        /// </summary>
        private static void ProcessAllPlcDevices()
        {
            // 从Modbus系统单例获取设备配置（考虑到可能为null，做安全检查）
            var modbusDevices = ModbusSystem.Instance?.ModbusCommunications;
            if (modbusDevices == null || !modbusDevices.Any())
            {
                LogAction("未找到PLC设备配置，跳过处理");
                return;
            }

            // 遍历所有PLC类型设备，使用Where过滤避免在循环内判断
            foreach (var device in modbusDevices.Where(d => d.设备类型 == "PLC"))
            {
                try
                {
                    // 处理单个设备，异常在此处捕获，确保循环继续
                    ProcessPlcDevice(device);
                }
                catch (Exception ex)
                {
                    LogAction($"处理设备[{device.设备名称}]异常：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理单个PLC设备数据
        /// 执行流程：
        /// 1. 校验设备配置有效性
        /// 2. 获取对应PLC通信实例并校验
        /// 3. 检查连接状态，断开则尝试重连
        /// 4. 从实时管理器获取该设备的点位配置
        /// 5. 按输入/输出类型分组处理信号
        /// </summary>
        /// <param name="device">PLC设备配置DTO，包含名称、IP、端口、站号等信息</param>
        private static void ProcessPlcDevice(ModbusCommunicationDto device)
        {
            // 空值检查：避免传入null导致后续异常
            if (device == null)
            {
                LogAction("设备配置为空，跳过处理");
                return;
            }

            // 获取PLC通信实例：从系统维护的PLC列表中匹配设备名称
            var plc = ModbusSystem.Instance.PlcModbusList
                .FirstOrDefault(p => p.DeviceName == device.设备名称);

            if (plc == null)
            {
                LogAction($"未找到设备[{device.设备名称}]的PLC通信实例");
                return;
            }

            // 连接状态检查与重连机制：确保通信链路有效
            if (!plc.IsConnected)
            {
                LogAction($"设备[{device.设备名称}]未连接，尝试重连...");
                // 调用连接方法，参数来自设备配置
                plc.Connect(PLC_Modbus.PLCType.H5u, device.设备IP, device.设备Port, device.站号);
                // 重连失败则跳过本次处理
                if (!plc.IsConnected)
                {
                    LogAction($"设备[{device.设备名称}]重连失败，跳过本次处理");
                    return;
                }
            }

            // 获取点位配置：从实时管理器获取该设备的所有点位定义（输入/输出）
            var deviceDios = ModbusDioRealTimeManager.GetDeviceConfigByDevice(device.设备名称);
            if (!deviceDios.Any())
            {
                LogAction($"设备[{device.设备名称}]未配置任何点位，跳过处理");
                return;
            }

            // 按信号类型分组：减少循环内的条件判断，提高处理效率
            var inputDios = deviceDios.Where(d => d.点位类型 == "输入").ToList();
            var outputDios = deviceDios.Where(d => d.点位类型 == "输出").ToList();

            // 分别处理输入和输出信号
            ProcessInputSignals(plc, inputDios);
            ProcessOutputSignals(plc, outputDios);
        }

        /// <summary>
        /// 批量处理输入信号（从PLC读取数据到系统）
        /// 核心逻辑：
        /// 1. 遍历输入信号列表
        /// 2. 按数据类型（int16/bool）分发到对应处理方法
        /// 3. 捕获单个信号处理异常，确保其他信号正常处理
        /// </summary>
        /// <param name="plc">PLC通信实例，提供读取方法</param>
        /// <param name="inputDios">输入信号配置列表</param>
        private static void ProcessInputSignals(PLC_Modbus plc, List<ModbusCommunicationDioDto> inputDios)
        {
            foreach (var dio in inputDios)
            {
                try
                {
                    // 按数据类型分发：便于扩展新类型（如float）
                    if (dio.数据类型 == "int16")
                    {
                        ProcessInt16InputSignal(plc, dio);
                    }
                    else if (dio.数据类型 == "bool")
                    {
                        ProcessBoolInputSignal(plc, dio);
                    }
                    else
                    {
                        LogAction($"未知数据类型[{dio.数据类型}]，点位：{dio.设备名称}_{dio.点位索引}");
                    }
                }
                catch (Exception ex)
                {
                    // 隔离单个信号的处理异常
                    LogAction($"处理输入信号[{dio.点位名称}]异常：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理int16类型输入信号
        /// 流程：
        /// 1. 调用PLC读取方法获取信号值
        /// 2. 校验读取结果，失败则记录日志并标记数据无效
        /// 3. 调用业务逻辑方法处理信号值
        /// 4. 将有效数据同步至实时数据管理器
        /// </summary>
        /// <param name="plc">PLC通信实例</param>
        /// <param name="dio">信号配置（包含点位索引、名称等）</param>
        private static void ProcessInt16InputSignal(PLC_Modbus plc, ModbusCommunicationDioDto dio)
        {
            // 读取int16类型信号（通过Modbus协议）
            var readResult = plc.ReadInt16(dio.点位索引);
            if (!readResult.IsSuccess)
            {
                LogAction($"读取int16信号[{dio.点位名称}]失败：{readResult.Message}");
                return;
            }

            short value = readResult.Content;
            // 调用业务逻辑处理（与数据存储分离）
            ProcessInt16Input(dio.点位名称, value);
            // 同步至实时数据管理器，供系统其他模块使用
            ModbusDioRealTimeManager.UpdateRealTimeValue(dio.设备名称, dio.点位索引, value);
        }

        /// <summary>
        /// 处理bool类型输入信号
        /// 流程与int16版本一致，针对bool类型做特殊处理
        /// </summary>
        /// <param name="plc">PLC通信实例</param>
        /// <param name="dio">信号配置</param>
        private static void ProcessBoolInputSignal(PLC_Modbus plc, ModbusCommunicationDioDto dio)
        {
            var readResult = plc.ReadBool(dio.点位索引);
            if (!readResult.IsSuccess)
            {
                LogAction($"读取bool信号[{dio.点位名称}]失败：{readResult.Message}");
                return;
            }

            bool value = readResult.Content;
            ProcessBoolInput(dio.点位类型, dio.点位名称, value);
            ModbusDioRealTimeManager.UpdateRealTimeValue(dio.设备名称, dio.点位索引, value);
        }

        /// <summary>
        /// 批量处理输出信号（从系统写入数据到PLC）
        /// 逻辑与输入处理类似，目前仅支持int16类型，可扩展
        /// </summary>
        /// <param name="plc">PLC通信实例，提供写入方法</param>
        /// <param name="outputDios">输出信号配置列表</param>
        private static void ProcessOutputSignals(PLC_Modbus plc, List<ModbusCommunicationDioDto> outputDios)
        {
            foreach (var dio in outputDios)
            {
                try
                {
                    if (dio.数据类型 == "int16")
                    {
                        ProcessInt16OutputSignal(plc, dio);
                    }
                    // 可扩展支持其他类型（如bool、float）
                    // else if (dio.数据类型 == "bool") { ... }
                }
                catch (Exception ex)
                {
                    LogAction($"处理输出信号[{dio.点位名称}]异常：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理int16类型输出信号
        /// 设计要点：
        /// 1. 仅处理已知信号（通过点位名称匹配），避免误写未知信号
        /// 2. 写入结果校验，成功则同步至实时管理器
        /// 3. 失败则记录日志，便于排查通信问题
        /// </summary>
        /// <param name="plc">PLC通信实例</param>
        /// <param name="dio">输出信号配置</param>
        private static void ProcessInt16OutputSignal(PLC_Modbus plc, ModbusCommunicationDioDto dio)
        {
            // 获取信号值及是否已知的标记（使用元组返回多个值）
            var (value, isKnownSignal) = GetOutputSignalValue(dio.点位名称);

            // 过滤未知信号，防止误操作
            if (!isKnownSignal)
            {
                LogAction($"跳过未知输出信号[{dio.点位名称}]，不执行写入");
                return;
            }

            // 执行写入操作
            var writeResult = plc.WriteInt16(value, dio.点位索引);

            if (writeResult.IsSuccess)
            {
                // 写入成功后更新实时数据
                ModbusDioRealTimeManager.UpdateRealTimeValue(dio.设备名称, dio.点位索引, value);
            }
            else
            {
                LogAction($"写入int16信号[{dio.点位名称}]失败：{writeResult.Message}");
            }
        }

        /// <summary>
        /// 获取输出信号值（业务逻辑映射）
        /// 作用：将系统状态（如运行中、报警中）转换为PLC可识别的信号值（0/1）
        /// 返回值：元组(信号值, 是否为已知信号)，便于上层处理
        /// </summary>
        /// <param name="pointName">信号点位名称（与PLC配置对应）</param>
        /// <returns>包含信号值和是否已知的元组</returns>
        private static (short Value, bool IsKnown) GetOutputSignalValue(string pointName)
        {
            // 使用switch表达式实现清晰的映射关系
            return pointName switch
            {
                "手动暂停" => (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Paused ? (short)1 : (short)0, true),
                "软件报警" => (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming ? (short)1 : (short)0, true),
                "初始化完成" => (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.HomeSuccess ? (short)1 : (short)0, true),
                "自动运行中" => (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Running ? (short)1 : (short)0, true),

                //"前滑台允许放料" => MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Running ? (short)1 : (short)0,
                //"前滑台清放料完成信号" => MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Running ? (short)1 : (short)0,

                _ => (0, false) // 未知信号：值为0，标记为未知
            };
        }

        /// <summary>
        /// 处理int16类型输入信号的业务逻辑
        /// 作用：将PLC输入的控制指令（如启动、暂停）转换为系统状态更新
        /// 设计：按点位名称和运行模式（自动/手动）做分支处理
        /// </summary>
        /// <param name="pointName">信号名称（对应PLC控制指令）</param>
        /// <param name="value">信号值（1表示有效，0表示无效）</param>
        private static void ProcessInt16Input(string pointName, short value)
        {
            var runModel = MenuProxy.Instance.MachineRunModel;
           
            // 使用带条件的switch分支，清晰区分不同模式下的指令处理
            switch (pointName)
            {
                //case "启动" when runModel == MachineMode.Auto:
                //    Controller.Instance.IsMachineStartMonitored = value == 1;
                //    if (value == 1&& MenuProxy.Instance.MachineRunStatus==MachineRunStatus.HomeSuccess)
                //    {
                //        Controller.Instance.StartStation();
                //    }
                    
                //    break;
                //case "复位" when runModel == MachineMode.Auto:
                //    Controller.Instance.IsMachineResetMonitored = value == 1;
                //    if (value == 1 && Controller.Instance.IsMachinePauseMonitored == true)
                //    {
                //        if (Controller.Instance.StationInfo.CachedMachineRunStatus == MachineRunStatus.EmergencyStopped)
                //        {
                //            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.Reseting);
                //        }
                //        else
                //        {
                //            Controller.Instance.UpdateMachineRunStatus(Controller.Instance.StationInfo.CachedMachineRunStatus);
                //        }
                //    }
                //    break;
                //case "暂停" when runModel == MachineMode.Auto:
                //    MenuProxy.Instance.MachineRunStatus = value == 1 ? MachineRunStatus.Paused : MachineRunStatus.Running;
                //    Controller.Instance.IsMachinePauseMonitored = value == 1;
                //    break;
                //case "急停" when runModel == MachineMode.Auto:
                //    Controller.Instance.IsMachineEmergencyStopMonitored = value == 1;
                //    // 急停复位时重置系统状态
                //    if (value != 1 && MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                //    {
                //        Project.Instance.Controller.Reset();
                //    }
                //    break;
                //case "手自动":
                //    // 切换自动/手动模式
                //    MenuProxy.Instance.MachineRunModel = value == 1 ? MachineMode.Auto : MachineMode.Manual;
                //    break;
                //case "初始化" when runModel == MachineMode.Auto:
                //    Controller.Instance.IsMachineInitializingMonitored = value == 1;
                //    if (value == 1 && MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Reseting)
                //    {
                //        Controller.Instance.InitStation();
                //    }
                //    break;



                case "呼叫组装":
                    if (value==1)
                    {
                        Controller.Instance.StationInfo.皮带线允许组装 = 1;
                    }
                    break;


            }
        }

        /// <summary>
        /// 处理bool类型输入信号的业务逻辑
        /// 目前仅预留框架，可根据实际需求扩展
        /// </summary>
        /// <param name="pointDefinition">信号组定义（如"左Epson"）</param>
        /// <param name="pointName">信号名称（如"非安全区域状态"）</param>
        /// <param name="value">信号值（true/false）</param>
        private static void ProcessBoolInput(string pointDefinition, string pointName, bool value)
        {
            switch (pointDefinition)
            {
                case "Epson2" when pointName == "非安全区域状态":
                    // 左机械臂非安全区域状态处理逻辑
                    break;
                case "Epson1" when pointName == "非安全区域状态":
                    // 右机械臂非安全区域状态处理逻辑
                    break;
                    // 可扩展其他bool类型信号处理
            }
        }
    }


    /// <summary>
    /// PLC心跳监控器（异步版）
    /// 核心作用：周期性验证PLC通信链路可用性，异常时自动重连
    /// 设计亮点：
    /// 1. 采用"翻转值"机制（0→1→0循环）验证通信双向性，比单纯发送固定值更可靠
    /// 2. 全异步操作（async/await），避免IO阻塞导致的线程浪费
    /// 3. 集成实时数据管理器，统一维护心跳状态，支持系统全局查询
    /// 4. 异常时自动触发重连，提升系统自愈能力
    /// </summary>
    internal static class PlcHeartbeatMonitor
    {
        /// <summary>
        /// PLC心跳监控核心逻辑入口（异步）
        /// 目前监控固定PLC（"H5u-1"）和点位（"8"），可扩展为配置驱动
        /// </summary>
        /// <param name="token">取消令牌，用于外部终止监控线程</param>
        /// <returns>异步任务</returns>
        public static async Task RunPlcBeatAsync(CancellationToken token)
        {
            // 检查指定PLC的心跳，ConfigureAwait(false)避免上下文切换提升性能
            //await CheckPlcAsync("H5u-1", "81", token).ConfigureAwait(false);
            //await CheckPlcAsync("H5u-2", "592", token).ConfigureAwait(false);

            await Task.WhenAll(CheckPlcAsync("H5u-1", "81", token), CheckPlcAsync("H5u-2", "592", token)).ConfigureAwait(false);

        }

        /// <summary>
        /// PLC心跳检查通用方法（异步）
        /// 完整流程：
        /// 1. 验证PLC实例和实时管理器状态
        /// 2. 从管理器获取当前心跳值
        /// 3. 计算翻转后的值并写入PLC
        /// 4. 读取PLC响应值并验证一致性
        /// 5. 更新管理器状态，异常时触发重连
        /// 6. 完善的异常处理（含取消信号）
        /// </summary>
        /// <param name="plcName">PLC设备名称（用于匹配实例）</param>
        /// <param name="beatIndex">心跳信号点位索引（Modbus地址）</param>
        /// <param name="token">取消令牌</param>
        /// <returns>异步任务</returns>
        private static async Task CheckPlcAsync(string plcName, string beatIndex, CancellationToken token)
        {
            // 获取PLC通信实例并检查依赖初始化状态
            var plc = ModbusSystem.Instance.PlcModbusList
                .FirstOrDefault(r => r.DeviceName == plcName);
            if (plc == null || !ModbusDioRealTimeManager.IsInitialized) return;

            try
            {
                // 从实时数据管理器获取当前心跳状态（int16类型）
                var currentData = ModbusDioRealTimeManager.GetRealTimeData(plcName, beatIndex);
                // 修正：判断为short类型，非0视为true，0视为false
                bool currentHeartbeat = currentData?.RealTimeValue is short value ? value != 0 : false;

                // 计算翻转值：每次发送与当前值相反的信号（0→1→0...）
                bool newHeartbeatValue = !currentHeartbeat;

                // 异步写入心跳值（true→1，false→0）
                await plc.WriteInt16Async(Convert.ToInt16(newHeartbeatValue), beatIndex)
                    .ConfigureAwait(false);

                // 异步读取PLC返回的心跳值（验证是否与发送值一致）
                var readResult = await plc.ReadInt16Async(beatIndex)
                    .ConfigureAwait(false);

                if (readResult.IsSuccess)
                {
                    // 读取结果为int16，转换为bool（非0为true）
                    bool responseValue = readResult.Content != 0;
                    bool isBeatNormal = responseValue == newHeartbeatValue;

                    // 更新实时管理器：存储响应值（int16类型）并标记是否正常
                    ModbusDioRealTimeManager.UpdateRealTimeValue(
                        plcName,
                        beatIndex,
                        readResult.Content,  // 直接存储int16原始值
                        isBeatNormal);

                    // 心跳异常时触发重连
                    if (!isBeatNormal)
                    {
                        RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                            $"PLC心跳异常: {plcName}, 触发重连");

                        // 获取设备配置并执行异步重连
                        var device = ModbusSystem.Instance.ModbusCommunications
                            .FirstOrDefault(d => d.设备名称 == plcName);

                        if (device != null)
                        {
                            // 使用_ = 忽略任务结果，避免阻塞
                            _ = plc.ConnectAsync(plc.PlcType, device.设备IP,
                                                device.设备Port, device.站号);
                        }
                    }
                }
                else
                {
                    // 读取失败时保持当前int16值，但标记为无效
                    short currentValue = currentData?.RealTimeValue as short? ?? (short)0;
                    ModbusDioRealTimeManager.UpdateRealTimeValue(
                        plcName, beatIndex, currentValue, false);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 正常取消操作，不记录异常
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"PLC心跳检查异常: {plcName}, {ex.Message}");
                // 异常时使用当前int16值（或0）并标记无效
                short errorValue = 0;
                ModbusDioRealTimeManager.UpdateRealTimeValue(
                    plcName, beatIndex, errorValue, false);
            }
        }
    }

    #endregion

    #region 机器人通信处理器
    /// <summary>
    /// 机器人通信处理器
    /// 核心作用：周期性与机器人进行Modbus通信，读取关键输入信号（如异常状态），更新系统监控状态
    /// 设计思路：
    /// 1. 复用PLC模块架构，保持代码风格和处理流程一致
    /// 2. 集成实时数据管理器，统一存储机器人信号数据
    /// 3. 按"设备→信号类型→数据类型"分层处理，便于扩展多机器人
    /// 4. 完善的连接管理和异常处理，确保通信稳定性
    /// </summary>
    internal static class RobotProcessor
    {
        /// <summary>
        /// 日志记录委托
        /// 与PLC模块保持一致接口，支持统一日志配置
        /// </summary>
        public static Action<string> LogAction { get; set; } = message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

        /// <summary>
        /// 实时数据管理器初始化标记
        /// 与PLC模块实现一致，确保资源按需加载和线程安全
        /// </summary>
        private static readonly Lazy<bool> _realtimeManagerInitialized = new Lazy<bool>(() =>
        {
            try
            {
                if (!ModbusDioRealTimeManager.IsInitialized)
                {
                    ModbusDioRealTimeManager.Initialize();
                }
                LogAction("机器人实时数据管理器初始化成功");
                return true;
            }
            catch (Exception ex)
            {
                LogAction($"机器人实时数据管理器初始化失败：{ex.Message}");
                return false;
            }
        });

        /// <summary>
        /// 机器人监控核心逻辑入口
        /// 流程与PLC模块完全一致，确保跨设备处理逻辑统一
        /// </summary>
        /// <param name="token">取消令牌，用于控制线程退出</param>
        public static void RunRobot(CancellationToken token)
        {
            if (!_realtimeManagerInitialized.Value)
            {
                LogAction("实时数据管理器未初始化，机器人监控无法启动");
                return;
            }

            LogAction("机器人监控线程启动");

            try
            {
                ProcessAllRobotDevices(token);
            }
            catch (Exception ex)
            {
                LogAction($"机器人批量处理异常：{ex.Message}");
            }

            LogAction("机器人监控线程已停止");
        }

        /// <summary>
        /// 批量处理所有机器人设备
        /// 与PLC批量处理逻辑一致，仅过滤条件改为"Robot"类型
        /// </summary>
        /// <param name="token">取消令牌</param>
        private static void ProcessAllRobotDevices(CancellationToken token)
        {
            var modbusDevices = ModbusSystem.Instance?.ModbusCommunications;
            if (modbusDevices == null || !modbusDevices.Any())
            {
                LogAction("未找到机器人设备配置，跳过处理");
                return;
            }

            // 遍历所有机器人类型设备，支持外部取消
            foreach (var device in modbusDevices.Where(d => d.设备类型 == "Robot"))
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    ProcessRobotDevice(device);
                }
                catch (Exception ex)
                {
                    LogAction($"处理机器人[{device.设备名称}]异常：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理单个机器人设备数据
        /// 与PLC单设备处理流程一致，包含连接检查、重连、点位获取等步骤
        /// </summary>
        /// <param name="device">机器人设备配置DTO</param>
        private static void ProcessRobotDevice(ModbusCommunicationDto device)
        {
            if (device == null)
            {
                LogAction("机器人配置为空，跳过处理");
                return;
            }

            // 获取机器人通信实例
            var robot = ModbusSystem.Instance.RobotModbusList
                .FirstOrDefault(r => r.DeviceName == device.设备名称);

            if (robot == null)
            {
                LogAction($"未找到机器人[{device.设备名称}]的通信实例");
                return;
            }

            // 连接状态检查与自动重连
            if (!robot.IsConnected)
            {
                LogAction($"机器人[{device.设备名称}]未连接，尝试重连...");
                // 使用机器人当前类型进行重连（更灵活适配多型号机器人）
                robot.Connect(robot.CurrentRobotType, device.设备IP, device.设备Port, device.站号);
                if (!robot.IsConnected)
                {
                    LogAction($"机器人[{device.设备名称}]重连失败，跳过本次处理");
                    return;
                }
            }

            // 从实时管理器获取点位配置
            var deviceDios = ModbusDioRealTimeManager.GetDeviceConfigByDevice(device.设备名称);
            if (!deviceDios.Any())
            {
                LogAction($"机器人[{device.设备名称}]未配置任何点位，跳过处理");
                return;
            }

            // 仅处理输入信号（机器人目前无需输出控制）
            var inputDios = deviceDios.Where(d => d.点位类型 == "输入").ToList();
            ProcessInputSignals(robot, inputDios);
        }

        /// <summary>
        /// 批量处理机器人输入信号
        /// 与PLC输入处理逻辑一致，按数据类型分发
        /// </summary>
        /// <param name="robot">机器人通信实例</param>
        /// <param name="inputDios">输入信号配置列表</param>
        private static void ProcessInputSignals(Robot_Modbus robot, List<ModbusCommunicationDioDto> inputDios)
        {
            foreach (var dio in inputDios)
            {
                try
                {
                    switch (dio.数据类型)
                    {
                        case "int16":
                            ProcessInt16InputSignal(robot, dio);
                            break;
                        case "bool":
                            ProcessBoolInputSignal(robot, dio);
                            break;
                        default:
                            LogAction($"未知数据类型[{dio.数据类型}]，点位：{dio.设备名称}_{dio.点位索引}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogAction($"处理机器人输入信号[{dio.点位名称}]异常：{ex.Message}");
                    // 异常时标记数据无效
                    ModbusDioRealTimeManager.UpdateRealTimeValue(dio.设备名称, dio.点位索引, null, false);
                }
            }
        }

        /// <summary>
        /// 处理机器人int16类型输入信号
        /// 扩展点：通过点位定义（如"C030"）区分信号组，便于业务逻辑分类
        /// </summary>
        /// <param name="robot">机器人通信实例</param>
        /// <param name="dio">信号配置</param>
        private static void ProcessInt16InputSignal(Robot_Modbus robot, ModbusCommunicationDioDto dio)
        {
            var readResult = robot.ReadInt16(dio.点位索引);
            if (!readResult.IsSuccess)
            {
                LogAction($"读取机器人int16信号[{dio.点位名称}]失败：{readResult.Message}");
                ModbusDioRealTimeManager.UpdateRealTimeValue(dio.设备名称, dio.点位索引, null, false);
                return;
            }

            short value = readResult.Content;
            // 处理C030组信号（业务约定的关键信号组）
            if (dio.点位定义 == "C030" && dio.点位名称 == "机械手异常")
            {
                Controller.Instance.IsRobotAlarmMonitored = value == 1;
            }
            // 同步至实时数据管理器
            ModbusDioRealTimeManager.UpdateRealTimeValue(dio.设备名称, dio.点位索引, value);
        }

        /// <summary>
        /// 处理机器人bool类型输入信号
        /// 与int16处理逻辑对称，针对bool类型信号
        /// </summary>
        /// <param name="robot">机器人通信实例</param>
        /// <param name="dio">信号配置</param>
        private static void ProcessBoolInputSignal(Robot_Modbus robot, ModbusCommunicationDioDto dio)
        {
            //var readResult = robot.ReadBool(dio.点位索引);

            var readResult = robot.ReadEpsonIoPoint(robot, dio);


            if (!readResult.IsSuccess)
            {
                LogAction($"读取机器人bool信号[{dio.点位名称}]失败：{readResult.Message}");
                ModbusDioRealTimeManager.UpdateRealTimeValue(dio.设备名称, dio.点位索引, null, false);
                return;
            }

            bool value = readResult.Content;

            switch (dio.点位名称)
            {
                case "左下相机拍照位1":  //   0>1   才刷新
                    if (value&&Controller.Instance.StationInfo.Epson2下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2下相机请求拍照 =1;
                        Controller.Instance.StationInfo.Epson2下相机请求位置号 = 1;
                        Controller.Instance.StationInfo.Epson2下相机是否刷新 = false;
                    }
                    break;
                case "左下相机拍照位2":
                    if (value&&Controller.Instance.StationInfo.Epson2下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson2下相机请求位置号 = 2;
                        Controller.Instance.StationInfo.Epson2下相机是否刷新 = false;
                    }
                    break;
                case "左下相机拍照位3":
                    if (value&& Controller.Instance.StationInfo.Epson2下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson2下相机请求位置号 = 3;
                        Controller.Instance.StationInfo.Epson2下相机是否刷新 = false;

                    }
                    break;
                case "左下相机拍照位4":
                    if (value&& Controller.Instance.StationInfo.Epson2下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson2下相机请求位置号 = 4;
                        Controller.Instance.StationInfo.Epson2下相机是否刷新 = false;
                    }
                    break;
                case "左下相机拍照位5":
                    if (value&& Controller.Instance.StationInfo.Epson2下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson2下相机请求位置号 = 5;
                        Controller.Instance.StationInfo.Epson2下相机是否刷新 = false;
                    }
                    break;
                case "左下相机拍照位6":
                    if (value && Controller.Instance.StationInfo.Epson2下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson2下相机请求位置号 = 6;
                        Controller.Instance.StationInfo.Epson2下相机是否刷新 = false;
                    }
                    break;
                case "Epson2请求数据1":
                    if (value && Controller.Instance.StationInfo.左皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson2请求安装数据号 = 1;
                        Controller.Instance.StationInfo.左皮带线是否刷新 = false;
                    }
                    break;
                case "Epson2请求数据2":
                    if (value && Controller.Instance.StationInfo.左皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson2请求安装数据号 = 2;
                        Controller.Instance.StationInfo.左皮带线是否刷新 = false;
                    }
                    break;
                case "Epson2请求数据3":
                    if (value && Controller.Instance.StationInfo.左皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson2请求安装数据号 = 3;
                        Controller.Instance.StationInfo.左皮带线是否刷新 = false;
                    }
                    break;
                case "Epson2请求数据4":
                    if (value && Controller.Instance.StationInfo.左皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson2请求安装数据号 = 4;
                        Controller.Instance.StationInfo.左皮带线是否刷新 = false;
                    }
                    break;
                case "Epson2请求数据5":
                    if (value && Controller.Instance.StationInfo.左皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson2请求安装数据号 = 5;
                        Controller.Instance.StationInfo.左皮带线是否刷新 = false;
                    }
                    break;
                case "Epson2请求数据6":
                    if (value && Controller.Instance.StationInfo.左皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson2皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson2请求安装数据号 = 6;
                        Controller.Instance.StationInfo.左皮带线是否刷新 = false;
                    }
                    break;



                case "右下相机拍照位1":
                    if (value && Controller.Instance.StationInfo.Epson1下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson1下相机请求位置号 = 1;
                        Controller.Instance.StationInfo.Epson1下相机是否刷新 = false;
                    }
                    break;
                case "右下相机拍照位2":
                    if (value && Controller.Instance.StationInfo.Epson1下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson1下相机请求位置号 = 2;
                        Controller.Instance.StationInfo.Epson1下相机是否刷新 = false;
                    }
                    break;
                case "右下相机拍照位3":
                    if (value && Controller.Instance.StationInfo.Epson1下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson1下相机请求位置号 = 3;
                        Controller.Instance.StationInfo.Epson1下相机是否刷新 = false;

                    }
                    break;
                case "右下相机拍照位4":
                    if (value && Controller.Instance.StationInfo.Epson1下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson1下相机请求位置号 = 4;
                        Controller.Instance.StationInfo.Epson1下相机是否刷新 = false;
                    }
                    break;
                case "右下相机拍照位5":
                    if (value && Controller.Instance.StationInfo.Epson1下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson1下相机请求位置号 = 5;
                        Controller.Instance.StationInfo.Epson1下相机是否刷新 = false;
                    }
                    break;
                case "右下相机拍照位6":
                    if (value && Controller.Instance.StationInfo.Epson1下相机是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1下相机请求拍照 = 1;
                        Controller.Instance.StationInfo.Epson1下相机请求位置号 = 6;
                        Controller.Instance.StationInfo.Epson1下相机是否刷新 = false;
                    }
                    break;
                case "Epson1请求数据1":
                    if (value && Controller.Instance.StationInfo.右皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson1请求安装数据号 = 1;
                        Controller.Instance.StationInfo.右皮带线是否刷新 = false;
                    }
                    break;
                case "Epson1请求数据2":
                    if (value && Controller.Instance.StationInfo.右皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson1请求安装数据号 = 2;
                        Controller.Instance.StationInfo.右皮带线是否刷新 = false;
                    }
                    break;
                case "Epson1请求数据3":
                    if (value && Controller.Instance.StationInfo.右皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson1请求安装数据号 = 3;
                        Controller.Instance.StationInfo.右皮带线是否刷新 = false;
                    }
                    break;
                case "Epson1请求数据4":
                    if (value && Controller.Instance.StationInfo.右皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson1请求安装数据号 = 4;
                        Controller.Instance.StationInfo.右皮带线是否刷新 = false;
                    }
                    break;
                case "Epson1请求数据5":
                    if (value && Controller.Instance.StationInfo.右皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson1请求安装数据号 = 5;
                        Controller.Instance.StationInfo.右皮带线是否刷新 = false;
                    }
                    break;
                case "Epson1请求数据6":
                    if (value && Controller.Instance.StationInfo.右皮带线是否刷新)
                    {
                        Controller.Instance.StationInfo.Epson1皮带线请求结果 = 1;
                        Controller.Instance.StationInfo.Epson1请求安装数据号 = 6;
                        Controller.Instance.StationInfo.右皮带线是否刷新 = false;
                    }
                    break;
                case "Epson1流水线组装完成":
                    Controller.Instance.StationInfo.Epson1流水线组装完成 =Convert.ToInt32(value);
                    break;
                case "Epson2流水线组装完成":
                    Controller.Instance.StationInfo.Epson2流水线组装完成 =Convert.ToInt32(value);
                    break;
                case "Epson1呼叫组装":
                    Controller.Instance.StationInfo.Epson1呼叫组装 = Convert.ToInt32(value);
                    break;
                case "Epson2呼叫组装":
                    Controller.Instance.StationInfo.Epson2呼叫组装 = Convert.ToInt32(value);
                    break;
            }



            // 同步至实时数据管理器
            ModbusDioRealTimeManager.UpdateRealTimeValue(dio.设备名称, dio.点位索引, value);
        }
    }

    /// <summary>
    /// 机器人心跳监控器（异步版）
    /// 设计完全复用PLC心跳监控器架构，仅适配机器人通信实例
    /// 核心差异：监控对象为机器人设备，其余逻辑（翻转值验证、异步处理、自动重连）完全一致
    /// </summary>
    internal static class RobotHeartbeatMonitor
    {
        // 心跳状态变量（记录上次发送的值，用于翻转验证）
        private static bool _robotHeartbeat1 = false;
        // 用于同步异步操作的锁对象
        private static readonly object _heartbeatLock = new object();

        /// <summary>
        /// Robot心跳监控核心逻辑（异步版）
        /// 目前仅监控"H5u-1"Robot，可扩展至其他Robot
        /// </summary>
        /// <param name="token">取消令牌（用于线程退出控制）</param>
        public static async Task RunRobotBeatAsync(CancellationToken token)
        {
            // 检查"H5u-1"Robot的心跳（点位索引8，直接使用静态变量_robotHeartbeat1）
            await CheckRobotAsync("H5u-1", "8", token).ConfigureAwait(false);
        }

        /// <summary>
        /// Robot心跳检查（异步通用模板方法）
        /// 流程：发送翻转心跳值 → 读取响应值 → 验证一致性 → 异常时触发重连
        /// </summary>
        /// <param name="robotName">Robot名称（用于匹配实例）</param>
        /// <param name="beatIndex">心跳信号点位索引（Modbus地址）</param>
        /// <param name="token">取消令牌</param>
        private static async Task CheckRobotAsync(string robotName, string beatIndex, CancellationToken token)
        {
            // 获取对应名称的Robot通信实例
            var robot = ModbusSystem.Instance.RobotModbusList
                .FirstOrDefault(r => r.DeviceName == robotName);
            if (robot == null) return;

            try
            {
                // 读取当前心跳状态（加锁保证线程安全）
                bool currentHeartbeat;
                lock (_heartbeatLock)
                {
                    currentHeartbeat = _robotHeartbeat1;
                }

                // 异步写入心跳值（翻转上次的值：true→1，false→0）
                await robot.WriteInt16Async(Convert.ToInt16(!currentHeartbeat), beatIndex)
                    .ConfigureAwait(false);

                // 异步读取Robot返回的心跳值
                var readResult = await robot.ReadInt16Async(beatIndex)
                    .ConfigureAwait(false);
                if (!readResult.IsSuccess) return;

                // 线程安全地处理心跳状态（避免并发修改）
                lock (_heartbeatLock)
                {
                    bool newValue = Convert.ToBoolean(readResult.Content);
                    bool isBeatNormal = newValue != _robotHeartbeat1;

                    // 更新心跳状态变量
                    _robotHeartbeat1 = newValue;

                    // 心跳异常时触发重连
                    if (!isBeatNormal)
                    {
                        // 获取该Robot的配置信息
                        var device = ModbusSystem.Instance.ModbusCommunications.First(d => d.设备名称 == robotName);
                        // 调用异步连接方法重新建立通信
                        _ = robot.ConnectAsync(robot.CurrentRobotType, device.设备IP, device.设备Port, device.站号);
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 正常取消，不处理
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"Robot心跳检查异常: {ex.Message}");
            }
        }
    }
    #endregion


    /// <summary>
    /// 系统状态更新器
    /// 核心作用：统一处理UI界面的状态显示，包括设备连接状态、安全门状态、运行模式及系统核心状态
    /// 设计思路：将UI更新操作统一封装，通过主线程调度避免跨线程异常，按优先级显示关键状态
    /// </summary>
    internal static class StatusUpdater
    {
        /// <summary>
        /// 根据警告颜色触发对应操作（替代原RedWarning方法，统一警告处理逻辑）
        /// 不同颜色对应不同级别的警告（如绿色=启动，红色=急停）
        /// </summary>
        /// <param name="warningColor">警告颜色枚举（Green/White/Orange/Red）</param>
        public static void TriggerWarning(WarningColor warningColor)
        {
            switch (warningColor)
            {
                case WarningColor.Green: // 绿色警告：启动程序（预留逻辑）
                                         // Project.Instance.Controller.StartStation();
                    break;

                case WarningColor.White: // 白色警告：初始化程序（预留逻辑）
                                         // Project.Instance.Controller.InitStation();
                    break;

                case WarningColor.Orange: // 橙色警告：软件报警
                                          // 切换到主线程更新UI（避免跨线程操作异常）
                    WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                    {
                        // 非急停状态下才切换到报警状态并显示报警窗口
                        if (MenuProxy.Instance.MachineRunStatus != MachineRunStatus.EmergencyStopped)
                        {
                            MenuProxy.Instance.MachineRunStatus = MachineRunStatus.Alarming;
                            AlarmWindowManager.ShowAlarmWindow(); // 显示报警窗口
                        }
                    });
                    break;

                case WarningColor.Red: // 红色警告：机台急停
                    WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                    {
                        // 非急停状态下才执行停止操作
                        if (MenuProxy.Instance.MachineRunStatus != MachineRunStatus.EmergencyStopped)
                        {
                            Project.Instance.Controller.Stop(); // 触发机台停止
                        }
                    });
                    break;
            }
        }

        /// <summary>
        /// 系统状态更新核心逻辑
        /// 统一调度各类UI状态更新操作，确保所有界面操作在主线程执行
        /// </summary>
        /// <param name="token">取消令牌（用于线程退出控制）</param>
        public static void UpdateRunStatus(CancellationToken token)
        {
            try
            {
                // 所有UI更新操作通过工具类切换到主线程执行，避免跨线程异常
                WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
                {
                    HandleTouchScreenReset();       // 处理触摸屏复位与启动逻辑
                    UpdateTcpipConnectionStatus();  // 更新TCPIP设备连接状态显示
                    UpdateModbusConnectionStatus(); // 更新Modbus设备连接状态显示
                    UpdateSafetyDoorStatus();       // 更新安全门状态显示
                    UpdateOperationModeDisplay();   // 更新运行模式（点检/联机等）显示
                    UpdateSystemStatusText();       // 更新系统核心状态文本（急停/报警等）
                });
            }
            catch (Exception ex)
            {
                // 记录异常日志
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 处理触摸屏复位与启动逻辑
        /// 响应机台复位监控信号（触发复位按钮）和启动监控信号（恢复运行状态）
        /// </summary>
        private static void HandleTouchScreenReset()
        {
            // 机台复位监控信号有效时，触发报警窗口的手动复位按钮
            if (Controller.Instance.IsMachineResetMonitored)
            {
                if (Frm_Main.Instance.frm_AlarmWindow.Visible)
                {
                    Frm_Main.Instance.frm_AlarmWindow.btn_手动复位.PerformClick(); // 模拟按钮点击
                }
                Controller.Instance.IsMachineResetMonitored = false; // 复位信号处理后重置
            }

            // 机台启动信号有效且复位后启动标记为true时，恢复缓存的运行状态
            if (Controller.Instance.IsMachineStartMonitored && Controller.Instance.StationInfo.机台复位后启动)
            {
                Controller.Instance.StationInfo.机台复位后启动 = false; // 重置标记
                                                                 // 当前状态为暂停时，恢复到缓存的运行状态（如从暂停恢复为运行）
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Paused)
                {
                    Controller.Instance.UpdateMachineRunStatus(Controller.Instance.StationInfo.CachedMachineRunStatus);
                }
            }
        }

        /// <summary>
        /// 更新TCPIP连接状态显示
        /// 在UI上显示所有TCP服务器和客户端的连接状态（绿色=连接，红色=断开）
        /// </summary>
        private static void UpdateTcpipConnectionStatus()
        {
            // 获取TCPIP设备配置列表
            var tcpDevices = TcpIpSystem.Instance.TcpIpCommunications;
            // 获取UI上的TCPIP状态按钮（用于显示整体状态和下拉列表）
            var tcpStatusButton = Frm_Main.Instance.tssButton_TCPIP_State;

            // 无TCPIP设备配置时，隐藏状态按钮
            if (tcpDevices == null || !tcpDevices.Any())
            {
                tcpStatusButton.Visible = false;
                return;
            }

            // 显示状态按钮，默认颜色为绿色（有一个设备断开则变为红色）
            tcpStatusButton.Visible = true;
            tcpStatusButton.ForeColor = Color.Green;

            // 更新所有TCP服务器的连接状态
            foreach (var server in TcpIpSystem.Instance.TcpServers)
            {
                UpdateConnectionItem(
                    parent: tcpStatusButton,
                    deviceName: server.Name,
                    isConnected: server._clients.Count > 0, // 有客户端连接即为成功
                    connectedText: "存在连接的客户端！",
                    disconnectedText: "无客户端连接！"
                );
            }

            // 更新所有TCP客户端的连接状态
            foreach (var client in TcpIpSystem.Instance.TcpClients)
            {
                UpdateConnectionItem(
                    parent: tcpStatusButton,
                    deviceName: client.Name,
                    isConnected: client.IsConnected, // 客户端已连接即为成功
                    connectedText: "存在连接的服务器！",
                    disconnectedText: "未连接到服务器！"
                );
            }
        }

        /// <summary>
        /// 更新Modbus连接状态显示
        /// 在UI上显示所有PLC和机器人的Modbus连接状态（绿色=连接，红色=断开）
        /// </summary>
        private static void UpdateModbusConnectionStatus()
        {
            // 获取Modbus设备配置列表
            var modbusDevices = ModbusSystem.Instance.ModbusCommunications;
            // 获取UI上的Modbus状态按钮
            var modbusStatusButton = Frm_Main.Instance.tssButton_Modbus_State;

            // 无Modbus设备配置时，隐藏状态按钮
            if (modbusDevices == null || !modbusDevices.Any())
            {
                modbusStatusButton.Visible = false;
                return;
            }

            // 显示状态按钮，默认颜色为绿色（有一个设备断开则变为红色）
            modbusStatusButton.Visible = true;
            modbusStatusButton.ForeColor = Color.Green;

            // 遍历设备类型，更新对应设备的连接状态
            foreach (var device in modbusDevices)
            {
                switch (device.设备类型)
                {
                    // 更新PLC的连接状态
                    case "PLC":
                        foreach (var plc in ModbusSystem.Instance.PlcModbusList)
                        {
                            UpdateConnectionItem(
                                parent: modbusStatusButton,
                                deviceName: plc.DeviceName,
                                isConnected: plc.IsConnected, // PLC的连接状态标识
                                connectedText: "--已连接！",
                                disconnectedText: "--未连接！"
                            );
                        }
                        break;

                    // 更新机器人的连接状态
                    case "Robot":
                        foreach (var robot in ModbusSystem.Instance.RobotModbusList)
                        {
                            UpdateConnectionItem(
                                parent: modbusStatusButton,
                                deviceName: robot.DeviceName,
                                isConnected: robot.IsConnected, // 机器人的连接状态标识
                                connectedText: "--已连接！",
                                disconnectedText: "--未连接！"
                            );
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 更新安全门状态显示
        /// 在UI上显示安全门的锁定/解锁状态（红色=锁定，灰色=解锁）
        /// </summary>
        private static void UpdateSafetyDoorStatus()
        {
            #region 机台安全门
            // 获取UI上的安全门状态标签
            var safetyDoorLabel = Frm_Main.Instance.toolStripLabel_机台安全门打开;
            // 获取当前安全门状态（true=锁定，false=解锁）
            var isLocked = Controller.Instance.IsSafetyDoorStatusMonitored;

            // 更新标签文本和颜色
            safetyDoorLabel.Text = isLocked ? "安全门锁定" : "安全门解锁";
            safetyDoorLabel.ForeColor = isLocked ? Color.DarkGray : Color.Red;
            #endregion

            #region 上料安全门
            // 获取UI上的安全门状态标签
            //var 上料safetyDoorLabel = Frm_Main.Instance.toolStripLabel_上料安全门打开;
            //// 获取当前安全门状态（true=锁定，false=解锁）
            //var is上料Locked = Controller.Instance.IsSafetyDoorStatusMonitored;

            //// 更新标签文本和颜色
            //上料safetyDoorLabel.Text = is上料Locked ? "前安全门锁定" : "前安全门解锁";
            //上料safetyDoorLabel.ForeColor = is上料Locked ? Color.DarkGray : Color.Red;
            #endregion

            #region 下料安全门
            // 获取UI上的安全门状态标签
            //var 下料safetyDoorLabel = Frm_Main.Instance.toolStripLabel_下料安全门打开;
            //// 获取当前安全门状态（true=锁定，false=解锁）
            //var is下料Locked = Controller.Instance.IsSafetyDoorStatusMonitored;

            //// 更新标签文本和颜色
            //下料safetyDoorLabel.Text = is下料Locked ? "后安全门锁定" : "后安全门解锁";
            //下料safetyDoorLabel.ForeColor = is下料Locked ? Color.DarkGray : Color.Red;
            #endregion


        }

        /// <summary>
        /// 更新运行模式（点检/联机等）显示
        /// 在UI上显示当前机台的运行模式（如"联机模式"、"点检模式"）
        /// </summary>
        private static void UpdateOperationModeDisplay()
        {
            // 获取UI上的运行模式标签
            var modeLabel = Frm_Main.Instance.toolStripStatusLabel_单联机;
            // 根据当前操作类型更新标签文本（switch表达式匹配）
            modeLabel.Text = MenuProxy.Instance.OperateType switch
            {
                OperateMode.Online => "联机模式",
                OperateMode.Standalone => "单机模式",
                _ => "未知模式" // 未知类型的默认文本
            };
        }

        /// <summary>
        /// 更新系统核心状态文本（急停/报警等）
        /// 按优先级显示最关键的系统状态（如急停>STO>安全门>报警等），并触发对应警告
        /// </summary>
        private static void UpdateSystemStatusText()
        {
            // 获取UI上的系统状态标签
            var systemStatusLabel = Frm_Main.Instance.tss_SystemStatus;
            // 获取当前机台运行状态
            var GeneralStatus = MenuProxy.Instance.MachineRunStatus;

            // 急停状态处理（最高优先级）
            if (GeneralStatus == MachineRunStatus.EmergencyStopped)
            {
                MenuProxy.Instance.EnableReset = true; // 允许复位操作
                systemStatusLabel.Text = "急停开启";
                systemStatusLabel.ForeColor = Color.Red;

                // 若报警窗口已显示，触发手动复位按钮
                if (Frm_Main.Instance.frm_AlarmWindow.Visible)
                {
                    Frm_Main.Instance.frm_AlarmWindow.btn_手动复位.PerformClick();
                }
                TriggerWarning(WarningColor.Red); // 触发红色警告（急停）
                return;
            }

            // 非急停状态处理
            MenuProxy.Instance.EnableReset = false; // 禁用复位操作
            var controller = Controller.Instance;

            // 按优先级获取系统状态信息（文本+颜色）
            var (statusText, statusColor) = GetSystemStatusInfo(controller, GeneralStatus);
            systemStatusLabel.Text = statusText;
            systemStatusLabel.ForeColor = statusColor;

            // 特殊情况：联机模式下手动运行的提示（禁止自动流程）
            if (MenuProxy.Instance.MachineRunModel == MachineMode.Manual
                && (GeneralStatus == MachineRunStatus.Running || GeneralStatus == MachineRunStatus.Homing))
            {
                systemStatusLabel.Text = "机台处于手动模式，不允许自动执行流程";
                systemStatusLabel.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// 通用方法：更新连接状态项（适用于TCPIP/Modbus等设备）
        /// 统一处理设备连接状态的UI显示逻辑，支持父级按钮颜色联动（有一个断开则变红）
        /// </summary>
        /// <param name="parent">父级ToolStrip按钮（用于显示整体状态和下拉列表）</param>
        /// <param name="deviceName">设备名称（用于匹配下拉项）</param>
        /// <param name="isConnected">是否连接成功（true=连接，false=断开）</param>
        /// <param name="connectedText">连接成功时的描述文本</param>
        /// <param name="disconnectedText">连接失败时的描述文本</param>
        private static void UpdateConnectionItem(ToolStripDropDownItem parent, string deviceName, bool isConnected, string connectedText, string disconnectedText)
        {
            // 从父级下拉项中查找对应设备的状态项
            var statusItem = parent.DropDownItems.Cast<ToolStripItem>()
                                  .FirstOrDefault(item => item.Name == deviceName);
            if (statusItem == null) return; // 未找到对应项时返回

            // 更新状态项的文本和颜色
            statusItem.Text = isConnected ? $"{deviceName}{connectedText}" : $"{deviceName}{disconnectedText}";
            statusItem.ForeColor = isConnected ? Color.Green : Color.Red;

            // 只要有一个设备连接失败，父级按钮显示红色（覆盖默认绿色）
            if (!isConnected)
            {
                parent.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// 获取系统状态信息（文本+颜色）
        /// 按优先级返回最关键的系统状态（从高到低：STO→安全门→光栅→报警→暂停）
        /// </summary>
        /// <param name="controller">控制器实例（包含各类监控状态）</param>
        /// <param name="GeneralStatus">当前机台运行状态</param>
        /// <returns>元组（状态文本，状态颜色）</returns>
        private static (string Text, Color Color) GetSystemStatusInfo(Controller controller, MachineRunStatus GeneralStatus)
        {
            // 按优先级判断并返回对应状态
            if (controller.IsMachineInternalStoMonitored)
                return ("机台STO", Color.Red);
            if (controller.烟雾报警)
                return ("烟雾报警", Color.Red);
            if (controller.覆膜机缺料报警)
                return ("覆膜机缺料", Color.Red);
            if (controller.超声波焊接报警)
                return ("超声波焊接报警", Color.Red);
            if (GeneralStatus == MachineRunStatus.Alarming)
                return ("软件报警", Color.Red);
            if (GeneralStatus == MachineRunStatus.Paused)
                return ("程序暂停中", Color.Red);

            // 无异常状态时返回空文本（绿色）
            return ("", Color.Green);
        }
    }
}
