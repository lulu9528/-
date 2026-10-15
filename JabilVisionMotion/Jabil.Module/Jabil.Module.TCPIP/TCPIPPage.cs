using Jabil.Module.Log.Lib;
using Jabil.Module.TCPIP.Frm;
using Sunny.UI;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Jabil.Module.TCPIP
{
    /// <summary>
    /// TCPIP模块主页面，用于切换控制页面和配置页面
    /// </summary>
    public partial class TCPIPPage : UIPage
    {
        #region 枚举定义
        /// <summary>
        /// 页面模式枚举
        /// </summary>
        private enum PageMode
        {
            /// <summary>
            /// 无模式
            /// </summary>
            None,
            /// <summary>
            /// TCPIP控制页面
            /// </summary>
            TcpIpControlPage,
            /// <summary>
            /// TCPIP配置页面
            /// </summary>
            TcpIpConfigPage
        }
        #endregion

        #region 私有字段
        private PageMode _currentPageMode = PageMode.None;
        #endregion

        #region 单例模式
        private static readonly Lazy<TCPIPPage> _instance = new Lazy<TCPIPPage>(
            () => new TCPIPPage(), 
            LazyThreadSafetyMode.ExecutionAndPublication);
        
        /// <summary>
        /// 获取页面唯一实例
        /// </summary>
        public static TCPIPPage Instance => _instance.Value;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化TCPIP主页面
        /// </summary>
        private TCPIPPage()
        {
            InitializeComponent();
            InitializeControlStyles();
            InitializeEvents();
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
        /// 初始化事件绑定
        /// </summary>
        private void InitializeEvents()
        {
            Load += TcpIpPage_Load;
            tsBtnTcpIpControl.Click += TsBtnTcpIpControl_Click;
            tsBtnTcpIpConfig.Click += TsBtnTcpIpConfig_Click;
            // 权限变更事件订阅
            // PermissionSystem.Instance.OnLoginUserChanged += OnLoginUserChanged;
        }
        #endregion

        #region 页面加载与权限
        /// <summary>
        /// 页面加载事件
        /// </summary>
        private void TcpIpPage_Load(object sender, EventArgs e)
        {
            try
            {
                UpdatePermission();
                // 默认加载控制页面
                SwitchToPage(PageMode.TcpIpControlPage);
            }
            catch (Exception ex)
            {
                Instance.Enabled = true;
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 更新权限控制
        /// </summary>
        private void UpdatePermission()
        {
            // 示例权限控制逻辑
            // tsBtnTcpIpControl.Visible = PermissionSystem.CheckPermission("TCPIP/控制", out _);
            // tsBtnTcpIpConfig.Visible = PermissionSystem.CheckPermission("TCPIP/配置", out _);
        }

        /// <summary>
        /// 登录用户变更事件处理
        /// </summary>
        private void OnLoginUserChanged(object sender, EventArgs e)
        {
            UpdatePermission();
        }
        #endregion

        #region 页面切换
        /// <summary>
        /// 切换到指定页面
        /// </summary>
        /// <param name="pageMode">目标页面模式</param>
        private void SwitchToPage(PageMode pageMode)
        {
            if (_currentPageMode == pageMode) return;

            try
            {
                // 清除当前页面
                contentPanel.Visible = false;
                contentPanel.Controls.Clear();

                // 根据目标页面模式加载对应的页面
                var targetPage = GetTargetPage(pageMode);
                if (targetPage != null)
                {
                    // 配置页面属性
                    targetPage.TopLevel = false;
                    targetPage.Dock = DockStyle.Fill;
                    targetPage.AutoScaleMode = AutoScaleMode.None;
                    
                    // 添加到容器并显示
                    contentPanel.Controls.Add(targetPage);
                    targetPage.Show();
                    
                    // 更新当前模式和按钮状态
                    _currentPageMode = pageMode;
                    UpdateButtonStates();
                    
                    contentPanel.Visible = true;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                UIMessageBox.ShowError($"页面切换失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取目标页面实例
        /// </summary>
        private UIPage GetTargetPage(PageMode pageMode)
        {
            return pageMode switch
            {
                PageMode.TcpIpControlPage => Frm_TCPIPControl.Instance,
                PageMode.TcpIpConfigPage => Frm_TCPIPConfig.Instance,
                _ => null
            };
        }

        /// <summary>
        /// 更新按钮状态样式
        /// </summary>
        private void UpdateButtonStates()
        {
            // 重置所有按钮样式
            tsBtnTcpIpControl.ForeColor = Color.Black;
            tsBtnTcpIpConfig.ForeColor = Color.Black;
            tsBtnTcpIpControl.Font = new Font(tsBtnTcpIpControl.Font, FontStyle.Regular);
            tsBtnTcpIpConfig.Font = new Font(tsBtnTcpIpConfig.Font, FontStyle.Regular);

            // 设置当前页面对应的按钮样式
            switch (_currentPageMode)
            {
                case PageMode.TcpIpControlPage:
                    tsBtnTcpIpControl.ForeColor = Color.Blue;
                    tsBtnTcpIpControl.Font = new Font(tsBtnTcpIpControl.Font, FontStyle.Bold);
                    break;
                case PageMode.TcpIpConfigPage:
                    tsBtnTcpIpConfig.ForeColor = Color.Blue;
                    tsBtnTcpIpConfig.Font = new Font(tsBtnTcpIpConfig.Font, FontStyle.Bold);
                    break;
            }
        }
        #endregion

        #region 按钮事件处理
        /// <summary>
        /// TCPIP控制按钮点击事件
        /// </summary>
        private void TsBtnTcpIpControl_Click(object sender, EventArgs e)
        {
            try
            {
                tsBtnTcpIpControl.Enabled = false;
                SwitchToPage(PageMode.TcpIpControlPage);
            }
            finally
            {
                tsBtnTcpIpControl.Enabled = true;
            }
        }

        /// <summary>
        /// TCPIP配置按钮点击事件
        /// </summary>
        private void TsBtnTcpIpConfig_Click(object sender, EventArgs e)
        {
            try
            {
                tsBtnTcpIpConfig.Enabled = false;
                SwitchToPage(PageMode.TcpIpConfigPage);
            }
            finally
            {
                tsBtnTcpIpConfig.Enabled = true;
            }
        }
        #endregion
    }
}
    