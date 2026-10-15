using System;

namespace Jabil.Module.IO.Common.Dtos
{
    /// <summary>
    /// IO卡数据传输对象（DTO - Data Transfer Object）
    /// 作用：
    /// 1. 用于IO卡基本信息在各层之间的传输（如UI层与服务层、服务层与数据层）
    /// 2. 封装IO卡的核心属性，仅包含需要展示或交互的字段，避免暴露底层实体细节
    /// 3. 支持属性变更通知（继承自BaseDto），适合作为UI绑定的数据源（如WinForm/WPF的双向绑定）
    /// </summary>
    public class IOCardDto : BaseDto
    {
        #region 私有字段（存储属性值，支持属性变更通知）
        /// <summary>
        /// 卡类型私有字段（如"3400"、"7230"）
        /// 与公共属性CardType关联，通过SetProperty方法实现值变更通知
        /// </summary>
        private string _cardType;

        /// <summary>
        /// 卡号索引私有字段（硬件通信标识）
        /// 存储ushort类型（0~65535），匹配硬件索引范围
        /// </summary>
        private ushort _cardIndex;
        #endregion

        #region 公共属性（对外暴露的字段，支持UI绑定和数据传输）
        /// <summary>
        /// 卡类型（硬件型号）
        /// 取值示例："3400"、"7230"、"M60"、"EC_8003"等
        /// 用途：区分不同型号的IO卡，决定驱动适配和通信协议
        /// </summary>
        public string 卡类型
        {
            get => _cardType;
            set => SetProperty(ref _cardType, value); // 调用BaseDto的SetProperty方法，值变更时触发通知
        }

        /// <summary>
        /// 卡号索引（硬件通信标识）
        /// 类型：ushort（无符号16位整数），范围0~65535
        /// 用途：硬件通信时的寻址标识（如Modbus地址、板卡编号），同一型号卡索引唯一
        /// </summary>
        public ushort 卡号索引
        {
            get => _cardIndex;
            set => SetProperty(ref _cardIndex, value);
        }
        #endregion
    }
}