using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.Setting.Frm;
using Sunny.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JabilVisionMotion.Frm.UserPage
{
    public partial class SettingPage : UIPage
    {
        // 单例实例（线程安全优化）
        private static readonly object _lock = new object();
        private static SettingPage _instance;
        public static SettingPage Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    lock (_lock)
                    {
                        if (_instance == null || _instance.IsDisposed)
                        {
                            _instance = new SettingPage();
                        }
                    }
                }
                return _instance;
            }
        }

        // 页面类型枚举（明确表示当前需要显示的页面）
        private enum DisplayPageType
        {
            None,
            ProjectSetting,
            SystemSetting
        }

        private DisplayPageType _currentPageType = DisplayPageType.None;

        public SettingPage()
        {
            InitializeComponent();
        }

        #region 事件处理（命名更清晰，明确对应功能）
        private void tsbtnProjectSetting_Click(object sender, EventArgs e)
        {
            SwitchPage(DisplayPageType.ProjectSetting);
        }

        private void tsbtnSystemSetting_Click(object sender, EventArgs e)
        {
            SwitchPage(DisplayPageType.SystemSetting);
        }

        private void uiPanel_window_Load(object sender, EventArgs e)
        {
            // 初始加载项目设置页面
            SwitchPage(DisplayPageType.ProjectSetting);
        }
        #endregion

        #region 核心方法（提取重复逻辑，增强可维护性）
        /// <summary>
        /// 切换显示的设置页面
        /// </summary>
        /// <param name="targetPage">目标页面类型</param>
        private void SwitchPage(DisplayPageType targetPage)
        {
            if (_currentPageType == targetPage) return; // 避免重复加载

            try
            {
                _currentPageType = targetPage;
                Form targetForm = targetPage switch
                {
                    DisplayPageType.ProjectSetting => Frm_Project.Instance,
                    DisplayPageType.SystemSetting => Frm_System.Instance,
                    _ => null
                };

                if (targetForm != null)
                {
                    LoadFormToPanel(targetForm,
                        targetPage == DisplayPageType.ProjectSetting
                            ? "打开项目参数设置界面！"
                            : "打开系统参数设置界面！");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 将目标窗体加载到容器面板中（通用逻辑提取）
        /// </summary>
        /// <param name="form">要加载的窗体</param>
        /// <param name="logInfo">操作日志信息</param>
        private void LoadFormToPanel(Form form, string logInfo)
        {
            // 清除面板并加载新窗体
            uiPanel_window.Visible = false;
            uiPanel_window.Controls.Clear();

            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.AutoScaleMode = AutoScaleMode.None;
            uiPanel_window.Controls.Add(form);
            form.Show();

            uiPanel_window.Visible = true;

            // 记录操作日志
            RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Black, logInfo);
        }
        #endregion
    }
}