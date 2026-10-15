using Jabil.Module.Permission.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Jabil.Module.Permission.Forms
{
    /// <summary>
    /// 权限管理页面窗体
    /// 继承自Windows Forms的Form，用于承载权限管理相关的WPF用户控件
    /// 实现单例模式，确保应用中只有一个权限管理页面实例
    /// </summary>
    public partial class PermissionPage : Form
    {
        /// <summary>
        /// 构造函数
        /// 初始化资源并设置窗体样式
        /// </summary>
        public PermissionPage()
        {
            // 确保WPF应用资源已加载
            EnsureApplicationResources();
            // 初始化Windows Forms控件（由设计器生成）
            InitializeComponent();
            // 启用双缓冲优化绘图性能，减少闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            // 确保所有绘制操作都在WM_PAINT消息中完成，提高绘制效率
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>
        /// 窗体单例实例
        /// 静态变量存储唯一实例
        /// </summary>
        private static PermissionPage _instance;

        /// <summary>
        /// 单例访问属性
        /// 提供全局访问点，确保只创建一个PermissionPage实例
        /// </summary>
        public static PermissionPage Instance
        {
            get
            {
                // 懒加载：首次访问时创建实例
                if (_instance == null)
                    _instance = new PermissionPage();
                return _instance;
            }
        }

        /// <summary>
        /// 确保WPF应用程序资源已初始化
        /// 处理Windows Forms与WPF混合编程时的资源加载问题
        /// </summary>
        public static void EnsureApplicationResources()
        {
            // 检查是否已存在WPF应用程序实例
            if (System.Windows.Application.Current == null)
            {
                // 创建WPF应用程序对象
                new System.Windows.Application();
                // 设置关闭模式为显式关闭（避免WPF应用自动关闭）
                System.Windows.Application.Current.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                // 加载并合并权限模块的资源字典（包含样式、模板等）
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(
                    // 加载嵌入式资源中的Permission.xaml资源字典
                    System.Windows.Application.LoadComponent(
                        new Uri("Jabil.Module.Permission;component/Permission.xaml", UriKind.Relative))
                    as System.Windows.ResourceDictionary);
            }
        }
    }
}