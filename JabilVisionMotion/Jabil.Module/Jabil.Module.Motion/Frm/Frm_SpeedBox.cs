using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Sunny.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Jabil.Module.Motion.Frm
{
    /// <summary>
    /// 速度调节窗体
    /// 用于设置和显示运动系统的速度比例，并同步到主控制窗体
    /// </summary>
    public partial class Frm_SpeedBox : UIForm
    {
        #region 常量与字段
        // 速度比例显示格式常量，便于统一维护
        private const string SpeedDisplayFormat = "{0}%";
        private const string MainFormSpeedFormat = "速度比例：+{0}%";

        /// <summary>
        /// 选择结果
        /// </summary>
        public ConfirmBoxResult Result { get; private set; } = ConfirmBoxResult.No;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化速度调节窗体
        /// </summary>
        public Frm_SpeedBox()
        {
            InitializeComponent();
            // 启用双缓冲减少绘制闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            // 初始化UI事件绑定
            InitializeEvents();
        }

        /// <summary>
        /// 初始化事件绑定
        /// 集中管理事件订阅，便于维护
        /// </summary>
        private void InitializeEvents()
        {
            Load += Frm_SpeedBox_Load;
            btn_cancel.Click += btn_cancel_Click;
            uiTrackBar_速度比例.ValueChanged += uiTrackBar_速度比例_ValueChanged;
            
            // 添加鼠标悬停效果
            btn_cancel.MouseEnter += BtnCancel_MouseEnter;
            btn_cancel.MouseLeave += BtnCancel_MouseLeave;
        }
        #endregion

        #region 窗体加载
        /// <summary>
        /// 窗体加载时初始化速度比例
        /// </summary>
        private void Frm_SpeedBox_Load(object sender, EventArgs e)
        {
            try
            {

                // 更新当前窗体UI
                UpdateLocalSpeedDisplay(Project.Instance.StationInfo.RunningSpeedRatio);

                // 同步更新主控制窗体
                UpdateMotionControlSpeed(Project.Instance.StationInfo.RunningSpeedRatio);

                // 窗体属性设置
                TopMost = true;
                Result = ConfirmBoxResult.No;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void btn_cancel_Click(object sender, EventArgs e)
        {
            try
            {
                Result = ConfirmBoxResult.Yes;
                Hide();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 速度比例滑块值变化事件
        /// </summary>
        private void uiTrackBar_速度比例_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                int newSpeed = uiTrackBar_速度比例.Value;
                // 更新全局速度比例
                Project.Instance.StationInfo.RunningSpeedRatio = newSpeed;

                // 更新当前窗体显示
                UpdateLocalSpeedDisplay(newSpeed);

                // 同步到主控制窗体
                UpdateMotionControlSpeed(newSpeed);
                
                // 根据速度值改变显示颜色
                UpdateSpeedDisplayColor(newSpeed);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        
        /// <summary>
        /// 按钮鼠标进入事件
        /// </summary>
        private void BtnCancel_MouseEnter(object sender, EventArgs e)
        {
            btn_cancel.FillColor = Color.FromArgb(0, 115, 170);
            btn_cancel.FillColor2 = Color.FromArgb(0, 95, 150);
        }
        
        /// <summary>
        /// 按钮鼠标离开事件
        /// </summary>
        private void BtnCancel_MouseLeave(object sender, EventArgs e)
        {
            btn_cancel.FillColor = Color.FromArgb(0, 95, 150);
            btn_cancel.FillColor2 = Color.FromArgb(0, 75, 130);
        }
        #endregion

        #region UI更新方法
        /// <summary>
        /// 更新当前窗体的速度显示
        /// </summary>
        /// <param name="speed">速度比例值</param>
        private void UpdateLocalSpeedDisplay(int speed)
        {
            lab_速度比例显示.Text = string.Format(SpeedDisplayFormat, speed);
        }
        
        /// <summary>
        /// 根据速度值更新显示颜色
        /// </summary>
        /// <param name="speed">速度比例值</param>
        private void UpdateSpeedDisplayColor(int speed)
        {
            if (speed < 50)
            {
                lab_速度比例显示.ForeColor = Color.LightBlue;
            }
            else if (speed < 100)
            {
                lab_速度比例显示.ForeColor = Color.Gold;
            }
            else
            {
                lab_速度比例显示.ForeColor = Color.OrangeRed;
            }
        }

        /// <summary>
        /// 同步更新运动控制主窗体的速度显示
        /// 包含线程安全检查和空引用保护
        /// </summary>
        /// <param name="speed">速度比例值</param>
        private void UpdateMotionControlSpeed(int speed)
        {
            // 空引用检查：确保主窗体实例存在
            if (Frm_MotionControl.Instance == null || Frm_MotionControl.Instance.IsDisposed)
                return;

            // 线程安全更新：如果需要则切换到UI线程
            if (Frm_MotionControl.Instance.InvokeRequired)
            {
                Frm_MotionControl.Instance.Invoke(new Action<int>(UpdateMotionControlSpeed), speed);
                return;
            }

            // 控件有效性检查：确保控件已创建
            if (Frm_MotionControl.Instance.uiTrackBar_速度比例.IsHandleCreated &&
                !Frm_MotionControl.Instance.uiTrackBar_速度比例.IsDisposed)
            {
                Frm_MotionControl.Instance.uiTrackBar_速度比例.Value = speed;
                Frm_MotionControl.Instance.uiLabel_速度比例显示.Text = string.Format(MainFormSpeedFormat, speed);
            }
        }
        #endregion
    }
}