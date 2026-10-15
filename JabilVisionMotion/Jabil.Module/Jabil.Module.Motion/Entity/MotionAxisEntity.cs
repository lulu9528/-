using SqlSugar;
using System;

namespace Jabil.Module.Motion.Entity
{
    /// <summary>
    /// 运动轴实体类
    /// 映射数据库中的MotionAxis表，存储运动轴的配置信息
    /// </summary>
    [SugarTable("MotionAxis")]
    public class MotionAxisEntity : BaseEntity
    {
        /// <summary>
        /// 轴名称
        /// </summary>
        [SugarColumn(ColumnName = "轴名称", ColumnDescription = "运动轴的名称标识")]
        public string 轴名称 { get; set; }

        /// <summary>
        /// 轴类型
        /// </summary>
        [SugarColumn(ColumnName = "轴类型", ColumnDescription = "运动轴的类型分类")]
        public string 轴类型 { get; set; }

        /// <summary>
        /// 轴卡类型
        /// </summary>
        [SugarColumn(ColumnName = "轴卡类型", ColumnDescription = "所属轴卡的类型")]
        public string 轴卡类型 { get; set; }

        /// <summary>
        /// 轴卡卡号索引
        /// </summary>
        [SugarColumn(ColumnName = "轴卡卡号索引", ColumnDescription = "轴卡在系统中的卡号索引")]
        public ushort 轴卡卡号索引 { get; set; }

        /// <summary>
        /// 轴卡轴号索引
        /// </summary>
        [SugarColumn(ColumnName = "轴卡轴号索引", ColumnDescription = "轴在轴卡上的轴号索引")]
        public ushort 轴卡轴号索引 { get; set; }

        /// <summary>
        /// 脉冲比
        /// </summary>
        [SugarColumn(ColumnName = "脉冲比", ColumnDescription = "运动控制的脉冲比例参数")]
        public int 脉冲比 { get; set; }

        /// <summary>
        /// 到位信号范围
        /// </summary>
        [SugarColumn(ColumnName = "到位信号范围", ColumnDescription = "判定为到位的信号范围阈值")]
        public uint 到位信号范围 { get; set; }

        /// <summary>
        /// 到位信号稳定时间
        /// </summary>
        [SugarColumn(ColumnName = "到位信号稳定时间", ColumnDescription = "到位信号需要保持稳定的时间")]
        public uint 到位信号稳定时间 { get; set; }

        /// <summary>
        /// 缓停减速度
        /// </summary>
        [SugarColumn(ColumnName = "缓停减速度", ColumnDescription = "缓慢停止时的减速度参数")]
        public double 缓停减速度 { get; set; }

        /// <summary>
        /// 急停加速度
        /// </summary>
        [SugarColumn(ColumnName = "急停加速度", ColumnDescription = "紧急停止时的加速度参数")]
        public double 急停加速度 { get; set; }

        /// <summary>
        /// 单轴加速度
        /// </summary>
        [SugarColumn(ColumnName = "单轴加速度", ColumnDescription = "单轴运动时的加速度参数")]
        public double 单轴加速度 { get; set; }

        /// <summary>
        /// 单轴减速度
        /// </summary>
        [SugarColumn(ColumnName = "单轴减速度", ColumnDescription = "单轴运动时的减速度参数")]
        public double 单轴减速度 { get; set; }

        /// <summary>
        /// 加加速时间
        /// </summary>
        [SugarColumn(ColumnName = "加加速时间", ColumnDescription = "加速度变化的时间参数")]
        public short 加加速时间 { get; set; }

        /// <summary>
        /// 单轴运行速度
        /// </summary>
        [SugarColumn(ColumnName = "单轴运行速度", ColumnDescription = "单轴运动时的运行速度")]
        public double 单轴运行速度 { get; set; }

        /// <summary>
        /// 正向软限位
        /// </summary>
        [SugarColumn(ColumnName = "正向软限位", ColumnDescription = "正向运动的软件限位位置")]
        public int 正向软限位 { get; set; }

        /// <summary>
        /// 负向软限位
        /// </summary>
        [SugarColumn(ColumnName = "负向软限位", ColumnDescription = "负向运动的软件限位位置")]
        public int 负向软限位 { get; set; }

        /// <summary>
        /// 回零方法
        /// </summary>
        [SugarColumn(ColumnName = "回零方法", ColumnDescription = "回零操作所采用的方法")]
        public short 回零方法 { get; set; }

        /// <summary>
        /// 回零速度1
        /// </summary>
        [SugarColumn(ColumnName = "回零速度1", ColumnDescription = "回零过程中的第一阶段速度")]
        public uint 回零速度1 { get; set; }

        /// <summary>
        /// 回零速度2
        /// </summary>
        [SugarColumn(ColumnName = "回零速度2", ColumnDescription = "回零过程中的第二阶段速度")]
        public uint 回零速度2 { get; set; }

        /// <summary>
        /// 回零加速度
        /// </summary>
        [SugarColumn(ColumnName = "回零加速度", ColumnDescription = "回零过程中的加速度参数")]
        public uint 回零加速度 { get; set; }

        /// <summary>
        /// 回零偏移
        /// </summary>
        [SugarColumn(ColumnName = "回零偏移", ColumnDescription = "回零后的位置偏移量")]
        public int 回零偏移 { get; set; }

        /// <summary>
        /// 限位信号锁定
        /// </summary>
        [SugarColumn(ColumnName = "限位信号锁定", ColumnDescription = "限位信号的锁定状态设置")]
        public short 限位信号锁定 { get; set; }

        /// <summary>
        /// 绑定代码
        /// </summary>
        [SugarColumn(ColumnName = "绑定代码", ColumnDescription = "与其他系统组件绑定的代码标识")]
        public string 绑定代码 { get; set; }
    }
}
