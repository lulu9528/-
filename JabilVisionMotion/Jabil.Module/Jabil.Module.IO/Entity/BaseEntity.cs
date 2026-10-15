using SqlSugar;
using System;

namespace Jabil.Module.IO.Entity
{
    /// <summary>
    /// IO模块基础实体类（抽象类）
    /// 作用：为所有IO相关的实体类提供通用字段和数据库映射配置，实现代码复用
    /// 设计原则：
    /// - 包含所有实体共有的核心字段（主键、业务标识、审计信息、删除标识）
    /// - 标记为abstract，强制子类继承，避免直接实例化
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 主键ID（自增）
        /// 数据库层面的唯一标识，用于记录的增删改查定位
        /// 采用自增整数类型，提升查询和索引性能
        /// </summary>
        [SugarColumn(
            IsPrimaryKey = true, // 标记为主键
            IsIdentity = true,  // 标记为自增（数据库自动生成唯一值）
            ColumnDescription = "主键ID，自增唯一标识，用于数据库层面定位记录"
        )]
        public int ID { get; set; }

        /// <summary>
        /// 卡名称（业务唯一标识）
        /// 用于业务层面标识IO卡的名称（如"Line1_IOCard01"），需在系统中保持唯一
        /// 作为IO卡及其点位的关联字段（子实体通过此名称关联到所属IO卡）
        /// </summary>
        [SugarColumn(
            ColumnName = "卡名称", // 数据库列名（英文，与业务代码命名一致）
            Length = 100, // 限制最大长度为100字符，满足业务命名需求
            IsNullable = false, // 非空约束：卡名称为必填项
            ColumnDescription = "IO卡的业务名称，系统内唯一，用于关联卡与点位的关系"
        )]
        public string 卡名称 { get; set; }
    }
}