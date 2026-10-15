using Jabil.Lib;
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
    /// 权限可见性转换器
    /// 实现IValueConverter接口，根据用户权限控制UI元素的可见性
    /// 用于基于用户权限动态显示或隐藏界面元素
    /// </summary>
    [ValueConversion(typeof(UserDto), typeof(Visibility))] // 声明转换的源类型和目标类型
    public class PermissionVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：根据用户权限和参数判断元素可见性
        /// </summary>
        /// <param name="value">源值（用户数据传输对象UserDto）</param>
        /// <param name="targetType">目标类型（Visibility）</param>
        /// <param name="parameter">权限标识参数（字符串形式的权限名称）</param>
        /// <param name="culture">区域文化信息（未使用）</param>
        /// <returns>元素的可见性状态（Visible/Hidden/Collapsed）</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 用户对象为空时，元素不可见（折叠）
            if (value is null) return Visibility.Collapsed;

            // 验证源值是否为UserDto类型
            if (value is UserDto user)
            {
                // 无权限参数时，默认元素可见
                if (parameter is null) return Visibility.Visible;

                // 调用权限系统检查用户是否拥有指定权限
                // parameter.ToString()为权限标识，out参数接收额外信息（未使用）
                var hasPermission = PermissionSystem.CheckPermission(parameter.ToString(), out _);

                // 有权限则可见，无权限则隐藏（保留布局空间）
                return hasPermission ? Visibility.Visible : Visibility.Hidden;
            }

            // 源值类型不正确时，元素不可见（折叠）
            return Visibility.Collapsed;
        }

        /// <summary>
        /// 反向转换：未实现
        /// 从可见性状态转换回用户权限的逻辑（本场景不需要）
        /// </summary>
        /// <param name="value">可见性状态</param>
        /// <param name="targetType">目标类型（UserDto）</param>
        /// <param name="parameter">权限标识参数</param>
        /// <param name="culture">区域文化信息</param>
        /// <returns>转换结果</returns>
        /// <exception cref="NotImplementedException">始终抛出，因为未实现</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 设备运行状态转换器
    /// 实现IValueConverter接口，将设备运行状态枚举转换为字符串
    /// 用于在UI上显示格式化的设备状态文本
    /// </summary>
    public class MachineRunStatusConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：将设备运行状态枚举转换为格式化字符串
        /// </summary>
        /// <param name="value">源值（设备运行状态枚举MachineRunStatus）</param>
        /// <param name="targetType">目标类型（字符串）</param>
        /// <param name="parameter">转换参数（未使用）</param>
        /// <param name="culture">区域文化信息（未使用）</param>
        /// <returns>格式化的状态字符串（用|替换枚举中的逗号）</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 状态为空时，返回"None"
            if (value is null) return "None";

            // 验证源值是否为MachineRunStatus枚举类型
            if (value is MachineRunStatus status)
            {
                // 将枚举的字符串表示中的逗号替换为|（通常用于多状态展示）
                return status.ToString().Replace(",", "|");
            }

            // 源值类型不正确时，返回"None"
            return "None";
        }

        /// <summary>
        /// 反向转换：未实现
        /// 从字符串转换回设备运行状态枚举的逻辑（本场景不需要）
        /// </summary>
        /// <param name="value">状态字符串</param>
        /// <param name="targetType">目标类型（MachineRunStatus）</param>
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