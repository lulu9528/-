﻿using Jabil.Lib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Globalization;

namespace Jabil.Module.Modbus.Common.Dtos
{
    /// <summary>
    /// Modbus通信DIO点位数据传输对象
    /// 用于Modbus数字输入输出点位信息的传输与展示
    /// </summary>
    public class ModbusCommunicationDioDto : BaseDto
    {
        private string _pointName;
        private string _pointType;
        private string _pointIndex;
        private int _pointValue;
        private string _pointDefinition;
        private string _dataType;
        private string _bindingCode;

        /// <summary>
        /// 点位名称
        /// </summary>
        public string 点位名称
        {
            get => _pointName;
            set => SetProperty(ref _pointName, value);
        }

        /// <summary>
        /// 点位类型
        /// 固定值：输入、输出
        /// </summary>
        [TypeConverter(typeof(PointTypeConverter))]
        public string 点位类型
        {
            get => _pointType;
            set => SetProperty(ref _pointType, value);
        }

        /// <summary>
        /// 点位索引
        /// 格式规范:
        /// 示例: "1"、"1.1"
        /// </summary>
        public string 点位索引
        {
            get => _pointIndex;
            set => SetProperty(ref _pointIndex, value);
        }

        /// <summary>
        /// 点位值
        /// 根据数据类型存储对应的值（bool类型使用1/0表示）
        /// </summary>
        public int 点位值
        {
            get => _pointValue;
            set => SetProperty(ref _pointValue, value);
        }

        /// <summary>
        /// 点位定义
        /// 描述点位的具体功能和用途，例如："急停信号输入"、"报警指示灯输出"
        /// </summary>
        [TypeConverter(typeof(PointDefinitionConverter))]
        public string 点位定义
        {
            get => _pointDefinition;
            set => SetProperty(ref _pointDefinition, value);
        }

        /// <summary>
        /// 数据类型
        /// 支持：bool、int16、int32、uint16、uint32
        /// 对应Modbus协议中的数据格式
        /// </summary>
        [TypeConverter(typeof(DataTypeConverter))]
        public string 数据类型
        {
            get => _dataType;
            set => SetProperty(ref _dataType, value);
        }

        /// <summary>
        /// 绑定代码
        /// 关联的业务代码或标识，用于业务逻辑关联，如："ALM001"
        /// </summary>
        public string 绑定代码
        {
            get => _bindingCode;
            set => SetProperty(ref _bindingCode, value);
        }

        /// <summary>
        /// 创建时间
        /// 数据创建时的时间戳，默认为当前时间
        /// 在Frm_ModbusDioManager窗体中显示时为只读
        /// </summary>
        [ReadOnly(true)]
        public DateTime 创建时间
        {
            get => base.创建时间;
            set => base.创建时间 = value;
        }

        /// <summary>
        /// 验证当前DTO数据的有效性
        /// </summary>
        /// <returns>验证结果及错误信息</returns>
        public (bool IsValid, List<string> Errors) ValidateDto()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(设备名称))
                errors.Add("设备名称不能为空");

            if (string.IsNullOrWhiteSpace(点位名称))
                errors.Add("点位名称不能为空");
            else if (点位名称.Length > 100)
                errors.Add("点位名称长度不能超过100个字符");

            if (string.IsNullOrWhiteSpace(点位类型) ||
                !new[] { "输入", "输出" }.Contains(点位类型))
                errors.Add("点位类型必须为'输入'或'输出'");

            if (string.IsNullOrWhiteSpace(点位索引))
                errors.Add("点位索引不能为空");
            else if (!IsValidPointIndex(点位索引))
                errors.Add("点位索引格式无效，应符合1或1.1这样的格式");

            if (string.IsNullOrWhiteSpace(数据类型) ||
                !new[] { "bool", "int16", "int32", "uint16", "uint32" }.Contains(数据类型))
                errors.Add("数据类型不支持，请使用bool、int16、int32、uint16、uint32");

