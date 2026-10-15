using SqlSugar;
using System;
using System.ComponentModel.DataAnnotations;

namespace Jabil.Module.Modbus.Entity
{
    /// <summary>
    /// Modbus通信DIO点位实体类
    /// 存储Modbus协议下数字输入输出点位的配置信息
    /// 与数据库表ModbusCommunicationDIO映射
    /// </summary>
    [SugarTable("ModbusCommunicationDIO", TableDescription = "Modbus数字量输入输出点位配置表")]
    public class ModbusCommunicationDioEntity : BaseEntity
    {
        /// <summary>
        /// 点位名称
        /// 用于标识该数字量信号的唯一名称
        /// </summary>
        [SugarColumn(ColumnName = "点位名称", Length = 100, IsNullable = false, ColumnDescription = "数字量信号的标识名称")]
        [Required(ErrorMessage = "点位名称不能为空")]
        [MaxLength(100, ErrorMessage = "点位名称长度不能超过100个字符")]
        public string 点位名称 { get; set; }

        /// <summary>
        /// 点位类型
        /// 区分数字输入和数字输出类型
        /// </summary>
        [SugarColumn(ColumnName = "点位类型", Length = 20, IsNullable = false, ColumnDescription = "点位类型：DI(数字输入)、DO(数字输出)")]
        [Required(ErrorMessage = "点位类型不能为空")]
        [RegularExpression("^(DI|DO)$", ErrorMessage = "点位类型只能是DI或DO")]
        public string 点位类型 { get; set; }

        /// <summary>
        /// 点位索引
        /// Modbus协议中的寄存器地址或线圈地址
        /// 格式示例：1 ,1.1
        /// </summary>
        [SugarColumn(ColumnName = "点位索引", IsNullable = false, ColumnDescription = "Modbus设备中的点位地址索引")]
        [Required(ErrorMessage = "点位索引不能为空")]
        public string 点位索引 { get; set; }

        /// <summary>
        /// 点位值
        /// 数字量的当前值（0或1）
        /// </summary>
        [SugarColumn(ColumnName = "点位值", IsNullable = false, ColumnDescription = "数字量当前值（0-关闭，1-开启）")]
        [Range(0, 1, ErrorMessage = "点位值只能是0或1")]
        public int 点位值 { get; set; }

        /// <summary>
        /// 点位定义
        /// 详细描述该点位的功能、用途及相关业务说明
        /// </summary>
        [SugarColumn(ColumnName = "点位定义", Length = 500, IsNullable = true, ColumnDescription = "点位功能和用途的详细描述")]
        [MaxLength(500, ErrorMessage = "点位定义长度不能超过500个字符")]
        public string 点位定义 { get; set; }

        /// <summary>
        /// 数据类型
        /// 点位值的数据类型描述
        /// </summary>
        [SugarColumn(ColumnName = "数据类型", Length = 30, IsNullable = false, ColumnDescription = "点位值的数据类型")]
        [Required(ErrorMessage = "数据类型不能为空")]
        [MaxLength(30, ErrorMessage = "数据类型长度不能超过30个字符")]
        public string 数据类型 { get; set; }


        /// <summary>
        /// 绑定代码
        /// 关联的业务模块代码或设备功能标识
        /// 用于业务系统与硬件点位的映射关联
        /// </summary>
        [SugarColumn(ColumnName = "绑定代码", Length = 50, IsNullable = true, ColumnDescription = "关联的业务代码或功能标识")]
        [MaxLength(50, ErrorMessage = "绑定代码长度不能超过50个字符")]
        public string 绑定代码 { get; set; }


    }
}