using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration; // 提供Windows Forms与WPF集成的类
using System.Windows.Interop; // 提供与Windows句柄相关的类

namespace Jabil.Module.Permission.Forms
{
    /// <summary>
    /// 自定义ElementHost控件
    /// 继承自ElementHost，用于在Windows Forms中承载WPF元素
    /// 解决了WPF控件在WinForms环境中的键盘事件处理问题
    /// </summary>
    public class MyElementHost : ElementHost
    {
        /// <summary>
        /// 重写Dispose方法，释放资源
        /// 清理事件订阅，防止内存泄漏
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected override void Dispose(bool disposing)
        {
            if (_contentControl != null)
            {
                // 取消事件订阅
                _contentControl.Loaded -= OnContentControlOnLoaded;
                _contentControl = null;
            }

            // 调用基类Dispose方法
            base.Dispose(disposing);
        }

        /// <summary>
        /// 内部使用的ContentControl
        /// 作为WPF元素的容器，用于处理加载事件
        /// </summary>
        private ContentControl _contentControl;

        /// <summary>
        /// 重写Child属性，隐藏基类实现
        /// 包装WPF元素到ContentControl中，以便处理加载事件
        /// </summary>
        public new UIElement Child
        {
            get { return base.Child; }
            set
            {
                // 创建ContentControl作为容器
                _contentControl = new ContentControl();
                // 订阅Loaded事件
                _contentControl.Loaded += OnContentControlOnLoaded;
                // 设置Content为传入的WPF元素
                _contentControl.Content = value;
                // 将ContentControl设置为基类的Child
                base.Child = _contentControl;
            }
        }

        /// <summary>
        /// ContentControl加载完成事件处理方法
        /// 获取窗口句柄并添加钩子处理函数
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        private void OnContentControlOnLoaded(object sender, RoutedEventArgs e)
        {
            // 获取与ContentControl关联的HwndSource（窗口句柄源）
            var s = PresentationSource.FromVisual(_contentControl) as HwndSource;
            // 添加钩子函数处理窗口消息
            s?.AddHook(ChildHwndSourceHook);
        }

        // 以下常量定义对话框消息处理的标志位
        private const UInt32 DLGC_WANTARROWS = 0x0001; // 需要处理方向键
        private const UInt32 DLGC_WANTTAB = 0x0002;    // 需要处理Tab键
        private const UInt32 DLGC_WANTALLKEYS = 0x0004;// 需要处理所有按键
        private const UInt32 DLGC_HASSETSEL = 0x0008;  // 有选中的文本
        private const UInt32 DLGC_WANTCHARS = 0x0080;  // 需要处理字符输入
        private const UInt32 WM_GETDLGCODE = 0x0087;   // 获取对话框消息处理代码的消息

        /// <summary>
        /// 窗口消息钩子处理函数
        /// 处理WM_GETDLGCODE消息，指定控件需要处理的键盘事件
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="msg">消息ID</param>
        /// <param name="wParam">消息参数W</param>
        /// <param name="lParam">消息参数L</param>
        /// <param name="handled">是否已处理消息</param>
        /// <returns>处理结果</returns>
        static IntPtr ChildHwndSourceHook(IntPtr hwnd,
          int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 只处理WM_GETDLGCODE消息
            if (msg != WM_GETDLGCODE) return IntPtr.Zero;

            // 标记消息已处理
            handled = true;

            // 返回需要处理的键盘事件标志组合
            // 包括字符输入、方向键和文本选择
            return new IntPtr(DLGC_WANTCHARS | DLGC_WANTARROWS | DLGC_HASSETSEL);
        }
    }
}