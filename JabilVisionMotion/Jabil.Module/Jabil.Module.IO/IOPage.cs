﻿using Jabil.Lib;
using Jabil.Module.IO.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using Sunny.UI;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.IO
{
    /// <summary>
    /// IO模块主页面（容器页）
    /// 功能：作为IO控制页和配置页的容器，提供页面切换、权限控制和异步加载能力
    /// 设计模式：单例模式（确保全局唯一实例）、异步编程模式（提升UI响应性）
    /// </summary>
    public partial class IOPage : UIPage
    {
        #region 单例模式实现（线程安全）
        /// <summary>
        /// 懒加载单例实例
        /// 特性：
        /// - 线程安全：Lazy<T>默认线程安全模式，多线程环境下仅创建一个实例
        /// - 延迟初始化：首次访问时才实例化，减少应用启动时的资源占用
        /// </summary>
        private static readonly Lazy<IOPage> _lazyInstance = new Lazy<IOPage>(() => new IOPage());

        /// <summary>
        /// 全局唯一访问点
        /// 外部通过IOPage.Instance获取页面实例，保证整个应用中只有一个IOPage实例
        /// </summary>
        public static IOPage Instance => _lazyInstance.Value;

        /// <summary>
        /// 私有构造函数
        /// 作用：防止外部通过new关键字创建实例，确保单例模式的唯一性
        /// </summary>
        private IOPage()
        {
            InitializeComponent();
            // 启用双缓冲和禁止擦除背景
            // 解决UI频繁刷新时的闪烁问题，提升视觉体验
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
        #endregion

        #region 字段与枚举定义
        /// <summary>
        /// 页面模式枚举
        /// 定义系统支持的子页面类型，用于标准化页面切换逻辑
        /// 避免使用字符串或数字标识页面导致的歧义及维护成本
        /// </summary>
        private enum PageMode
        {
            /// <summary>初始状态（无页面加载）</summary>
            None,
            /// <summary>IO控制页面（实时监控IO状态）</summary>
            IOControl,
            /// <summary>IO配置页面（设置IO卡参数）</summary>
            IOConfig
        }

        /// <summary>
        /// 当前激活的页面模式
        /// 作用：记录当前显示的页面，避免重复加载相同页面，提升性能
        /// </summary>
        private PageMode _currentPageMode = PageMode.None;

        /// <summary>
        /// 取消令牌源
        /// 作用：管理异步操作的生命周期，支持在页面切换或关闭时优雅取消未完成的任务
        /// 避免异步操作已无用但仍在执行导致的资源浪费或异常
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// 加载状态标记
        /// 作用：防止并发加载（如用户快速点击多个页面按钮），避免控件操作冲突
        /// </summary>
        private bool _isLoading = false;
        #endregion

        #region 页面切换核心逻辑（异步实现）
        /// <summary>
        /// 异步切换到指定的子页面模式
        /// 核心流程：状态检查→显示加载→清除当前页面→加载目标页面→更新状态
        /// </summary>
        /// <param name="targetMode">目标页面模式（IOControl/IOConfig）</param>
        /// <param name="cancellationToken">取消令牌，用于终止未完成的切换操作</param>
        private async Task SwitchToPageAsync(PageMode targetMode, CancellationToken cancellationToken)
        {
            // 防止并发加载和无效切换
            // 若目标页面与当前页面相同，或正在加载其他页面，则直接返回
            if (_currentPageMode == targetMode || _isLoading)
                return;

            try
            {
                _isLoading = true; // 标记为加载中，阻止并发操作
                ShowLoadingIndicator(); // 显示加载提示，提升用户体验

                // 异步清除当前页面（等待清除完成后再加载新页面）
                // ConfigureAwait(true)：确保后续代码在UI线程执行（因涉及UI操作）
                await ClearCurrentPageAsync(cancellationToken).ConfigureAwait(true);

                // 根据目标模式加载对应的子页面
                switch (targetMode)
                {
                    case PageMode.IOControl:
                        // 加载IO控制页面（单例实例，避免重复创建）
                        await LoadChildFormAsync(Frm_IOControl.Instance, cancellationToken).ConfigureAwait(true);
                        // 更新工具栏按钮样式，视觉上区分当前激活的页面
                        UpdateButtonStyle(toolStripButton_IOControl, toolStripButton_IOConfig);
                        break;
                    case PageMode.IOConfig:
                        // 加载IO配置页面（单例实例）
                        await LoadChildFormAsync(Frm_IOConfig.Instance, cancellationToken).ConfigureAwait(true);
                        UpdateButtonStyle(toolStripButton_IOConfig, toolStripButton_IOControl);
                        break;
                }

                _currentPageMode = targetMode; // 更新当前页面状态
            }
            catch (OperationCanceledException)
            {
                // 操作被正常取消（如用户快速切换页面），仅记录日志不报错
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue, "页面切换操作已取消");
            }
            catch (Exception ex)
            {
                // 记录异常详情（包含堆栈信息），便于问题排查
                RLog.SaveError(ex);
                // 显示错误信息，提示用户加载失败
                ShowErrorPanel($"页面加载失败: {ex.Message}");
            }
            finally
            {
                HideLoadingIndicator(); // 隐藏加载提示
                _isLoading = false; // 重置加载状态，允许后续操作
            }
        }

        /// <summary>
        /// 异步清除当前页面
        /// 功能：移除容器中现有控件，为加载新页面做准备
        /// 线程安全：确保在UI线程执行控件操作（避免跨线程操作异常）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task ClearCurrentPageAsync(CancellationToken cancellationToken)
        {
            // 检查是否已收到取消请求，若已取消则抛出异常终止操作
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            // 判断当前线程是否为UI线程
            if (uiPanel_window.InvokeRequired)
            {
                // 非UI线程：通过BeginInvoke将操作委托到UI线程执行
                // 1. 定义要在UI线程执行的操作（清除页面）
                Action action = () => ClearCurrentPageImpl(cancellationToken);
                // 2. 使用Task.Factory.FromAsync包装BeginInvoke/EndInvoke，将异步操作转换为Task
                // 便于外部使用await等待操作完成
                await Task.Factory.FromAsync(
                    uiPanel_window.BeginInvoke(action), // 开始异步调用
                    uiPanel_window.EndInvoke           // 结束异步调用（获取结果）
                ).ConfigureAwait(true); // 继续在UI线程执行后续代码
            }
            else
            {
                // UI线程：直接执行清除操作
                ClearCurrentPageImpl(cancellationToken);
            }
        }

        /// <summary>
        /// 清除页面的实际实现（核心逻辑）
        /// 被ClearCurrentPageAsync调用，仅在UI线程执行
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private void ClearCurrentPageImpl(CancellationToken cancellationToken)
        {
            // 隐藏容器面板，避免清除过程中显示空白或混乱的UI
            uiPanel_window.Visible = false;

            // 检查是否已取消，若未取消则继续执行
            if (!cancellationToken.IsCancellationRequested)
            {
                // 延迟50ms：给UI线程留出时间处理渲染，减少闪烁
                // Wait(cancellationToken)：允许在延迟期间响应取消请求
                Task.Delay(50, cancellationToken).Wait(cancellationToken);
                // 清除容器中所有控件（释放资源）
                uiPanel_window.Controls.Clear();
            }
        }

        /// <summary>
        /// 异步加载子窗体到容器面板中
        /// 功能：将子页面（IO控制/配置页）嵌入当前容器，并初始化显示
        /// </summary>
        /// <param name="childForm">要加载的子窗体实例（单例）</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task LoadChildFormAsync(Form childForm, CancellationToken cancellationToken)
        {
            // 校验子窗体实例有效性，避免空引用异常
            if (childForm == null) return;

            // 若子窗体实现了IAsyncInitializable接口，调用其异步初始化方法
            // 适用场景：子窗体有耗时的初始化操作（如加载配置、连接设备）
            if (childForm is IAsyncInitializable asyncForm)
            {
                await asyncForm.InitializeAsync(cancellationToken).ConfigureAwait(true);
            }

            // 确保在UI线程添加子窗体（控件操作必须在UI线程执行）
            if (uiPanel_window.InvokeRequired)
            {
                // 非UI线程：委托到UI线程执行
                Action action = () => AddChildFormToPanel(childForm);
                await Task.Factory.FromAsync(
                    uiPanel_window.BeginInvoke(action),
                    uiPanel_window.EndInvoke
                ).ConfigureAwait(true);
            }
            else
            {
                // UI线程：直接添加子窗体
                AddChildFormToPanel(childForm);
            }

            // 延迟50ms：等待子窗体渲染完成后再显示容器，避免UI闪烁
            await Task.Delay(50, cancellationToken).ConfigureAwait(true);
            uiPanel_window.Visible = true; // 显示容器，展示子窗体
        }

        /// <summary>
        /// 将子窗体添加到面板的实际实现
        /// 负责子窗体的属性配置和显示
        /// </summary>
        /// <param name="childForm">子窗体实例</param>
        private void AddChildFormToPanel(Form childForm)
        {
            // 配置子窗体属性，使其适应容器布局
            ConfigureChildForm(childForm);
            // 将子窗体添加到容器控件集合
            uiPanel_window.Controls.Add(childForm);
            // 显示子窗体
            childForm.Show();
        }

        /// <summary>
        /// 配置子窗体属性，确保与父容器兼容
        /// </summary>
        /// <param name="childForm">子窗体实例</param>
        private void ConfigureChildForm(Form childForm)
        {
            childForm.TopLevel = false; // 设为非顶级窗体（允许嵌入其他容器）
            childForm.Dock = DockStyle.Fill; // 填充整个容器，适应容器大小变化
            childForm.AutoScaleMode = AutoScaleMode.None; // 禁用自动缩放，避免尺寸错乱
            childForm.FormBorderStyle = FormBorderStyle.None; // 移除边框，与容器融为一体
        }

        /// <summary>
        /// 更新工具栏按钮样式，区分激活与非激活状态
        /// 通过视觉差异提示用户当前所在页面
        /// </summary>
        /// <param name="activeBtn">当前激活的按钮（对应当前页面）</param>
        /// <param name="inactiveBtn">非激活的按钮（对应其他页面）</param>
        private void UpdateButtonStyle(ToolStripButton activeBtn, ToolStripButton inactiveBtn)
        {
            // 激活按钮样式：深灰色+粗体，突出显示
            activeBtn.ForeColor = Color.DimGray;
            activeBtn.Font = new Font(activeBtn.Font, FontStyle.Bold);

            // 非激活按钮样式：浅灰色+常规字体，弱化显示
            inactiveBtn.ForeColor = Color.Gray;
            inactiveBtn.Font = new Font(inactiveBtn.Font, FontStyle.Regular);
        }
        #endregion

        #region 加载状态管理
        /// <summary>
        /// 显示加载指示器
        /// 功能：在页面切换过程中显示提示信息，告知用户操作正在进行
        /// </summary>
        private void ShowLoadingIndicator()
        {
            // 确保在UI线程执行
            if (InvokeRequired)
            {
                Invoke(new Action(ShowLoadingIndicator));
                return;
            }

            // 移除可能存在的错误面板（避免叠加显示）
            var errorPanel = uiPanel_window.Controls.OfType<UIPanel>().FirstOrDefault(p => p.Name == "errorPanel");
            if (errorPanel != null)
            {
                uiPanel_window.Controls.Remove(errorPanel);
            }

            // 创建加载面板（容器）
            var loadingPanel = new UIPanel
            {
                Name = "loadingPanel", // 唯一标识，便于后续移除
                Dock = DockStyle.Fill, // 填充容器
                BackColor = Color.White // 背景色与整体UI协调
            };

            // 创建加载提示标签
            var loadingLabel = new UILabel
            {
                Text = "正在加载页面，请稍候...", // 提示文本
                Dock = DockStyle.Fill, // 填充面板
                TextAlign = ContentAlignment.MiddleCenter, // 文字居中
                Font = new Font("微软雅黑", 12F, FontStyle.Bold) // 突出显示的字体
            };

            // 组装并显示加载指示器
            loadingPanel.Controls.Add(loadingLabel);
            uiPanel_window.Controls.Add(loadingPanel);
            uiPanel_window.Visible = true;
        }

        /// <summary>
        /// 隐藏加载指示器
        /// 功能：页面加载完成后移除加载提示，显示实际内容
        /// </summary>
        private void HideLoadingIndicator()
        {
            // 确保在UI线程执行
            if (InvokeRequired)
            {
                Invoke(new Action(HideLoadingIndicator));
                return;
            }

            // 查找并移除加载面板
            var loadingPanel = uiPanel_window.Controls.OfType<UIPanel>().FirstOrDefault(p => p.Name == "loadingPanel");
            if (loadingPanel != null)
            {
                uiPanel_window.Controls.Remove(loadingPanel);
            }
        }

        /// <summary>
        /// 显示错误信息面板
        /// 功能：当页面加载失败时，向用户展示错误详情
        /// </summary>
        /// <param name="message">错误信息</param>
        private void ShowErrorPanel(string message)
        {
            // 确保在UI线程执行
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ShowErrorPanel), message);
                return;
            }

            // 创建错误面板（容器）
            var errorPanel = new UIPanel
            {
                Name = "errorPanel", // 唯一标识
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // 创建错误提示标签
            var errorLabel = new UILabel
            {
                Text = message, // 错误详情
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.Red // 红色字体突出错误
            };

            // 组装并显示错误面板
            errorPanel.Controls.Add(errorLabel);
            uiPanel_window.Controls.Add(errorPanel);
            uiPanel_window.Visible = true;
        }
        #endregion

        #region 事件处理方法（异步）
        /// <summary>
        /// 页面加载事件（初始化入口）
        /// 执行流程：权限更新→注册事件→加载默认页面
        /// </summary>
        private async void IOPage_Load(object sender, EventArgs e)
        {
            try
            {
                // 异步更新权限（耗时操作在后台执行，不阻塞UI）
                await UpdatePermissionAsync().ConfigureAwait(true);

                // 注册权限变更事件（用户切换后动态更新权限）
                PermissionSystem.Instance.OnLoginUserChanged += OnLoginUserChanged;

                // 若未加载任何页面，则默认加载IO控制页
                if (_currentPageMode == PageMode.None)
                {
                    _cts = new CancellationTokenSource(); // 初始化取消令牌源
                    // 加载默认页面（IO控制页）
                    await SwitchToPageAsync(PageMode.IOControl, _cts.Token).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                // 记录初始化异常
                RLog.SaveError(ex);
                // 显示初始化失败信息
                ShowErrorPanel($"初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// "IO控制"按钮点击事件（异步处理）
        /// 功能：响应用户点击，切换到IO控制页面
        /// </summary>
        private async void toolStripButton_IOControl_Click(object sender, EventArgs e)
        {
            // 防止重复点击（按钮禁用时直接返回）
            if (!toolStripButton_IOControl.Enabled) return;

            toolStripButton_IOControl.Enabled = false; // 禁用按钮，防止并发操作
            try
            {
                // 取消可能正在进行的页面切换操作（如用户快速点击多个按钮）
                _cts?.Cancel();
                _cts = new CancellationTokenSource(); // 创建新的取消令牌源

                // 切换到IO控制页面
                await SwitchToPageAsync(PageMode.IOControl, _cts.Token).ConfigureAwait(true);
            }
            finally
            {
                toolStripButton_IOControl.Enabled = true; // 恢复按钮状态
            }
        }

        /// <summary>
        /// "IO配置"按钮点击事件（异步处理）
        /// 功能：响应用户点击，切换到IO配置页面
        /// </summary>
        private async void toolStripButton_IOConfig_Click(object sender, EventArgs e)
        {
            // 防止重复点击
            if (!toolStripButton_IOConfig.Enabled) return;

            toolStripButton_IOConfig.Enabled = false; // 禁用按钮
            try
            {
                // 取消当前操作，创建新令牌
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                // 切换到IO配置页面
                await SwitchToPageAsync(PageMode.IOConfig, _cts.Token).ConfigureAwait(true);
            }
            finally
            {
                toolStripButton_IOConfig.Enabled = true; // 恢复按钮状态
            }
        }

        /// <summary>
        /// 登录用户变更事件处理方法
        /// 功能：当登录用户切换时，重新更新页面权限（如不同用户有不同操作权限）
        /// </summary>
        private async void OnLoginUserChanged(object sender, EventArgs e)
        {
            await UpdatePermissionAsync().ConfigureAwait(true);
        }
        #endregion

        #region 权限控制逻辑（异步）
        /// <summary>
        /// 异步更新页面权限
        /// 功能：根据当前登录用户的权限，控制按钮和页面的可见性
        /// 确保用户只能访问其有权限的功能
        /// </summary>
        private async Task UpdatePermissionAsync()
        {
            try
            {
                // 在后台线程检查权限（权限检查可能涉及数据库/配置文件读取，避免阻塞UI）
                bool hasConfigPerm = await Task.Run(() =>
                {
                    // 检查"手动/IO/配置"权限项（实际项目中权限项应从权限系统获取）
                    return PermissionSystem.CheckPermission("手动/IO/配置", out _);
                }).ConfigureAwait(true); // 回到UI线程执行后续操作

                // 控制"IO配置"按钮的可见性（无权限则隐藏）
                toolStripButton_IOConfig.Visible = hasConfigPerm;

                // 若子窗体实现了异步权限接口，调用其异步更新方法
                if (Frm_IOConfig.Instance is IAsyncInitializable asyncConfigForm)
                {
                    await asyncConfigForm.UpdatePermissionAsync().ConfigureAwait(true);
                }
                else
                {
                    // 否则直接控制子窗体可见性
                    Frm_IOConfig.Instance.Visible = hasConfigPerm;
                }

                // 权限变更时的页面自动调整：
                // 若当前显示的是无权限的页面，自动切换到有权限的页面
                if (_currentPageMode == PageMode.IOConfig && !hasConfigPerm)
                {
                    await SwitchToPageAsync(PageMode.IOControl, CancellationToken.None).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                // 记录权限更新异常
                RLog.SaveError(ex);
            }
        }
        #endregion

    }

    #region 异步初始化接口
    /// <summary>
    /// 异步初始化接口
    /// 设计目的：为子窗体提供统一的异步初始化和权限更新规范
    /// 实现类：需要异步加载数据或更新权限的子窗体（如FrmIOControl、FrmIOConfig）
    /// </summary>
    public interface IAsyncInitializable
    {
        /// <summary>
        /// 异步初始化方法
        /// 用于子窗体加载耗时数据（如从数据库/设备读取配置）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步更新权限方法
        /// 用于子窗体根据用户权限动态调整UI元素（如隐藏无权限的按钮）
        /// </summary>
        Task UpdatePermissionAsync();
    }
    #endregion
}