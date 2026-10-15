using System;

namespace Jabil.Lib.Parameter
{
    /// <summary>
    /// 视觉系统参数配置类
    /// 用于存储和管理机器视觉模块的核心参数，特别是图像存储相关的设置
    /// 支持序列化，可用于参数的持久化存储（如保存到配置文件）
    /// </summary>
    [Serializable] // 标记为可序列化，允许对象转换为字节流进行存储或传输
    public class VisionParameters // 类名优化：使用更规范的PascalCase命名，明确表示"视觉参数"
    {
        #region 图像存储基础配置
        /// <summary>
        /// 是否存储合格（OK）图像的开关状态
        /// true：拍摄的合格产品图像将被保存
        /// false：合格产品图像不保存（节省存储空间）
        /// 默认值：true（启用存储）
        /// </summary>
        public bool SaveOkImages { get; set; } = true;

        /// <summary>
        /// 图像文件的自动保留天数
        /// 超过该天数的历史图像将被自动清理（需配合定时任务实现）
        /// 默认值：7天（平衡存储占用与追溯需求）
        /// </summary>
        public int SaveImagesRetentionDays { get; set; } = 7;

        /// <summary>
        /// 是否存储不合格（NG）图像的开关状态
        /// true：拍摄的不合格产品图像将被保存
        /// false：不合格产品图像不保存
        /// 默认值：true（建议保留，用于质量分析）
        /// </summary>
        public bool SaveNgImages { get; set; } = true;
        #endregion

        #region 兼容字段（用于配置文件解析或历史版本兼容）
        /// <summary>
        /// 合格图像存储开关的字符串形式（兼容旧配置格式）
        /// 通常用于从文本配置文件（如INI）读取时的临时存储，后续可转换为bool类型的SaveOkImages
        /// </summary>
        public string _bSaveOKImages;

        /// <summary>
        /// 不合格图像存储开关的字符串形式（兼容旧配置格式）
        /// 用途同_bSaveOKImages，用于兼容字符串格式的配置项
        /// </summary>
        public string _bSaveNGImages;

        /// <summary>
        /// 合格图像的保留天数（兼容旧配置字段）
        /// 与SaveImagesRetentionDays功能一致，用于平滑过渡旧版本参数
        /// </summary>
        public int _SaveOKImageDays;

        /// <summary>
        /// 不合格图像的保留天数（兼容旧配置字段）
        /// 与SaveImagesRetentionDays功能一致，用于历史参数兼容
        /// </summary>
        public int _SaveNGImageDays;
        #endregion

        #region 构造函数（可选，用于参数初始化或兼容处理）
        /// <summary>
        /// 初始化视觉参数类的实例
        /// 可在此处添加旧版本参数向新版本的迁移逻辑
        /// </summary>
        public VisionParameters()
        {
            // 示例：如果存在旧字段值，优先使用旧字段初始化新属性（用于升级兼容）
            if (_SaveOKImageDays > 0)
                SaveImagesRetentionDays = _SaveOKImageDays;

            // 解析字符串类型的开关状态到bool属性（假设旧配置用"1"/"0"表示开关）
            if (bool.TryParse(_bSaveOKImages, out bool okSave))
                SaveOkImages = okSave;

            if (bool.TryParse(_bSaveNGImages, out bool ngSave))
                SaveNgImages = ngSave;
        }


        public CoordinateData 飞达左拍照标准位 = new CoordinateData();

        public CoordinateData 飞达右拍照标准位 = new CoordinateData();

        public CoordinateData 飞达左旋转中心 = new CoordinateData();

        public CoordinateData 飞达右旋转中心 = new CoordinateData();

        public CoordinateData 飞达左补偿 = new CoordinateData();

        public CoordinateData 飞达右补偿 = new CoordinateData();




        #endregion
    }
}