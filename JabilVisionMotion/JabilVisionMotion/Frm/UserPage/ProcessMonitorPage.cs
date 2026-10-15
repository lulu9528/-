using JabilVisionMotion.Lib;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Frm.UserPage
{
    /// <summary>
    /// 流程监控页面类
    /// 继承自Sunny.UI的UIPage控件，用于实时监控系统的运行流程和状态
    /// 提供可视化的流程进度展示、状态指示和关键参数监控功能
    /// </summary>
    public partial class ProcessMonitorPage : UIPage
    {
        /// <summary>
        /// 初始化ProcessMonitorPage的新实例
        /// 调用InitializeComponent()加载设计器生成的控件布局
        /// 同时配置UI渲染优化参数，提升界面显示效果
        /// </summary>
        public ProcessMonitorPage()
        {
            InitializeComponent();

            // 启用双缓冲优化，减少界面重绘时的闪烁现象
            // 适用于需要频繁更新的监控界面，提升用户体验
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            // 确保所有绘制操作都通过WM_PAINT消息进行
            // 避免在非客户区域绘制时产生的闪烁问题
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>
        /// 窗体对象实例字段
        /// 用于存储ProcessMonitorPage的单例实例
        /// 保证在应用程序生命周期内仅存在一个流程监控页面实例
        /// </summary>
        private static ProcessMonitorPage _instance;

        /// <summary>
        /// 单例实例访问属性
        /// 提供全局唯一的流程监控页面访问点
        /// 实现懒加载和实例有效性检查（处理已释放的情况）
        /// </summary>
        public static ProcessMonitorPage Instance
        {
            get
            {
                // 检查实例是否为空或已被释放（IsDisposed为true）
                // 若满足任一条件则创建新实例，确保始终能获取有效的页面实例
                if (_instance == null || _instance.IsDisposed)
                    _instance = new ProcessMonitorPage();
                return _instance;
            }
        }
    }
}
