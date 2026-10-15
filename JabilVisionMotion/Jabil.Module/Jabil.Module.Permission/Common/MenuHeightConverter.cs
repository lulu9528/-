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
    /// 菜单高度转换器
    /// 实现IValueConverter接口，用于调整菜单相关元素的高度
    /// 支持根据原始高度和偏移量计算目标高度
    /// </summary>
    public class MenuHeightConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：计算调整后的高度
        /// 将原始高度减去两倍的偏移量（通常用于去除上下边距）
        /// </summary>
        /// <param name="value">原始高度值（预期为double类型）</param>
        /// <param name="targetType">目标类型（通常为double）</param>
        /// <param name="parameter">偏移量参数（字符串形式的数值）</param>
        /// <param name="culture">区域文化信息（用于数值解析）</param>
        /// <returns>调整后的高度值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 检查输入值是否为有效的double类型高度
            if (value is double height)
            {
                // 检查是否提供了偏移量参数且能转换为double
                if (parameter != null && double.TryParse(parameter.ToString(), out double offset))
                {
                    // 计算调整后的高度：原始高度减去两倍偏移量（上下各偏移一次）
                    return height - 2 * offset;
                }
                // 无有效偏移量时，直接返回原始高度
                return height;
            }
            // 输入值无效时，返回原始值（通常会导致绑定失败提示）
            return value;
        }

        /// <summary>
        /// 反向转换：未实现
        /// 从调整后的高度转换回原始高度的逻辑（本转换器中不需要）
        /// </summary>
        /// <param name="value">调整后的高度值</param>
        /// <param name="targetType">原始类型（通常为double）</param>
        /// <param name="parameter">偏移量参数</param>
        /// <param name="culture">区域文化信息</param>
        /// <returns>转换结果</returns>
        /// <exception cref="NotImplementedException">始终抛出，因为未实现</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}