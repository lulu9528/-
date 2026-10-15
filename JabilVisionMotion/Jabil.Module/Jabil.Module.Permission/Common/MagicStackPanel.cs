using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// 自定义堆栈面板控件
    /// 继承自StackPanel，实现动态生成交替背景色的边框元素
    /// 用于创建具有斑马条纹效果的列表或区域
    /// </summary>
    public class MagicStackPanel : StackPanel
    {
        /// <summary>
        /// 第一种背景色画刷
        /// 白色(#FFFFFF)，用于偶数索引的边框元素
        /// </summary>
        private SolidColorBrush color1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));

        /// <summary>
        /// 第二种背景色画刷
        /// 浅灰色(#EEEEEE)，用于奇数索引的边框元素
        /// </summary>
        private SolidColorBrush color2 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE"));

        /// <summary>
        /// 项高度属性
        /// 控制整个MagicStackPanel的总高度，同时决定生成的边框元素数量
        /// 依赖属性支持数据绑定、样式和动画
        /// </summary>
        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        /// <summary>
        /// 项高度依赖属性
        /// 作为ItemHeight属性的后备存储，定义属性元数据和变更回调
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(
                "ItemHeight",
                typeof(double),
                typeof(MagicStackPanel),
                new PropertyMetadata(0.0, ItemHeightPropertyChanged) // 默认值0.0，变更时调用ItemHeightPropertyChanged
            );

        /// <summary>
        /// 项高度属性变更回调
        /// 当ItemHeight值发生变化时触发，重新生成子元素
        /// </summary>
        /// <param name="d">当前MagicStackPanel实例</param>
        /// <param name="e">属性变更事件参数，包含新旧值</param>
        private static void ItemHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 将DependencyObject转换为MagicStackPanel实例
            MagicStackPanel panel = (MagicStackPanel)d;

            // 清除现有子元素
            panel.Children.Clear();

            // 设置面板总高度为ItemHeight的值
            panel.Height = panel.ItemHeight;

            // 计算需要生成的边框元素数量：总高度除以单个边框高度(36)
            int index = (int)panel.ItemHeight / 36;

            // 循环生成指定数量的边框元素
            for (int i = 0; i < index; i++)
            {
                // 创建边框元素，设置固定高度36
                Border border = new Border()
                {
                    Height = 36,
                    // 交替设置背景色：偶数索引用color1，奇数索引用color2
                    Background = i % 2 == 0 ? panel.color1 : panel.color2
                };
                // 将边框添加到面板的子元素集合
                panel.Children.Add(border);
            }
        }
    }
}