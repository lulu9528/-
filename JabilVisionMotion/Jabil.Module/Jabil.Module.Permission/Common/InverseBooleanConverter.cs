using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 布尔值取反转换器
    /// 实现IValueConverter接口，用于在绑定中将bool值反转
    /// 例如：将true转换为false，false转换为true
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))] // 声明转换的源类型和目标类型
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：将源bool值取反
        /// </summary>
        /// <param name="value">源值（bool类型）</param>
        /// <param name="targetType">目标类型（必须是bool）</param>
        /// <param name="parameter">转换参数（未使用）</param>
        /// <param name="culture">区域文化信息（未使用）</param>
        /// <returns>取反后的bool值</returns>
        /// <exception cref="NotImplementedException">当目标类型不是bool时抛出</exception>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 验证目标类型是否为bool
            if (targetType != typeof(bool))
                throw new NotImplementedException();

            // 将源值转换为bool并取反
            return !(bool)value;
        }

        /// <summary>
        /// 反向转换：未实现
        /// 从目标值转换回源值的逻辑（本转换器中不需要）
        /// </summary>
        /// <param name="value">目标值</param>
        /// <param name="targetType">源类型</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域文化信息</param>
        /// <returns>转换结果</returns>
        /// <exception cref="NotImplementedException">始终抛出，因为未实现</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// 布尔值到可见性转换器
    /// 实现IValueConverter接口，将bool值转换为Visibility枚举值
    /// 用于控制UI元素的显示与隐藏
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：将bool值转换为Visibility
        /// </summary>
        /// <param name="value">源值（预期为bool类型）</param>
        /// <param name="targetType">目标类型（通常是Visibility）</param>
        /// <param name="parameter">转换参数（未使用）</param>
        /// <param name="culture">区域文化信息（未使用）</param>
        /// <returns>Visible（如果值为true）或Collapsed（如果值为false或无效）</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 处理null值：默认隐藏
            if (value is null) return Visibility.Collapsed;

            // 处理bool值：true显示，false隐藏
            if (value is bool val)
            {
                return val ? Visibility.Visible : Visibility.Collapsed;
            }

            // 非bool值默认隐藏
            return Visibility.Collapsed;
        }

        /// <summary>
        /// 反向转换：未实现
        /// 从Visibility转换回bool值的逻辑（本转换器中不需要）
        /// </summary>
        /// <param name="value">目标值（Visibility）</param>
        /// <param name="targetType">源类型（bool）</param>
        /// <param name="parameter">转换参数</param>
        /// <param name="culture">区域文化信息</param>
        /// <returns>转换结果</returns>
        /// <exception cref="NotImplementedException">始终抛出，因为未实现</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}