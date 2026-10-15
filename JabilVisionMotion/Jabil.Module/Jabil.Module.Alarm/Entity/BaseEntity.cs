using SqlSugar;
using System;

namespace Jabil.Module.Alarm.Entity
{
    /// <summary>
    /// 实体基类
    /// 所有业务实体类的父类，提供通用的基础字段
    /// </summary>
    public class BaseEntity
    {
        /// <summary>
        /// 自增主键ID
        /// 数据库表的唯一标识字段，自动增长
        /// </summary>
        [SugarColumn(ColumnName = "ID", IsPrimaryKey = true, IsIdentity = true)]
        public int ID { get; set; }

        /// <summary>
        /// 设备名称
        /// 关联的设备唯一名称，作为业务主键
        /// </summary>
        [SugarColumn(ColumnName = "设备名称", ColumnDescription = "设备唯一标识名称")]
        public string 设备名称 { get; set; }
    }
}
