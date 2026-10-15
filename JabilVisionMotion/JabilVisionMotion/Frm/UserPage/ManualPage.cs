using Jabil.Lib;
using Jabil.Module.IO;
using Jabil.Module.IO.Frm;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus;
using Jabil.Module.Modbus.Frm;
using Jabil.Module.Motion;
using Jabil.Module.Motion.Frm;
using Jabil.Module.TCPIP;
using Jabil.Module.TCPIP.Frm;
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
    /// 手动操作页面，用于切换显示各类手动控制界面
    /// 支持IO控制、Modbus通信、TCP/IP配置、运动控制和跟踪功能
    /// 采用单例模式确保全局唯一实例，通过工具条按钮实现不同功能页面的切换
    /// 继承自Sunny.UI的UIPage控件，保持系统UI风格一致性
    /// </summary>
    public partial class ManualPage : UIPage
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 页面唯一实例（线程安全的延迟初始化）
        /// 使用Lazy<T>实现延迟加载，确保在首次使用时才初始化实例
        /// LazyThreadSafetyMode.ExecutionAndPublication确保多线程环境下的线程安全
        /// </summary>
        private static readonly Lazy<ManualPage> _instance = new Lazy<ManualPage>(
            () => new ManualPage(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取页面唯一实例的公开属性
        /// 外部通过ManualPage.Instance访问，保证全局只有一个实例
        /// </summary>
        public static ManualPage Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数，防止外部直接实例化
        /// 确保页面只能通过单例模式获取，保证实例唯一性
        /// </summary>
        private ManualPage()
        {
            InitializeComponent();
            InitializeSettings();
        }
        #endregion

        #region 枚举定义
        /// <summary>
        /// 页面显示模式枚举，定义了手动操作页面支持的子功能类型
        /// 用于统一管理页面切换状态，避免使用魔术数字，提高代码可读性
        /// </summary>
        enum FormPage
        {
            /// <summary>
            /// 无模式（初始状态）
            /// 表示尚未加载任何子功能页面
            /// </summary>
            None,
            /// <summary>
            /// IO操作页面
            /// 对应显示IO控制相关的功能界面
            /// </summary>
            IOPage,
            /// <summary>
            /// Modbus操作页面
            /// 对应显示Modbus通信控制界面
            /// </summary>
            ModbusPage,
            /// <summary>
            /// TCP/IP操作页面
            /// 对应显示TCP/IP网络配置界面
            /// </summary>
            TCPIPPage,
            /// <summary>
            /// 运动控制页面
            /// 对应显示运动轴控制界面
            /// </summary>
            MotionPage,
            /// <summary>
            /// 跟踪页面
            /// 对应显示跟踪功能相关界面
            /// </summary>
            TrackPage
        }
        #endregion

        #region 字段
        /// <summary>
        /// 当前显示的模式
        /// 用于记录当前激活的子功能页面状态，避免重复加载相同内容
        /// </summary>
        private FormPage _currentFormMode = FormPage.None;

        /// <summary>
        /// 按钮与模式的映射关系（用于统一处理按钮状态）
        /// 实现按钮与对应功能模式的关联，便于批量处理按钮样式和状态
        /// </summary>
        private readonly Dictionary<FormPage, ToolStripButton> _modeButtonMap = new Dictionary<FormPage, ToolStripButton>();
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化页面设置和事件绑定
        /// 集中进行UI样式优化、映射关系初始化和事件绑定
        /// </summary>
        private void InitializeSettings()
        {
            // 启用双缓冲和异步绘制，减少界面闪烁
            // OptimizedDoubleBuffer：启用双缓冲以提高绘制性能
            // AllPaintingInWmPaint：减少绘制闪烁，确保所有绘制操作都通过WM_PAINT消息进行
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);

            // 初始化模式与按钮的映射关系
            InitializeModeButtonMap();

            // 绑定所有事件处理方法
            BindEvents();
        }

        /// <summary>
        /// 初始化模式与按钮的映射关系
        /// 将每个功能模式与对应的工具条按钮关联，便于统一管理
        /// </summary>
        private void InitializeModeButtonMap()
        {
            _modeButtonMap[FormPage.IOPage] = toolStripButton_IO;
            _modeButtonMap[FormPage.ModbusPage] = toolStripButton_Modbus;
            _modeButtonMap[FormPage.TCPIPPage] = toolStripButton_TCPIP;
            _modeButtonMap[FormPage.MotionPage] = toolStripButton_Motion;
            _modeButtonMap[FormPage.TrackPage] = toolStripButton_Track;
        }

        /// <summary>
        /// 集中绑定所有事件
        /// 将事件绑定逻辑集中管理，便于维护和修改
        /// </summary>
        private void BindEvents()
        {
            // 页面加载完成事件
            Load += ManualPage_Load;
            // IO操作按钮点击事件
            toolStripButton_IO.Click += ToolStripButton_IO_Click;
            // Modbus操作按钮点击事件
            toolStripButton_Modbus.Click += ToolStripButton_Modbus_Click;
            // 运动控制按钮点击事件
            toolStripButton_Motion.Click += ToolStripButton_Motion_Click;
            // TCP/IP操作按钮点击事件
            toolStripButton_TCPIP.Click += ToolStripButton_TCPIP_Click;
            // 跟踪页面按钮点击事件
            toolStripButton_Track.Click += ToolStripButton_Track_Click;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 页面加载事件（初始化按钮可见性）
        /// 根据系统配置动态显示或隐藏功能按钮
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ManualPage_Load(object sender, EventArgs e)
        {
            try
            {
                // 加载过程中禁用页面，防止用户进行无效操作
                Enabled = false;

                // 根据系统中是否存在相关硬件/配置，设置各功能按钮的可见性
                SetButtonVisibility(toolStripButton_IO, IOSystem.Instance.IOCards);
                SetButtonVisibility(toolStripButton_Modbus, ModbusSystem.Instance.ModbusCommunications);
                SetButtonVisibility(toolStripButton_Motion, MotionSystem.Instance.MotionCards);
                SetButtonVisibility(toolStripButton_TCPIP, TcpIpSystem.Instance.TcpIpCommunications);
                // 跟踪按钮可见性可根据实际需求设置
                // toolStripButton_Track.Visible = ...;

                // 加载完成后启用页面
                Enabled = true;
            }
            catch (Exception ex)
            {
                // 记录异常信息到日志系统
                RLog.SaveError(ex);
                // 异常发生时恢复页面可用状态
                Enabled = true;
            }
        }

        /// <summary>
        /// IO操作按钮点击事件
        /// 响应用户点击，触发IO操作页面的切换
        /// </summary>
        /// <param name="sender">事件发送者（通常是按钮控件）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_IO_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.IOPage, toolStripButton_IO);
        }

        /// <summary>
        /// Modbus操作按钮点击事件
        /// 响应用户点击，触发Modbus操作页面的切换
        /// </summary>
        /// <param name="sender">事件发送者（通常是按钮控件）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_Modbus_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.ModbusPage, toolStripButton_Modbus);
        }

        /// <summary>
        /// 运动控制按钮点击事件
        /// 响应用户点击，触发运动控制页面的切换
        /// </summary>
        /// <param name="sender">事件发送者（通常是按钮控件）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_Motion_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.MotionPage, toolStripButton_Motion);
        }

        /// <summary>
        /// TCP/IP操作按钮点击事件
        /// 响应用户点击，触发TCP/IP操作页面的切换
        /// </summary>
        /// <param name="sender">事件发送者（通常是按钮控件）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_TCPIP_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.TCPIPPage, toolStripButton_TCPIP);
        }

        /// <summary>
        /// 跟踪页面按钮点击事件
        /// 响应用户点击，触发跟踪页面的切换
        /// </summary>
        /// <param name="sender">事件发送者（通常是按钮控件）</param>
        /// <param name="e">事件参数</param>
        private void ToolStripButton_Track_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormPage.TrackPage, toolStripButton_Track);
        }
        #endregion

        #region 页面切换逻辑
        /// <summary>
        /// 统一处理按钮点击逻辑
        /// 封装按钮点击的通用处理流程，包括状态控制和异常处理
        /// </summary>
        /// <param name="targetMode">目标模式</param>
        /// <param name="button">触发点击的按钮</param>
        private void HandleButtonClick(FormPage targetMode, ToolStripButton button)
        {
            try
            {
                // 点击处理期间禁用按钮，防止重复点击
                button.Enabled = false;
                // 执行页面切换
                SwitchForm(targetMode);
            }
            catch (Exception ex)
            {
                // 记录异常信息
                RLog.SaveError(ex);
            }
            finally
            {
                // 无论是否发生异常，都恢复按钮可用状态
                button.Enabled = true;
            }
        }

        /// <summary>
        /// 切换显示不同的子页面
        /// 实现子功能页面的切换逻辑，包括子窗体获取、准备、状态更新等
        /// </summary>
        /// <param name="targetMode">目标显示模式</param>
        private void SwitchForm(FormPage targetMode)
        {
            try
            {
                // 如果当前已是目标模式，无需切换，直接返回
                // 避免重复加载相同内容，提高性能
                if (_currentFormMode == targetMode) return;

                // 根据目标模式获取对应的子页面实例
                var childForm = GetChildFormByMode(targetMode);
                if (childForm == null) return;

                // 准备并显示子页面
                PrepareChildForm(childForm);

                // 更新按钮颜色状态，突出显示当前选中的功能
                UpdateButtonColors(targetMode);

                // 记录操作日志，便于系统审计和问题排查
                LogOperation(targetMode);

                // 更新当前模式为目标模式
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
        /// </summary>
        /// <param name="mode">功能模式</param>
        /// <returns>对应的子页面窗体实例</returns>
        private Form GetChildFormByMode(FormPage mode)
        {
            return mode switch
            {
                FormPage.IOPage => IOPage.Instance,           // IO操作页面（单例）
                FormPage.ModbusPage => ModbusPage.Instance,   // Modbus操作页面（单例）
                FormPage.TCPIPPage => TCPIPPage.Instance,     // TCP/IP操作页面（单例）
                FormPage.MotionPage => MotionPage.Instance,   // 运动控制页面（单例）
                // FormPage.TrackPage => TrackPage.Instance,   // 跟踪页面（单例，假设存在）
                _ => null                                     // 其他情况返回null
            };
        }

        /// <summary>
        /// 准备子页面并添加到容器
        /// 配置子页面属性，确保其正确嵌入到当前页面的容器中
        /// </summary>
        /// <param name="childForm">要显示的子页面</param>
        private void PrepareChildForm(Form childForm)
        {
            // 隐藏容器，避免切换过程中显示闪烁
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
        /// 更新按钮颜色（当前选中按钮置灰，其他置白）
        /// 通过样式差异直观显示当前激活的功能页面
        /// </summary>
        private void UpdateButtonColors(FormPage activeMode)
        {
            // 验证映射表是否初始化，避免空引用异常
            if (_modeButtonMap == null)
                throw new InvalidOperationException("按钮映射表未初始化，请先调用InitializeModeButtonMap方法");

            foreach (var item in _modeButtonMap)
            {
                FormPage mode = item.Key;
                ToolStripButton button = item.Value;

                // 跳过空按钮，避免空引用异常
                if (button == null)
                    continue;

                // 设置选中和未选中状态的样式
                if (mode == activeMode)
                {
                    button.ForeColor = Color.DimGray;
                    button.BackColor = Color.LightGray; // 增加背景色区分
                    button.Font = new Font(button.Font, FontStyle.Bold); // 选中按钮文字加粗
                }
                else
                {
                    button.ForeColor = Color.White;
                    button.BackColor = Color.Transparent; // 恢复默认背景
                    button.Font = new Font(button.Font, FontStyle.Regular); // 恢复正常字体
                }
            }
        }

        /// <summary>
        /// 记录操作日志
        /// 记录用户切换功能页面的操作，便于系统审计和问题追踪
        /// </summary>
        /// <param name="mode">操作的功能模式</param>
        private void LogOperation(FormPage mode)
        {
            // 根据模式生成对应的日志消息
            var logMessage = mode switch
            {
                FormPage.IOPage => "打开IO操作界面！",
                FormPage.ModbusPage => "打开Modbus操作界面！",
                FormPage.TCPIPPage => "打开TCPIP操作界面！",
                FormPage.MotionPage => "打开Motion操作界面！",
                FormPage.TrackPage => "打开Track操作界面！",
                _ => "切换到未知界面！"
            };

            // 保存操作日志
            RLog.SaveLog(LogType.Operation, Jabil.Module.Log.Lib.LogLevel.Info, Color.Black, logMessage);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 根据列表数据设置按钮可见性
        /// 当系统中存在相关配置或硬件时才显示对应的功能按钮
        /// </summary>
        /// <param name="button">要设置的按钮</param>
        /// <param name="list">数据列表（通常是硬件配置列表）</param>
        private void SetButtonVisibility(ToolStripButton button, System.Collections.IEnumerable list)
        {
            // 检查列表是否为null且包含元素
            bool hasItems = false;
            if (list != null)
            {
                foreach (var item in list)
                {
                    hasItems = true;
                    break; // 只要有一个元素就说明存在配置
                }
            }
            // 根据是否有配置项设置按钮可见性
            button.Visible = hasItems;
        }
        #endregion
    }
}
