using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Motion.Common.Dtos
{
    /// <summary>
    /// 基础数据传输对象类
    /// 实现INotifyPropertyChanged接口，提供属性变更通知功能
    /// 所有DTO类应继承此类以获得统一的属性通知机制
    /// </summary>
    public class BaseDto : INotifyPropertyChanged
    {
        private int _id;

        /// <summary>
        /// 唯一标识符
        /// </summary>
        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 设置属性值并触发变更通知（仅在值发生实际变化时）
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">存储字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称（自动推断）</param>
        /// <returns>是否发生了值变更</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            // 只有当值发生实际变化时才触发通知，减少不必要的UI更新
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名称（默认使用调用成员名称）</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
