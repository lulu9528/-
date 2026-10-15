using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Vision.Entity
{
    /// <summary>
    /// 视觉工具实体类
    /// 映射数据库中的VisionTool表，存储视觉工具的配置信息
    /// </summary>
    [SugarTable("VisionTool")]
    public class VisionToolEntity : BaseEntity
    {
        /// <summary>
        /// 检测工具名称
        /// 工具的唯一标识名称，用于显示和识别
        /// </summary>
        [SugarColumn(ColumnName = nameof(检测工具名称), Length = 100, IsNullable = false)]
        public string 检测工具名称 { get; set; }

        /// <summary>
        /// 检测工具类型
        /// 工具的类型标识，如CogAcqFifoTool、CogToolBlock等
        /// 用于区分不同类型的视觉工具
        /// </summary>
        [SugarColumn(ColumnName = nameof(检测工具类型), Length = 50, IsNullable = false)]
        public string 检测工具类型 { get; set; }

        /// <summary>
        /// 检测工具索引
        /// 同类型工具的序号，用于区分同一类型的不同工具实例
        /// </summary>
        [SugarColumn(ColumnName = nameof(检测工具索引), IsNullable = false)]
        public int 检测工具索引 { get; set; }

        /// <summary>
        /// 检测工具路径
        /// 工具配置文件的存储路径，用于加载和保存工具配置
        /// </summary>
        [SugarColumn(ColumnName = nameof(检测工具路径), Length = 255, IsNullable = true)]
        public string 检测工具路径 { get; set; }

        /// <summary>
        /// 重写ToString方法，返回工具的关键信息
        /// 便于调试和日志记录
        /// </summary>
        /// <returns>包含工具类型、名称和索引的字符串</returns>
        public override string ToString()
        {
            return $"{检测工具类型}[{检测工具索引}]: {检测工具名称}";
        }
    }
}
