using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jabil.Module.Alarm.Common
{
    /// <summary>
    /// 基础数据传输对象(DTO)基类
    /// 实现INotifyPropertyChanged接口，支持属性变更通知
    /// 所有业务DTO类的父类，提供通用基础字段和通知功能
    /// </summary>
    public class BaseDto : INotifyPropertyChanged
    {
        private int _id;
        /// <summary>
        /// 自增主键ID
        /// 对应实体类的主键字段
        /// </summary>
        public int ID
        {
            get => _id;
            set
            {
                // 仅在值发生变化时触发通知，减少不必要的更新
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _deviceName;
        /// <summary>
        /// 设备名称
        /// 关联的设备唯一标识名称
        /// </summary>
        public string 设备名称
        {
            get => _deviceName;
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
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
        /// </summary>
        /// <param name="propertyName">属性名称，默认使用调用者属性名</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            // 安全触发事件，避免空引用异常
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
