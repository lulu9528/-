using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// TreeView选中项行为类
    /// 继承自Behavior<TreeView>，用于增强TreeView的选中项功能
    /// 实现TreeView选中项与ViewModel属性的双向绑定，以及右键点击选中功能
    /// </summary>
    public class TreeViewSelectedItemBehavior : Behavior<TreeView>
    {
        /// <summary>
        /// 选中项依赖属性
        /// 用于与ViewModel中的属性进行双向绑定，同步TreeView的选中项
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),                // 属性名称
            typeof(object),                      // 属性类型
            typeof(TreeViewSelectedItemBehavior),// 所有者类型
            new FrameworkPropertyMetadata(       // 属性元数据
                null,                            // 默认值
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault // 支持双向绑定
            )
        );

        /// <summary>
        /// 选中项属性
        /// 封装依赖属性的访问，用于绑定操作
        /// </summary>
        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        /// <summary>
        /// 行为附加到TreeView时调用
        /// 注册TreeView的事件处理程序，开始监听选中项变化和鼠标右键事件
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            // 注册选中项变化事件，用于同步选中项到依赖属性
            AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
            // 注册鼠标右键按下事件，用于实现右键点击选中功能
            AssociatedObject.PreviewMouseRightButtonDown += AssociatedObject_PreviewMouseRightButtonDown;
        }

        /// <summary>
        /// 鼠标右键按下事件处理程序
        /// 实现右键点击TreeView项时自动选中该项目
        /// </summary>
        /// <param name="sender">事件源（TreeView）</param>
        /// <param name="e">鼠标事件参数</param>
        private void AssociatedObject_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 获取鼠标点击的原始元素，并尝试向上查找其父级TreeViewItem
            if (e.OriginalSource is DependencyObject d)
            {
                // 找到包含点击元素的TreeViewItem并使其获得焦点（即选中）
                d.FindParent<TreeViewItem>()?.Focus();
                // 标记事件已处理，防止事件冒泡影响其他处理
                e.Handled = true;
            }
        }

        /// <summary>
        /// 行为从TreeView分离时调用
        /// 注销已注册的事件处理程序，防止内存泄漏
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            // 注销选中项变化事件
            AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
        }

        /// <summary>
        /// TreeView选中项变化事件处理程序
        /// 将TreeView的选中项同步到依赖属性，实现与ViewModel的绑定
        /// </summary>
        /// <param name="sender">事件源（TreeView）</param>
        /// <param name="e">选中项变化事件参数，包含新旧选中项</param>
        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // 更新依赖属性值，同步到ViewModel
            SelectedItem = e.NewValue;
        }
    }

    /// <summary>
    /// 树视图辅助类
    /// 提供视觉树相关的辅助方法，用于查找元素的父级控件
    /// </summary>
    public static class TreeHelper
    {
        /// <summary>
        /// 在视觉树中查找指定类型的父级元素
        /// </summary>
        /// <typeparam name="T">要查找的父级元素类型（必须继承自DependencyObject）</typeparam>
        /// <param name="value">起始元素，从此元素开始向上查找</param>
        /// <returns>找到的父级元素，若未找到则返回null</returns>
        public static T FindParent<T>(this DependencyObject value) where T : DependencyObject
        {
            DependencyObject parent = value;
            // 循环向上查找父级，直到找到指定类型或到达视觉树顶部
            while (parent != null && (parent as T) == null)
                parent = VisualTreeHelper.GetParent(parent);
            // 返回找到的父级元素（已转换为指定类型）
            return parent as T;
        }
    }
}