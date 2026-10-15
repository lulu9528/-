using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Vision.Common.Dtos
{
    /// <summary>
    /// DTO基类
    /// 所有数据传输对象的抽象基类，实现INotifyPropertyChanged接口
    /// 提供属性变更通知机制，支持UI数据绑定
    /// </summary>
    public abstract class BaseDto : INotifyPropertyChanged
    {
        private int _id;

        /// <summary>
        /// 主键ID
        /// 与实体类的ID字段对应
        /// </summary>
        public int ID
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 属性变更事件
        /// 当属性值发生变化时触发
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// 通知UI该属性值已更新
        /// </summary>
        /// <param name="propertyName">属性名称，默认由调用方自动传入</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // 线程安全的事件触发方式
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 设置属性值并触发变更通知
        /// 通用属性设置方法，简化属性定义
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">存储字段引用</param>
        /// <param name="value">新属性值</param>
        /// <param name="propertyName">属性名称，默认由调用方自动传入</param>
        /// <returns>如果值发生变化则返回true，否则返回false</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
