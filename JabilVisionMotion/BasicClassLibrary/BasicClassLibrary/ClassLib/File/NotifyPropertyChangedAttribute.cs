using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

// 定义一个特性，用于标记类需要自动实现属性变化通知
public class NotifyPropertyChangedBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
