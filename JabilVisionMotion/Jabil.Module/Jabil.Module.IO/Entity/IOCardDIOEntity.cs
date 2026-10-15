using SqlSugar;
using System;

namespace Jabil.Module.IO.Entity
{
    /// <summary>
    /// IO卡点位实体类
    /// 对应数据库表"IOCardDIO"，用于存储单张IO卡上所有输入/输出点位的详细配置
    /// 继承自BaseEntity，自动获取主键ID、所属IO卡名称（CardName）、创建时间等公共字段
    /// 核心作用：建立IO卡硬件点位与业务逻辑的关联，支持点位状态监控和控制
    /// </summary>
    [SugarTable("IOCardDIO", TableDescription = "IO卡点位信息表，存储每张IO卡的输入输出点位配置，包括名称、类型、索引等")]
    public class IOCardDIOEntity : BaseEntity
    {
        /// <summary>
        /// 点位名称（业务标识）
        /// 用于业务层面标识点位的名称，如"传送带启动信号"、"急停按钮输入"等
        /// 命名需体现点位的实际功能，便于运维和业务理解
        /// </summary>
        [SugarColumn(
            ColumnName = "点位名称", // 数据库列名（中文，直观反映业务含义）
            Length = 100, // 限制最大长度为100字符，平衡描述性和存储效率
            IsNullable = false, // 非空约束：点位名称为必填项
            ColumnDescription = "点位的业务名称，用于标识点位功能（如'急停按钮输入'）"
        )]
        public string 点位名称 { get; set; }

        /// <summary>
        /// 点位类型（输入/输出）
        /// 区分点位的硬件类型："输入"（DI）表示外部信号输入（如传感器、按钮），"输出"（DO）表示控制信号输出（如继电器、指示灯）
        /// 类型决定了点位的读写权限（输入点只读，输出点可写）
        /// </summary>
        [SugarColumn(
            ColumnName = "点位类型", // 数据库列名（中文，明确区分类型）
            Length = 20, // 限制长度为20字符，足够存储"输入"/"输出"及扩展类型
            IsNullable = false, // 非空约束：类型为必填项
            ColumnDescription = "点位的硬件类型，取值为'输入'（DI）或'输出'（DO），决定读写权限"
        )]
        public string 点位类型 { get; set; }

        /// <summary>
        /// 点位索引（卡内唯一标识）
        /// 点位在所属IO卡内的硬件编号（如第3个输入点索引为3），范围与IO卡型号相关（通常0~255）
        /// 与CardName（继承自BaseEntity）组合形成唯一标识，确保同一张卡内索引不重复
        /// </summary>
        [SugarColumn(
            ColumnName = "点位索引", // 数据库列名（英文，与硬件参数命名一致）
            IsNullable = false, // 非空约束：索引为必填项
            ColumnDescription = "点位在IO卡内的硬件编号，同一张卡内唯一，用于硬件寻址"
        )]
        public ushort 点位索引 { get; set; } // 使用ushort类型，适配多数IO卡的索引范围（0~65535）

        /// <summary>
        /// 绑定代码（业务逻辑关联）
        /// 关联到业务系统中的功能标识，如"ConveyorStart"（传送带启动）、"EmergencyStop"（急停）
        /// 用于将硬件点位与业务逻辑绑定，实现"点位状态变化→触发业务动作"的联动
        /// </summary>
        [SugarColumn(
            ColumnName = "绑定代码", // 数据库列名（中文，体现与业务的绑定关系）
            Length = 50, // 限制长度为50字符，适配多数业务标识的命名规范
            IsNullable = true, // 允许为空：部分点位可能暂未绑定业务逻辑
            ColumnDescription = "与点位关联的业务功能标识，用于实现硬件与业务逻辑的联动"
        )]
        public string 绑定代码 { get; set; }
    }
}