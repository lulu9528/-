using Jabil.Lib.Common;
using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;

namespace Jabil.Module.Modbus.Common.Dtos
{
    /// <summary>
    /// DTO基础类，实现属性变更通知和数据验证接口
    /// 所有业务DTO类的基类，提供属性变更通知、值比较、数据验证及时间追踪功能
    /// </summary>
    public class BaseDto : INotifyPropertyChanged
    {
        private int _id;
        private string _deviceName;
        private DateTime _createTime = DateTime.Now;
        private DateTime _updateTime = DateTime.Now;
        private bool _isEnabled = true;

        private readonly Dictionary<string, List<string>> _validationErrors = new Dictionary<string, List<string>>();

        /// <summary>
        /// 主键ID
        /// </summary>
        [ReadOnly(true)]
        public int ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 设备名称
        /// </summary>
        [TypeConverter(typeof(DeviceNameConverter))]
        public string 设备名称
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        /// <summary>
        /// 创建时间
        /// 数据创建时的时间戳，默认为当前时间
        /// </summary>
        public DateTime 创建时间
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        /// <summary>
        /// 更新时间
        /// 数据最后更新时的时间戳，自动维护
        /// </summary>
        public DateTime 更新时间
        {
            get => _updateTime;
            protected set => SetProperty(ref _updateTime, value);
        }

        /// <summary>
        /// 是否启用
        /// 标识该点位是否启用监控和操作，默认启用
        /// </summary>
        public bool 是否启用
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 数据验证错误事件
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// IDataErrorInfo实现：获取指定属性错误
        /// </summary>
        /// <param name="columnName">属性名称</param>
        /// <returns>错误信息</returns>
        public string this[string columnName] =>
            GetErrors(columnName)?.Cast<string>()?.FirstOrDefault() ?? string.Empty;

        /// <summary>
        /// 获取指定属性的验证错误
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <returns>错误信息集合</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return _validationErrors.Values.SelectMany(errors => errors).ToList();
            }

            if (_validationErrors.TryGetValue(propertyName, out var errors))
            {
                return new List<string>(errors); // 返回副本避免外部修改内部集合
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名称，默认使用调用成员名称</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // 排除系统维护字段的递归更新
            if (propertyName != nameof(更新时间) &&
                propertyName != nameof(创建时间))
            {
                更新时间 = DateTime.Now; // 自动更新时间戳
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            ValidateProperty(propertyName);
        }

        /// <summary>
        /// 通过属性表达式触发变更通知
        /// 用于编译时检查属性名称的正确性
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="propertyExpression">属性表达式，如 x => x.PropertyName</param>
        protected void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            var memberExpr = propertyExpression.Body as MemberExpression;
            if (memberExpr?.Member is PropertyInfo propertyInfo)
            {
                OnPropertyChanged(propertyInfo.Name);
            }
        }

        /// <summary>
        /// 安全设置属性值并触发变更通知
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">存储字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>是否发生了值变更</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            // 类型为字符串时进行空值统一处理
            if (typeof(T) == typeof(string) && value == null)
            {
                value = (T)(object)string.Empty;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 添加属性验证错误
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        /// <param name="errorMessage">错误信息</param>
        protected void AddError(string propertyName, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentNullException(nameof(errorMessage));

            if (!_validationErrors.ContainsKey(propertyName))
            {
                _validationErrors[propertyName] = new List<string>();
            }

            if (!_validationErrors[propertyName].Contains(errorMessage))
            {
                _validationErrors[propertyName].Add(errorMessage);
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// 清除指定属性的验证错误
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected void ClearErrors(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            if (_validationErrors.Remove(propertyName))
            {
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// 触发错误变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 验证属性（可在派生类中重写实现具体验证逻辑）
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void ValidateProperty(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            // 清除该属性现有错误
            ClearErrors(propertyName);

            // 基础验证逻辑
            switch (propertyName)
            {
                case nameof(ID):
                    if (ID < 0)
                    {
                        AddError(propertyName, "ID不能为负数");
                    }
                    break;
                case nameof(设备名称):
                    if (string.IsNullOrWhiteSpace(设备名称))
                    {
                        AddError(propertyName, "设备名称不能为空");
                    }
                    else if (设备名称.Length > 100)
                    {
                        AddError(propertyName, "设备名称长度不能超过100个字符");
                    }
                    break;
                case nameof(创建时间):
                    if (创建时间 > DateTime.Now)
                    {
                        AddError(propertyName, "创建时间不能晚于当前时间");
                    }
                    break;
                case nameof(更新时间):
                    if (更新时间 < 创建时间)
                    {
                        AddError(propertyName, "更新时间不能早于创建时间");
                    }
                    break;
            }
        }
    }
}