using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.IO.Common.Dtos
{
    /// <summary>
    /// IO模块基础数据传输对象（DTO - Base Data Transfer Object）
    /// 设计定位：
    /// 1. 所有IO相关DTO的抽象基类，提供通用字段和属性变更通知能力
    /// 2. 实现INotifyPropertyChanged接口，支持UI层数据绑定（值变化时自动刷新界面）
    /// 3. 封装通用属性设置逻辑，统一处理值变更判断和事件触发
    /// 核心价值：减少重复代码，统一DTO行为，提升UI交互体验
    /// </summary>
    public abstract class BaseDto : INotifyPropertyChanged
    {
        #region 私有字段（存储通用属性值）
        /// <summary>
        /// 主键ID私有字段
        /// 对应数据库表的自增主键，用于唯一标识记录
        /// </summary>
        private int _id;

        /// <summary>
        /// 卡名称私有字段
        /// 存储IO卡的业务唯一标识（如"Line1_IO_001"）
        /// </summary>
        private string _cardName;
        #endregion

        #region 公共属性（所有IO相关DTO共享的核心字段）
        /// <summary>
        /// 主键ID（数据库层面唯一标识）
        /// 类型：int（自增整数）
        /// 用途：
        /// - 关联数据库记录，支持CRUD操作
        /// - 跨表关联时作为外键（如点位DTO关联到IO卡DTO）
        /// 注意：新增记录时通常无需手动设置，由数据库自动生成
        /// </summary>
        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value); // 调用通用设置方法，自动处理变更通知
        }

        /// <summary>
        /// 卡名称（业务层面唯一标识）
        /// 类型：string（非空，系统内唯一）
        /// 用途：
        /// - 标识IO卡的业务名称（如"装配线左侧IO卡"）
        /// - 作为DTO间关联的业务键（如点位DTO通过CardName归属到具体IO卡）
        /// - 便于用户识别和操作，比ID更具业务含义
        /// </summary>
        public string 卡名称
        {
            get => _cardName;
            set => SetProperty(ref _cardName, value);
        }
        #endregion

        #region 属性变更通知（支持UI绑定的核心机制）
        /// <summary>
        /// 属性变更事件（实现INotifyPropertyChanged接口的核心事件）
        /// 触发时机：当DTO的属性值发生变化时
        /// 作用：通知UI层数据已更新，触发界面刷新（如WinForm的DataBinding）
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知的保护方法
        /// 封装事件触发逻辑，避免外部直接调用
        /// </summary>
        /// <param name="propertyName">发生变化的属性名称</param>
        /// <remarks>
        /// [CallerMemberName]特性：自动获取调用方法的名称，无需手动传入属性名
        /// 例如：在Id属性的setter中调用时，propertyName自动为"Id"
        /// </remarks>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // 若有订阅者，则触发事件（线程安全的简化写法）
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 通用属性设置方法（模板方法）
        /// 统一处理：值变更判断→更新字段→触发通知的完整流程
        /// </summary>
        /// <typeparam name="T">属性数据类型（泛型支持任意类型）</typeparam>
        /// <param name="storage">字段引用（需用ref关键字传递，确保能修改原始字段）</param>
        /// <param name="value">新属性值</param>
        /// <param name="propertyName">属性名称（自动获取，无需手动传入）</param>
        /// <returns>是否发生值变更（true：值已更新；false：新值与旧值相同）</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            // 使用泛型相等比较器判断值是否变化（支持null和值类型/引用类型）
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false; // 值未变化，直接返回

            storage = value; // 更新字段值
            OnPropertyChanged(propertyName); // 触发属性变更通知
            return true; // 通知调用者值已更新
        }
        #endregion
    }
}