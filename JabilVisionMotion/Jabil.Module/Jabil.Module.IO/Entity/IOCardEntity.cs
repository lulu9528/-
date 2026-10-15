using SqlSugar;
using System;

namespace Jabil.Module.IO.Entity
{
    /// <summary>
    /// IO卡实体类
    /// 对应数据库表"IOCard"，用于存储IO卡的基础配置信息
    /// 继承自BaseEntity，自动获得主键ID、卡名称、创建时间等公共字段
    /// </summary>
    [SugarTable("IOCard", TableDescription = "IO卡信息表，存储所有IO卡的基础配置（型号、索引、启用状态等）")]
    public class IOCardEntity : BaseEntity
    {
        /// <summary>
        /// 卡类型（硬件型号）
        /// 例如："3400"、"7230"、"M60"等，用于区分不同硬件型号的IO卡
        /// 与硬件驱动适配逻辑相关，型号决定了支持的通信协议和功能
        /// </summary>
        [SugarColumn(
            ColumnName = "卡类型", // 数据库列名（中文，兼容现有表结构）
            Length = 50, // 限制最大长度为50字符，避免存储过长字符串
            IsNullable = false, // 非空约束：卡类型为必填项
            ColumnDescription = "IO卡的硬件型号标识，如3400、7230等，决定硬件适配逻辑",
            IndexGroupNameList = new[] { "Idx_IOCard_Type" } // 为卡类型创建索引，提升按类型查询的效率
        )]
        public string 卡类型 { get; set; }

        /// <summary>
        /// 卡号索引（硬件寻址标识）
        /// 用于硬件通信时的设备寻址（如Modbus地址、板卡编号等），同一型号下索引唯一
        /// 类型为ushort（无符号16位整数），支持0~65535范围的索引值
        /// </summary>
        [SugarColumn(
            ColumnName = "卡号索引", // 数据库列名（中文，兼容现有表结构）
            IsNullable = false, // 非空约束：索引为必填项
            ColumnDescription = "IO卡的硬件索引编号，用于通信时定位设备（如Modbus地址）",
            UniqueGroupNameList = new[] { "UQ_IOCard_CardName_Index" } // 联合唯一约束（与BaseEntity中的CardName组合）
            // 联合约束确保：同一卡名称下不允许重复的索引值，避免硬件寻址冲突
        )]
        public ushort 卡号索引 { get; set; }
    }
}