using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Control;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Jabil.Module.Motion.Frm
{
    /// <summary>
    /// 运动状态监控页面
    /// 用于集中展示所有运动轴的实时运行状态和参数
    /// 支持按选定点位绑定的轴优先排序
    /// </summary>
    public partial class Frm_MotionStatus : UIPage
    {
        #region 字段与属性
        // 存储已创建的运动状态控件，便于后续管理和更新
        private readonly Dictionary<string, MotionStatu> _motionStatusControls = new Dictionary<string, MotionStatu>();

        // 内容容器，用于放置所有运动状态控件
        private Panel _contentPanel;

        // 存储选定点位绑定的轴名称列表（用于排序）
        private List<string> _selectedPointBindingAxes = new List<string>();

        // 防抖计时器：避免短时间内频繁切换导致的多次刷新
        private System.Timers.Timer _debounceTimer;

        // 水平滚动条（可选，处理轴过多的情况）
        private HScrollBar _hScrollBar;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化运动状态监控页面
        /// </summary>
        public Frm_MotionStatus()
        {
            InitializeComponent();
            // 启用双缓冲减少绘制闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw, true);
            // 初始化事件绑定
            InitializeEvents();
            // 初始化滚动相关组件
            InitializeScrollComponents();
        }

        /// <summary>
        /// 初始化滚动相关组件
        /// </summary>
        private void InitializeScrollComponents()
        {
            // 创建内容面板用于容纳所有运动状态控件
            _contentPanel = new Panel
            {
                Location = new Point(0, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.None
            };

            // 创建水平滚动条（处理轴过多的情况）
            _hScrollBar = new HScrollBar
            {
                Dock = DockStyle.Bottom,
                Visible = false
            };

            // 创建容器面板组织内容和滚动条
            var containerPanel = new Panel { Dock = DockStyle.Fill };
            containerPanel.Controls.Add(_contentPanel);
            containerPanel.Controls.Add(_hScrollBar);

            // 将容器面板添加到状态面板
            uiPanel_Status.Controls.Clear();
            uiPanel_Status.Controls.Add(containerPanel);
        }

        /// <summary>
        /// 设置选定点位绑定的轴列表（带防抖处理）
        /// </summary>
        /// <param name="bindingAxes">绑定的轴名称列表</param>
        public void SetSelectedPointBindingAxes(List<string> bindingAxes)
        {
            // 防抖处理：300ms内连续调用只执行最后一次
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Timers.Timer(300) { AutoReset = false };
            _debounceTimer.Elapsed += (s, e) =>
            {
                // 切换到UI线程执行
                BeginInvoke(new Action(() =>
                {
                    if (bindingAxes != null)
                    {
                        _selectedPointBindingAxes = bindingAxes.Distinct().ToList();
                    }
                    else
                    {
                        _selectedPointBindingAxes.Clear();
                    }

                    // 优化：只重新排序现有控件，不重建
                    ReorderExistingControls();
                }));
                _debounceTimer.Dispose();
            };
            _debounceTimer.Start();
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
        private static readonly Lazy<Frm_MotionStatus> _lazyInstance = new Lazy<Frm_MotionStatus>(() => new Frm_MotionStatus());
        public static Frm_MotionStatus Instance => _lazyInstance.Value;
        #endregion

        #region 页面加载与数据绑定
        /// <summary>
        /// 页面加载时初始化运动状态控件
        /// </summary>
        private void Frm_MotionStatus_Load(object sender, EventArgs e)
        {
            try
            {
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
        /// 加载运动状态控件（只在首次或轴数量变化时创建）
        /// </summary>
        private void LoadMotionStatusControls()
        {
            try
            {
                Enabled = false;
                // 只有首次加载或轴列表变化时才重建控件
                if (_motionStatusControls.Count == 0 ||
                    _motionStatusControls.Count != MotionSystem.Instance.MotionAxes.Count())
                {
                    _contentPanel.Controls.Clear();
                    _motionStatusControls.Clear();
                    InitializeAllControls(); // 首次初始化所有控件
                }
                else
                {
                    ReorderExistingControls(); // 已存在则只排序
                }
            }
            finally
            {
                Enabled = true;
            }
        }

        /// <summary>
        /// 初始化所有控件（仅在必要时执行）
        /// </summary>
        private void InitializeAllControls()
        {
            int currentX = 0;
            const int spacing = 10;
            var sortedAxes = SortAxesByBindingPriority(MotionSystem.Instance.MotionAxes);

            foreach (var axis in sortedAxes)
            {
                if (axis == null || string.IsNullOrWhiteSpace(axis.轴名称) ||
                    _motionStatusControls.ContainsKey(axis.轴名称))
                    continue;

                var statusControl = new MotionStatu(axis);
                statusControl.Location = new Point(currentX, 0);
                statusControl.MinimumSize = new Size(120, 400);
                statusControl.Margin = new Padding(0, 0, spacing, 0);
                statusControl.Text = axis.轴名称;

                // 设置初始绑定状态
                UpdateControlBindingState(statusControl, axis.轴名称);

                _contentPanel.Controls.Add(statusControl);
                _motionStatusControls.Add(axis.轴名称, statusControl);
                currentX += statusControl.Width + spacing;
            }
        }

        /// <summary>
        /// 重新排序现有控件（核心优化点：不销毁重建，只调整位置）
        /// </summary>
        private void ReorderExistingControls()
        {
            if (_motionStatusControls.Count == 0) return;

            try
            {
                // 暂停控件绘制以减少闪烁
                _contentPanel.SuspendLayout();

                var sortedAxes = SortAxesByBindingPriority(MotionSystem.Instance.MotionAxes);
                int currentX = 0;
                const int spacing = 10;

                // 只调整控件位置，不重新创建
                foreach (var axis in sortedAxes)
                {
                    if (axis == null || !_motionStatusControls.TryGetValue(axis.轴名称, out var control))
                        continue;

                    // 更新位置
                    control.Location = new Point(currentX, 0);
                    // 更新绑定状态视觉标识（不重建控件）
                    UpdateControlBindingState(control, axis.轴名称);

                    currentX += control.Width + spacing;
                }
            }
            finally
            {
                // 恢复控件绘制
                _contentPanel.ResumeLayout(true);
            }
        }

        /// <summary>
        /// 更新控件的绑定状态视觉标识（仅更新必要属性）
        /// </summary>
        private void UpdateControlBindingState(MotionStatu control, string axisName)
        {
            bool isBound = _selectedPointBindingAxes.Contains(axisName);
            // 只在状态变化时更新，减少UI操作
            if (control.BackColor != (isBound ? Color.FromArgb(240, 255, 240) : SystemColors.Control))
            {
                control.BackColor = isBound ? Color.FromArgb(240, 255, 240) : SystemColors.Control;
                // 轻量级刷新，只重绘控件本身
                control.Invalidate();
            }
        }
        #endregion

        #region 排序逻辑
        /// <summary>
        /// 按绑定优先级排序轴：选定点位绑定的轴优先，保持绑定顺序
        /// </summary>
        /// <param name="axes">所有轴列表</param>
        /// <returns>排序后的轴列表</returns>
        private List<MotionAxisDto> SortAxesByBindingPriority(IEnumerable<MotionAxisDto> axes)
        {
            if (axes == null) return new List<MotionAxisDto>();

            var allAxes = axes.ToList();
            var sortedAxes = new List<MotionAxisDto>();

            // 1. 首先添加选定点位绑定的轴（按绑定顺序）
            foreach (var axisName in _selectedPointBindingAxes)
            {
                var axis = allAxes.FirstOrDefault(a => a?.轴名称 == axisName);
                if (axis != null)
                {
                    sortedAxes.Add(axis);
                    allAxes.Remove(axis); // 避免重复添加
                }
            }

            // 2. 然后添加剩余的未绑定轴（按名称排序）
            sortedAxes.AddRange(allAxes
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.轴名称))
                .OrderBy(a => a.轴名称));

            return sortedAxes;
        }
        #endregion

    }
}
