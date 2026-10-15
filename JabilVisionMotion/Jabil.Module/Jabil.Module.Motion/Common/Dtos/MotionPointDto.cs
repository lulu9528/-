using Jabil.Lib.Common;
using Jabil.Module.Motion.Common.Dtos;
using System;

namespace Jabil.Module.Motion.Common.Dtos
{
    /// <summary>
    /// 运动点位数据传输对象
    /// 包含点位的配置信息和各轴的目标位置及运行速度
    /// </summary>
    public class MotionPointDto : BaseDto
    {
        private string _pointName;
        /// <summary>
        /// 点位名称
        /// </summary>
        public string 点位名称
        {
            get => _pointName;
            set => SetProperty(ref _pointName, value);
        }

        private string _bindingCode;
        /// <summary>
        /// 绑定代码
        /// </summary>
        public string 绑定代码
        {
            get => _bindingCode;
            set => SetProperty(ref _bindingCode, value);
        }

        private string _pointType;
        /// <summary>
        /// 点位类型
        /// </summary>
        public string 点位类型
        {
            get => _pointType;
            set => SetProperty(ref _pointType, value);
        }

        private string _pointDefinition;
        /// <summary>
        /// 点位定义
        /// </summary>
        public string 点位定义
        {
            get => _pointDefinition;
            set => SetProperty(ref _pointDefinition, value);
        }

        #region X轴相关属性
        private string _xAxisBinding;
        /// <summary>
        /// X轴绑定轴
        /// </summary>
        public string X轴绑定轴
        {
            get => _xAxisBinding;
            set => SetProperty(ref _xAxisBinding, value);
        }

        private double _xTargetPosition;
        /// <summary>
        /// X轴目标位置
        /// </summary>
        public double X轴目标位置
        {
            get => _xTargetPosition;
            set => SetProperty(ref _xTargetPosition, value);
        }

        private double _xRunningSpeed;
        /// <summary>
        /// X轴运行速度
        /// </summary>
        public double X轴运行速度
        {
            get => _xRunningSpeed;
            set => SetProperty(ref _xRunningSpeed, value);
        }
        #endregion

        #region Y轴相关属性
        private string _yAxisBinding;
        /// <summary>
        /// Y轴绑定轴
        /// </summary>
        public string Y轴绑定轴
        {
            get => _yAxisBinding;
            set => SetProperty(ref _yAxisBinding, value);
        }

        private double _yTargetPosition;
        /// <summary>
        /// Y轴目标位置
        /// </summary>
        public double Y轴目标位置
        {
            get => _yTargetPosition;
            set => SetProperty(ref _yTargetPosition, value);
        }

        private double _yRunningSpeed;
        /// <summary>
        /// Y轴运行速度
        /// </summary>
        public double Y轴运行速度
        {
            get => _yRunningSpeed;
            set => SetProperty(ref _yRunningSpeed, value);
        }
        #endregion

        #region Z轴相关属性
        private string _zAxisBinding;
        /// <summary>
        /// Z轴绑定轴
        /// </summary>
        public string Z轴绑定轴
        {
            get => _zAxisBinding;
            set => SetProperty(ref _zAxisBinding, value);
        }

        private double _zTargetPosition;
        /// <summary>
        /// Z轴目标位置
        /// </summary>
        public double Z轴目标位置
        {
            get => _zTargetPosition;
            set => SetProperty(ref _zTargetPosition, value);
        }

        private double _zRunningSpeed;
        /// <summary>
        /// Z轴运行速度
        /// </summary>
        public double Z轴运行速度
        {
            get => _zRunningSpeed;
            set => SetProperty(ref _zRunningSpeed, value);
        }
        #endregion

        #region U轴相关属性
        private string _uAxisBinding;
        /// <summary>
        /// U轴绑定轴
        /// </summary>
        public string U轴绑定轴
        {
            get => _uAxisBinding;
            set => SetProperty(ref _uAxisBinding, value);
        }

        private double _uTargetPosition;
        /// <summary>
        /// U轴目标位置
        /// </summary>
        public double U轴目标位置
        {
            get => _uTargetPosition;
            set => SetProperty(ref _uTargetPosition, value);
        }

        private double _uRunningSpeed;
        /// <summary>
        /// U轴运行速度
        /// </summary>
        public double U轴运行速度
        {
            get => _uRunningSpeed;
            set => SetProperty(ref _uRunningSpeed, value);
        }
        #endregion

        #region R轴相关属性
        private string _rAxisBinding;
        /// <summary>
        /// R轴绑定轴
        /// </summary>
        public string R轴绑定轴
        {
            get => _rAxisBinding;
            set => SetProperty(ref _rAxisBinding, value);
        }

        private double _rTargetPosition;
        /// <summary>
        /// R轴目标位置
        /// </summary>
        public double R轴目标位置
        {
            get => _rTargetPosition;
            set => SetProperty(ref _rTargetPosition, value);
        }

        private double _rRunningSpeed;
        /// <summary>
        /// R轴运行速度
        /// </summary>
        public double R轴运行速度
        {
            get => _rRunningSpeed;
            set => SetProperty(ref _rRunningSpeed, value);
        }
        #endregion
    }
}
