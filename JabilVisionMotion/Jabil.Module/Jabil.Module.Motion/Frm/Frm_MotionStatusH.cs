using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Control;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Jabil.Module.Motion.Frm
{
    /// <summary>
    /// 运动状态监控页面
    /// 用于集中展示所有运动轴的实时运行状态和参数
    /// </summary>
    public partial class Frm_MotionStatusH : UIPage
    {
        #region 字段与属性
        // 存储已创建的运动状态控件，便于后续管理和更新
        private readonly Dictionary<string, MotionStatuH> _motionStatusControls = new Dictionary<string, MotionStatuH>();
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化运动状态监控页面
        /// </summary>
        public Frm_MotionStatusH()
        {
            InitializeComponent();
            // 启用双缓冲减少绘制闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            // 初始化事件绑定
            InitializeEvents();
        }

        /// <summary>
        /// 初始化事件绑定
        /// </summary>
        private void InitializeEvents()
        {
            Load += Frm_MotionStatus_Load;
        }
        #endregion

        #region 单例模式改进
        /// <summary>
        /// 页面实例对象
        /// 使用Lazy初始化确保线程安全
        /// </summary>
        private static readonly Lazy<Frm_MotionStatusH> _lazyInstance = new Lazy<Frm_MotionStatusH>(() => new Frm_MotionStatusH());
        public static Frm_MotionStatusH Instance => _lazyInstance.Value;
        #endregion

        #region 页面加载与数据绑定
        /// <summary>
        /// 页面加载时初始化运动状态控件
        /// </summary>
        private void Frm_MotionStatus_Load(object sender, EventArgs e)
        {
            try
            {
                // 确保在UI线程执行控件操作
                if (InvokeRequired)
                {
                    Invoke(new Action(LoadMotionStatusControls));
                }
                else
                {
                    LoadMotionStatusControls();
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 加载运动状态控件
        /// 根据运动轴列表创建并添加对应的状态显示控件
        /// </summary>
        private void LoadMotionStatusControls()
        {
            try
            {
                Enabled = false;
                uiPanel_Status.Controls.Clear();
                _motionStatusControls.Clear();

                // 验证运动系统实例和轴列表有效性
                if (MotionSystem.Instance?.MotionAxes == null || !MotionSystem.Instance.MotionAxes.Any())
                {
                    return;
                }

                // 为每个运动轴创建状态控件
                foreach (var axis in MotionSystem.Instance.MotionAxes)
                {
                    // 跳过无效的运动轴配置
                    if (axis == null || string.IsNullOrWhiteSpace(axis.轴名称))
                    {
                        continue;
                    }

                    // 避免重复创建相同轴的控件
                    if (!_motionStatusControls.ContainsKey(axis.轴名称))
                    {
                        var statusControl = new MotionStatuH(axis);
                        statusControl.Dock = DockStyle.Top;
                        statusControl.Margin = new Padding(5, 0, 5, 0); // 添加间距提升美观度

                        uiPanel_Status.Controls.Add(statusControl);
                        _motionStatusControls.Add(axis.轴名称, statusControl);
                    }
                }
            }
            finally
            {
                Enabled = true;
            }
        }
        #endregion
    }
}
