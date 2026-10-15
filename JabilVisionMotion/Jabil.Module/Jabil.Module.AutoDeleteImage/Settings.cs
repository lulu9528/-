using System;
using System.IO;
using System.Xml.Serialization;

namespace Jabil.Module.AutoDeleteImage
{
    /// <summary>
    /// 应用程序设置类，存储所有用户配置
    /// </summary>
    [Serializable]
    public class Settings
    {
        /// <summary>
        /// OK图片的保留天数
        /// </summary>
        public int OkImageRetentionDays { get; set; } = 30;

        /// <summary>
        /// NG图片的保留天数
        /// </summary>
        public int NgImageRetentionDays { get; set; } = 7;

        /// <summary>
        /// 被监控的文件夹路径
        /// </summary>
        public string MonitoredFolder { get; set; } = string.Empty;

        /// <summary>
        /// 用于识别OK图片的关键字（多个关键字用逗号分隔）
        /// </summary>
        public string OkKeyword { get; set; } = "ok";

        /// <summary>
        /// 用于识别NG图片的关键字（多个关键字用逗号分隔）
        /// </summary>
        public string NgKeyword { get; set; } = "ng";

        /// <summary>
        /// 是否包含子文件夹
        /// </summary>
        public bool IncludeSubfolders { get; set; } = true;

        /// <summary>
        /// 删除文件前是否需要确认
        /// </summary>
        public bool ConfirmBeforeDeletion { get; set; } = true;

        /// <summary>
        /// 自动检查的间隔时间（分钟）
        /// </summary>
        public int CheckIntervalMinutes { get; set; } = 60;

        /// <summary>
        /// 是否在开机时自动开始监控
        /// </summary>
        public bool AutoStartMonitoring { get; set; } = true;

        /// <summary>
        /// 是否在开机启动时自动隐藏窗口
        /// </summary>
        public bool AutoHideOnStartup { get; set; } = true;
    }
}
