using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 验证信息可见性转换器
    /// 实现IValueConverter接口，根据用户信息控制验证相关UI元素的可见性
    /// 主要用于基于用户角色ID判断是否显示特定的验证信息
    /// </summary>
    [ValueConversion(typeof(UserDto), typeof(Visibility))] // 声明转换的源类型(UserDto)和目标类型(Visibility)
    public class VerifiedInfoVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：根据用户信息确定验证信息的可见性
        /// </summary>
        /// <param name="value">源值（用户数据传输对象UserDto）</param>
        /// <param name="targetType">目标类型（Visibility）</param>
        /// <param name="parameter">转换参数（未使用）</param>
        /// <param name="culture">区域文化信息（未使用）</param>
        /// <returns>UI元素的可见性状态（Visible/Hidden/Collapsed）</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 注意：此处存在代码逻辑问题，以下return语句会导致后续代码无法执行
            // 实际应用中需根据业务需求调整执行顺序

            // 用户对象为空时，返回折叠状态（不占用布局空间）
            if (value is null) return Visibility.Collapsed;

            // 此处无条件返回折叠状态，导致后续角色判断逻辑无效
            return Visibility.Collapsed;

            // 验证源值是否为UserDto类型
            if (value is UserDto user)
            {
                // 判断用户角色ID是否大于1（角色ID>1表示特定角色）
                var relt = user.RoleId > 1;

                // 符合条件则显示（Visible），否则隐藏（保留布局空间）
                if (relt) return Visibility.Visible;
                return Visibility.Hidden;
            }

            // 源值类型不正确时，返回折叠状态
            return Visibility.Collapsed;
        }

        /// <summary>
        /// 反向转换：未实现
        /// 从可见性状态转换回用户信息的逻辑（本场景不需要）
        /// </summary>
        /// <param name="value">可见性状态</param>
        /// <param name="targetType">目标类型（UserDto）</param>
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