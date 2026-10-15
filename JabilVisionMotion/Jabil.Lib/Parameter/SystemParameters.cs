using System;

namespace Jabil.Lib.Parameter
{
    /// <summary>
    /// 系统参数配置类
    /// 存储应用程序的核心配置信息，包括界面行为、数据存储策略、运行模式等基础设置
    /// 支持序列化，用于参数的持久化存储（如保存到配置文件）
    /// </summary>
    [Serializable]
    public class SystemParameters  // 类名优化：使用完整复数形式，符合C#命名规范
    {
        #region 界面与启动配置
        /// <summary>
        /// 程序启动时默认显示的页面
        /// 默认为None（无指定页面），实际运行时可根据业务需求设置为首页、生产页等
        /// </summary>
        public FormPage DefaultForm { get; set; } = FormPage.None;

        /// <summary>
        /// 主窗体初始高度（像素）
        /// 默认为80像素，实际运行时会根据屏幕分辨率自适应调整
        /// </summary>
        public int MainFormHeight { get; set; } = 80;

        /// <summary>
        /// 主窗体初始宽度（像素）
        /// 默认为100像素，实际运行时会根据屏幕分辨率自适应调整
        /// </summary>
        public int MainFormWidth { get; set; } = 100;

        /// <summary>
        /// 是否允许用户调整主窗体大小
        /// true：允许拖拽边框改变窗体尺寸
        /// false：固定窗体大小，防止误操作导致界面布局错乱
        /// 默认值：true（允许调整）
        /// </summary>
        public bool AllowResizeForm { get; set; } = true;

        /// <summary>
        /// 程序启动后是否自动最大化窗口
        /// true：启动后占据整个屏幕
        /// false：保持初始尺寸显示
        /// 默认值：false（不自动最大化）
        /// </summary>
        public bool MaximizeAfterStart { get; set; } = false;

        /// <summary>
        /// 是否锁定窗口布局
        /// true：禁止用户调整界面控件位置和大小
        /// false：允许自定义界面布局
        /// 默认值：true（锁定布局，保证界面一致性）
        /// </summary>
        public bool LockLayout { get; set; } = true;
        #endregion

        #region 运行模式配置
        /// <summary>
        /// 程序启动后是否自动进行通讯连接（如设备通讯、数据库连接等）
        /// true：启动后无需手动操作，自动建立必要连接
        /// false：需用户手动触发连接操作
        /// 默认值：false（手动连接，提高启动速度）
        /// </summary>
        public bool AutoConnectAfterStart { get; set; } = false;

        /// <summary>
        /// 开机后是否自动切换到自动运行模式
        /// true：无需人工干预，程序直接进入自动生产状态
        /// false：启动后停留在手动模式，等待用户确认
        /// 默认值：true（自动模式，提高生产效率）
        /// </summary>
        public bool SwitchToAutoModeOnStartup { get; set; } = true;  // 属性名优化：更清晰表达"启动时切换到自动模式"

        /// <summary>
        /// 操作系统启动后是否自动运行本程序
        /// true：添加到系统启动项，随系统自动启动
        /// false：需用户手动打开程序
        /// 默认值：false（手动启动，避免资源占用）
        /// </summary>
        public bool AutoStartWithSystem { get; set; } = false;  // 属性名优化：明确与系统启动的关联

        /// <summary>
        /// 程序启动后是否直接显示生产界面
        /// true：启动后跳过欢迎页/首页，直接进入生产操作界面
        /// false：按正常流程显示首页
        /// 默认值：false（显示首页，便于用户了解系统状态）
        /// </summary>
        public bool ShowProductionFormAfterStart { get; set; } = false;

        /// <summary>
        /// 程序启动后是否自动锁定界面（需要登录解锁）
        /// true：启动后立即锁定，防止未授权操作
        /// false：启动后保持解锁状态
        /// 默认值：false（不自动锁定，方便操作人员快速使用）
        /// </summary>
        public bool AutoLockAfterStart { get; set; } = false;
        #endregion

        #region 数据存储配置
        /// <summary>
        /// 业务数据的自动保留天数
        /// 超过该天数的历史数据会被自动清理（需配合定时任务实现）
        /// 默认值：7天（平衡数据追溯需求与存储空间占用）
        /// </summary>
        public int DataRetentionDays { get; set; } = 7;  // 属性名优化：明确表示"数据保留天数"

        /// <summary>
        /// 程序退出时是否自动保存当前配置
        /// true：退出前自动保存参数修改，无需手动点击保存
        /// false：需用户手动触发保存操作，否则修改可能丢失
        /// 默认值：true（自动保存，防止数据丢失）
        /// </summary>
        public bool SaveOnExit { get; set; } = true;  // 属性名优化：更简洁清晰

        /// <summary>
        /// 是否在程序启动和关闭时自动备份程序配置
        /// true：每次启动/关闭时创建配置备份，用于故障恢复
        /// false：不自动备份，需手动执行备份操作
        /// 默认值：true（自动备份，提高系统可靠性）
        /// </summary>
        public bool AutoBackupProgram { get; set; } = true;
        #endregion

        #region 系统标识与权限配置
        /// <summary>
        /// 公司名称（用于界面标题、报表抬头等场景）
        /// 默认值：捷普科技（无锡）有限公司
        /// </summary>
        private string _companyName = "捷普科技（无锡）有限公司";
        public string CompanyName
        {
            get => _companyName;
            set => _companyName = value ?? throw new ArgumentNullException(nameof(value), "公司名称不能为空");  // 增加空值校验
        }

        /// <summary>
        /// 项目名称（当前运行的项目/产品线标识）
        /// 默认值：未命名（提示用户进行配置）
        /// </summary>
        private string _programTitle = "未命名";
        public string ProgramTitle
        {
            get => _programTitle;
            set => _programTitle = value ?? throw new ArgumentNullException(nameof(value), "项目名称不能为空");  // 增加空值校验
        }

        /// <summary>
        /// 系统显示语言
        /// 默认值：中文（Chinese）
        /// </summary>
        public LanguageType Language { get; set; } = LanguageType.Chinese;

        /// <summary>
        /// 设备名称（当前运行程序的设备标识，用于多设备组网场景）
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// 是否启用权限管控
        /// true：根据用户角色限制操作权限（如修改参数、启动生产等）
        /// false：关闭权限校验，所有用户拥有全部操作权限
        /// 默认值：false（简化操作，适合单机场景）
        /// </summary>
        public bool EnablePermissionControl { get; set; } = false;
        #endregion

        #region 生产配置
        /// <summary>
        /// 待加载的作业（Job）名称
        /// 程序启动时会自动加载该名称对应的生产作业配置
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// 当前生产型号的名称
        /// 用于标识正在生产的产品型号，关联生产数据统计
        /// </summary>
        public string ProductName { get; set; }
        #endregion
    }
}
