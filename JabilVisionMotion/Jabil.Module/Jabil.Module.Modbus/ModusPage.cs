using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus.Frm;
using Jabil.Module.Permission;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Jabil.Module.Modbus
{
    /// <summary>
    /// Modbus主页面
    /// 负责管理和切换不同类型的Modbus子页面（PLC、机器人、配置页面）
    /// </summary>
    public partial class ModbusPage : UIPage
    {
        #region 单例模式
        /// <summary>
        /// 页面实例
        /// </summary>
        private static ModbusPage _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static ModbusPage Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new ModbusPage();
                return _instance;
            }
        }
        #endregion

        #region 字段与枚举
        /// <summary>
        /// 子页面类型枚举
        /// </summary>
        private enum FormMode
        {
            None,               // 无页面
            ModbusRobotPage,    // 机器人页面
            ModbusPLCPage,      // PLC页面
            ModbusConfigPage    // 配置页面（修正拼写错误）
        }

        /// <summary>
        /// 当前激活的页面类型
        /// </summary>
        private FormMode _currentFormMode = FormMode.None;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 初始化ModbusPage实例
        /// </summary>
        public ModbusPage()
        {
            InitializeComponent();
            // 启用双缓冲减少闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>
        /// 页面加载事件
        /// 初始化按钮显示状态和权限
        /// </summary>
        private void ModbusPage_Load(object sender, EventArgs e)
        {
            try
            {
                // 初始化按钮可见性（根据系统中存在的DeviceType）
                InitializeToolButtons();

                // 更新权限控制
                UpdatePermission();

                // 订阅用户登录状态变化事件（注释保留，需要时可启用）
                // PermissionSystem.Instance.OnLoginUserChanged += OnLoginUserChanged;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化工具按钮可见性
        /// 根据系统中注册的ModbusDeviceType显示对应的按钮
        /// </summary>
        private void InitializeToolButtons()
        {
            // 默认隐藏所有按钮
            toolStripButton_ModbusRobot.Visible = false;
            toolStripButton_ModbusPLC.Visible = false;

            var devices = ModbusSystem.Instance.ModbusCommunications;
            if (devices == null || !devices.Any()) return;

            // 检查是否存在PLC设备
            bool hasPlc = devices.Any(d => d.设备类型 == "PLC");
            // 检查是否存在机器人设备
            bool hasRobot = devices.Any(d => d.设备类型 == "Robot");

            toolStripButton_ModbusPLC.Visible = hasPlc;
            toolStripButton_ModbusRobot.Visible = hasRobot;
        }
        #endregion

        #region 页面切换逻辑
        /// <summary>
        /// 切换子页面
        /// </summary>
        /// <param name="formMode">要切换的页面类型</param>
        private void SwitchForm(FormMode formMode)
        {
            try
            {
                if (_currentFormMode == formMode) return;

                WFUtil.UpdateMainThreadUIAsync(this, () =>
                {
                    ClearCurrentPage();

                    // 修复switch表达式的问题，确保返回类型一致
                    Form form = null;
                    ToolStripButton button = null;

                    switch (formMode)
                    {
                        case FormMode.ModbusRobotPage:
                            form = Frm_ModbusRobot.Instance;
                            button = toolStripButton_ModbusRobot;
                            break;
                        case FormMode.ModbusPLCPage:
                            form = Frm_ModbusPLC.Instance;
                            button = toolStripButton_ModbusPLC;
                            break;
                        case FormMode.ModbusConfigPage:
                            form = Frm_ModbusConfig.Instance;
                            button = toolStripButton_ModbusConfig;
                            break;
                    }

                    if (form != null)
                    {
                        LoadChildForm(form);
                        _currentFormMode = formMode;
                        UpdateButtonStyles(button);
                    }
                    else
                    {
                        _currentFormMode = FormMode.None;
                    }

                    uiPanel_window.Visible = true;
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        /// <summary>
        /// 清除当前显示的子页面
        /// </summary>
        private void ClearCurrentPage()
        {
            uiPanel_window.Visible = false;
            uiPanel_window.Controls.Clear();
        }

        /// <summary>
        /// 加载子页面到容器
        /// </summary>
        /// <param name="childForm">子页面实例</param>
        private void LoadChildForm(Form childForm)
        {
            childForm.TopLevel = false;
            childForm.Dock = DockStyle.Fill;
            childForm.AutoScaleMode = AutoScaleMode.None;
            uiPanel_window.Controls.Add(childForm);
            childForm.Show();
        }

        /// <summary>
        /// 更新按钮样式（高亮当前选中的按钮）
        /// </summary>
        /// <param name="activeButton">当前激活的按钮</param>
        private void UpdateButtonStyles(ToolStripItem activeButton)
        {
            // 重置所有按钮颜色
            toolStripButton_ModbusPLC.ForeColor = Color.Gray;
            toolStripButton_ModbusRobot.ForeColor = Color.Gray;
            toolStripButton_ModbusConfig.ForeColor = Color.Gray;

            // 高亮当前选中的按钮
            activeButton.ForeColor = Color.DimGray;
        }
        #endregion

        #region 权限控制
        /// <summary>
        /// 用户登录状态变化事件处理
        /// </summary>
        private void OnLoginUserChanged(object sender, EventArgs e)
        {
            UpdatePermission();
        }

        /// <summary>
        /// 更新权限（控制按钮可见性）
        /// </summary>
        private void UpdatePermission()
        {
            // 示例：根据权限控制按钮可见性
            // toolStripButton_IOControl.Visible = PermissionSystem.CheckPermission("手动/IO/监控");
        }
        #endregion

        #region 按钮事件处理
        /// <summary>
        /// 配置页面按钮点击事件
        /// </summary>
        private void toolStripButton_ModbusConfig_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormMode.ModbusConfigPage, sender as ToolStripButton);
        }

        /// <summary>
        /// 机器人页面按钮点击事件
        /// </summary>
        private void toolStripButton_ModbusRobot_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormMode.ModbusRobotPage, sender as ToolStripButton);
        }

        /// <summary>
        /// PLC页面按钮点击事件
        /// </summary>
        private void toolStripButton_ModbusPLC_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormMode.ModbusPLCPage, sender as ToolStripButton);
        }

        /// <summary>
        /// 统一处理按钮点击逻辑
        /// </summary>
        /// <param name="formMode">目标页面类型</param>
        /// <param name="button">点击的按钮</param>
        private void HandleButtonClick(FormMode formMode, ToolStripButton button)
        {
            try
            {
                WFUtil.UpdateMainThreadUIAsync(this, () =>
                {
                    button.Enabled = false;
                    SwitchForm(formMode);
                    button.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                button.Enabled = true; // 异常时确保按钮恢复可用
            }
        }
        #endregion
    }
}
