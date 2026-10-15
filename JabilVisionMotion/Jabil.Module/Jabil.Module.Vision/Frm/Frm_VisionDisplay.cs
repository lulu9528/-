using Sunny.UI;
using System;
using System.Windows.Forms;

namespace Jabil.Module.Vision.Frm
{
    /// <summary>
    /// 视觉检测结果显示页面
    /// 功能：展示视觉检测的实时图像和分析结果
    /// 设计模式：线程安全的单例模式，确保应用中只有一个实例
    /// </summary>
    public partial class Frm_VisionDisplay : UIPage
    {
        #region 单例模式实现

        /// <summary>
        /// 窗体对象实例
        /// 私有字段确保只能通过Instance属性访问
        /// </summary>
        private static Frm_VisionDisplay _instance;

        /// <summary>
        /// 线程同步锁
        /// 确保多线程环境下实例创建的安全性
        /// </summary>
        private static readonly object _syncLock = new object();

        /// <summary>
        /// 单例实例访问属性
        /// 采用双重检查锁定模式确保线程安全和高性能
        /// </summary>
        public static Frm_VisionDisplay Instance
        {
            get
            {
                // 第一次检查：未加锁，提高常规操作性能
                if (_instance == null || _instance.IsDisposed)
                {
                    // 加锁确保只有一个线程进入实例创建逻辑
                    lock (_syncLock)
                    {
                        // 第二次检查：防止多线程同时通过第一次检查后重复创建实例
                        if (_instance == null || _instance.IsDisposed)
                        {
                            // 确保在UI线程上创建控件实例
                            if (Application.OpenForms.Count > 0)
                            {
                                _instance = Application.OpenForms[0].Invoke(new Func<Frm_VisionDisplay>(() => new Frm_VisionDisplay())) as Frm_VisionDisplay;
                            }
                            else
                            {
                                _instance = new Frm_VisionDisplay();
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 构造函数与初始化

        /// <summary>
        /// 私有构造函数
        /// 防止外部直接实例化，确保单例模式有效性
        /// </summary>
        private Frm_VisionDisplay()
        {
            try
            {
                InitializeComponent();
                InitializeControlStyles();
            }
            catch (Exception ex)
            {
                throw; // 重新抛出异常，确保调用方知晓初始化失败
            }
        }

        /// <summary>
        /// 初始化控件样式
        /// 集中设置控件绘制相关样式，提升UI性能
        /// </summary>
        private void InitializeControlStyles()
        {
            // 启用双缓冲，减少绘制闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            // 确保只在WM_PAINT消息时绘制，提高绘制效率
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            // 启用用户绘制，允许自定义绘制逻辑
            SetStyle(ControlStyles.UserPaint, true);
            // 忽略窗口消息的背景擦除，减少闪烁
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            // 应用样式设置
            UpdateStyles();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 刷新显示内容
        /// 用于更新视觉检测结果和图像显示
        /// </summary>
        public void RefreshDisplay()
        {
            // 确保在UI线程执行刷新操作
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshDisplay));
                return;
            }

            try
            {
                // 具体刷新逻辑实现
                // TODO: 添加实际的刷新代码
            }
            catch (Exception ex)
            {
                UIMessageBox.ShowError($"刷新显示失败: {ex.Message}");
            }
        }

        #endregion
    }
}
