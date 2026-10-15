using Jabil.Module.Log.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.Modbus.Entity;
using Jabil.Module.Modbus.Service;
using Jabil.Module.Modbus;  // 添加这行以引用ModbusSystem
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jabil.Module.Modbus.Control;

namespace Jabil.Module.Modbus.Frm
{
    public partial class Frm_ModbusDeviceManager : UIPage
    {
        private readonly ModbusCommunicationService _modbusService;
        private List<ModbusCommunicationDto> _modbusDevices;
        private ModbusCommunicationDto _selectedDevice;

        public Frm_ModbusDeviceManager()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            
            _modbusService = new ModbusCommunicationService();
            _modbusDevices = new List<ModbusCommunicationDto>();
        }

        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static Frm_ModbusDeviceManager _instance;
        public static Frm_ModbusDeviceManager Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_ModbusDeviceManager();
                return _instance;
            }
        }

        #region 事件处理
        private async void Frm_ModbusDeviceManager_Load(object sender, EventArgs e)
        {
            try
            {
                await LoadModbusDevicesAsync();
                SetupDataGridView();
                
                // 初始化时设置PropertyGrid为只读状态
                SetPropertyGridReadOnly(true);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("加载数据时发生错误: " + ex.Message);
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                var newDevice = new ModbusCommunicationDto
                {
                    设备名称 = "",
                    设备类型 = "",
                    站号 = 1,
                    设备IP = "192.168.1.1",
                    设备Port = 502,
                    超时 = 3000,
                    是否启用 = true
                };

                propertyGrid_配置修改.SelectedObject = newDevice;
                _selectedDevice = newDevice;
                btnConfirm.Enabled = true;
                btnConfirm.Text = "新增";
                
                // 设置PropertyGrid为可编辑状态
                SetPropertyGridReadOnly(false);
                
                // 显示新增设备的修改建议
                ShowModificationSuggestions(newDevice);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("添加设备时发生错误: " + ex.Message);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (uiDataGridView1.SelectedRows.Count > 0)
                {
                    var selectedDevice = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDto;
                    if (selectedDevice != null)
                    {
                        propertyGrid_配置修改.SelectedObject = selectedDevice;
                        _selectedDevice = selectedDevice;
                        btnConfirm.Enabled = true;
                        btnConfirm.Text = "保存";
                        
                        // 设置PropertyGrid为可编辑状态
                        SetPropertyGridReadOnly(false);
                        
                        // 显示修改建议
                        ShowModificationSuggestions(selectedDevice);
                    }
                }
                else
                {
                    ShowWarning("请选择要编辑的设备");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("编辑设备时发生错误: " + ex.Message);
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (uiDataGridView1.SelectedRows.Count > 0)
                {
                    var selectedDevice = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDto;
                    if (selectedDevice != null)
                    {
                        if (ShowAskDialog("确认删除设备: " + selectedDevice.设备名称 + "?"))
                        {
                            var result = await _modbusService.DeleteAsync(selectedDevice);
                            if (result.Status)
                            {
                                _modbusDevices.Remove(selectedDevice);
                                await RefreshDataGridViewAsync();
                                ShowSuccess("删除成功");
                                
                                // 刷新ModbusSystem中的数据，实现窗体间联动
                                await ModbusSystem.Instance.RefreshDataAsync();
                                
                                // 通知所有相关的ModbusControlUI控件刷新显示
                                NotifyModbusControlUIsToRefresh();
                            }
                            else
                            {
                                ShowError("删除失败: " + result.Message);
                            }
                        }
                    }
                }
                else
                {
                    ShowWarning("请选择要删除的设备");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("删除设备时发生错误: " + ex.Message);
            }
        }

        private async void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                if (_selectedDevice != null)
                {
                    // 验证数据
                    if (string.IsNullOrWhiteSpace(_selectedDevice.设备名称))
                    {
                        ShowWarning("设备名称不能为空");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(_selectedDevice.设备IP))
                    {
                        ShowWarning("设备IP不能为空");
                        return;
                    }
                    
                    // 检查是否存在重复的设备名称
                    var isDuplicate = _modbusDevices.Any(p => 
                        p.设备名称 == _selectedDevice.设备名称 &&
                        // 排除自身（如果是更新操作）
                        p.ID != _selectedDevice.ID);

                    if (isDuplicate)
                    {
                        ShowWarning("已存在相同名称的设备，请修改设备名称后重试。");
                        return;
                    }

                    // 保存数据
                    var result = await _modbusService.AddOrUpdateAsync(_selectedDevice);
                    if (result.Status)
                    {
                        ShowSuccess("保存成功");
                        await LoadModbusDevicesAsync();
                        btnConfirm.Enabled = false;
                        btnConfirm.Text = "确认";
                        
                        // 设置PropertyGrid为只读状态
                        SetPropertyGridReadOnly(true);
                        
                        // 刷新ModbusSystem中的数据，实现窗体间联动
                        await ModbusSystem.Instance.RefreshDataAsync();
                        
                        // 通知所有相关的ModbusControlUI控件刷新显示
                        NotifyModbusControlUIsToRefresh();
                        
                        // 自动选中刚刚保存的行
                        SelectSavedRow(_selectedDevice);
                        
                        // 如果有选中的行，重新显示选中行的数据并保持只读状态
                        if (uiDataGridView1.SelectedRows.Count > 0)
                        {
                            var selectedDevice = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDto;
                            if (selectedDevice != null)
                            {
                                propertyGrid_配置修改.SelectedObject = selectedDevice;
                                SetPropertyGridReadOnly(true);
                            }
                        }
                    }
                    else
                    {
                        ShowError("保存失败: " + result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("保存设备时发生错误: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                propertyGrid_配置修改.SelectedObject = null;
                _selectedDevice = null;
                btnConfirm.Enabled = false;
                btnConfirm.Text = "确认";
                
                // 设置PropertyGrid为只读状态
                SetPropertyGridReadOnly(true);
                
                // 如果有选中的行，重新显示选中行的数据并保持只读状态
                if (uiDataGridView1.SelectedRows.Count > 0)
                {
                    var selectedDevice = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDto;
                    if (selectedDevice != null)
                    {
                        propertyGrid_配置修改.SelectedObject = selectedDevice;
                        SetPropertyGridReadOnly(true);
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        
        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                await LoadModbusDevicesAsync();
                ShowSuccess("刷新完成");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("刷新数据时发生错误: " + ex.Message);
            }
        }
        
        // DataGridView选中行改变事件
        private void uiDataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                // 只在没有正在进行的编辑操作时更新PropertyGrid
                if (_selectedDevice == null || (btnConfirm.Enabled == false && btnConfirm.Text != "新增" && btnConfirm.Text != "保存"))
                {
                    if (uiDataGridView1.SelectedRows.Count > 0)
                    {
                        var selectedDevice = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDto;
                        if (selectedDevice != null)
                        {
                            propertyGrid_配置修改.SelectedObject = selectedDevice;
                            // 保持PropertyGrid只读状态
                            SetPropertyGridReadOnly(true);
                        }
                    }
                    else
                    {
                        propertyGrid_配置修改.SelectedObject = null;
                        // 设置PropertyGrid为只读状态
                        SetPropertyGridReadOnly(true);
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        
        // 设置PropertyGrid的只读状态
        private void SetPropertyGridReadOnly(bool isReadOnly)
        {
            try
            {
                // 直接控制PropertyGrid的只读状态
                if (isReadOnly)
                {
                    propertyGrid_配置修改.Enabled = false;
                    // 只读模式：禁用确认按钮
                    btnConfirm.Enabled = false;
                    btnConfirm.Text = "确认";
                }
                else
                {
                    // 编辑模式：启用确认按钮
                    btnConfirm.Enabled = true;
                    propertyGrid_配置修改.Enabled = true;
                }

                // 刷新PropertyGrid显示
                propertyGrid_配置修改.Refresh();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 私有方法
        private async Task LoadModbusDevicesAsync()
        {
            try
            {
                var result = await _modbusService.GetAllAsync();
                if (result.Status)
                {
                    _modbusDevices = result.Result as List<ModbusCommunicationDto> ?? new List<ModbusCommunicationDto>();
                    await RefreshDataGridViewAsync();
                }
                else
                {
                    ShowError("加载设备列表失败: " + result.Message);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("加载设备列表时发生错误: " + ex.Message);
            }
        }

        private async Task RefreshDataGridViewAsync()
        {
            uiDataGridView1.DataSource = null;
            uiDataGridView1.DataSource = _modbusDevices;
            await Task.Delay(1); // 确保UI刷新
        }

        private void SetupDataGridView()
        {
            // 设置DataGridView列标题
            if (uiDataGridView1.Columns.Count > 0)
            {
                uiDataGridView1.Columns["设备名称"].HeaderText = "设备名称";
                uiDataGridView1.Columns["设备类型"].HeaderText = "设备类型";
                uiDataGridView1.Columns["站号"].HeaderText = "站号";
                uiDataGridView1.Columns["设备IP"].HeaderText = "设备IP";
                uiDataGridView1.Columns["设备Port"].HeaderText = "端口";
                uiDataGridView1.Columns["超时"].HeaderText = "超时(毫秒)";
                uiDataGridView1.Columns["是否启用"].HeaderText = "是否启用";
                
                // 隐藏不需要显示的列
                if (uiDataGridView1.Columns.Contains("ID"))
                    uiDataGridView1.Columns["ID"].Visible = false;
                if (uiDataGridView1.Columns.Contains("创建时间"))
                    uiDataGridView1.Columns["创建时间"].Visible = false;
                if (uiDataGridView1.Columns.Contains("更新时间"))
                    uiDataGridView1.Columns["更新时间"].Visible = false;
            }
            
            // 添加DataGridView选中行改变事件处理程序
            uiDataGridView1.SelectionChanged += uiDataGridView1_SelectionChanged;
        }

        private void ShowSuccess(string message)
        {
            UIMessageTip.ShowOk(message, 1000);
        }

        private void ShowWarning(string message)
        {
            UIMessageTip.ShowWarning(message, 1000);
        }

        private void ShowError(string message)
        {
            UIMessageTip.ShowError(message, 1000);
        }

        private bool ShowAskDialog(string message)
        {
            return Frm_ConfirmBox.Show(message, "确认") == ConfirmBoxResult.Yes;
        }
        
        /// <summary>
        /// 显示修改建议
        /// </summary>
        /// <param name="device">选中的设备</param>
        private void ShowModificationSuggestions(ModbusCommunicationDto device)
        {
            try
            {
                var suggestions = new List<string>();
                
                suggestions.Add("设备配置建议:");
                suggestions.Add("- 设备名称应简洁明了，能准确描述设备功能");
                suggestions.Add("- 设备IP应确保在网络中唯一且可访问");
                suggestions.Add("- 端口号通常为502，可根据实际设备配置调整");
                suggestions.Add("- 超时时间建议设置在1000-5000毫秒之间");
                suggestions.Add("- 站号根据设备实际配置设置");
                
                // 显示建议
                var suggestionText = string.Join("\n", suggestions);
                UIMessageTip.ShowOk(suggestionText, 3000);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        
        /// <summary>
        /// 通知所有相关的ModbusControlUI控件刷新显示
        /// </summary>
        private async void NotifyModbusControlUIsToRefresh()
        {
            try
            {
                // 使用异步延迟确保保存操作完成后再刷新UI
                await Task.Delay(100);
                
                // 遍历所有打开的窗体
                foreach (Form form in Application.OpenForms)
                {
                    if (form == null) continue;
                    
                    // 递归查找所有ModbusControlUI控件
                    var modbusControls = FindControls<ModbusControlUI>(form);
                    
                    // 刷新所有控件
                    foreach (var modbusControl in modbusControls)
                    {
                        if (modbusControl != null)
                        {
                            // 使用异步方式在后台线程中执行刷新操作，避免阻塞UI线程
                            await Task.Run(() =>
                            {
                                try
                                {
                                    // 在UI线程中执行刷新操作
                                    form.Invoke(new Action(() =>
                                    {
                                        try
                                        {
                                            modbusControl.RefreshDioPoints();
                                        }
                                        catch (Exception ex)
                                        {
                                            RLog.SaveError(ex, $"刷新ModbusControlUI时发生错误: {ex.Message}");
                                        }
                                    }));
                                }
                                catch (Exception ex)
                                {
                                    RLog.SaveError(ex, "调用UI线程刷新时发生错误");
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "通知ModbusControlUI刷新时发生错误");
            }
        }
        
        /// <summary>
        /// 递归查找窗体中的指定类型控件
        /// </summary>
        /// <typeparam name="T">控件类型</typeparam>
        /// <param name="control">要查找的控件</param>
        /// <returns>找到的控件集合</returns>
        private IEnumerable<T> FindControls<T>(System.Windows.Forms.Control control) where T : System.Windows.Forms.Control
        {
            if (control == null) yield break;
            
            // 如果当前控件是指定类型，则返回它
            if (control is T tControl)
                yield return tControl;
            
            // 递归查找子控件
            if (control.Controls == null) yield break;
            
            foreach (System.Windows.Forms.Control child in control.Controls)
            {
                foreach (var foundControl in FindControls<T>(child))
                {
                    yield return foundControl;
                }
            }
        }
        
        /// <summary>
        /// 选中刚刚保存的行
        /// </summary>
        /// <param name="savedDevice">已保存的设备</param>
        private void SelectSavedRow(ModbusCommunicationDto savedDevice)
        {
            try
            {
                // 确保数据源已正确设置
                if (uiDataGridView1.DataSource is List<ModbusCommunicationDto> dataSource)
                {
                    // 查找匹配的行（通过ID匹配，如果ID为空则通过设备名称匹配）
                    ModbusCommunicationDto targetRow = null;
                    
                    // 检查ID是否有效（不为空且不为默认值）
                    if (savedDevice.ID != null && !string.IsNullOrEmpty(savedDevice.ID.ToString()) && savedDevice.ID.ToString() != "0")
                    {
                        // 优先通过ID匹配
                        targetRow = dataSource.FirstOrDefault(p => p.ID == savedDevice.ID);
                    }
                    
                    // 如果通过ID未找到或者ID为空，则通过设备名称匹配
                    if (targetRow == null)
                    {
                        targetRow = dataSource.FirstOrDefault(p => p.设备名称 == savedDevice.设备名称);
                    }
                    
                    // 如果找到了匹配的行，则选中它
                    if (targetRow != null)
                    {
                        // 找到目标行在数据源中的索引
                        int rowIndex = dataSource.IndexOf(targetRow);
                        if (rowIndex >= 0 && rowIndex < uiDataGridView1.Rows.Count)
                        {
                            // 取消当前所有选择
                            uiDataGridView1.ClearSelection();
                            
                            // 选中目标行
                            uiDataGridView1.Rows[rowIndex].Selected = true;
                            
                            // 确保选中的行在可视区域内
                            uiDataGridView1.FirstDisplayedScrollingRowIndex = rowIndex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "选中保存行时发生错误");
            }
        }
    }
}
#endregion