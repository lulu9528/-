using SqlSugar;
using System;

namespace Jabil.Module.Alarm.Entity
{
    /// <summary>
    /// 报警通信DIO实体类
    /// 映射数据库中的AlarmCommunicationDIO表
    /// 存储报警通信数字输入输出设备的相关配置信息
    /// </summary>
    [SugarTable("AlarmCommunicationDIO")]
    public class AlarmCommunicationDIOEntity : BaseEntity
    {
        /// <summary>
        /// 点位类型
        /// 描述DIO点位的类型（如输入、输出等）
        /// </summary>
        [SugarColumn(ColumnName = "点位类型")]
        public string 点位类型 { get; set; }

        /// <summary>
        /// 点位索引
        /// DIO点位的唯一索引，用于标识不同点位
        /// 对应数据库中的"点位索引"字段
        /// </summary>
        [SugarColumn(ColumnName = "点位索引", IsPrimaryKey = true)] // 假设点位索引是主键
        public string 点位索引 { get; set; }

        /// <summary>
        /// 报警内容
        /// 当该点位触发报警时的具体描述信息
        /// </summary>
        [SugarColumn(ColumnName = "报警内容")]
        public string 报警内容 { get; set; }

        /// <summary>
        /// 优先级
        /// 报警的优先级级别（数字越大优先级越高）
        /// </summary>
        [SugarColumn(ColumnName = "优先级")]
        public int 优先级 { get; set; }

        /// <summary>
        /// 报警类型
        /// 报警的分类（如设备故障、通讯中断等）
        /// </summary>
        [SugarColumn(ColumnName = "报警类型")]
        public string 报警类型 { get; set; }

        /// <summary>
        /// 绑定代码
        /// 与该报警点位关联的业务代码或设备代码
        /// </summary>
        [SugarColumn(ColumnName = "绑定代码")]
        public string 绑定代码 { get; set; }

        /// <summary>
        /// 站别
        /// 该报警点位所属的站点或工位信息
        /// </summary>
        [SugarColumn(ColumnName = "站别")]
        public string 站别 { get; set; }
    }
}
