using System;
using System.ComponentModel;
using System.Windows.Forms;
using Sunny.UI;

namespace Jabil.Module.TCPIP.Control
{
    /// <summary>
    /// DIO点位控制控件，用于显示和操作DIO点位状态
    /// </summary>
    public partial class DIO : UIUserControl
    {
        #region 事件定义
        /// <summary>
        /// 控件点击事件委托
        /// </summary>
        /// <param name="sender">触发事件的控件</param>
        /// <param name="e">事件参数</param>
        public delegate void ControlClickEventHandler(object sender, EventArgs e);

        /// <summary>
        /// 当控件的任何部分被点击时触发
        /// </summary>
        public event ControlClickEventHandler ControlClick;
        #endregion

        #region 私有字段
        private string _pointName = "DI-";
        private bool _pointState;
        private string _messageFormat = "";
        #endregion

        #region 公共属性
        /// <summary>
        /// 获取或设置点位名称
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
                    lblPointName.Text = _pointName;
                }
            }
        }

        /// <summary>
        /// 获取或设置点位状态
        /// </summary>
        [Description("点位状态（true为开，false为关）"), Category("控件定义")]
        public bool PointState
        {
            get => _pointState;
            set
            {
                if (_pointState != value)
                {
                    _pointState = value;
                    UpdateControlDisplay();
                }
            }
        }

        /// <summary>
        /// 获取或设置报文格式
        /// </summary>
        [Description("报文格式定义"), Category("控件定义")]
        public string MessageFormat
        {
            get => _messageFormat;
            set
            {
                if (_messageFormat != value)
                {
                    _messageFormat = value;
                    lblMessageFormat.Text = _messageFormat;
                }
            }
        }
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化DIO控件实例
        /// </summary>
        public DIO()
        {
            InitializeComponent();
            InitializeControlStyles();
            InitializeChildControls();
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

        /// <summary>
        /// 初始化子控件状态
        /// </summary>
        private void InitializeChildControls()
        {
            lblPointName.Active = false;
            lblMessageFormat.Active = false;
            uiLightState.State = UILightState.Off;
        }
        #endregion

        #region 界面更新
        /// <summary>
        /// 更新控件显示状态
        /// </summary>
        private void UpdateControlDisplay()
        {
            uiLightState.State = _pointState ? UILightState.On : UILightState.Off;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 触发控件点击事件
        /// </summary>
        /// <param name="e">事件参数</param>
        protected virtual void OnControlClick(EventArgs e)
        {
            ControlClick?.Invoke(this, e);
        }

        private void lblPointName_Click(object sender, EventArgs e)
        {
            OnControlClick(e);
        }

        private void lblMessageFormat_Click(object sender, EventArgs e)
        {
            OnControlClick(e);
        }

        private void uiLightState_Click(object sender, EventArgs e)
        {
            OnControlClick(e);
        }

        // 可选：添加控件本身的点击事件处理
        private void DioControl_Click(object sender, EventArgs e)
        {
            OnControlClick(e);
        }
        #endregion
    }
}
