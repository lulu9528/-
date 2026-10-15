using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// 密码框扩展类
    /// 提供附加属性实现PasswordBox的Password属性双向绑定
    /// 解决WPF原生PasswordBox的Password属性不支持绑定的问题
    /// </summary>
    public class PassWordEx
    {
        /// <summary>
        /// 获取附加属性PassWord的值
        /// </summary>
        /// <param name="obj">依赖对象（通常是PasswordBox）</param>
        /// <returns>当前存储的密码字符串</returns>
        public static string GetPassWord(DependencyObject obj)
        {
            return (string)obj.GetValue(PassWordProperty);
        }

        /// <summary>
        /// 设置附加属性PassWord的值
        /// </summary>
        /// <param name="obj">依赖对象（通常是PasswordBox）</param>
        /// <param name="value">要设置的密码字符串</param>
        public static void SetPassWord(DependencyObject obj, string value)
        {
            obj.SetValue(PassWordProperty, value);
        }

        /// <summary>
        /// PassWord附加属性
        /// 作为PasswordBox的Password属性与ViewModel绑定的中间桥梁
        /// 支持双向绑定，实现密码的读写同步
        /// </summary>
        public static readonly DependencyProperty PassWordProperty =
            DependencyProperty.RegisterAttached(
                "PassWord",                  // 属性名称
                typeof(string),              // 属性类型
                typeof(PassWordEx),          // 所有者类型
                new PropertyMetadata(        // 属性元数据
                    string.Empty,            // 默认值为空字符串
                    OnPassWordPropertyChanged // 属性值变更回调
                )
            );

        /// <summary>
        /// PassWord属性值变更回调
        /// 当绑定的ViewModel属性值变化时，同步更新PasswordBox的Password属性
        /// </summary>
        /// <param name="sender">触发事件的依赖对象（PasswordBox）</param>
        /// <param name="e">属性变更事件参数，包含新旧值</param>
        static void OnPassWordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // 将依赖对象转换为PasswordBox
            var passWord = sender as PasswordBox;
            // 获取新的密码值
            string pw = (string)e.NewValue;

            // 确保PasswordBox不为空且密码值不一致时才更新，避免循环同步
            if (passWord != null && passWord.Password != pw)
            {
                passWord.Password = pw;
            }
        }
    }

    /// <summary>
    /// 密码框行为类
    /// 继承自Behavior<PasswordBox>，用于监听PasswordBox的密码变化
    /// 实现PasswordBox到PassWord附加属性的同步，完成双向绑定
    /// </summary>
    public class PasswordBehavior : Behavior<PasswordBox>
    {
        /// <summary>
        /// 行为附加到目标元素时调用
        /// 注册PasswordChanged事件，监听密码框内容变化
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            // 为关联的PasswordBox注册密码变化事件
            AssociatedObject.PasswordChanged += AssociatedObject_PasswordChanged;
        }

        /// <summary>
        /// 密码框内容变化事件处理
        /// 当用户输入密码导致内容变化时，同步更新PassWord附加属性
        /// </summary>
        /// <param name="sender">事件源（PasswordBox）</param>
        /// <param name="e">路由事件参数</param>
        private void AssociatedObject_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // 将事件源转换为PasswordBox
            var passWord = sender as PasswordBox;
            // 获取当前附加属性的值
            string pw = PassWordEx.GetPassWord(passWord);

            // 确保PasswordBox不为空且密码值不一致时才更新，避免循环同步
            if (passWord != null && passWord.Password != pw)
            {
                // 更新附加属性值，从而同步到ViewModel
                PassWordEx.SetPassWord(passWord, passWord.Password);
            }
        }

        /// <summary>
        /// 行为从目标元素分离时调用
        /// 注销事件，防止内存泄漏
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            // 确保关联对象不为空时才注销事件
            if (AssociatedObject != null)
            {
                AssociatedObject.PasswordChanged -= AssociatedObject_PasswordChanged;
            }
        }
    }
}