using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.IO.Common.Dtos
{
    /// <summary>
    /// IO卡点位数据传输对象（DTO - Data Transfer Object）
    /// 作用：
    /// 1. 封装IO卡输入（DI）/输出（DO）点位的详细信息，用于各层间数据传输（如UI层、服务层、数据层）
    /// 2. 作为UI界面展示点位信息的数据源，支持属性变更通知（继承自BaseDto）
    /// 3. 隔离底层实体与上层业务，仅暴露需要交互的字段，提升数据安全性
    /// </summary>
    public class IOCardDIODto : BaseDto
    {
        #region 私有字段（存储属性值，支持变更通知）
        /// <summary>
        /// 点位名称私有字段（如"急停输入"、"电机启动输出"）
        /// 与公共属性PointName关联，通过SetProperty方法触发UI更新
        /// </summary>
        private string _pointName;

        /// <summary>
        /// 点位类型私有字段（"输入"或"输出"）
        /// 区分点位的硬件类型，决定其读写权限
        /// </summary>
        private string _pointType;

        /// <summary>
        /// 点位索引私有字段（卡内唯一编号）
        /// 存储ushort类型，匹配硬件点位的索引范围（0~65535）
        /// </summary>
        private ushort _pointIndex;

        /// <summary>
        /// 绑定代码私有字段（业务逻辑标识）
        /// 存储与点位关联的业务功能编码
        /// </summary>
        private string _bindingCode;
        #endregion

        #region 公共属性（对外暴露的交互字段）
        /// <summary>
        /// 点位名称（业务标识）
        /// 取值示例："传送带到位检测"、"报警灯控制"
        /// 用途：在UI界面展示点位的业务含义，便于操作人员识别
        /// </summary>
        public string 点位名称
        {
            get => _pointName;
            set => SetProperty(ref _pointName, value); // 调用BaseDto的SetProperty，值变更时通知UI
        }

        /// <summary>
        /// 点位类型（硬件类型）
        /// 可选值："输入"（DI，数字输入）、"输出"（DO，数字输出）
        /// 用途：
        /// - 区分点位的硬件特性（输入为只读，输出可读写）
        /// - 决定UI界面的交互方式（输出点位可点击操作，输入点位仅展示）
        /// </summary>
        public string 点位类型
        {
            get => _pointType;
            set => SetProperty(ref _pointType, value);
        }

        /// <summary>
        /// 点位索引（卡内唯一标识）
        /// 类型：ushort（无符号16位整数），范围0~65535
        /// 用途：
        /// - 对应IO卡硬件上的点位编号（如第3个输入点索引为3）
        /// - 作为硬件通信时的寻址依据，确保操作的是目标点位
        /// - 同一张IO卡内索引唯一，避免点位冲突
        /// </summary>
        public ushort 点位索引
        {
            get => _pointIndex;
            set => SetProperty(ref _pointIndex, value);
        }

        /// <summary>
        /// 绑定代码（业务逻辑关联）
        /// 取值示例："Conveyor_InPlace"、"Alarm_Light_On"
        /// 用途：将硬件点位与业务功能绑定，实现"点位状态变化→触发业务动作"的联动
        /// 例如：当"传送带到位检测"点位触发时，通过绑定代码调用对应的物料处理逻辑
        /// </summary>
        public string 绑定代码
        {
            get => _bindingCode;
            set => SetProperty(ref _bindingCode, value);
        }
        #endregion
    }
}