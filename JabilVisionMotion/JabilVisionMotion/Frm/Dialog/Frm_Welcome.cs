using Cognex.VisionPro.FGGigE;
using Cognex.VisionPro.FGGigE.Implementation.Internal;
using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus;
using Jabil.Module.TCPIP;
using JabilVisionMotion.Lib;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Frm.Dialog
{
    /// <summary>
    /// 欢迎窗体，用于展示程序启动进度
    /// </summary>
    public partial class Frm_Welcome : UIForm
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 窗体对象实例（线程安全的延迟初始化）
        /// </summary>
        private static readonly Lazy<Frm_Welcome> _instance = new Lazy<Frm_Welcome>(
            () => new Frm_Welcome(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取窗体唯一实例
        /// </summary>
        public static Frm_Welcome Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数防止外部实例化
        /// </summary>
        private Frm_Welcome()
        {
            InitializeComponent();
            // 设置双缓冲以减少闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 窗体加载事件
        /// </summary>
        private void Frm_Welcome_Load(object sender, EventArgs e)
        {
            try
            {
                // 获取并显示程序版本号
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                lbl_version.Text = version != null ? $"V {version}" : "V 未知版本";

                // 初始化进度条
                bar_step.Maximum = 100;
                bar_step.Value = 0;
                lbl_step.Text = string.Empty;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 退出按钮点击事件
        /// </summary>
        private void Btn_Exit_Click(object sender, EventArgs e)
        {
            try
            {
                btn_exit.Enabled = false;
                // 优雅退出程序
                Application.Exit();
                // 强制终止以防有未关闭的线程
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                btn_exit.Enabled = true;
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 更新进度和消息（线程安全）
        /// </summary>
        /// <param name="percentValue">进度百分比（0-100）</param>
        /// <param name="stepMsg">当前步骤消息</param>
        public void UpdateMsg(int percentValue, string stepMsg)
        {
            // 确保进度值在有效范围内
            percentValue = Math.Max(0, Math.Min(percentValue, 100));
            // 跨线程安全调用
            if (InvokeRequired)
            {
                // 使用BeginInvoke避免阻塞调用线程
                BeginInvoke(new Action<int, string>(UpdateMsg), percentValue, stepMsg);
                return;
            }

            // 更新UI
            bar_step.Value = percentValue;
            lbl_step.Text = string.IsNullOrEmpty(stepMsg)
                ? string.Empty
                : $"{stepMsg}......";
        }
        #endregion
    }
}