            if (!string.IsNullOrWhiteSpace(点位定义) && 点位定义.Length > 500)
                errors.Add("点位定义长度不能超过500个字符");

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 验证点位索引格式是否有效
        /// </summary>
        private bool IsValidPointIndex(string index)
        {
            // 支持"1"或"1.1"这样的格式
            if (string.IsNullOrWhiteSpace(index))
                return false;

            // 检查是否为纯数字格式（如"1"）
            if (int.TryParse(index, out int integerResult))
            {
                return integerResult > 0; // 必须是正整数
            }

            // 检查是否为小数格式（如"1.1"）
            if (decimal.TryParse(index, out decimal decimalResult))
            {
                // 必须包含且仅包含一个小数点
                var parts = index.Split('.');
                if (parts.Length != 2)
                    return false;

                // 整数部分和小数部分都必须是数字
                if (!int.TryParse(parts[0], out int integerPart) || !int.TryParse(parts[1], out int decimalPart))
                    return false;

                // 整数部分必须大于0
                return integerPart > 0;
            }

            return false;
        }

        /// <summary>
        /// 复制当前DTO实例
        /// </summary>
        /// <returns>新的DTO副本</returns>
        public ModbusCommunicationDioDto Clone()
        {
            return new ModbusCommunicationDioDto
            {
                ID = this.ID,
                设备名称 = this.设备名称,
                点位名称 = this.点位名称,
                点位类型 = this.点位类型,
                点位索引 = this.点位索引,
                点位值 = this.点位值,
                点位定义 = this.点位定义,
                数据类型 = this.数据类型,
                绑定代码 = this.绑定代码,
                是否启用 = this.是否启用,
                创建时间 = this.创建时间,
                更新时间 = this.更新时间
            };
        }

        /// <summary>
        /// 重写ToString方法，返回点位标识信息
        /// </summary>
        /// <returns>点位标识字符串</returns>
        public override string ToString()
        {
            return $"{设备名称}-{点位类型}-{点位定义}-{点位名称}({点位索引})";
        }

    }

    /// <summary>
    /// 点位定义转换器，用于在PropertyGrid中显示下拉列表，同时支持用户输入
    /// </summary>
    public class PointDefinitionConverter : StringConverter
    {
        // 用于存储设备点位定义的静态字段
        private static Dictionary<string, List<string>> _devicePointDefinitions = new Dictionary<string, List<string>>();

        /// <summary>
        /// 设置指定设备的点位定义列表
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="definitions">点位定义列表</param>
        public static void SetDevicePointDefinitions(string deviceName, List<string> definitions)
        {
            if (string.IsNullOrEmpty(deviceName))
                return;

            _devicePointDefinitions[deviceName] = definitions ?? new List<string>();
        }

        /// <summary>
        /// 获取指定设备的点位定义列表
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>点位定义列表</returns>
        public static List<string> GetDevicePointDefinitions(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName) || !_devicePointDefinitions.ContainsKey(deviceName))
                return new List<string>();

            return _devicePointDefinitions[deviceName];
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            // 不是独占的，允许用户输入自定义值
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            // 获取当前设备下的所有点位定义
            var standardValues = new List<string>();
            
            // 如果上下文对象是ModbusCommunicationDioDto，则获取设备名称
            if (context?.Instance is ModbusCommunicationDioDto dto)
            {
                string deviceName = dto.设备名称;
                if (!string.IsNullOrEmpty(deviceName))
                {
                    // 获取当前设备下的点位定义
                    var deviceDefinitions = GetDevicePointDefinitions(deviceName);
                    standardValues.AddRange(deviceDefinitions);
                }
            }
                        
            return new StandardValuesCollection(standardValues.Distinct().ToList());
        }
    }

    /// <summary>
    /// 点位类型转换器，用于在PropertyGrid中显示下拉列表
    /// </summary>
    public class PointTypeConverter : TypeConverter
    {
        private static readonly string[] PointTypes = { "输入", "输出" };

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(PointTypes);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
                return stringValue;
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
                return value?.ToString() ?? string.Empty;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
    
    /// <summary>
    /// 设备名称转换器，用于在PropertyGrid中显示为只读
    /// </summary>
    public class DeviceNameConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
                return value?.ToString() ?? string.Empty;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
    
    /// <summary>
    /// 数据类型转换器，用于在PropertyGrid中显示下拉列表
    /// </summary>
    public class DataTypeConverter : TypeConverter
    {
        private static readonly string[] DataTypes = { "bool", "int16", "int32", "uint16", "uint32" };

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(DataTypes);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
                return stringValue;
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
                return value?.ToString() ?? string.Empty;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}