using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.ProductData.Frm;
using Jabil.Module.ProductData.Lib;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JabilVisionMotion.Frm.UserPage
{
    /// <summary>
    /// 产品数据页面，用于切换显示各类生产数据统计界面
    /// 支持产能统计、稼动率分析和抛料率监控等核心生产指标展示
    /// 采用单例模式确保全局唯一实例，通过工具条按钮实现不同统计页面的切换
    /// 继承自Sunny.UI的UIPage控件，保持与系统UI风格的一致性
    /// </summary>
    public partial class ProductDataPage : UIPage
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 页面唯一实例（线程安全的延迟初始化）
        /// 使用Lazy<T>实现延迟加载，确保在首次使用时才初始化实例
        /// LazyThreadSafetyMode.ExecutionAndPublication确保多线程环境下的线程安全
        /// 避免多线程同时创建实例导致的资源竞争问题
        /// </summary>
        private static readonly Lazy<ProductDataPage> _instance = new Lazy<ProductDataPage>(
            () => new ProductDataPage(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取页面唯一实例的公开属性
        /// 外部通过ProductDataPage.Instance访问，保证全局只有一个实例
        /// 提供统一的访问入口，避免重复创建页面消耗资源
        /// </summary>
        public static ProductDataPage Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数，防止外部直接实例化
        /// 确保页面只能通过单例模式获取，保证实例唯一性
        /// 符合单例模式的设计原则，禁止外部随意创建实例
        /// </summary>
        private ProductDataPage()
        {
            InitializeComponent();
            InitializeSettings();
        }
        #endregion

        #region 枚举定义
        /// <summary>
        /// 页面显示模式枚举，定义了产品数据页面支持的统计类型
        /// 用于统一管理页面切换状态，避免使用魔术数字，提高代码可读性
        /// 枚举值与实际功能页面一一对应，便于维护和扩展
        /// </summary>
        enum FormPage
        {
            /// <summary>
            /// 无模式（初始状态）
            /// 表示尚未加载任何统计页面
            /// 作为页面初始化时的默认状态
            /// </summary>
            None,
            /// <summary>
            /// 产能统计页面
            /// 显示产品生产数量、效率等产能相关指标
            /// 对应Frm_Yield子窗体
            /// </summary>
            YieldPage,
            /// <summary>
            /// 稼动率统计页面
            /// 展示设备运行时间占比、利用率等稼动率指标
            /// 对应Frm_UtilizationRate子窗体
            /// </summary>
            UtilizationRatePage,
            /// <summary>
            /// 抛料率统计页面
            /// 显示物料抛料数量、比例等抛料相关数据
            /// 对应Frm_MaterialThrowingRate子窗体
            /// </summary>
            MaterialThrowingRatePage
        }
        #endregion

        #region 字段
        /// <summary>
        /// 当前显示的模式
        /// 用于记录当前激活的统计页面状态，避免重复加载相同内容
        /// 通过状态判断实现页面切换的优化，提升性能
        /// </summary>
        private FormPage _currentFormMode = FormPage.None;

        /// <summary>
        /// 模式与按钮的映射关系
        /// 将每个统计模式与对应的工具条按钮关联，便于统一管理按钮状态和样式
        /// 使用字典结构实现一对多映射，简化按钮状态更新逻辑
        /// </summary>
        private readonly Dictionary<FormPage, ToolStripButton> _modeButtonMap = new Dictionary<FormPage, ToolStripButton>();

        /// <summary>
        /// 模式与操作日志的映射关系
        /// 存储每种统计模式对应的操作日志文本，便于统一记录操作行为
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
            // OptimizedDoubleBuffer：启用双缓冲以提高绘制性能，特别适用于频繁刷新的统计界面
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
        /// 当需要添加新的统计页面时，只需在此处添加相应映射即可
        /// </summary>
        private void InitializeMappings()
        {
            // 模式与按钮的映射：将统计模式与对应的工具条按钮关联
            _modeButtonMap[FormPage.YieldPage] = toolStripButton_Yield;
            _modeButtonMap[FormPage.UtilizationRatePage] = toolStripButton_UtilizationRate;
            _modeButtonMap[FormPage.MaterialThrowingRatePage] = toolStripButton_MaterialThrowingRate;

            // 模式与日志信息的映射：为每种统计模式定义对应的操作日志文本
            _modeLogMap[FormPage.YieldPage] = "打开产能统计显示界面！";
            _modeLogMap[FormPage.UtilizationRatePage] = "打开稼动率统计显示界面！";
            _modeLogMap[FormPage.MaterialThrowingRatePage] = "打开抛料率统计显示界面！";
        }

        /// <summary>
        /// 集中绑定所有事件
        /// 将事件绑定逻辑集中管理，便于维护和修改
        /// 避免在构造函数中直接绑定事件导致的代码混乱
        /// </summary>
        private void BindEvents()
        {
            // 页面加载完成事件
            Load += ProductDataPage_Load;
            // 产能统计按钮点击事件
            toolStripButton_Yield.Click += ToolStripButton_Yield_Click;
            // 稼动率统计按钮点击事件
            toolStripButton_UtilizationRate.Click += ToolStripButton_UtilizationRate_Click;
            // 抛料率统计按钮点击事件
            toolStripButton_MaterialThrowingRate.Click += ToolStripButton_MaterialThrowingRate_Click;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 页面加载事件处理方法
        /// 页面首次加载时执行的初始化操作，设置默认显示的统计页面
        /// 采用异常捕获机制，确保页面加载失败时仍能正常运行
        /// </summary>
        /// <param name="sender">事件发送者（通常是当前页面实例）</param>
        /// <param name="e">事件参数</param>
        private void ProductDataPage_Load(object sender, EventArgs e)
        {
            try
            {
                // 初始显示产能统计页面
                SwitchForm(FormPage.YieldPage);
            }
            catch (Exception ex)
            {
                // 记录异常信息到日志系统，便于问题排查
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 产能统计按钮点击事件
        /// 响应用户点击，触发产能统计页面的切换
        /// 调用统一处理方法，确保操作逻辑的一致性
        /// </summary>
        /// <param name="sender">事件发送者（通常是toolStripButton_Yield按钮）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_Yield_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.YieldPage);
        }

        /// <summary>
        /// 稼动率统计按钮点击事件
        /// 响应用户点击，触发稼动率统计页面的切换
        /// 调用统一处理方法，确保操作逻辑的一致性
        /// </summary>
        /// <param name="sender">事件发送者（通常是toolStripButton_UtilizationRate按钮）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_UtilizationRate_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.UtilizationRatePage);
        }

        /// <summary>
        /// 抛料率统计按钮点击事件
        /// 响应用户点击，触发抛料率统计页面的切换
        /// 调用统一处理方法，确保操作逻辑的一致性
        /// </summary>
        /// <param name="sender">事件发送者（通常是toolStripButton_MaterialThrowingRate按钮）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_MaterialThrowingRate_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.MaterialThrowingRatePage);
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
                // 禁用当前按钮防止重复点击，避免用户快速多次点击导致的异常
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
        /// 实现统计页面的切换逻辑，包括子窗体获取、准备、状态更新等
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

                // 根据目标模式获取对应的子页面实例
                var childForm = GetChildFormByMode(targetMode);
                if (childForm == null) return;

                // 准备并显示子页面
                PrepareChildForm(childForm);

                // 更新按钮状态，突出显示当前选中的统计页面
                UpdateButtonStates(targetMode);

                // 记录操作日志，便于系统审计和问题排查
                LogOperation(targetMode);

                // 更新当前模式为目标模式，记录状态变化
                _currentFormMode = targetMode;
            }
            catch (Exception ex)
            {
                // 记录切换过程中的异常信息
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 根据模式获取对应的子页面
        /// 封装子页面实例的获取逻辑，使用switch表达式简化条件判断
        /// 隔离子页面实例的创建逻辑，便于维护和扩展
        /// </summary>
        /// <param name="mode">统计模式</param>
        /// <returns>对应的子页面窗体实例，若模式不匹配则返回null</returns>
        private Form GetChildFormByMode(FormPage mode)
        {
            return mode switch
            {
                FormPage.YieldPage => Frm_Yield.Instance,                   // 产能统计页面（单例）
                FormPage.UtilizationRatePage => Frm_UtilizationRate.Instance, // 稼动率统计页面（单例）
                FormPage.MaterialThrowingRatePage => Frm_MaterialThrowingRate.Instance, // 抛料率统计页面（单例）
                _ => null                                                   // 其他情况返回null
            };
        }

        /// <summary>
        /// 准备子页面并添加到容器
        /// 配置子页面属性，确保其正确嵌入到当前页面的容器中
        /// 标准化子页面的显示方式，保证所有子页面的展示效果一致
        /// </summary>
        /// <param name="childForm">要显示的子页面</param>
        private void PrepareChildForm(Form childForm)
        {
            // 隐藏容器，避免切换过程中显示闪烁，提升用户体验
            uiPanel_window.Visible = false;
            // 清空容器中现有控件，防止残留旧内容
            uiPanel_window.Controls.Clear();

            // 设置子页面属性，使其能正确嵌入到容器中
            childForm.TopLevel = false;       // 设置为非顶级窗口，允许嵌入到其他容器
            childForm.Dock = DockStyle.Fill;  // 填充整个容器，确保占满可用空间
            childForm.AutoScaleMode = AutoScaleMode.None; // 禁用自动缩放，避免布局错乱

            // 将子页面添加到容器并显示
            uiPanel_window.Controls.Add(childForm);
            childForm.Show();

            // 显示容器，完成切换
            uiPanel_window.Visible = true;
        }

        /// <summary>
        /// 更新按钮状态（选中状态视觉区分）
        /// 通过样式差异直观显示当前激活的统计页面
        /// 帮助用户清晰识别当前操作的功能模块
        /// </summary>
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
        }

        /// <summary>
        /// 记录操作日志
        /// 记录用户切换统计页面的操作，便于系统审计和问题追踪
        /// 统一的日志记录入口，确保操作可追溯
        /// </summary>
        /// <param name="mode">操作的统计模式</param>
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
                RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Black, "切换到未知数据统计界面！");
            }
        }
        #endregion
    }
}
