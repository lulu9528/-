using Jabil.Module.Serialport.Entity;
using SqlSugar;
using System;

namespace Jabil.Module.Serialport.Entities
{
    /// <summary>
    /// 串口DIO（数字输入输出）配置实体类
    /// 对应数据库表：SerialportDIO
    /// </summary>
    [SugarTable("SerialportDIO")]
    public class SerialportDIO : BaseEntity
    {
        /// <summary>
        /// 主键ID，自增
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ID { get; set; }

        /// <summary>
        /// 点位名称
        /// </summary>
        [SugarColumn(ColumnName = "点位名称", Length = 100, IsNullable = false)]
        public string 点位名称 { get; set; }

        /// <summary>
        /// 点位类型（输入/输出等）
        /// </summary>
        [SugarColumn(ColumnName = "点位类型", Length = 50, IsNullable = false)]
        public string 点位类型 { get; set; }

        /// <summary>
        /// 通讯格式定义
        /// </summary>
        [SugarColumn(ColumnName = "通讯格式定义", Length = 200, IsNullable = true)]
        public string 通讯格式定义 { get; set; }

        /// <summary>
        /// 绑定代码
        /// </summary>
        [SugarColumn(ColumnName = "绑定代码", Length = 100, IsNullable = true)]
        public string 绑定代码 { get; set; }
    }
}
