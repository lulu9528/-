using Jabil.Lib;
using Jabil.Module.IO.Frm;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Vision.Frm;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Frm.UserPage
{
    /// <summary>
    /// 视觉页面，用于切换显示各类视觉相关功能界面
    /// 支持VPP程序编辑、视觉手动操作控制和VPP配置管理等核心功能
    /// 采用单例模式确保全局唯一实例，通过工具条按钮实现不同功能页面的切换
    /// 继承自Sunny.UI的UIPage控件，保持与系统UI风格的一致性
    /// </summary>
    public partial class VisionPage : UIPage
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 页面唯一实例（线程安全的延迟初始化）
        /// 使用Lazy<T>实现延迟加载，确保在首次使用时才初始化实例
        /// LazyThreadSafetyMode.ExecutionAndPublication确保多线程环境下的线程安全
        /// 避免多线程同时创建实例导致的资源竞争问题
        /// </summary>
        private static readonly Lazy<VisionPage> _instance = new Lazy<VisionPage>(
            () => new VisionPage(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取页面唯一实例的公开属性
        /// 外部通过VisionPage.Instance访问，保证全局只有一个实例
        /// 提供统一的访问入口，避免重复创建页面消耗资源
        /// </summary>
        public static VisionPage Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数，防止外部直接实例化
        /// 确保页面只能通过单例模式获取，保证实例唯一性
        /// 符合单例模式的设计原则，禁止外部随意创建实例
        /// </summary>
        private VisionPage()
        {
            InitializeComponent();
            InitializeSettings();
        }
        #endregion

        #region 枚举定义
        /// <summary>
        /// 页面显示模式枚举，定义了视觉页面支持的功能类型
        /// 用于统一管理页面切换状态，避免使用魔术数字，提高代码可读性
        /// 枚举值与实际功能页面一一对应，便于维护和扩展
        /// </summary>
        enum FormPage
        {
            /// <summary>
            /// 无模式（初始状态）
            /// 表示尚未加载任何功能页面
            /// 作为页面初始化时的默认状态
            /// </summary>
            None,
            /// <summary>
            /// VPP编辑页面
            /// 用于编辑和管理视觉处理程序（Vision Processing Program）
            /// 对应Frm_VPTool子窗体
            /// </summary>
            VPPPage,
            /// <summary>
            /// 手动操作页面
            /// 提供视觉系统的手动控制界面，支持实时操作和监控
            /// 包含Frm_VisionControl和Frm_OperateView两个子窗体
            /// </summary>
            ManualPage,
            /// <summary>
            /// VPP配置页面
            /// 用于配置VPP程序的运行参数和环境设置
            /// 对应VPP配置相关的子窗体
            /// </summary>
            VPPSettingPage
        }
        #endregion

        #region 字段
        /// <summary>
        /// 当前显示的模式
        /// 用于记录当前激活的功能页面状态，避免重复加载相同内容
        /// 通过状态判断实现页面切换的优化，提升性能
        /// </summary>
        private FormPage _currentFormMode = FormPage.None;

        /// <summary>
        /// 模式与按钮的映射关系
        /// 将每个功能模式与对应的工具条按钮关联，便于统一管理按钮状态和样式
        /// 使用字典结构实现一对多映射，简化按钮状态更新逻辑
        /// </summary>
        private readonly Dictionary<FormPage, ToolStripButton> _modeButtonMap = new Dictionary<FormPage, ToolStripButton>();

        /// <summary>
        /// 模式与操作日志的映射关系
        /// 存储每种功能模式对应的操作日志文本，便于统一记录操作行为
        /// 集中管理日志文本，避免在代码中散落大量字符串常量
        /// </summary>
        private readonly Dictionary<FormPage, string> _modeLogMap = new Dictionary<FormPage, string>();
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化页面设置和事件绑定
        /// 集中进行UI样式优化、映射关系初始化和事件绑定
        /// 作为页面初始化的入口方法，确保初始化逻辑的完整性
        /// </summary>
        private void InitializeSettings()
        {
            // 启用双缓冲和异步绘制，减少界面闪烁
            // OptimizedDoubleBuffer：启用双缓冲以提高绘制性能，特别适用于视觉界面的实时图像显示
            // AllPaintingInWmPaint：减少绘制闪烁，确保所有绘制操作都通过WM_PAINT消息进行
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);

            // 初始化模式与按钮、日志的映射关系
            InitializeMappings();

            // 绑定所有事件处理方法
            BindEvents();
        }

        /// <summary>
        /// 初始化模式与按钮、日志的映射关系
        /// 集中配置模式与UI元素及日志文本的对应关系，提高可维护性
        /// 当需要添加新的视觉功能页面时，只需在此处添加相应映射即可
        /// </summary>
        private void InitializeMappings()
        {
            // 模式与按钮的映射：将功能模式与对应的工具条按钮关联
            _modeButtonMap[FormPage.VPPPage] = toolStripButton_VPP;
            _modeButtonMap[FormPage.ManualPage] = toolStripButton_Manual;
            _modeButtonMap[FormPage.VPPSettingPage] = toolStripButton_配置;

            // 模式与日志信息的映射：为每种功能模式定义对应的操作日志文本
            _modeLogMap[FormPage.VPPPage] = "打开VPP编辑界面！";
            _modeLogMap[FormPage.ManualPage] = "打开视觉手动操作界面！";
            _modeLogMap[FormPage.VPPSettingPage] = "打开VPP配置界面！";
        }

        /// <summary>
        /// 集中绑定所有事件
        /// 将事件绑定逻辑集中管理，便于维护和修改
        /// 避免在构造函数中直接绑定事件导致的代码混乱
        /// </summary>
        private void BindEvents()
        {
            // 页面加载完成事件
            Load += VisionPage_Load;
            // VPP编辑按钮点击事件
            toolStripButton_VPP.Click += ToolStripButton_VPP_Click;
            // 手动操作按钮点击事件
            toolStripButton_Manual.Click += ToolStripButton_Manual_Click;
            // 配置按钮点击事件
            toolStripButton_配置.Click += ToolStripButton_Config_Click;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 页面加载事件处理方法
        /// 页面首次加载时执行的初始化操作，设置初始显示状态
        /// 采用异常捕获机制，确保页面加载失败时仍能正常运行
        /// </summary>
        /// <param name="sender">事件发送者（通常是当前页面实例）</param>
        /// <param name="e">事件参数</param>
        private void VisionPage_Load(object sender, EventArgs e)
        {
            try
            {
                // 初始状态为无模式，不加载任何子页面
                // 可根据实际业务需求修改为默认显示某个功能页面
                SwitchForm(FormPage.None);
            }
            catch (Exception ex)
            {
                // 记录异常信息到日志系统，便于问题排查
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// VPP编辑按钮点击事件
        /// 响应用户点击，触发VPP编辑页面的切换
        /// 调用统一处理方法，确保操作逻辑的一致性
        /// </summary>
        /// <param name="sender">事件发送者（通常是toolStripButton_VPP按钮）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_VPP_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.VPPPage);
        }

        /// <summary>
        /// 手动操作按钮点击事件
        /// 响应用户点击，触发视觉手动操作页面的切换
        /// 调用统一处理方法，确保操作逻辑的一致性
        /// </summary>
        /// <param name="sender">事件发送者（通常是toolStripButton_Manual按钮）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_Manual_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.ManualPage);
        }

        /// <summary>
        /// 配置按钮点击事件
        /// 响应用户点击，触发VPP配置页面的切换
        /// 调用统一处理方法，确保操作逻辑的一致性
        /// </summary>
        /// <param name="sender">事件发送者（通常是toolStripButton_配置按钮）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_Config_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.VPPSettingPage);
        }
        #endregion

        #region 页面切换逻辑
        /// <summary>
        /// 统一处理按钮点击逻辑
        /// 封装按钮点击的通用处理流程，包括按钮状态控制和异常处理
        /// 避免在每个按钮事件中重复编写相同逻辑，提高代码复用性
        /// </summary>
        /// <param name="targetMode">目标模式</param>
        private void HandleButtonClick(FormPage targetMode)
        {
            try
            {
                // 禁用当前按钮防止重复点击，避免用户快速多次点击导致的界面异常
                if (_modeButtonMap.TryGetValue(targetMode, out var button))
                {
                    button.Enabled = false;
                }

                // 执行页面切换
                SwitchForm(targetMode);
            }
            catch (Exception ex)
            {
                // 记录异常信息，确保问题可追踪
                RLog.SaveError(ex);
            }
            finally
            {
                // 无论是否发生异常，都恢复按钮可用状态，保证界面交互正常
                if (_modeButtonMap.TryGetValue(targetMode, out var button))
                {
                    button.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 切换显示不同的子页面
        /// 实现功能页面的切换逻辑，包括子窗体获取、准备、状态更新等
        /// 作为页面切换的核心方法，协调各环节的执行顺序
        /// </summary>
        /// <param name="targetMode">目标显示模式</param>
        private void SwitchForm(FormPage targetMode)
        {
            try
            {
                // 如果当前已是目标模式，无需切换，直接返回
                // 避免重复加载相同内容，提高性能和用户体验
                if (_currentFormMode == targetMode) return;

                // 处理无模式状态（清空容器）
                if (targetMode == FormPage.None)
                {
                    ClearContainer();
                    _currentFormMode = FormPage.None;
                    return;
                }

                // 获取目标子页面并添加到容器，若成功则更新状态
                if (PrepareChildForms(targetMode))
                {
                    // 更新按钮状态，突出显示当前选中的功能页面
                    UpdateButtonStates(targetMode);

                    // 记录操作日志，便于系统审计和问题排查
                    LogOperation(targetMode);

                    // 更新当前模式为目标模式，记录状态变化
                    _currentFormMode = targetMode;
                }
            }
            catch (Exception ex)
            {
                // 记录切换过程中的异常信息
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 根据模式准备对应的子页面
        /// 封装不同功能模式对应的子窗体加载逻辑
        /// 支持单窗体和多窗体组合的复杂界面布局
        /// </summary>
        /// <param name="mode">功能模式</param>
        /// <returns>是否成功加载子页面</returns>
        private bool PrepareChildForms(FormPage mode)
        {
            // 清空容器中现有控件，防止残留旧内容
            ClearContainer();

            switch (mode)
            {
                case FormPage.VPPPage:
                    // 添加VPP编辑页面（单窗体）
                    var vppForm = Frm_VPTool.Instance;
                    vppForm.TopLevel = false;       // 设置为非顶级窗口，允许嵌入到其他容器
                    vppForm.Dock = DockStyle.Fill;  // 填充整个容器，确保占满可用空间
                    vppForm.AutoScaleMode = AutoScaleMode.None; // 禁用自动缩放，避免布局错乱
                    uiPanel_window.Controls.Add(vppForm);
                    vppForm.Show();
                    return true;

                case FormPage.ManualPage:
                    // 添加视觉手动操作相关的两个窗体（多窗体组合）
                    // 左侧控制面板
                    var visionControl = Frm_VisionControl.Instance;
                    visionControl.TopLevel = false;
                    visionControl.Dock = DockStyle.Left;  // 停靠在左侧
                    visionControl.AutoScaleMode = AutoScaleMode.None;
                    uiPanel_window.Controls.Add(visionControl);
                    visionControl.Show();

                    // 底部操作视图
                    var operateView = Frm_OperateView.Instance;
                    operateView.TopLevel = false;
                    operateView.Dock = DockStyle.Bottom; // 停靠在底部
                    operateView.AutoScaleMode = AutoScaleMode.None;
                    uiPanel_window.Controls.Add(operateView);
                    operateView.Show();
                    return true;

                case FormPage.VPPSettingPage:
                    //添加VPP配置页面（当前注释掉，需根据实际情况实现）
                    // 需确保Frm_VPPSetting类存在并正确实现
                    var settingForm = Frm_VPPSetting.Instance;
                    settingForm.TopLevel = false;
                    settingForm.Dock = DockStyle.Fill;
                    settingForm.AutoScaleMode = AutoScaleMode.None;
                    uiPanel_window.Controls.Add(settingForm);
                    settingForm.Show();
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 清空容器并隐藏
        /// 封装容器清理逻辑，在页面切换前确保容器状态干净
        /// 避免不同功能页面的控件残留和冲突
        /// </summary>
        private void ClearContainer()
        {
            uiPanel_window.Visible = false;  // 隐藏容器，避免清理过程中显示异常
            uiPanel_window.Controls.Clear(); // 清空容器中的所有控件
        }

        /// <summary>
        /// 更新按钮状态（选中状态视觉区分）
        /// 通过样式差异直观显示当前激活的功能页面
        /// 帮助用户清晰识别当前操作的功能模块
        /// </summary>
        /// <param name="activeMode">当前激活的模式</param>
        private void UpdateButtonStates(FormPage activeMode)
        {
            foreach (var item in _modeButtonMap)
            {
                var mode = item.Key;
                var button = item.Value;

                // 跳过空按钮，避免空引用异常，增强代码健壮性
                if (button == null) continue;

                // 设置选中和未选中状态的样式
                if (mode == activeMode)
                {
                    button.ForeColor = Color.DimGray;          // 选中状态文字颜色
                    button.Font = new Font(button.Font, FontStyle.Bold); // 选中状态文字加粗
                }
                else
                {
                    button.ForeColor = Color.White;            // 未选中状态文字颜色
                    button.Font = new Font(button.Font, FontStyle.Regular); // 未选中状态正常字体
                }
            }
            uiPanel_window.Visible = true; // 显示容器，完成页面切换
        }

        /// <summary>
        /// 记录操作日志
        /// 记录用户切换功能页面的操作，便于系统审计和问题追踪
        /// 统一的日志记录入口，确保操作可追溯
        /// </summary>
        /// <param name="mode">操作的功能模式</param>
        private void LogOperation(FormPage mode)
        {
            // 尝试从映射表中获取对应的日志信息
            if (_modeLogMap.TryGetValue(mode, out var logMessage))
            {
                RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Black, logMessage);
            }
            else
            {
                // 处理未知模式的日志记录，确保异常情况也能被记录
                RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Black, "切换到未知视觉界面！");
            }
        }
        #endregion
    }
}
