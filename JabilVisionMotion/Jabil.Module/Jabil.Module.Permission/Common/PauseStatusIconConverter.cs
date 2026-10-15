using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 暂停状态图标转换器
    /// 实现IValueConverter接口，根据数值状态转换为对应的图标名称
    /// 通常用于将暂停/播放状态映射到UI图标（如FontAwesome图标）
    /// </summary>
    public class PauseStatusIconConverter : IValueConverter
    {
        /// <summary>
        /// 正向转换：将数值状态转换为图标名称
        /// </summary>
        /// <param name="value">输入值（预期为整数，表示暂停/播放状态）</param>
        /// <param name="targetType">目标类型（通常为字符串）</param>
        /// <param name="parameter">转换参数（未使用）</param>
        /// <param name="culture">区域文化信息（用于数值解析）</param>
        /// <returns>图标名称字符串（"Pause"或"PlayPause"）</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 验证输入值是否有效并尝试转换为整数
            if (value != null && int.TryParse(value.ToString(), out int val))
            {
                // 根据数值判断状态：0表示"Pause"（暂停），其他值表示"PlayPause"（播放/暂停切换）
                if (val == 0)
                {
                    return "Pause";
                }
                else
                {
                    return "PlayPause";
                }
            }
            // 输入值无效时，默认返回"Pause"图标
            return "Pause";
        }

        /// <summary>
        /// 反向转换：未实现
        /// 从图标名称转换回数值状态的逻辑（本转换器中不需要）
        /// </summary>
        /// <param name="value">图标名称</param>
        /// <param name="targetType">目标类型（通常为整数）</param>
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