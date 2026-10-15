using Jabil.Lib.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Vision.Common.Dtos
{
    /// <summary>
    /// 视觉检测点位DTO
    /// 用于视觉检测点位数据的传输和UI绑定
    /// 继承自BaseDto，支持属性变更通知
    /// </summary>
    public class VisionInspectionDto : BaseDto
    {
        private string _检测点位名称;
        /// <summary>
        /// 检测点位名称
        /// 点位的唯一标识名称，用于显示和识别
        /// </summary>
        public string 检测点位名称
        {
            get => _检测点位名称;
            set => SetProperty(ref _检测点位名称, value);
        }

        private string _检测点位类型;
        /// <summary>
        /// 检测点位类型
        /// 点位的分类标识，如外观检测、尺寸测量等
        /// </summary>
        public string 检测点位类型
        {
            get => _检测点位类型;
            set => SetProperty(ref _检测点位类型, value);
        }

        private string _检测点位定义;
        /// <summary>
        /// 检测点位定义
        /// 点位的具体功能描述或检测标准
        /// </summary>
        public string 检测点位定义
        {
            get => _检测点位定义;
            set => SetProperty(ref _检测点位定义, value);
        }

        private int _检测点位索引;
        /// <summary>
        /// 检测点位索引
        /// 点位的序号，用于排序和流程控制
        /// </summary>
        public int 检测点位索引
        {
            get => _检测点位索引;
            set => SetProperty(ref _检测点位索引, value);
        }

        private string _绑定取像工具名称;
        /// <summary>
        /// 绑定取像工具名称
        /// 关联的图像采集工具名称
        /// </summary>
        public string 绑定取像工具名称
        {
            get => _绑定取像工具名称;
            set => SetProperty(ref _绑定取像工具名称, value);
        }

        private string _绑定检测工具名称;
        /// <summary>
        /// 绑定检测工具名称
        /// 关联的视觉检测工具名称
        /// </summary>
        public string 绑定检测工具名称
        {
            get => _绑定检测工具名称;
            set => SetProperty(ref _绑定检测工具名称, value);
        }

        private string _绑定标定工具名称;
        /// <summary>
        /// 绑定标定工具名称
        /// 关联的坐标标定工具名称
        /// </summary>
        public string 绑定标定工具名称
        {
            get => _绑定标定工具名称;
            set => SetProperty(ref _绑定标定工具名称, value);
        }

        private string _产品颜色定义;
        /// <summary>
        /// 产品颜色定义
        /// 该点位检测的产品颜色标准
        /// </summary>
        public string 产品颜色定义
        {
            get => _产品颜色定义;
            set => SetProperty(ref _产品颜色定义, value);
        }

        private double _取像工具曝光值;
        /// <summary>
        /// 取像工具曝光值
        /// 关联取像工具的曝光参数
        /// </summary>
        public double 取像工具曝光值
        {
            get => _取像工具曝光值;
            set => SetProperty(ref _取像工具曝光值, value);
        }

        private double _取像工具对比度值;
        /// <summary>
        /// 取像工具对比度值
        /// 关联取像工具的对比度参数
        /// </summary>
        public double 取像工具对比度值
        {
            get => _取像工具对比度值;
            set => SetProperty(ref _取像工具对比度值, value);
        }

        private string _绑定代码;
        /// <summary>
        /// 绑定代码
        /// 点位关联的业务逻辑代码或脚本标识
        /// </summary>
        public string 绑定代码
        {
            get => _绑定代码;
            set => SetProperty(ref _绑定代码, value);
        }
    }
}
