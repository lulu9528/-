using Jabil.Lib.Common;
using System;

namespace Jabil.Module.Vision.Common.Dtos
{
    /// <summary>
    /// 视觉工具DTO
    /// 用于视觉工具数据的传输和UI绑定
    /// 继承自BaseDto，支持属性变更通知机制
    /// </summary>
    public class VisionToolDto : BaseDto
    {
        private string _检测工具名称;
        /// <summary>
        /// 检测工具名称
        /// 工具的唯一标识名称，用于显示和识别
        /// </summary>
        public string 检测工具名称
        {
            get => _检测工具名称;
            set => SetProperty(ref _检测工具名称, value);
        }

        private string _检测工具类型;
        /// <summary>
        /// 检测工具类型
        /// 工具的分类标识，如CogAcqFifoTool、CogToolBlock等
        /// </summary>
        public string 检测工具类型
        {
            get => _检测工具类型;
            set => SetProperty(ref _检测工具类型, value);
        }

        private int _检测工具索引;
        /// <summary>
        /// 检测工具索引
        /// 同类型工具的序号，用于区分同一类型的不同工具实例
        /// </summary>
        public int 检测工具索引
        {
            get => _检测工具索引;
            set => SetProperty(ref _检测工具索引, value);
        }

        private string _检测工具路径;
        /// <summary>
        /// 检测工具路径
        /// 工具配置文件的存储路径，用于加载和保存工具配置
        /// </summary>
        public string 检测工具路径
        {
            get => _检测工具路径;
            set => SetProperty(ref _检测工具路径, value);
        }
    }
}
