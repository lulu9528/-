// 引入视图相关的命名空间，包含LoginView、AddUserView等视图类
using Jabil.Module.Permission.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 定义权限模块的命名空间
namespace Jabil.Module.Permission
{
    /// <summary>
    /// 视图定位器类，用于统一管理和提供应用程序中的各个视图实例
    /// 采用单例模式设计，确保整个应用中每种视图只有一个实例
    /// </summary>
    public class ViewLocator
    {
        /// <summary>
        /// 懒加载单例实例的持有者
        /// Lazy<T>确保实例在首次使用时才被创建，并且是线程安全的
        /// </summary>
        private static Lazy<ViewLocator> lazy => new Lazy<ViewLocator>(() => new ViewLocator());

        /// <summary>
        /// 提供全局访问点，获取ViewLocator的唯一实例
        /// </summary>
        public static ViewLocator Instance => lazy.Value;

        /// <summary>
        /// 登录视图的私有实例
        /// </summary>
        private LoginView _loginView;

        /// <summary>
        /// 添加用户视图的私有实例
        /// </summary>
        private AddUserView _addUserView;

        /// <summary>
        /// 主视图的私有实例
        /// </summary>
        private MainView _mainView;

        /// <summary>
        /// 私有构造函数，防止外部直接创建实例
        /// 在构造函数中初始化所有视图实例
        /// </summary>
        private ViewLocator()
        {
            _loginView = new LoginView();
            _addUserView = new AddUserView();
            _mainView = new MainView();
        }

        /// <summary>
        /// 登录视图的公共访问属性
        /// 提供对_loginView实例的只读访问
        /// </summary>
        public LoginView LoginView => _loginView;

        /// <summary>
        /// 添加用户视图的公共访问属性
        /// 提供对_addUserView实例的只读访问
        /// </summary>
        public AddUserView AddUserView => _addUserView;

        /// <summary>
        /// 主视图的公共访问属性
        /// 提供对_mainView实例的只读访问
        /// </summary>
        public MainView MainView => _mainView;
    }
}