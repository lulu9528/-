using SqlSugar;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Alarm.Entity
{
    /// <summary>
    /// 过程错误实体类
    /// 映射数据库中的ProcessError表
    /// 存储生产过程中发生的错误报警信息
    /// </summary>
    [SugarTable("ProcessError")]
    public class ProcessErrorEntity 
    {

        /// <summary>
        /// 报警ID
        /// 错误报警的唯一标识符
        /// </summary>
        [SugarColumn(ColumnName = "报警ID", IsPrimaryKey = true)]
        public string 报警ID { get; set; }

        /// <summary>
        /// 异常开始时间
        /// 错误发生的具体时间
        /// </summary>
        [SugarColumn(ColumnName = "异常开始时间")]
        public string 异常开始时间 { get; set; }

        /// <summary>
        /// 结束时间
        /// 错误被解决的时间
        /// </summary>
        [SugarColumn(ColumnName = "结束时间")]
        public string 结束时间 { get; set; }

        /// <summary>
        /// 报警内容
        /// 错误的详细描述信息
        /// </summary>
        [SugarColumn(ColumnName = "报警内容")]
        public string 报警内容 { get; set; }

        /// <summary>
        /// 耗时
        /// 错误从发生到解决的持续时间（单位：分钟）
        /// </summary>
        [SugarColumn(ColumnName = "耗时")]
        public double 耗时 { get; set; }


        /// <summary>
        /// 日期
        /// 错误发生的日期
        /// </summary>
        [SugarColumn(ColumnName = "日期")]
        public DateTime 日期 { get; set; }


        /// <summary>
        /// 站别
        /// 发生错误的站点或工位标识
        /// </summary>
        [SugarColumn(ColumnName = "站别")]
        public string 站别 { get; set; }

    }
}
