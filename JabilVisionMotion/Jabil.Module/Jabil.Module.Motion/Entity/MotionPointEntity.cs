using SqlSugar;
using System;

namespace Jabil.Module.Motion.Entity
{
    /// <summary>
    /// 运动点位实体类
    /// 映射数据库中的MotionPoint表，存储运动点位的配置信息
    /// </summary>
    [SugarTable("MotionPoint")]
    public class MotionPointEntity : BaseEntity
    {
        /// <summary>
        /// 点位名称
        /// </summary>
        [SugarColumn(ColumnName = "点位名称", ColumnDescription = "运动点位的名称标识")]
        public string 点位名称 { get; set; }

        /// <summary>
        /// 绑定代码
        /// </summary>
        [SugarColumn(ColumnName = "绑定代码", ColumnDescription = "与其他系统组件绑定的代码标识")]
        public string 绑定代码 { get; set; }

        /// <summary>
        /// 点位类型
        /// </summary>
        [SugarColumn(ColumnName = "点位类型", ColumnDescription = "运动点位的类型分类")]
        public string 点位类型 { get; set; }

        /// <summary>
        /// 点位定义
        /// </summary>
        [SugarColumn(ColumnName = "点位定义", ColumnDescription = "运动点位的具体定义信息")]
        public string 点位定义 { get; set; }

        #region X轴相关属性
        /// <summary>
        /// X轴绑定轴
        /// </summary>
        [SugarColumn(ColumnName = "X轴绑定轴", ColumnDescription = "X轴所绑定的物理轴标识")]
        public string X轴绑定轴 { get; set; }

        /// <summary>
        /// X轴目标位置
        /// </summary>
        [SugarColumn(ColumnName = "X轴目标位置", ColumnDescription = "X轴的目标位置坐标")]
        public double X轴目标位置 { get; set; }

        /// <summary>
        /// X轴运行速度
        /// </summary>
        [SugarColumn(ColumnName = "X轴运行速度", ColumnDescription = "X轴运动时的运行速度")]
        public double X轴运行速度 { get; set; }
        #endregion

        #region Y轴相关属性
        /// <summary>
        /// Y轴绑定轴
        /// </summary>
        [SugarColumn(ColumnName = "Y轴绑定轴", ColumnDescription = "Y轴所绑定的物理轴标识")]
        public string Y轴绑定轴 { get; set; }

        /// <summary>
        /// Y轴目标位置
        /// </summary>
        [SugarColumn(ColumnName = "Y轴目标位置", ColumnDescription = "Y轴的目标位置坐标")]
        public double Y轴目标位置 { get; set; }

        /// <summary>
        /// Y轴运行速度
        /// </summary>
        [SugarColumn(ColumnName = "Y轴运行速度", ColumnDescription = "Y轴运动时的运行速度")]
        public double Y轴运行速度 { get; set; }
        #endregion

        #region Z轴相关属性
        /// <summary>
        /// Z轴绑定轴
        /// </summary>
        [SugarColumn(ColumnName = "Z轴绑定轴", ColumnDescription = "Z轴所绑定的物理轴标识")]
        public string Z轴绑定轴 { get; set; }

        /// <summary>
        /// Z轴目标位置
        /// </summary>
        [SugarColumn(ColumnName = "Z轴目标位置", ColumnDescription = "Z轴的目标位置坐标")]
        public double Z轴目标位置 { get; set; }

        /// <summary>
        /// Z轴运行速度
        /// </summary>
        [SugarColumn(ColumnName = "Z轴运行速度", ColumnDescription = "Z轴运动时的运行速度")]
        public double Z轴运行速度 { get; set; }
        #endregion

        #region U轴相关属性
        /// <summary>
        /// U轴绑定轴
        /// </summary>
        [SugarColumn(ColumnName = "U轴绑定轴", ColumnDescription = "U轴所绑定的物理轴标识")]
        public string U轴绑定轴 { get; set; }

        /// <summary>
        /// U轴目标位置
        /// </summary>
        [SugarColumn(ColumnName = "U轴目标位置", ColumnDescription = "U轴的目标位置坐标")]
        public double U轴目标位置 { get; set; }

        /// <summary>
        /// U轴运行速度
        /// </summary>
        [SugarColumn(ColumnName = "U轴运行速度", ColumnDescription = "U轴运动时的运行速度")]
        public double U轴运行速度 { get; set; }
        #endregion

        #region R轴相关属性
        /// <summary>
        /// R轴绑定轴
        /// </summary>
        [SugarColumn(ColumnName = "R轴绑定轴", ColumnDescription = "R轴所绑定的物理轴标识")]
        public string R轴绑定轴 { get; set; }

        /// <summary>
        /// R轴目标位置
        /// </summary>
        [SugarColumn(ColumnName = "R轴目标位置", ColumnDescription = "R轴的目标位置坐标")]
        public double R轴目标位置 { get; set; }

        /// <summary>
        /// R轴运行速度
        /// </summary>
        [SugarColumn(ColumnName = "R轴运行速度", ColumnDescription = "R轴运动时的运行速度")]
        public double R轴运行速度 { get; set; }
        #endregion
    }
}
