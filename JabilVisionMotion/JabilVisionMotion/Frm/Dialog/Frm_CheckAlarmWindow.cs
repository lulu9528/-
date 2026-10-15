using Jabil.Lib;
using Jabil.Module.Alarm.Common.Dtos;
using Jabil.Module.Alarm.Service;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using JabilVisionMotion.Edit;
using JabilVisionMotion.Edit.Process.ModbusControlProcess;
using JabilVisionMotion.Lib;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Frm.Dialog
{
    /// <summary>
    /// 报警窗口，用于展示和处理设备报警信息
    /// </summary>
    public partial class Frm_AlarmWindow : UIForm
    {
        /// <summary>
        /// 私有构造函数防止外部实例化
        /// </summary>
        public Frm_AlarmWindow()
        {
            InitializeComponent();
            InitializeEvents();
        }

        #region 字段与枚举
        /// <summary>
        /// 报警忽略或重新检测的结果状态
        /// </summary>
        private static CheckAlarmWindowResult _alarmHandleResult = CheckAlarmWindowResult.Reset;

        /// <summary>
        /// 用于记录报警处理时间的计时器
        /// </summary>
        private readonly Stopwatch _alarmProcessingStopwatch = new Stopwatch();

        /// <summary>
        /// 报警开始时间
        /// </summary>
        private DateTime _alarmStartTime;

        /// <summary>
        /// 报警结束时间
        /// </summary>
        private DateTime _alarmEndTime;

        /// <summary>
        /// 用于控制报警闪烁效果的定时器
        /// </summary>
        private System.Threading.Timer _flashTimer;
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化事件绑定
        /// </summary>
        private void InitializeEvents()
        {
            this.btn_手动复位.Click += btn_ManualReset_Click;
            this.chk_拍照NG强制跳过.CheckedChanged += chk_SkipCaptureNg_CheckedChanged;
            this.VisibleChanged += Frm_AlarmWindow_VisibleChanged;
            this.FormClosing += Frm_AlarmWindow_FormClosing;
        }
        #endregion

        #region 闪烁效果控制
        /// <summary>
        /// 启动报警闪烁效果
        /// </summary>
        private void StartFlashing()
        {
            try
            {
                if (_flashTimer != null) return;

                // 每1.5秒切换一次颜色
                _flashTimer = new System.Threading.Timer(FlashTimerCallback, null, 0, 1500);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 闪烁定时器回调函数
        /// </summary>
        private void FlashTimerCallback(object state)
        {
            // 确保UI操作在主线程执行
            WFUtil.UpdateMainThreadUIAsync(Frm_Main.Instance, () =>
            {
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming)
                {
                    uiPanel1.FillColor = uiPanel1.FillColor == Color.Red ? Color.White : Color.Red;
                }
                else
                {
                    StopFlashing();
                }
            });
        }

        /// <summary>
        /// 停止报警闪烁效果并重置颜色
        /// </summary>
        private void StopFlashing()
        {
            try
            {
                if (_flashTimer == null) return;

                // 停止并释放定时器
                _flashTimer.Dispose();
                _flashTimer = null;

                // 重置面板颜色
                uiPanel1.FillColor = Color.White;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 窗体加载事件
        /// </summary>
        private void Frm_AlarmWindow_Load(object sender, EventArgs e)
        {
            try
            {
                // 启用双缓冲减少闪烁
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

                // 启动闪烁效果
                StartFlashing();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 窗体可见性变化事件
        /// </summary>
        private void Frm_AlarmWindow_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (MenuProxy.Instance.MachineRunStatus != MachineRunStatus.Alarming)
                    return;

                // 确保UI操作在主线程执行
                if (InvokeRequired)
                {
                    Invoke(new Action(HandleVisibleChanged));
                }
                else
                {
                    HandleVisibleChanged();
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 处理可见性变化的实际逻辑
        /// </summary>
        private void HandleVisibleChanged()
        {
            if (!Visible) return;

            // 启动报警计时
            _alarmProcessingStopwatch.Restart();
            _alarmStartTime = DateTime.Now;

            // 更新机器运行状态视图
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.Alarming);

            // 确保窗口置顶
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            // 显示报警信息
            uiTextBox1.Text = Project.Instance.GlobalVariables.AlarmMessage;

            // 重置强制跳过选项
            chk_拍照NG强制跳过.Checked = false;

            this.Location = new Point(560, 449);
                 
        }

        /// <summary>
        /// 手动复位按钮点击事件
        /// </summary>
        private void btn_ManualReset_Click(object sender, EventArgs e)
        {
            try
            {

                btn_手动复位.Enabled = false;

                // 停止闪烁效果
                StopFlashing();

                // 更新机器状态为暂停
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.Paused);

                // 记录报警处理结果
                _alarmHandleResult = CheckAlarmWindowResult.ManualReset;
                Controller.Instance.StationInfo.先复位再启动 = true;

                // 隐藏窗口
                Hide();

                // 停止计时并记录结束时间
                _alarmProcessingStopwatch.Stop();
                _alarmEndTime = DateTime.Now;

                // 记录异常处理时间
                RecordAlarmProcessingTime();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                btn_手动复位.Enabled = true;
            }
        }

        /// <summary>
        /// 拍照NG强制跳过复选框状态变化事件
        /// </summary>
        private void chk_SkipCaptureNg_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                Controller.Instance.StationInfo._拍照NG强制跳过 = chk_拍照NG强制跳过.Checked;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 窗体关闭事件（阻止真正关闭，改为隐藏）
        /// </summary>
        private void Frm_AlarmWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 记录报警处理时间信息
        /// </summary>
        private void RecordAlarmProcessingTime()
        {
            var processError = new ProcessErrorDto
            {
                报警ID = Project.Instance.GlobalVariables.AlarmId,
                异常开始时间 = _alarmStartTime.ToString("HH:mm:ss"),
                结束时间 = _alarmEndTime.ToString("HH:mm:ss"),
                报警内容 = uiTextBox1.Text,
                耗时 = Math.Round(_alarmProcessingStopwatch.Elapsed.TotalSeconds, 1),
                日期 = DateTime.Now.Date,
                站别 = Controller.Instance.StationInfo.StationId
            };

            ProcessErrorService.Instance.Add(processError);
        }
        #endregion

        #region 窗体移动
        // 引入Windows API
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;


        // 在需要拖拽的控件上（如自定义标题栏面板）添加鼠标按下事件
        private void panelTitle_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 释放鼠标捕获
                ReleaseCapture();
                // 发送拖拽消息
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
        #endregion

    }

    /// <summary>
    /// 报警窗口管理类，负责安全地创建和显示报警窗口
    /// 处理跨线程访问和单例实例管理
    /// </summary>
    public static class AlarmWindowManager
    {
        // 线程同步锁，确保单例创建的线程安全
        private static readonly object _syncLock = new object();

        /// <summary>
        /// 安全显示报警窗口
        /// 自动处理跨线程访问和窗口状态管理
        /// </summary>
        public static void ShowAlarmWindow()
        {
            if (Frm_Main.Instance.InvokeRequired)
            {
                // 跨线程调用，使用BeginInvoke避免阻塞
                Frm_Main.Instance.BeginInvoke(new Action(ShowAlarmWindow));
                return;
            }
            // 确保单例实例被正确初始化
            EnsureInstanceCreated();
            // 获取报警窗口实例
            var alarmWindow = Frm_Main.Instance.frm_AlarmWindow;

            // 检查是否需要跨线程调用
            if (alarmWindow.InvokeRequired)
            {
                // 如果当前线程不是创建窗口的线程，通过Invoke切换到UI线程执行
                alarmWindow.Invoke(new Action(() =>
                {
                    alarmWindow.TopLevel = true;
                    alarmWindow.Show();
                    alarmWindow.BringToFront(); // 确保窗口置顶
                }));
            }
            else
            {
                // 如果在UI线程，直接执行
                alarmWindow.TopLevel = true;
                alarmWindow.Show();
                alarmWindow.BringToFront();
            }
        }

        /// <summary>
        /// 确保报警窗口实例被创建（线程安全）
        /// </summary>
        private static void EnsureInstanceCreated()
        {
            // 双重检查锁定模式，确保高效且线程安全的单例创建
            if (Frm_Main.Instance.frm_AlarmWindow == null)
            {
                lock (_syncLock)
                {
                    if (Frm_Main.Instance.frm_AlarmWindow == null)
                    {
                        // 触发实例创建（假设Frm_AlarmWindow的Instance属性会初始化实例）
                        var temp = new Frm_AlarmWindow();
                    }
                }
            }
        }

        /// <summary>
        /// 安全关闭报警窗口
        /// </summary>
        public static void CloseAlarmWindow()
        {
            if (Frm_Main.Instance.frm_AlarmWindow == null) return;

            if (Frm_Main.Instance.frm_AlarmWindow.InvokeRequired)
            {
                Frm_Main.Instance.frm_AlarmWindow.BeginInvoke(new Action(CloseAlarmWindow));
            }
            else
            {
                if (Frm_Main.Instance.frm_AlarmWindow.Visible)
                {
                    Frm_Main.Instance.frm_AlarmWindow.Hide();
                }
            }
        }
    }

    /// <summary>
    /// 报警窗口处理结果枚举
    /// </summary>
    internal enum CheckAlarmWindowResult
    {
        /// <summary>
        /// 重置状态
        /// </summary>
        Reset,

        /// <summary>
        /// 手动复位
        /// </summary>
        ManualReset
    }



}
