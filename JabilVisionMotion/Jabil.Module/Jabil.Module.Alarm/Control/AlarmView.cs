﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using Jabil.Lib;
using Sunny.UI;
using LogLevel = Jabil.Module.Log.Lib.LogLevel;
using Jabil.Module.Log.Lib;

namespace Jabil.Module.Alarm.Control
{
    /// <summary>
    /// 报警视图用户控件
    /// 用于展示、筛选和管理报警信息，支持不同级别报警的分类显示
    /// </summary>
    public partial class AlarmView : UIUserControl
    {
        #region 常量与枚举

        /// <summary>
        /// 最大日志条目数量，超过则自动清空
        /// </summary>
        private const int MAX_LOG_ITEMS = 100;

        #endregion

        #region 私有成员

        /// <summary>
        /// 控件单例实例
        /// </summary>
        private static AlarmView _instance;

        /// <summary>
        /// 用于线程同步的锁对象
        /// </summary>
        private readonly object _lockObj = new object();

        /// <summary>
        /// 存储所有输出项的列表
        /// </summary>
        private readonly List<OutputItem> _outputItems = new List<OutputItem>();

        /// <summary>
        /// 不同级别日志的计数
        /// </summary>
        private int _infoCount;
        private int _warnCount;
        private int _errorCount;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取控件的单例实例
        /// </summary>
        public static AlarmView Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new AlarmView();
                }
                return _instance;
            }
        }

        #endregion

        #region 构造函数与初始化

        /// <summary>
        /// 初始化报警视图控件
        /// </summary>
        public AlarmView()
        {
            InitializeComponent();
            InitLanguage();
            InitializeEventHandlers();
        }

        /// <summary>
        /// 初始化语言设置
        /// </summary>
        private void InitLanguage()
        {
            try
            {
                if (Project.Instance.SystemParm.Language == LanguageType.English)
                {
                    Text = "Output";
                    listView1.Columns[0].Text = "Station";
                    listView1.Columns[1].Text = "Date";
                    listView1.Columns[2].Text = "Binding Code";
                    listView1.Columns[3].Text = "Error Start Time";
                    listView1.Columns[4].Text = "End Time";
                    listView1.Columns[5].Text = "Alarm Content";
                    listView1.Columns[6].Text = "Duration";

                    tsb_tip.Text = "Info";
                    tsb_warn.Text = "Warning";
                    tsb_error.Text = "Error";
                    历史日志ToolStripMenuItem.Text = "History Logs";
                    删除ToolStripMenuItem.Text = "Clear";
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 初始化事件处理程序
        /// </summary>
        private void InitializeEventHandlers()
        {
            // 绑定事件处理方法
            tsb_tip.Click += TsbTip_Click;
            tsb_warn.Click += TsbWarn_Click;
            tsb_error.Click += TsbError_Click;
            SizeChanged += AlarmView_SizeChanged;
            历史日志ToolStripMenuItem.Click += HistoryLogsToolStripMenuItem_Click;
            删除ToolStripMenuItem.Click += ClearToolStripMenuItem_Click;
        }

        #endregion

        #region 事件处理方法

        /// <summary>
        /// 提示信息筛选按钮点击事件
        /// </summary>
        private void TsbTip_Click(object sender, EventArgs e)
        {
            UpdateFilterState(tsb_tip, tsb_warn, tsb_error);
            FilterListByColor(Color.Green);
        }

        /// <summary>
        /// 警告信息筛选按钮点击事件
        /// </summary>
        private void TsbWarn_Click(object sender, EventArgs e)
        {
            UpdateFilterState(tsb_warn, tsb_tip, tsb_error);
            FilterListByColor(Color.Yellow);
        }

        /// <summary>
        /// 错误信息筛选按钮点击事件
        /// </summary>
        private void TsbError_Click(object sender, EventArgs e)
        {
            UpdateFilterState(tsb_error, tsb_tip, tsb_warn);
            FilterListByColor(Color.Red);
        }

        /// <summary>
        /// 控件大小改变事件
        /// </summary>
        private void AlarmView_SizeChanged(object sender, EventArgs e)
        {
            AdjustColumnWidths();
        }

        /// <summary>
        /// 历史日志菜单点击事件
        /// </summary>
        private void HistoryLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenHistoryLogs();
        }

        /// <summary>
        /// 清除日志菜单点击事件
        /// </summary>
        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearLog();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新筛选按钮的选中状态
        /// </summary>
        /// <param name="selectedButton">当前选中的按钮</param>
        /// <param name="unselectedButtons">需要取消选中的按钮</param>
        private void UpdateFilterState(ToolStripButton selectedButton, params ToolStripButton[] unselectedButtons)
        {
            WFUtil.UpdateMainThreadUIAsync(this, () =>
            {
                selectedButton.Checked = !selectedButton.Checked;
                foreach (var btn in unselectedButtons)
                {
                    btn.Checked = false;
                }
            });
        }

        /// <summary>
        /// 根据颜色筛选列表项
        /// </summary>
        /// <param name="filterColor">筛选颜色</param>
        private void FilterListByColor(Color filterColor)
        {
            WFUtil.UpdateMainThreadUIAsync(this, () =>
            {
                listView1.Items.Clear();

                // 获取需要显示的项（选中状态下按颜色筛选，未选中则显示全部）
                var itemsToShow = tsb_tip.Checked || tsb_warn.Checked || tsb_error.Checked
                    ? _outputItems.Where(item => item.Color == filterColor)
                    : _outputItems;

                // 添加到列表视图
                foreach (var item in itemsToShow)
                {
                    listView1.Items.Add(CreateListViewItem(item));
                }

                // 确保最后一项可见
                if (listView1.Items.Count > 0)
                {
                    listView1.EnsureVisible(listView1.Items.Count - 1);
                }
            });
        }

        /// <summary>
        /// 调整列表视图列宽
        /// </summary>
        private void AdjustColumnWidths()
        {
            WFUtil.UpdateMainThreadUIAsync(this, () =>
            {
                if (listView1.Columns.Count < 7) return;

                // 计算内容列的宽度（剩余空间）
                int totalFixedWidth = listView1.Columns.Cast<ColumnHeader>()
                    .Where((col, index) => index != 5)
                    .Sum(col => col.Width);

                listView1.Columns[5].Width = listView1.Width - totalFixedWidth - 24;
            });
        }

        /// <summary>
        /// 打开历史日志文件夹
        /// </summary>
        private void OpenHistoryLogs()
        {
            WFUtil.UpdateMainThreadUIAsync(this, () =>
            {
                try
                {
                    string logPath = $"{Project.Instance.ParameterPath.DataPath}\\Config\\Log\\Operation";
                    Process.Start(logPath);
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex);
                    Jabil.Module.Log.Frm.Frm_MessageBox.ShowError($"无法打开日志文件夹: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 清除所有日志
        /// </summary>
        private void ClearLog()
        {
            WFUtil.UpdateMainThreadUIAsync(this, () =>
            {
                lock (_lockObj)
                {
                    _infoCount = 0;
                    _warnCount = 0;
                    _errorCount = 0;
                    _outputItems.Clear();
                    listView1.Items.Clear();
                    UpdateCountDisplay();
                }
            });
        }

        /// <summary>
        /// 创建列表视图项
        /// </summary>
        /// <param name="item">输出项数据</param>
        /// <returns>ListViewItem对象</returns>
        private ListViewItem CreateListViewItem(OutputItem item)
        {
            var listItem = new ListViewItem
            {
                Text = item.Station,
                ForeColor = item.Color
            };

            listItem.SubItems.Add(item.Date);
            listItem.SubItems.Add(item.BindingCode);
            listItem.SubItems.Add(item.ErrorStartTime);
            listItem.SubItems.Add(item.EndTime);
            listItem.SubItems.Add(item.AlarmContent);
            listItem.SubItems.Add(item.Duration);

            return listItem;
        }

        /// <summary>
        /// 更新计数显示
        /// </summary>
        private void UpdateCountDisplay()
        {
            WFUtil.UpdateMainThreadUIAsync(this, () =>
            {
                tsb_tip.Text = Project.Instance.SystemParm.Language == LanguageType.English
                    ? $"Info({_infoCount})"
                    : $"提示({_infoCount})";

                tsb_warn.Text = Project.Instance.SystemParm.Language == LanguageType.English
                    ? $"Warning({_warnCount})"
                    : $"警告({_warnCount})";

                tsb_error.Text = Project.Instance.SystemParm.Language == LanguageType.English
                    ? $"Error({_errorCount})"
                    : $"错误({_errorCount})";
            });
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 输出报警信息到视图
        /// </summary>
        /// <param name="logLevel">日志级别</param>
        /// <param name="bindingCode">绑定代码</param>
        /// <param name="errorStartTime">异常开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="alarmContent">报警内容</param>
        /// <param name="duration">耗时</param>
        /// <param name="date">日期</param>
        /// <param name="station">站别</param>
        public void OutputMessage(LogLevel logLevel,
                                 string bindingCode,
                                 string errorStartTime,
                                 string endTime,
                                 string alarmContent,
                                 string duration,
                                 string date,
                                 string station)
        {
            try
            {
                // 根据日志级别获取对应的颜色
                Color itemColor = logLevel switch
                {
                    LogLevel.Warn => Color.Orange,
                    LogLevel.Error => Color.Red,
                    _ => Color.Green
                };

                WFUtil.UpdateMainThreadUIAsync(this, () =>
                {
                    lock (_lockObj)
                    {
                        // 空内容处理
                        if (string.IsNullOrEmpty(alarmContent))
                        {
                            var emptyItem = new ListViewItem { Text = string.Empty, ForeColor = itemColor };
                            emptyItem.SubItems.Add(alarmContent);
                            listView1.Items.Add(emptyItem);
                            return;
                        }

                        // 更新计数
                        switch (logLevel)
                        {
                            case LogLevel.Info: _infoCount++; break;
                            case LogLevel.Warn: _warnCount++; break;
                            case LogLevel.Error: _errorCount++; break;
                        }

                        // 创建输出项并添加到列表
                        var outputItem = new OutputItem
                        {
                            Station = station,
                            Date = date,
                            BindingCode = bindingCode,
                            ErrorStartTime = errorStartTime,
                            EndTime = endTime,
                            AlarmContent = alarmContent,
                            Duration = duration,
                            Color = itemColor
                        };

                        _outputItems.Add(outputItem);
                        listView1.Items.Add(CreateListViewItem(outputItem));
                        listView1.EnsureVisible(listView1.Items.Count - 1);

                        // 超过最大条目数则清空
                        if (_outputItems.Count > MAX_LOG_ITEMS)
                        {
                            ClearLog();
                        }

                        // 更新计数显示
                        UpdateCountDisplay();
                    }
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        #endregion

        #region 内部数据结构

        /// <summary>
        /// 输出项数据结构
        /// 存储单条报警信息的所有字段
        /// </summary>
        private class OutputItem
        {
            public string Station { get; set; }          // 站别
            public string Date { get; set; }             // 日期
            public string BindingCode { get; set; }      // 绑定代码
            public string ErrorStartTime { get; set; }   // 异常开始时间
            public string EndTime { get; set; }          // 结束时间
            public string AlarmContent { get; set; }     // 报警内容
            public string Duration { get; set; }         // 耗时
            public Color Color { get; set; }             // 显示颜色
        }

        #endregion
    }
}
