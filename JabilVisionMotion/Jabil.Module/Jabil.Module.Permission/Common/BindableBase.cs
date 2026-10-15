using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 可绑定基类
    /// 实现INotifyPropertyChanged接口，提供属性变更通知功能
    /// 作为MVVM模式中ViewModel和模型类的基类，支持数据绑定
    /// </summary>
    public abstract class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// 属性变更事件
        /// 当属性值发生变化时触发，通知绑定的UI元素更新
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 设置属性值并触发变更通知
        /// 仅当新值与旧值不同时才更新并通知，优化性能
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">属性的引用（存储位置）</param>
        /// <param name="value">新的属性值</param>
        /// <param name="propertyName">属性名称，默认由编译器自动获取调用成员名</param>
        /// <returns>如果值发生变更返回true，否则返回false</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            // 检查新值与旧值是否相等
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false; // 值未变更，无需处理
            }

            // 更新属性值
            storage = value;
            // 触发属性变更通知
            RaisePropertyChanged(propertyName);
            return true; // 值已变更
        }

        /// <summary>
        /// 设置属性值、执行变更操作并触发变更通知
        /// 支持在属性值变更后执行额外的操作
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">属性的引用（存储位置）</param>
        /// <param name="value">新的属性值</param>
        /// <param name="onChanged">属性值变更后执行的操作</param>
        /// <param name="propertyName">属性名称，默认由编译器自动获取调用成员名</param>
        /// <returns>如果值发生变更返回true，否则返回false</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            // 检查新值与旧值是否相等
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false; // 值未变更，无需处理
            }

            // 更新属性值
            storage = value;
            // 执行变更后的操作（如果有）
            onChanged?.Invoke();
            // 触发属性变更通知
            RaisePropertyChanged(propertyName);
            return true; // 值已变更
        }

        /// <summary>
        /// 触发属性变更通知
        /// 调用OnPropertyChanged方法处理事件触发
        /// </summary>
        /// <param name="propertyName">属性名称，默认由编译器自动获取调用成员名</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 处理属性变更事件的触发
        /// 当属性值变更时，通知所有订阅者
        /// </summary>
        /// <param name="args">包含属性名称的事件参数</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            // 触发事件（如果有订阅者）
            this.PropertyChanged?.Invoke(this, args);
        }
    }
}