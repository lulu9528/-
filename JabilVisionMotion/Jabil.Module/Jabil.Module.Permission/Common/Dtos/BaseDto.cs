using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 数据传输对象基类
    /// 实现INotifyPropertyChanged接口，提供属性变更通知功能
    /// 作为所有DTO（Data Transfer Object）类的基类，支持MVVM模式中的数据绑定
    /// </summary>
    public class BaseDto : INotifyPropertyChanged
    {
        /// <summary>
        /// 属性变更事件
        /// 当属性值发生变化时触发，通知绑定的UI元素更新
        /// 实现INotifyPropertyChanged接口的必要事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// 通知UI该属性值已变更，需要更新绑定的控件
        /// </summary>
        /// <param name="propertyName">发生变更的属性名称
        /// 默认为调用方法的属性名（通过CallerMemberName自动获取）
        /// </param>
        public void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            // 检查是否有订阅者，若有则触发事件
            // 使用空值传播运算符(?.)避免空引用异常
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}