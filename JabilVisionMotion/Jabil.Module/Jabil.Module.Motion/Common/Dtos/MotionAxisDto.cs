using Jabil.Lib.Common;
using Jabil.Module.Motion.Common.Dtos;
using System;

namespace Jabil.Module.Motion.Common.Dtos
{
    /// <summary>
    /// 运动轴数据传输对象
    /// 包含运动轴的配置信息和参数设置
    /// </summary>
    public class MotionAxisDto : BaseDto
    {
        private string _axisName;
        /// <summary>
        /// 轴名称
        /// </summary>
        public string 轴名称
        {
            get => _axisName;
            set => SetProperty(ref _axisName, value);
        }

        private string _axisType;
        /// <summary>
        /// 轴类型
        /// </summary>
        public string 轴类型
        {
            get => _axisType;
            set => SetProperty(ref _axisType, value);
        }

        private string _axisCardType;
        /// <summary>
        /// 轴卡类型
        /// </summary>
        public string 轴卡类型
        {
            get => _axisCardType;
            set => SetProperty(ref _axisCardType, value);
        }

        private short _axisCardIndex;
        /// <summary>
        /// 轴卡卡号索引
        /// </summary>
        public short 轴卡卡号索引
        {
            get => _axisCardIndex;
            set => SetProperty(ref _axisCardIndex, value);
        }

        private short _axisCardAxisNumber;
        /// <summary>
        /// 轴卡轴号索引
        /// </summary>
        public short 轴卡轴号索引
        {
            get => _axisCardAxisNumber;
            set => SetProperty(ref _axisCardAxisNumber, value);
        }

        private int _pulseRatio;
        /// <summary>
        /// 脉冲比
        /// </summary>
        public int 脉冲比
        {
            get => _pulseRatio;
            set => SetProperty(ref _pulseRatio, value);
        }

        private uint _inPositionSignalRange;
        /// <summary>
        /// 到位信号范围
        /// </summary>
        public uint 到位信号范围
        {
            get => _inPositionSignalRange;
            set => SetProperty(ref _inPositionSignalRange, value);
        }

        private uint _inPositionSignalStableTime;
        /// <summary>
        /// 到位信号稳定时间
        /// </summary>
        public uint 到位信号稳定时间
        {
            get => _inPositionSignalStableTime;
            set => SetProperty(ref _inPositionSignalStableTime, value);
        }

        private double _softStopDeceleration;
        /// <summary>
        /// 缓停减速度
        /// </summary>
        public double 缓停减速度
        {
            get => _softStopDeceleration;
            set => SetProperty(ref _softStopDeceleration, value);
        }

        private double _emergencyStopAcceleration;
        /// <summary>
        /// 急停加速度
        /// </summary>
        public double 急停加速度
        {
            get => _emergencyStopAcceleration;
            set => SetProperty(ref _emergencyStopAcceleration, value);
        }

        private double _singleAxisAcceleration;
        /// <summary>
        /// 单轴加速度
        /// </summary>
        public double 单轴加速度
        {
            get => _singleAxisAcceleration;
            set => SetProperty(ref _singleAxisAcceleration, value);
        }

        private double _singleAxisDeceleration;
        /// <summary>
        /// 单轴减速度
        /// </summary>
        public double 单轴减速度
        {
            get => _singleAxisDeceleration;
            set => SetProperty(ref _singleAxisDeceleration, value);
        }

        private short _jerkTime;
        /// <summary>
        /// 加加速时间
        /// </summary>
        public short 加加速时间
        {
            get => _jerkTime;
            set => SetProperty(ref _jerkTime, value);
        }

        private double _singleAxisRunningSpeed;
        /// <summary>
        /// 单轴运行速度
        /// </summary>
        public double 单轴运行速度
        {
            get => _singleAxisRunningSpeed;
            set => SetProperty(ref _singleAxisRunningSpeed, value);
        }

        private int _positiveSoftLimit;
        /// <summary>
        /// 正向软限位
        /// </summary>
        public int 正向软限位
        {
            get => _positiveSoftLimit;
            set => SetProperty(ref _positiveSoftLimit, value);
        }

        private int _negativeSoftLimit;
        /// <summary>
        /// 负向软限位
        /// </summary>
        public int 负向软限位
        {
            get => _negativeSoftLimit;
            set => SetProperty(ref _negativeSoftLimit, value);
        }

        private short _homingMethod;
        /// <summary>
        /// 回零方法
        /// </summary>
        public short 回零方法
        {
            get => _homingMethod;
            set => SetProperty(ref _homingMethod, value);
        }

        private uint _homingSpeed1;
        /// <summary>
        /// 回零速度1
        /// </summary>
        public uint 回零速度1
        {
            get => _homingSpeed1;
            set => SetProperty(ref _homingSpeed1, value);
        }

        private uint _homingSpeed2;
        /// <summary>
        /// 回零速度2
        /// </summary>
        public uint 回零速度2
        {
            get => _homingSpeed2;
            set => SetProperty(ref _homingSpeed2, value);
        }

        private uint _homingAcceleration;
        /// <summary>
        /// 回零加速度
        /// </summary>
        public uint 回零加速度
        {
            get => _homingAcceleration;
            set => SetProperty(ref _homingAcceleration, value);
        }

        private int _homingOffset;
        /// <summary>
        /// 回零偏移
        /// </summary>
        public int 回零偏移
        {
            get => _homingOffset;
            set => SetProperty(ref _homingOffset, value);
        }

        private short _limitSignalLock;
        /// <summary>
        /// 限位信号锁定
        /// </summary>
        public short 限位信号锁定
        {
            get => _limitSignalLock;
            set => SetProperty(ref _limitSignalLock, value);
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
    }
}
