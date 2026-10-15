using Jabil.Module.Log.Lib;
using Jabil.Module.TCPIP.Common.Dtos;
using Jabil.Module.TCPIP.Control;
using Jabil.Module.TCPIP.Lib;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Jabil.Module.TCPIP.Frm
{
    /// <summary>
    /// TCPIP控制主窗体，用于管理多个TCPIP设备的控制界面
    /// </summary>
    public partial class Frm_TCPIPControl : UIPage
    {
        #region 单例模式
        private static readonly Lazy<Frm_TCPIPControl> _instance = new Lazy<Frm_TCPIPControl>(
            () => new Frm_TCPIPControl(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取窗体唯一实例
        /// </summary>
        public static Frm_TCPIPControl Instance => _instance.Value;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化TCPIP控制主窗体
        /// </summary>
        private Frm_TCPIPControl()
        {
            InitializeComponent();
            InitializeControlStyles();
        }

        /// <summary>
        /// 设置控件样式以优化绘制性能
        /// </summary>
        private void InitializeControlStyles()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();
        }
        #endregion

        #region 窗体加载
        /// <summary>
        /// 窗体加载事件，初始化TCPIP设备控制界面
        /// </summary>
        private void FrmTcpIpControl_Load(object sender, EventArgs e)
        {
            try
            {
                LoadTcpIpDeviceControls();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowErrorMessage($"加载TCPIP控制界面失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载所有TCPIP设备的控制界面
        /// </summary>
        private void LoadTcpIpDeviceControls()
        {
            if (TcpIpSystem.Instance?.TcpIpCommunications == null ||
                TcpIpSystem.Instance.TcpIpCommunications.Count == 0)
            {
                ShowInformationMessage("没有找到TCPIP设备配置信息");
                return;
            }

            // 配置标签控件
            tcpipTabControl.Alignment = TabAlignment.Bottom;
            tcpipTabControl.Controls.Clear();

            foreach (var deviceConfig in TcpIpSystem.Instance.TcpIpCommunications)
            {
                try
                {
                    // 创建标签页
                    var tabPage = CreateTabPage(deviceConfig);

                    // 创建设备控制控件
                    var deviceControl = new TcpIpControlUI(deviceConfig);
                    ConfigureDeviceControl(deviceControl);

                    // 将控件添加到标签页
                    tabPage.Controls.Add(deviceControl);
                    tcpipTabControl.Controls.Add(tabPage);
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex);
                    ShowErrorMessage($"加载设备 [{deviceConfig.设备名称}] 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 创建标签页
        /// </summary>
        private TabPage CreateTabPage(TcpIpCommunicationDto deviceConfig)
        {
            return new TabPage
            {
                Name = $"tabPage_{deviceConfig.设备名称}",
                Text = deviceConfig.设备名称,
                Location = new Point(0, 40),
                Size = new Size(1582, 965),
                TabIndex = deviceConfig.设备索引
            };
        }

        /// <summary>
        /// 配置设备控制控件
        /// </summary>
        private void ConfigureDeviceControl(TcpIpControlUI control)
        {
            control.Dock = DockStyle.Fill;
            control.Name = control.GetType().Name + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        #endregion

        #region 消息提示
        /// <summary>
        /// 显示错误消息
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            UIMessageBox.ShowError(message);
        }

        /// <summary>
        /// 显示信息消息
        /// </summary>
        private void ShowInformationMessage(string message)
        {
            UIMessageBox.ShowInfo(message);
        }
        #endregion
    }
}
