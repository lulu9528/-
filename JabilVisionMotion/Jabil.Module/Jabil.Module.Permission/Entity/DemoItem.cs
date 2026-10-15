using Jabil.Module.Permission.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// 演示项类
    /// 继承自BindableBase，支持属性通知
    /// 用于封装演示内容的元数据和实例，通常用于导航或内容展示
    /// </summary>
    public class DemoItem : BindableBase
    {
        /// <summary>
        /// 内容类型
        /// 存储演示内容的类型信息，用于动态创建实例
        /// </summary>
        private readonly Type _contentType;

        /// <summary>
        /// 数据上下文
        /// 存储要传递给内容的数据源，用于绑定
        /// </summary>
        private readonly object _dataContext;

        /// <summary>
        /// 内容实例
        /// 存储动态创建的内容对象，采用懒加载方式初始化
        /// </summary>
        private object _content;

        /// <summary>
        /// 构造函数
        /// 初始化演示项的名称、内容类型和数据上下文
        /// </summary>
        /// <param name="name">演示项的名称，用于显示</param>
        /// <param name="contentType">内容的类型，必须是可实例化的类型</param>
        /// <param name="dataContext">可选的数据上下文，用于内容的数据绑定</param>
        public DemoItem(string name, Type contentType, object dataContext = null)
        {
            Name = name;
            _contentType = contentType;
            _dataContext = dataContext;
        }

        /// <summary>
        /// 演示项名称
        /// 用于在UI中显示的名称，如导航菜单文本
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 内容属性
        /// 提供对演示内容实例的访问，首次访问时动态创建
        /// </summary>
        public object Content
        {
            get
            {
                // 懒加载：首次访问时创建内容实例
                if (_content == null)
                    _content = CreateContent();
                return _content;
            }
        }

        /// <summary>
        /// 创建内容实例
        /// 根据存储的类型信息动态创建内容对象，并设置数据上下文
        /// </summary>
        /// <returns>创建的内容实例</returns>
        private object CreateContent()
        {
            // 使用反射创建内容类型的实例
            var content = Activator.CreateInstance(_contentType);

            // 如果存在数据上下文且内容是FrameworkElement，设置其DataContext
            if (_dataContext != null && content is FrameworkElement element)
            {
                element.DataContext = _dataContext;
            }

            return content;
        }
    }
}