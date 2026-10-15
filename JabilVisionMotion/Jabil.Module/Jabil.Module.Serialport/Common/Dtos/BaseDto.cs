using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Serialport.Common.Dtos
{
    /// <summary>
    /// 基础DTO类，实现INotifyPropertyChanged接口
    /// 提供属性变更通知功能，作为其他DTO的基类
    /// </summary>
    public class BaseDto : INotifyPropertyChanged
    {
        private string _comPort;

        /// <summary>
        /// 串口端口号（如"COM1"、"COM2"等）
        /// </summary>
        public string COM口
        {
            get => _comPort;
            set
            {
                if (_comPort != value)
                {
                    _comPort = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名称，默认使用调用成员名称</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
