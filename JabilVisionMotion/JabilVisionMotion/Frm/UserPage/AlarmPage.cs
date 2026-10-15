using Jabil.Module.Alarm.Frm;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
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
    /// 报警页面，用于切换显示报警履历、报警TOP和操作日志
    /// 采用单例模式确保页面唯一实例，通过工具条按钮切换不同的子窗体内容
    /// </summary>
    public partial class AlarmPage : UIPage
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 页面唯一实例（线程安全的延迟初始化）
        /// 使用Lazy<T>确保线程安全和延迟加载，提高性能
        /// </summary>
        private static readonly Lazy<AlarmPage> _instance = new Lazy<AlarmPage>(
            () => new AlarmPage(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取页面唯一实例的公开属性
        /// 外部通过AlarmPage.Instance访问，确保全局唯一
        /// </summary>
        public static AlarmPage Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数防止外部实例化
        /// 确保只能通过单例模式获取页面实例
        /// </summary>
        private AlarmPage()
        {
            InitializeComponent();
            InitializeStyles();
        }
        #endregion

        #region 枚举定义
        /// <summary>
        /// 页面显示模式枚举（对应不同的子窗体）
        /// 用于统一管理页面切换状态，避免使用魔术数字
        /// </summary>
        enum FormPage
        {
            /// <summary>
            /// 无模式（初始状态）
            /// </summary>
            None,
            /// <summary>
            /// 报警履历模式（显示报警历史记录）
            /// </summary>
            AlarmHistory,
            /// <summary>
            /// 报警TOP模式（显示报警统计排名）
            /// </summary>
            AlarmTop,
            /// <summary>
            /// 操作日志模式（显示系统操作记录）
            /// </summary>
            OperationLog
        }
        #endregion

        #region 字段
        /// <summary>
        /// 当前显示的模式
        /// 用于记录当前激活的子窗体状态，避免重复加载
        /// </summary>
        private FormPage _currentFormMode = FormPage.None;
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化页面样式
        /// 配置UI相关的基础设置，优化显示效果
        /// </summary>
        private void InitializeStyles()
        {
            // 启用双缓冲和异步绘制，减少界面闪烁
            // OptimizedDoubleBuffer: 启用双缓冲提高绘制性能
            // AllPaintingInWmPaint: 减少绘制闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);

            // 绑定所有事件处理方法
            BindEvents();
        }

        /// <summary>
        /// 绑定所有事件
        /// 集中管理事件绑定，提高代码可维护性
        /// </summary>
        private void BindEvents()
        {
            // 页面加载事件
            Load += AlarmPage_Load;
            // 报警履历按钮点击事件
            toolStripBtnAlarmHistory.Click += ToolStripBtnAlarmHistory_Click;
            // 报警TOP按钮点击事件
            toolStripBtnAlarmTop.Click += ToolStripBtnAlarmTop_Click;
            // 操作日志按钮点击事件
            toolStripBtnOperationLog.Click += ToolStripBtnOperationLog_Click;
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 页面加载事件处理方法
        /// 页面首次加载时执行的初始化操作
        /// </summary>
        private void AlarmPage_Load(object sender, EventArgs e)
        {
            try
            {
                // 加载过程中禁用页面，防止用户操作
                Enabled = false;
                // 初始显示操作日志页面
                SwitchForm(FormPage.OperationLog);
                // 加载完成后启用页面
                Enabled = true;
            }
            catch (Exception ex)
            {
                // 异常发生时恢复页面可用状态
                Enabled = true;
                // 记录异常信息到日志
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 切换到报警履历按钮点击事件
        /// 响应用户点击，切换到报警履历页面
        /// </summary>
        private void ToolStripBtnAlarmHistory_Click(object sender, EventArgs e)
        {
            SwitchForm(FormPage.AlarmHistory);
        }

        /// <summary>
        /// 切换到报警TOP按钮点击事件
        /// 响应用户点击，切换到报警TOP统计页面
        /// </summary>
        private void ToolStripBtnAlarmTop_Click(object sender, EventArgs e)
        {
            SwitchForm(FormPage.AlarmTop);
        }

        /// <summary>
        /// 切换到操作日志按钮点击事件
        /// 响应用户点击，切换到操作日志页面
        /// </summary>
        private void ToolStripBtnOperationLog_Click(object sender, EventArgs e)
        {
            SwitchForm(FormPage.OperationLog);
        }
        #endregion

        #region 页面切换逻辑
        /// <summary>
        /// 切换显示不同的子窗体
        /// 根据目标模式加载对应的子窗体，实现页面内容切换
        /// </summary>
        /// <param name="targetMode">目标显示模式</param>
        private void SwitchForm(FormPage targetMode)
        {
            try
            {
                // 如果当前已是目标模式，无需切换，直接返回
                if (_currentFormMode == targetMode) return;

                // 根据目标模式获取对应的子窗体实例
                // 使用switch表达式简化条件判断
                Form childForm = targetMode switch
                {
                    FormPage.AlarmHistory => Frm_Alarm.Instance,   // 报警履历窗体（单例）
                    FormPage.AlarmTop => Frm_AlarmTop.Instance,     // 报警TOP窗体（单例）
                    FormPage.OperationLog => Frm_Operation.Instance,// 操作日志窗体（单例）
                    _ => null                                       // 其他情况返回null
                };

                // 如果子窗体为空，直接返回
                if (childForm == null) return;

                // 准备并显示子窗体
                PrepareChildForm(childForm);

                // 更新当前模式为目标模式
                _currentFormMode = targetMode;
            }
            catch (Exception ex)
            {
                // 记录切换过程中的异常
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 准备子窗体并添加到容器
        /// 配置子窗体属性，确保正确显示在容器中
        /// </summary>
        /// <param name="childForm">要显示的子窗体</param>
        private void PrepareChildForm(Form childForm)
        {
            // 隐藏容器，避免切换过程中显示闪烁
            uiPanel_window.Visible = false;
            // 清空容器中现有控件，防止残留旧内容
            uiPanel_window.Controls.Clear();

            // 设置子窗体属性，使其能正确嵌入到容器中
            childForm.TopLevel = false;       // 非顶级窗口，允许嵌入到其他容器
            childForm.Dock = DockStyle.Fill;  // 填充整个容器
            childForm.AutoScaleMode = AutoScaleMode.None; // 禁用自动缩放，避免布局错乱

            // 将子窗体添加到容器并显示
            uiPanel_window.Controls.Add(childForm);
            childForm.Show();
            // 显示容器，完成切换
            uiPanel_window.Visible = true;
        }
        #endregion
    }
}
