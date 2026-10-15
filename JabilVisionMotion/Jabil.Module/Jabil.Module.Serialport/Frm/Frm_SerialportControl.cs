using Jabil.Module.Log.Lib;
using Jabil.Module.Serialport.Common.Dtos;
using Jabil.Module.Serialport.Control;
using Jabil.Module.Serialport.Lib;
using Sunny.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Jabil.Module.Serialport.Frm
{
    /// <summary>
    /// 串口控制主窗体
    /// 管理多个串口的标签页显示和控制
    /// </summary>
    public partial class Frm_SerialportControl : UIPage
    {
        #region 单例模式实现
        /// <summary>
        /// 窗体单例实例
        /// </summary>
        private static Frm_SerialportControl _instance;

        /// <summary>
        /// 获取窗体单例
        /// </summary>
        public static Frm_SerialportControl Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new Frm_SerialportControl();
                }
                return _instance;
            }
        }
        #endregion

        /// <summary>
        /// 初始化FrmSerialportControl实例
        /// </summary>
        public Frm_SerialportControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        private void FrmSerialportControl_Load(object sender, EventArgs e)
        {
            try
            {
                LoadSerialportTabs();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 加载所有串口对应的标签页
        /// </summary>
        private void LoadSerialportTabs()
        {
            if (SerialportSystem.Instance?.SerialportList == null ||
                SerialportSystem.Instance.SerialportList.Count == 0)
            {
                return;
            }

            // 清除现有标签页（避免重复加载）
            uiTabControl1.Controls.Clear();

            foreach (var serialportConfig in SerialportSystem.Instance.SerialportList)
            {
                var tabPage = CreateSerialportTabPage(serialportConfig);
                var control = CreateSerialportControl(serialportConfig);

                tabPage.Controls.Add(control);
                uiTabControl1.Controls.Add(tabPage);
            }

            // 设置标签页对齐方式
            uiTabControl1.Alignment = TabAlignment.Bottom;
        }

        /// <summary>
        /// 创建串口标签页
        /// </summary>
        /// <param name="config">串口配置</param>
        /// <returns>创建的标签页</returns>
        private TabPage CreateSerialportTabPage(SerialportDto config)
        {
            return new TabPage
            {
                Location = new Point(0, 40),
                Name = $"tabPage_{config.COM口}",
                Size = new Size(1582, 965),
                TabIndex = 0,
                Text = config.COM口
            };
        }

        /// <summary>
        /// 创建串口控制控件
        /// </summary>
        /// <param name="config">串口配置</param>
        /// <returns>创建的串口控制控件</returns>
        private SerialportControUI CreateSerialportControl(SerialportDto config)
        {
            return new SerialportControUI(config)
            {
                Dock = DockStyle.Fill,
                Location = new Point(3, 3),
                Name = config.COM口,
                Size = new Size(1000, 304)
            };
        }
    }
}
