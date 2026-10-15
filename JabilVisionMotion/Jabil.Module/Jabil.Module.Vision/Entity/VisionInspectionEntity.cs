using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Vision.Entity
{
    /// <summary>
    /// 视觉检测点位实体类
    /// 映射数据库中的VisionInspection表，存储检测点位的配置信息
    /// 包含点位基本信息、关联工具信息及图像采集参数等
    /// </summary>
    [SugarTable("VisionInspection")]
    public class VisionInspectionEntity : BaseEntity
    {
        /// <summary>
        /// 检测点位名称
        /// 点位的唯一标识名称，用于显示和识别
        /// </summary>
        [SugarColumn(ColumnName = nameof(检测点位名称), Length = 100, IsNullable = false)]
        public string 检测点位名称 { get; set; }

        /// <summary>
        /// 检测点位类型
        /// 点位的分类标识，如外观检测、尺寸测量、定位等
        /// </summary>
        [SugarColumn(ColumnName = nameof(检测点位类型), Length = 50, IsNullable = false)]
        public string 检测点位类型 { get; set; }

        /// <summary>
        /// 检测点位定义
        /// 点位的具体功能描述或检测标准定义
        /// </summary>
        [SugarColumn(ColumnName = nameof(检测点位定义), Length = 500, IsNullable = false)]
        public string 检测点位定义 { get; set; }

        /// <summary>
        /// 检测点位索引
        /// 点位的序号，用于排序和流程控制
        /// </summary>
        [SugarColumn(ColumnName = nameof(检测点位索引), IsNullable = false)]
        public int 检测点位索引 { get; set; }

        /// <summary>
        /// 绑定取像工具名称
        /// 关联的图像采集工具名称，用于获取检测所需图像
        /// </summary>
        [SugarColumn(ColumnName = nameof(绑定取像工具名称), Length = 100, IsNullable = true)]
        public string 绑定取像工具名称 { get; set; }

        /// <summary>
        /// 绑定检测工具名称
        /// 关联的视觉检测工具名称，用于执行具体检测算法
        /// </summary>
        [SugarColumn(ColumnName = nameof(绑定检测工具名称), Length = 100, IsNullable = true)]
        public string 绑定检测工具名称 { get; set; }

        /// <summary>
        /// 绑定标定工具名称
        /// 关联的坐标标定工具名称，用于坐标转换
        /// </summary>
        [SugarColumn(ColumnName = nameof(绑定标定工具名称), Length = 100, IsNullable = true)]
        public string 绑定标定工具名称 { get; set; }

        /// <summary>
        /// 产品颜色定义
        /// 该点位检测的产品颜色标准，用于颜色检测场景
        /// </summary>
        [SugarColumn(ColumnName = nameof(产品颜色定义), Length = 50, IsNullable = true)]
        public string 产品颜色定义 { get; set; }

        /// <summary>
        /// 取像工具曝光值
        /// 关联取像工具的曝光参数，影响图像质量
        /// </summary>
        [SugarColumn(ColumnName = nameof(取像工具曝光值), IsNullable = false, DefaultValue = "50.0")]
        public double 取像工具曝光值 { get; set; } = 50.0;

        /// <summary>
        /// 取像工具对比度值
        /// 关联取像工具的对比度参数，影响图像细节表现
        /// </summary>
        [SugarColumn(ColumnName = nameof(取像工具对比度值), IsNullable = false, DefaultValue = "50.0")]
        public double 取像工具对比度值 { get; set; } = 50.0;

        /// <summary>
        /// 绑定代码
        /// 点位关联的业务逻辑代码或脚本标识
        /// </summary>
        [SugarColumn(ColumnName = nameof(绑定代码), Length = 200, IsNullable = true)]
        public string 绑定代码 { get; set; }

        /// <summary>
        /// 重写ToString方法，返回检测点位的关键信息
        /// 便于调试和日志记录时快速识别点位
        /// </summary>
        /// <returns>包含点位类型、名称和索引的字符串</returns>
        public override string ToString()
        {
            return $"{检测点位类型}[{检测点位索引}]: {检测点位名称}";
        }
    }
}
