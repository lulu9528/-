using Jabil.Module.Log.Lib;
using Jabil.Module.Serialport.Frm;
using Sunny.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Jabil.Module.Serialport
{
    /// <summary>
    /// 串口管理主页面
    /// 负责切换串口控制页面和配置页面
    /// </summary>
    public partial class SerialportPage : UIPage
    {
        #region 枚举定义
        /// <summary>
        /// 页面模式枚举
        /// 表示当前显示的功能页面
        /// </summary>
        private enum PageMode
        {
            /// <summary>
            /// 无页面
            /// </summary>
            None,
            /// <summary>
            /// 串口控制页面
            /// </summary>
            SerialportControl,
            /// <summary>
            /// 串口配置页面
            /// </summary>
            SerialportConfig
        }
        #endregion

        #region 字段与属性
        /// <summary>
        /// 当前页面模式
        /// </summary>
        private PageMode _currentPageMode = PageMode.None;

        /// <summary>
        /// 单例实例
        /// </summary>
        private static SerialportPage _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static SerialportPage Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new SerialportPage();
                }
                return _instance;
            }
        }
        #endregion

        /// <summary>
        /// 初始化SerialportPage实例
        /// </summary>
        public SerialportPage()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>
        /// 页面加载事件
        /// </summary>
        private void SerialportPage_Load(object sender, EventArgs e)
        {
            try
            {
                Instance.Enabled = false;
                SwitchPage(PageMode.SerialportControl);
                Instance.Enabled = true;
            }
            catch (Exception ex)
            {
                Instance.Enabled = true;
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 切换显示的页面
        /// </summary>
        /// <param name="targetMode">目标页面模式</param>
        private void SwitchPage(PageMode targetMode)
        {
            if (_currentPageMode == targetMode) return; // 避免重复切换

            try
            {
                // 清除当前页面
                ClearCurrentPage();

                // 更新当前模式
                _currentPageMode = targetMode;

                // 加载目标页面
                var targetControl = CreateTargetPageControl(targetMode);
                if (targetControl != null)
                {
                    AddControlToContainer(targetControl);
                    UpdateButtonStyles(targetMode);
                    uiPanel_window.Visible = true;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                _currentPageMode = PageMode.None;
            }
        }

        /// <summary>
        /// 清除当前显示的页面
        /// </summary>
        private void ClearCurrentPage()
        {
            uiPanel_window.Visible = false;
            uiPanel_window.Controls.Clear();
        }

        /// <summary>
        /// 创建目标页面的控件
        /// </summary>
        /// <param name="mode">页面模式</param>
        /// <returns>创建的控件</returns>
        private UIPage CreateTargetPageControl(PageMode mode)
        {
            switch (mode)
            {
                case PageMode.SerialportControl:
                    return PrepareControl(Frm_SerialportControl.Instance);

                case PageMode.SerialportConfig:
                    // 假设串口配置页面的单例实现
                    // return PrepareControl(FrmSerialportConfig.Instance);
                    return null; // 实际项目中替换为配置页面实例

                default:
                    return null;
            }
        }

        /// <summary>
        /// 准备页面控件（设置属性）
        /// </summary>
        /// <param name="control">需要准备的控件</param>
        /// <returns>处理后的控件</returns>
        private UIPage PrepareControl(UIPage control)
        {
            control.TopLevel = false;
            control.Dock = DockStyle.Fill;
            control.AutoScaleMode = AutoScaleMode.None;
            control.Show();
            return control;
        }

        /// <summary>
        /// 将控件添加到容器
        /// </summary>
        /// <param name="control">要添加的控件</param>
        private void AddControlToContainer(UIPage control)
        {
            uiPanel_window.Controls.Add(control);
            uiPanel_window.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// 更新按钮样式（显示当前选中状态）
        /// </summary>
        /// <param name="currentMode">当前页面模式</param>
        private void UpdateButtonStyles(PageMode currentMode)
        {
            // 重置所有按钮颜色
            toolStripButton_SerialportControl.ForeColor = Color.Black;
            toolStripButton_SerialportConfig.ForeColor = Color.Black;

            // 设置当前页面对应的按钮颜色
            switch (currentMode)
            {
                case PageMode.SerialportControl:
                    toolStripButton_SerialportControl.ForeColor = Color.Gray;
                    break;
                case PageMode.SerialportConfig:
                    toolStripButton_SerialportConfig.ForeColor = Color.Gray;
                    break;
            }
        }

        /// <summary>
        /// 串口配置按钮点击事件
        /// </summary>
        private void toolStripButton_SerialportConfig_Click(object sender, EventArgs e)
        {
            try
            {
                toolStripButton_SerialportConfig.Enabled = false;
                SwitchPage(PageMode.SerialportConfig);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                toolStripButton_SerialportConfig.Enabled = true;
            }
        }

        /// <summary>
        /// 串口控制按钮点击事件
        /// </summary>
        private void toolStripButton_SerialportControl_Click(object sender, EventArgs e)
        {
            try
            {
                toolStripButton_SerialportControl.Enabled = false;
                SwitchPage(PageMode.SerialportControl);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                toolStripButton_SerialportControl.Enabled = true;
            }
        }
    }
}
