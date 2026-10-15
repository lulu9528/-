using Jabil.Lib;
using Jabil.Module.IO.Control;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Jabil.Module.IO.Frm
{
    /// <summary>
    /// IO控制窗体
    /// </summary>
    public partial class Frm_IOControl : UIPage
    {
        /// <summary>
        /// 窗体对象实例（单例模式）
        /// </summary>
        private static Frm_IOControl _instance;

        /// <summary>
        /// 获取窗体单例实例
        /// </summary>
        public static Frm_IOControl Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new Frm_IOControl();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public Frm_IOControl()
        {
            InitializeComponent();
            // 启用双缓冲减少闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // 注册事件处理
            Load += FrmIOControl_Load;
            PermissionSystem.Instance.OnLoginUserChanged += OnLoginUserChanged;
        }

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        private void FrmIOControl_Load(object sender, EventArgs e)
        {
            try
            {
                // 初始化IO卡标签页
                InitializeIOCardTabs();

                // 更新权限
                UpdatePermission();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 初始化IO卡标签页
        /// </summary>
        private void InitializeIOCardTabs()
        {
            if (IOSystem.Instance.IOCardDios?.Any() != true)
            {
                return;
            }

            // 清除现有标签页（防止重复加载）
            uiTabControl1.Controls.Clear();
            uiTabControl1.Alignment = TabAlignment.Bottom;

            foreach (var ioCard in IOSystem.Instance.IOCards)
            {
                // 创建标签页
                var tabPage = new TabPage
                {
                    Location = new Point(0, 40),
                    Name = $"tabPage_{ioCard.卡名称}",
                    Size = new Size(1582, 965),
                    TabIndex = uiTabControl1.Controls.Count,
                    Text = ioCard.卡名称,
                    UseVisualStyleBackColor = true
                };

                // 创建IO控制控件
                var ioControl = new IOControlUI(ioCard)
                {
                    Dock = DockStyle.Fill,
                    Location = new Point(3, 3),
                    Name = ioCard.卡名称,
                    Size = new Size(1000, 304),
                    TabIndex = ioCard.卡号索引
                };

                // 添加到标签页
                tabPage.Controls.Add(ioControl);
                uiTabControl1.Controls.Add(tabPage);
            }
        }

        /// <summary>
        /// 用户登录状态变化事件
        /// </summary>
        private void OnLoginUserChanged(object sender, EventArgs e)
        {
            // 确保在UI线程更新权限
            InvokeIfRequired(() => UpdatePermission());
        }

        /// <summary>
        /// 更新权限状态
        /// </summary>
        private void UpdatePermission()
        {
            try
            {
                // 检查权限并更新控件可见性
                bool hasPermission = PermissionSystem.CheckPermission("手动/IO/监控", out _);
                //toolStripButton_IOControl.Visible = hasPermission;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 线程安全的UI调用
        /// </summary>
        /// <param name="action">要执行的UI操作</param>
        private void InvokeIfRequired(Action action)
        {
            if (action == null) return;

            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}