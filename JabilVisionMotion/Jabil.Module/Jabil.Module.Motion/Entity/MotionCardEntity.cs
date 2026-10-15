using SqlSugar;
using System;

namespace Jabil.Module.Motion.Entity
{
    /// <summary>
    /// 轴卡实体类
    /// 映射数据库中的MotionCard表，存储轴卡的配置信息
    /// </summary>
    [SugarTable("MotionCard")]
    public class MotionCardEntity : BaseEntity
    {
        /// <summary>
        /// 轴卡名称
        /// </summary>
        [SugarColumn(ColumnName = "轴卡名称", ColumnDescription = "轴卡的名称标识")]
        public string 轴卡名称 { get; set; }

        /// <summary>
        /// 轴卡类型
        /// </summary>
        [SugarColumn(ColumnName = "轴卡类型", ColumnDescription = "轴卡的型号或类型分类")]
        public string 轴卡类型 { get; set; }

        /// <summary>
        /// 轴卡卡号
        /// </summary>
        [SugarColumn(ColumnName = "轴卡卡号", ColumnDescription = "轴卡在系统中的唯一编号")]
        public short 轴卡卡号 { get; set; }

        /// <summary>
        /// 轴卡参数文件
        /// </summary>
        [SugarColumn(ColumnName = "轴卡参数文件", ColumnDescription = "存储轴卡配置参数的文件路径")]
        public string 轴卡参数文件 { get; set; }

        /// <summary>
        /// 急停极性
        /// </summary>
        [SugarColumn(ColumnName = "急停极性", ColumnDescription = "急停信号的极性设置（高电平/低电平有效）")]
        public short 急停极性 { get; set; }
    }
}
