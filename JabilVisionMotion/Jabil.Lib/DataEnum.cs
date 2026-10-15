using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Jabil.Lib
{
    /// <summary>
    /// 控制模式枚举
    /// 定义设备的控制方式，区分不同的硬件交互模式
    /// </summary>
    public enum ControlMode
    {
        /// <summary>
        /// 纯PC控制模式
        /// 设备完全由计算机软件控制，无PLC参与
        /// 适用于模拟系统、单机版应用或纯软件解决方案
        /// </summary>
        [Display(Name = "纯PC控制")]
        PurePC,

        /// <summary>
        /// PLC控制模式
        /// 设备核心逻辑由PLC控制，PC作为上位机进行监控和配置
        /// 适用于工业自动化生产线、精密设备控制等场景
        /// </summary>
        [Display(Name = "PLC控制")]
        PLC,

        /// <summary>
        /// 混合控制模式
        /// 关键动作由PLC控制，辅助功能由PC软件实现
        /// 适用于需要兼顾实时性和灵活性的复杂系统
        /// </summary>
        [Display(Name = "混合控制")]
        Hybrid
    }

    /// <summary>
    /// 工位运行模式枚举
    /// 定义设备的不同工作模式，对应不同的业务场景和功能权限
    /// </summary>
    public enum OperateMode
    {
        /// <summary>
        /// 联机协同模式
        /// 设备与其他系统（如MES、上位机）联网协同工作，数据实时同步
        /// </summary>
        [Display(Name = "联机")]
        Online,

        /// <summary>
        /// 单机独立模式
        /// 设备脱离网络独立运行，数据本地存储，适用于离线场景
        /// </summary>
        [Display(Name = "单机")]
        Standalone,
    }

    /// <summary>
    /// 提示信息级别枚举
    /// 用于区分不同严重程度的提示信息，对应不同的UI展示方式（如颜色、图标）
    /// </summary>
    public enum TipLevel
    {
        /// <summary>
        /// 普通提示信息
        /// 用于告知用户正常操作结果或状态变化，无风险提示
        /// 例如："操作已完成"、"数据已保存"
        /// </summary>
        Tip,

        /// <summary>
        /// 警告信息
        /// 提示用户潜在风险或非预期情况，但不影响系统继续运行
        /// 例如："参数值接近阈值"、"网络延迟偏高"
        /// </summary>
        Warning,

        /// <summary>
        /// 错误信息
        /// 表示操作失败或系统异常，可能影响正常功能执行
        /// 例如："文件不存在"、"设备连接失败"
        /// </summary>
        Error,
    }

    /// <summary>
    /// 设备运行状态枚举（支持位运算组合）
    /// 采用Flags特性，可同时表示多种状态的组合（如"运行中+报警"）
    /// 每个状态使用2的幂次值，确保位运算唯一性
    /// </summary>
    [Flags]
    public enum MachineRunStatus
    {
        /// <summary>
        /// 设备初始状态或急停复位后的默认状态
        /// 表示无任何特定状态，通常为系统启动后的初始值
        /// </summary>
        None = 0,

        /// <summary>
        /// 暂停中状态
        /// 设备已停止运行但未复位，可通过"继续"操作恢复
        /// </summary>
        Paused = 1 << 2,  // 二进制：100，十进制：4

        /// <summary>
        /// 初始化中状态
        /// 设备正在执行回零、参数加载等初始化操作
        /// </summary>
        Homing = 1 << 4,  // 二进制：10000，十进制：16

        /// <summary>
        /// 初始化完成并等待运行状态
        /// 初始化操作成功完成，设备就绪等待启动指令
        /// </summary>
        HomeSuccess = 1 << 5,  // 二进制：100000，十进制：32

        /// <summary>
        /// 自动模式运行中状态
        /// 设备正在按预设程序自动执行生产流程
        /// </summary>
        Running = 1 << 6,  // 二进制：1000000，十进制：64

        /// <summary>
        /// 报警状态
        /// 设备检测到异常但未停机，需用户干预处理
        /// </summary>
        Alarming = 1 << 7,  // 二进制：10000000，十进制：128

        /// <summary>
        /// 复位中状态
        /// 设备正在执行复位操作，恢复到初始位置或默认参数
        /// </summary>
        Reseting = 1 << 8,  // 二进制：100000000，十进制：256

        /// <summary>
        /// 急停状态
        /// 设备因紧急情况触发急停，所有运动轴立即锁定
        /// 需手动复位急停按钮后才能恢复
        /// </summary>
        EmergencyStopped = 1 << 9  // 二进制：1000000000，十进制：512
    }

    /// <summary>
    /// 警告信息颜色标识枚举
    /// 定义不同级别警告信息对应的UI展示颜色，增强视觉区分度
    /// </summary>
    public enum WarningColor
    {
        /// <summary>
        /// 红色 - 表示严重错误
        /// 用于标识致命错误或需立即处理的紧急情况
        /// </summary>
        Red = 1,

        /// <summary>
        /// 橙色 - 表示重要警告
        /// 用于提示可能影响生产质量或效率的异常情况
        /// </summary>
        Orange = 2,

        /// <summary>
        /// 桃红色 - 表示一般警告
        /// 用于提示非紧急但需关注的异常
        /// </summary>
        HotPink = 3,

        /// <summary>
        /// 白色 - 表示中性信息
        /// 用于普通状态提示，无警告含义
        /// </summary>
        White = 4,

        /// <summary>
        /// 绿色 - 表示成功或正常状态
        /// 用于标识操作成功或系统正常运行
        /// </summary>
        Green = 5
    }

    /// <summary>
    /// 系统窗体页面枚举
    /// 定义系统中所有功能页面的标识，用于页面导航和状态管理
    /// </summary>
    public enum FormPage
    {
        /// <summary>
        /// 无页面
        /// 表示未指定页面或页面关闭状态
        /// </summary>
        None,

        /// <summary>
        /// 主页面
        /// 系统默认启动页面，包含核心功能入口和状态监控
        /// </summary>
        MainPage,

        /// <summary>
        /// 产品数据页面
        /// 展示和管理产品相关数据，如产能、合格率、历史记录等
        /// </summary>
        ProductDataPage,

        /// <summary>
        /// 流程监控页面
        /// 实时监控生产流程进度、设备状态和关键参数
        /// </summary>
        ProcessMonitorPage,

        /// <summary>
        /// 视觉页面
        /// 展示机器视觉系统的图像、检测结果和视觉参数配置
        /// </summary>
        VisionPage,

        /// <summary>
        /// 手动操作页面
        /// 提供设备手动控制接口，如单轴运动、手动执行工序等
        /// </summary>
        ManualPage,

        /// <summary>
        /// 设置页面
        /// 用于系统参数配置、用户管理、权限设置等
        /// </summary>
        SettingPage,

        /// <summary>
        /// 报警页面
        /// 显示系统历史报警记录和当前未处理报警，支持报警确认
        /// </summary>
        AlarmPage,

        /// <summary>
        /// 权限管理页面
        /// 管理用户角色、操作权限和登录验证设置
        /// </summary>
        PermissionPage,

        /// <summary>
        /// 帮助页面
        /// 提供系统使用指南、故障排查和版本信息等
        /// </summary>
        HelpPage
    }

    /// <summary>
    /// 系统支持的语言类型枚举
    /// 定义系统可切换的语言选项，用于国际化适配
    /// </summary>
    public enum LanguageType
    {
        /// <summary>
        /// 中文（简体）
        /// 系统默认语言，适用于中文环境用户
        /// </summary>
        Chinese,

        /// <summary>
        /// 英文（通用）
        /// 适用于国际用户或英文环境
        /// </summary>
        English
    }

    /// <summary>
    /// 流程运行状态枚举
    /// 描述单个业务流程的生命周期状态，用于流程控制和进度展示
    /// </summary>
    public enum ProcessStatus
    {
        /// <summary>
        /// 流程未运行
        /// 流程尚未启动或已结束
        /// </summary>
        NotRunning,

        /// <summary>
        /// 流程正在运行
        /// 流程处于执行中状态，未完成所有步骤
        /// </summary>
        Running,

        /// <summary>
        /// 流程运行报警
        /// 流程执行中发生异常，需处理后才能继续
        /// </summary>
        Alarm,

        /// <summary>
        /// 流程运行完成
        /// 流程所有步骤执行完毕，无异常终止
        /// </summary>
        Completed
    }

    /// <summary>
    /// 设备运行模式枚举
    /// 区分设备的手动/自动控制模式，对应不同的操作逻辑
    /// </summary>
    public enum MachineMode
    {
        /// <summary>
        /// 手动运行模式
        /// 由操作员通过按钮、手柄等设备手动控制每个动作
        /// 通常用于调试、维护或异常处理
        /// </summary>
        Manual = 1,

        /// <summary>
        /// 自动运行模式
        /// 设备根据预设程序自动执行完整流程，无需人工干预
        /// 适用于批量生产场景
        /// </summary>
        Auto = 2
    }

    /// <summary>
    /// 通用运行状态枚举
    /// 提供跨模块的通用状态描述，适用于各种场景的状态表示
    /// </summary>
    public enum GeneralStatus
    {
        /// <summary>
        /// 等待状态
        /// 等待外部触发条件（如信号、用户指令）
        /// </summary>
        Wait,

        /// <summary>
        /// 无错误状态
        /// 表示正常就绪状态，无任何错误或异常
        /// </summary>
        NoError,

        /// <summary>
        /// 运行中状态
        /// 正在执行指定操作或流程
        /// </summary>
        Running,

        /// <summary>
        /// 错误状态
        /// 发生错误导致无法继续执行
        /// </summary>
        Error
    }
}