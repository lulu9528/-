using Jabil.Lib.Common;
using Jabil.Module.Motion.Common.Dtos;
using System;

namespace Jabil.Module.Motion.Common.Dtos
{
    /// <summary>
    /// 轴卡数据传输对象
    /// 包含轴卡的基本配置信息和参数设置
    /// </summary>
    public class MotionCardDto : BaseDto
    {
        private string _cardName;
        /// <summary>
        /// 轴卡名称
        /// </summary>
        public string 轴卡名称
        {
            get => _cardName;
            set => SetProperty(ref _cardName, value);
        }

        private string _cardType;
        /// <summary>
        /// 轴卡类型
        /// </summary>
        public string 轴卡类型
        {
            get => _cardType;
            set => SetProperty(ref _cardType, value);
        }

        private short _cardNumber;
        /// <summary>
        /// 轴卡卡号
        /// </summary>
        public short 轴卡卡号
        {
            get => _cardNumber;
            set => SetProperty(ref _cardNumber, value);
        }

        private string _parameterFile;
        /// <summary>
        /// 轴卡参数文件
        /// </summary>
        public string 轴卡参数文件
        {
            get => _parameterFile;
            set => SetProperty(ref _parameterFile, value);
        }

        private short _emergencyStopPolarity;
        /// <summary>
        /// 急停极性
        /// </summary>
        public short 急停极性
        {
            get => _emergencyStopPolarity;
            set => SetProperty(ref _emergencyStopPolarity, value);
        }
    }
}
