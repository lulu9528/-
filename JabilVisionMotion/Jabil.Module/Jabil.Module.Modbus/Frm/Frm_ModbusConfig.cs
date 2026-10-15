using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Modbus.Frm
{
    public partial class Frm_ModbusConfig : UIPage
    {
        #region 单例模式
        /// <summary>
        /// 页面实例
        /// </summary>
        private static Frm_ModbusConfig _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static Frm_ModbusConfig Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_ModbusConfig();
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
            DeviceManagerPage,  // 设备管理页面
            DioManagerPage      // DIO点位管理页面
        }

        /// <summary>
        /// 当前激活的页面类型
        /// </summary>
        private FormMode _currentFormMode = FormMode.None;
        #endregion

        public Frm_ModbusConfig()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        #region  Control Event Handler
        private void Frm_ModbusConfig_Load(object sender, EventArgs e)
        {
            try
            {
                UpdatePermission();
                //PermissionSystem.Instance.OnLoginUserChanged += OnLoginUserChanged;
                
                // 默认显示设备管理页面
                SwitchForm(FormMode.DeviceManagerPage);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        private void OnLoginUserChanged(object sender, EventArgs e)
        {
            UpdatePermission();
        }
        
        private void UpdatePermission()
        {
            //this.toolStripButton_IOControl.Visible = PermissionSystem.CheckPermission("手动/IO/监控", out _) ? true : false;
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

                    // 获取要显示的窗体实例和对应的按钮
                    Form form = null;
                    ToolStripButton button = null;

                    switch (formMode)
                    {
                        case FormMode.DeviceManagerPage:
                            form = Frm_ModbusDeviceManager.Instance;
                            button = toolStripButton_DeviceManager;
                            break;
                        case FormMode.DioManagerPage:
                            form = Frm_ModbusDioManager.Instance;
                            button = toolStripButton_DioManager;
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
            toolStripButton_DeviceManager.ForeColor = Color.Gray;
            toolStripButton_DioManager.ForeColor = Color.Gray;

            // 高亮当前选中的按钮
            if (activeButton != null)
            {
                activeButton.ForeColor = Color.DimGray;
            }
        }
        #endregion

        #region 按钮事件处理
        /// <summary>
        /// 设备管理页面按钮点击事件
        /// </summary>
        private void toolStripButton_DeviceManager_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormMode.DeviceManagerPage, sender as ToolStripButton);
        }

        /// <summary>
        /// DIO点位管理页面按钮点击事件
        /// </summary>
        private void toolStripButton_DioManager_Click(object sender, EventArgs e)
        {
            HandleButtonClick(FormMode.DioManagerPage, sender as ToolStripButton);
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