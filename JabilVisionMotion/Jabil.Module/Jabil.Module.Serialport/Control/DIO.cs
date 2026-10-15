using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace Jabil.Module.Serialport.Control
{
    /// <summary>
    /// 串口DIO（数字输入输出）用户控件
    /// 用于显示和控制串口DIO点位信息
    /// </summary>
    public partial class DIO : UIUserControl
    {
        /// <summary>
        /// 控件点击事件委托
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        public delegate void ControlClickEventHandler(object sender, EventArgs e);

        /// <summary>
        /// 控件点击事件
        /// </summary>
        public event ControlClickEventHandler ControlClick;

        private string _pointName = "DI-";
        private bool _pointState = false;
        private string _messageFormat = "";

        /// <summary>
        /// 点位名称
        /// </summary>
        [Description("点位名称"), Category("控件定义")]
        public string PointName
        {
            get => _pointName;
            set
            {
                if (_pointName != value)
                {
                    _pointName = value;
                    labelPointName.Text = _pointName;
                }
            }
        }

        /// <summary>
        /// 点位状态
        /// </summary>
        [Description("点位状态"), Category("控件定义")]
        public bool PointState
        {
            get => _pointState;
            set
            {
                if (_pointState != value)
                {
                    _pointState = value;
                    UpdateControlAppearance();
                }
            }
        }

        /// <summary>
        /// 报文格式
        /// </summary>
        [Description("报文格式"), Category("控件定义")]
        public string MessageFormat
        {
            get => _messageFormat;
            set
            {
                if (_messageFormat != value)
                {
                    _messageFormat = value;
                    labelMessageFormat.Text = _messageFormat;
                }
            }
        }

        /// <summary>
        /// 初始化DIOControl实例
        /// </summary>
        public DIO()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            labelPointName.Active = false;
            labelMessageFormat.Active = false;
        }

        /// <summary>
        /// 更新控件外观显示
        /// </summary>
        private void UpdateControlAppearance()
        {
            uiLightState.State = _pointState ? UILightState.On : UILightState.Off;
        }

        /// <summary>
        /// 触发控件点击事件
        /// </summary>
        protected virtual void OnControlClick()
        {
            ControlClick?.Invoke(this, EventArgs.Empty);
        }

        private void labelPointName_Click(object sender, EventArgs e) => OnControlClick();

        private void labelMessageFormat_Click(object sender, EventArgs e) => OnControlClick();

        private void uiLightState_Click(object sender, EventArgs e) => OnControlClick();
    }
}
