using Jabil.Module.Log.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.Modbus.Service;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using Jabil.Module.Modbus.Control;
using Jabil.Lib;

namespace Jabil.Module.Modbus.Frm
{
    public partial class Frm_ModbusDioManager : UIPage
    {
        private readonly ModbusCommunicationDIOService _modbusDioService;
        private List<ModbusCommunicationDioDto> _modbusDioPoints;
        private ModbusCommunicationDioDto _selectedDioPoint;
        private string _selectedDevice = ""; // 当前选中的设备
        private string _selectedPointType = ""; // 当前选中的点位类型

        public Frm_ModbusDioManager()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            
            _modbusDioService = new ModbusCommunicationDIOService();
            _modbusDioPoints = new List<ModbusCommunicationDioDto>();
        }

        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static Frm_ModbusDioManager _instance;
        public static Frm_ModbusDioManager Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_ModbusDioManager();
                return _instance;
            }
        }

        #region 事件处理
        private async void Frm_ModbusDioManager_Load(object sender, EventArgs e)
        {
            try
            {
                await LoadModbusDioPointsAsync();
                SetupTreeView();
                SetupDataGridView();
                
                // 添加DataGridView选中行改变事件处理程序
                uiDataGridView1.SelectionChanged += uiDataGridView1_SelectionChanged;
                
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
                // 获取当前选中的设备和点位类型
                string deviceName = _selectedDevice;
                string pointType = _selectedPointType;
                
                // 如果没有选中设备，则使用默认值
                if (string.IsNullOrEmpty(deviceName))
                {
                    // 尝试从树形视图中获取第一个设备作为默认设备
                    if (uiTreeView1.Nodes.Count > 0 && uiTreeView1.Nodes[0].Nodes.Count > 0)
                    {
                        var deviceNodeText = uiTreeView1.Nodes[0].Nodes[0].Text;
                        // 提取设备名称，格式为"设备名称 (输入:X, 输出:Y)"
                        var deviceNameEndIndex = deviceNodeText.IndexOf(" (");
                        if (deviceNameEndIndex > 0)
                        {
                            deviceName = deviceNodeText.Substring(0, deviceNameEndIndex);
                        }
                        else
                        {
                            deviceName = deviceNodeText;
                        }
                    }
                    else
                    {
                        deviceName = "默认设备";
                    }
                }
                
                // 如果没有选中点位类型，则默认为输入
                if (string.IsNullOrEmpty(pointType))
                {
                    pointType = "输入";
                }
                
                // 生成示例索引（确保不重复）
                string exampleIndex = GenerateUniqueIndex(deviceName, pointType);
                
                // 生成不重复的绑定代码
                string bindingCode = GenerateUniqueBindingCode(deviceName, pointType);

                var newDioPoint = new ModbusCommunicationDioDto
                {
                    设备名称 = deviceName,
                    点位名称 = $"示例{pointType}点位",
                    点位类型 = pointType == "输出" ? "输出" : "输入",
                    点位索引 = exampleIndex,
                    点位值 = 0,
                    点位定义 = $"{deviceName}的示例{pointType}点位",
                    数据类型 = "bool",
                    绑定代码 = bindingCode,
                    是否启用 = true
                };

                propertyGrid_配置修改.SelectedObject = newDioPoint;
                _selectedDioPoint = newDioPoint;
                btnConfirm.Enabled = true;
                btnConfirm.Text = "新增";
                
                // 设置PropertyGrid为可编辑状态
                SetPropertyGridReadOnly(false);
                
                // 显示新增点位的修改建议
                ShowModificationSuggestions(newDioPoint);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("添加点位时发生错误: " + ex.Message);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            try
            {
                if (uiDataGridView1.SelectedRows.Count > 0)
                {
                    var selectedDioPoint = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDioDto;
                    if (selectedDioPoint != null)
                    {
                        propertyGrid_配置修改.SelectedObject = selectedDioPoint;
                        _selectedDioPoint = selectedDioPoint;
                        btnConfirm.Enabled = true;
                        btnConfirm.Text = "保存";
                        
                        // 设置PropertyGrid为可编辑状态
                        SetPropertyGridReadOnly(false);
                        
                        // 显示修改建议
                        ShowModificationSuggestions(selectedDioPoint);
                    }
                }
                else
                {
                    ShowWarning("请选择要编辑的点位");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("编辑点位时发生错误: " + ex.Message);
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                if (uiDataGridView1.SelectedRows.Count > 0)
                {
                    var selectedDioPoint = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDioDto;
                    if (selectedDioPoint != null)
                    {
                        if (ShowAskDialog("确认删除点位: " + selectedDioPoint.点位名称 + "?"))
                        {
                            var result = await _modbusDioService.DeleteAsync(selectedDioPoint);
                            if (result.Status)
                            {
                                _modbusDioPoints.Remove(selectedDioPoint);
                                await RefreshDataGridViewAsync();
                                ShowSuccess("删除成功");
                                
                                // 刷新ModbusSystem中的数据，实现窗体间联动
                                await ModbusSystem.Instance.RefreshDataAsync();
                                
                                // 刷新树形视图
                                SetupTreeView();
                                
                                // 更新设备点位定义列表
                                UpdateDevicePointDefinitions();

                                // 更新设备点位定义列表
                                UpdateDevicePointDefinitions();

                                // 通知所有ModbusControlUI控件刷新显示
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
                    ShowWarning("请选择要删除的点位");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("删除点位时发生错误: " + ex.Message);
            }
        }

        private async void btnConfirm_Click(object sender, EventArgs e)
        {
            try
            {
                if (_selectedDioPoint != null)
                {
                    // 验证数据
                    var validationResult = _selectedDioPoint.ValidateDto();
                    if (!validationResult.IsValid)
                    {
                        ShowWarning(string.Join("\n", validationResult.Errors));
                        return;
                    }

                    // 检查是否存在重复的点位（相同设备名称、点位名称、点位类型、点位索引）
                    var isDuplicate = _modbusDioPoints.Any(p => 
                        p.设备名称 == _selectedDioPoint.设备名称 &&
                        p.点位名称 == _selectedDioPoint.点位名称 &&
                        p.点位类型 == _selectedDioPoint.点位类型 &&
                        p.点位索引 == _selectedDioPoint.点位索引 &&
                        // 排除自身（如果是更新操作）
                        p.ID != _selectedDioPoint.ID);

                    if (isDuplicate)
                    {
                        ShowWarning("在同一设备下已存在相同的点位名称、点位类型和点位索引的组合，请修改后重试。");
                        return;
                    }
                    
                    // 检查索引是否在当前设备和点位类型下重复（不考虑点位名称）
                    var isIndexDuplicate = _modbusDioPoints.Any(p => 
                        p.设备名称 == _selectedDioPoint.设备名称 &&
                        p.点位类型 == _selectedDioPoint.点位类型 &&
                        p.点位索引 == _selectedDioPoint.点位索引 &&
                        // 排除自身（如果是更新操作）
                        p.ID != _selectedDioPoint.ID);

                    if (isIndexDuplicate)
                    {
                        ShowWarning($"在设备 {_selectedDioPoint.设备名称} 的 {_selectedDioPoint.点位类型} 类型下已存在索引为 {_selectedDioPoint.点位索引} 的点位，请修改索引后重试。");
                        return;
                    }

                    // 保存数据
                    var result = await _modbusDioService.AddOrUpdateAsync(_selectedDioPoint);
                    if (result.Status)
                    {
                        ShowSuccess("保存成功");
                        await LoadModbusDioPointsAsync();
                        btnConfirm.Enabled = false;
                        btnConfirm.Text = "确认";
                        
                        // 设置PropertyGrid为只读状态
                        SetPropertyGridReadOnly(true);
                        
                        // 刷新ModbusSystem中的数据，实现窗体间联动
                        await ModbusSystem.Instance.RefreshDataAsync();
                        
                        // 刷新树形视图和数据表格
                        SetupTreeView();
                        await RefreshDataGridViewAsync();
                        
                        // 更新设备点位定义列表
                        UpdateDevicePointDefinitions();

                        // 通知所有ModbusControlUI控件刷新显示
                        NotifyModbusControlUIsToRefresh();

                        // 自动选中刚刚保存的行
                        SelectSavedRow(_selectedDioPoint);
                        
                        // 如果有选中的行，重新显示选中行的数据并保持只读状态
                        if (uiDataGridView1.SelectedRows.Count > 0)
                        {
                            var selectedDioPoint = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDioDto;
                            if (selectedDioPoint != null)
                            {
                                propertyGrid_配置修改.SelectedObject = selectedDioPoint;
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
                ShowError("保存点位时发生错误: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                propertyGrid_配置修改.SelectedObject = null;
                _selectedDioPoint = null;
                btnConfirm.Enabled = false;
                btnConfirm.Text = "确认";
                
                // 设置PropertyGrid为只读状态
                SetPropertyGridReadOnly(true);
                
                // 如果有选中的行，重新显示选中行的数据并保持只读状态
                if (uiDataGridView1.SelectedRows.Count > 0)
                {
                    var selectedDioPoint = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDioDto;
                    if (selectedDioPoint != null)
                    {
                        propertyGrid_配置修改.SelectedObject = selectedDioPoint;
                        SetPropertyGridReadOnly(true);
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        

        
        // 树形视图节点点击事件
        private void uiTreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                // 重置选择
                _selectedDevice = "";
                _selectedPointType = "";
                
                // 如果点击的是根节点"所有设备"，则显示所有点位
                if (e.Node.Text.StartsWith("所有设备"))
                {
                    _selectedDevice = "";
                    _selectedPointType = "";
                }
                // 如果点击的是设备节点
                else if (e.Node.Level == 1)
                {
                    // 从节点文本中提取设备名称（去除个数信息）
                    var deviceText = e.Node.Text;
                    // 提取设备名称，格式为"设备名称 (输入:X, 输出:Y)"
                    var deviceNameEndIndex = deviceText.IndexOf(" (");
                    if (deviceNameEndIndex > 0)
                    {
                        _selectedDevice = deviceText.Substring(0, deviceNameEndIndex);
                    }
                    else
                    {
                        _selectedDevice = deviceText;
                    }
                    _selectedPointType = "";
                }
                // 如果点击的是点位类型节点（输入/输出）
                else if (e.Node.Level == 2)
                {
                    // 获取父节点（设备节点）
                    var deviceNode = e.Node.Parent;
                    var deviceText = deviceNode.Text;
                    // 提取设备名称，格式为"设备名称 (输入:X, 输出:Y)"
                    var deviceNameEndIndex = deviceText.IndexOf(" (");
                    if (deviceNameEndIndex > 0)
                    {
                        _selectedDevice = deviceText.Substring(0, deviceNameEndIndex);
                    }
                    else
                    {
                        _selectedDevice = deviceText;
                    }
                    
                    // 设置点位类型
                    if (e.Node.Text.StartsWith("输入"))
                    {
                        _selectedPointType = "输入";
                    }
                    else if (e.Node.Text.StartsWith("输出"))
                    {
                        _selectedPointType = "输出";
                    }
                }
                
               RefreshDataGridViewAsync();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("切换设备时发生错误: " + ex.Message);
            }
        }
        
        // DataGridView选中行改变事件
        private void uiDataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                // 只在没有正在进行的编辑操作时更新PropertyGrid
                if (_selectedDioPoint == null || (btnConfirm.Enabled == false && btnConfirm.Text != "新增" && btnConfirm.Text != "保存"))
                {
                    if (uiDataGridView1.SelectedRows.Count > 0)
                    {
                        var selectedDioPoint = uiDataGridView1.SelectedRows[0].DataBoundItem as ModbusCommunicationDioDto;
                        if (selectedDioPoint != null)
                        {
                            propertyGrid_配置修改.SelectedObject = selectedDioPoint;
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
        private async Task LoadModbusDioPointsAsync()
        {
            try
            {
                var result = await _modbusDioService.GetAllAsync();
                if (result.Status)
                {
                    _modbusDioPoints = result.Result as List<ModbusCommunicationDioDto> ?? new List<ModbusCommunicationDioDto>();
                    await RefreshDataGridViewAsync();
                    
                    // 更新设备点位定义列表
                    UpdateDevicePointDefinitions();
                    
                    // 更新树形视图
                    SetupTreeView();
                }
                else
                {
                    ShowError("加载点位列表失败: " + result.Message);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("加载点位列表时发生错误: " + ex.Message);
            }
        }

        // 设置树形视图
        private void SetupTreeView()
        {
            try
            {
                uiTreeView1.Nodes.Clear();
                
                // 添加根节点
                var rootNode = new TreeNode("所有设备 (" + _modbusDioPoints.Count + ")");
                rootNode.NodeFont = new Font("微软雅黑", 12, FontStyle.Bold);
                rootNode.ForeColor = Color.White;
                
                // 按设备名称分组
                var devices = _modbusDioPoints
                    .GroupBy(p => p.设备名称)
                    .Select(g => new { 
                        DeviceName = g.Key, 
                        Points = g.ToList()
                    })
                    .OrderBy(g => g.DeviceName)
                    .ToList();
                
                // 添加设备节点，并在每个设备下按点位类型分类
                foreach (var device in devices)
                {
                    var devicePoints = device.Points;
                    var diPoints = devicePoints.Where(p => p.点位类型 == "输入").ToList();
                    var doPoints = devicePoints.Where(p => p.点位类型 == "输出").ToList();
                    
                    // 在设备节点名称中显示输入和输出点位的个数
                    var deviceNode = new TreeNode($"{device.DeviceName} (输入:{diPoints.Count}, 输出:{doPoints.Count})");
                    deviceNode.NodeFont = new Font("微软雅黑", 11, FontStyle.Regular);
                    deviceNode.ForeColor = Color.LightGray;
                    
                    // 添加DI子节点
                    if (diPoints.Count > 0)
                    {
                        var diNode = new TreeNode($"输入 ({diPoints.Count})");
                        diNode.NodeFont = new Font("微软雅黑", 10, FontStyle.Regular);
                        diNode.ForeColor = Color.LightBlue;
                        deviceNode.Nodes.Add(diNode);
                    }
                    
                    // 添加DO子节点
                    if (doPoints.Count > 0)
                    {
                        var doNode = new TreeNode($"输出 ({doPoints.Count})");
                        doNode.NodeFont = new Font("微软雅黑", 10, FontStyle.Regular);
                        doNode.ForeColor = Color.LightCoral;
                        deviceNode.Nodes.Add(doNode);
                    }
                    
                    rootNode.Nodes.Add(deviceNode);
                }
                
                uiTreeView1.Nodes.Add(rootNode);
                rootNode.Expand();
                
                // 默认选中根节点
                _selectedDevice = "";
                _selectedPointType = "";
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("设置设备树时发生错误: " + ex.Message);
            }
        }

        private async Task RefreshDataGridViewAsync()
        {
            try
            {
                uiDataGridView1.DataSource = null;
                
                // 根据选中的设备和点位类型过滤数据
                List<ModbusCommunicationDioDto> filteredPoints;
                if (string.IsNullOrEmpty(_selectedDevice))
                {
                    // 如果没有选中设备，显示所有点位
                    filteredPoints = _modbusDioPoints;
                }
                else
                {
                    // 如果选中了设备，先按设备过滤
                    filteredPoints = _modbusDioPoints.Where(p => p.设备名称 == _selectedDevice).ToList();
                    
                    // 如果还选中了点位类型，再按点位类型过滤
                    if (!string.IsNullOrEmpty(_selectedPointType))
                    {
                        filteredPoints = filteredPoints.Where(p => p.点位类型 == _selectedPointType).ToList();
                    }
                }
                
                // 按设备名称和点位类型排序
                uiDataGridView1.DataSource = filteredPoints
                    .OrderBy(p => p.设备名称)
                    .ThenBy(p => p.点位类型)
                    .ThenBy(p => p.点位索引)
                    .ToList();
                
                // 设置DataGridView属性
                uiDataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                uiDataGridView1.MultiSelect = false;
                uiDataGridView1.ReadOnly = true;
                SetupDataGridView();
                await Task.Delay(1); // 确保UI刷新
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("刷新数据表格时发生错误: " + ex.Message);
            }
        }

        private void SetupDataGridView()
        {
            try
            {
                // 设置DataGridView列标题
                if (uiDataGridView1.Columns.Count > 0)
                {
                    // 只显示指定的列
                    uiDataGridView1.Columns["点位名称"].HeaderText = "点位名称";
                    uiDataGridView1.Columns["点位类型"].HeaderText = "点位类型";
                    uiDataGridView1.Columns["点位定义"].HeaderText = "点位定义";
                    
                    // 调整列宽以适应内容
                    uiDataGridView1.Columns["点位名称"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    uiDataGridView1.Columns["点位类型"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    uiDataGridView1.Columns["点位定义"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    
                    // 设置最小宽度，确保内容可读
                    if (uiDataGridView1.Columns["点位名称"].Width < 150)
                        uiDataGridView1.Columns["点位名称"].Width = 150;
                    if (uiDataGridView1.Columns["点位定义"].Width < 200)
                        uiDataGridView1.Columns["点位定义"].Width = 200;
                    
                    // 隐藏其他列
                    if (uiDataGridView1.Columns.Contains("点位索引"))
                        uiDataGridView1.Columns["点位索引"].Visible = false;
                    if (uiDataGridView1.Columns.Contains("点位值"))
                        uiDataGridView1.Columns["点位值"].Visible = false;
                    if (uiDataGridView1.Columns.Contains("数据类型"))
                        uiDataGridView1.Columns["数据类型"].Visible = false;
                    if (uiDataGridView1.Columns.Contains("绑定代码"))
                        uiDataGridView1.Columns["绑定代码"].Visible = false;
                    if (uiDataGridView1.Columns.Contains("是否启用"))
                        uiDataGridView1.Columns["是否启用"].Visible = false;
                    if (uiDataGridView1.Columns.Contains("设备名称"))
                        uiDataGridView1.Columns["设备名称"].Visible = false;
                    
                    // 隐藏系统列
                    if (uiDataGridView1.Columns.Contains("ID"))
                        uiDataGridView1.Columns["ID"].Visible = false;
                    if (uiDataGridView1.Columns.Contains("创建时间"))
                        uiDataGridView1.Columns["创建时间"].Visible = false;
                    if (uiDataGridView1.Columns.Contains("更新时间"))
                        uiDataGridView1.Columns["更新时间"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowError("设置数据表格时发生错误: " + ex.Message);
            }
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
        /// <param name="dioPoint">选中的DIO点位</param>
        private void ShowModificationSuggestions(ModbusCommunicationDioDto dioPoint)
        {
            try
            {
                var suggestions = new List<string>();
                
                // 根据点位类型提供修改建议
                if (dioPoint.点位类型 == "输入")
                {
                    suggestions.Add("输入点位(DI)建议:");
                    suggestions.Add("- 点位索引应以0或1开头");
                    suggestions.Add("- 数据类型通常为bool");
                }
                else if (dioPoint.点位类型 == "输出")
                {
                    suggestions.Add("输出点位(DO)建议:");
                    suggestions.Add("- 点位索引应以0或1开头");
                    suggestions.Add("- 数据类型通常为bool");
                }
                
                // 通用建议
                suggestions.Add("通用建议:");
                suggestions.Add("- 点位名称应简洁明了，能准确描述点位功能");
                suggestions.Add("- 点位定义应详细描述点位的具体用途");
                suggestions.Add("- 绑定代码可用于关联业务逻辑");
                
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
        /// 生成指定设备和点位类型下的唯一索引
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="pointType">点位类型</param>
        /// <returns>唯一的点位索引</returns>
        private string GenerateUniqueIndex(string deviceName, string pointType)
        {
            try
            {
                // 获取当前设备和点位类型下的所有点位
                var existingPoints = _modbusDioPoints
                    .Where(p => p.设备名称 == deviceName && p.点位类型 == pointType)
                    .ToList();
                
                // 如果没有现有点位，则使用默认索引
                if (existingPoints.Count == 0)
                {
                    return "1";
                }
                
                // 查找最大的索引值并加1
                int maxIndex = 1;
                foreach (var point in existingPoints)
                {
                    if (int.TryParse(point.点位索引, out int index))
                    {
                        if (index >= maxIndex)
                        {
                            maxIndex = index + 1;
                        }
                    }
                }
                
                return maxIndex.ToString();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                // 出现异常时返回默认索引
                return "1";
            }
        }
        
        /// <summary>
        /// 生成指定设备和点位类型下的唯一绑定代码
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="pointType">点位类型</param>
        /// <returns>唯一的绑定代码</returns>
        private string GenerateUniqueBindingCode(string deviceName, string pointType)
        {
            try
            {
                // 获取当前设备和点位类型下的所有点位
                var existingPoints = _modbusDioPoints
                    .Where(p => p.设备名称 == deviceName && p.点位类型 == pointType)
                    .ToList();
                
                // 生成基础代码前缀
                string prefix = $"{deviceName.Substring(0, Math.Min(3, deviceName.Length))}{pointType.Substring(0, 1)}";
                
                // 如果没有现有点位，则使用默认代码
                if (existingPoints.Count == 0)
                {
                    return $"{prefix}001";
                }
                
                // 查找最大的序号并加1
                int maxNumber = 1;
                foreach (var point in existingPoints)
                {
                    string bindingCode = point.绑定代码;
                    if (bindingCode.StartsWith(prefix) && bindingCode.Length > prefix.Length)
                    {
                        string numberPart = bindingCode.Substring(prefix.Length);
                        if (int.TryParse(numberPart, out int number))
                        {
                            if (number >= maxNumber)
                            {
                                maxNumber = number + 1;
                            }
                        }
                    }
                }
                
                return $"{prefix}{maxNumber:D3}";
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                // 出现异常时返回默认代码
                return "DEF001";
            }
        }
        
        /// <summary>
        /// 更新设备点位定义列表
        /// </summary>
        private void UpdateDevicePointDefinitions()
        {
            try
            {
                // 按设备名称分组点位定义
                var devicePointDefinitions = _modbusDioPoints
                    .Where(p => !string.IsNullOrEmpty(p.设备名称) && !string.IsNullOrEmpty(p.点位定义))
                    .GroupBy(p => p.设备名称)
                    .ToDictionary(g => g.Key, g => g.Select(p => p.点位定义).Distinct().ToList());

                // 更新转换器中的设备点位定义
                foreach (var kvp in devicePointDefinitions)
                {
                    PointDefinitionConverter.SetDevicePointDefinitions(kvp.Key, kvp.Value);
                }
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
                    
                    // 只刷新与当前保存的设备相关的控件
                    foreach (var modbusControl in modbusControls)
                    {
                        if (modbusControl != null  && 
                            modbusControl.DeviceName == _selectedDioPoint.设备名称)
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
        /// <param name="savedDioPoint">已保存的DIO点位</param>
        private void SelectSavedRow(ModbusCommunicationDioDto savedDioPoint)
        {
            try
            {
                // 确保数据源已正确设置
                if (uiDataGridView1.DataSource is List<ModbusCommunicationDioDto> dataSource)
                {
                    // 查找匹配的行（通过ID匹配，如果ID为空则通过设备名称、点位名称、点位类型和点位索引匹配）
                    ModbusCommunicationDioDto targetRow = null;
                    
                    // 检查ID是否有效（不为空且不为默认值）
                    if (savedDioPoint.ID != null && !string.IsNullOrEmpty(savedDioPoint.ID.ToString()) && savedDioPoint.ID.ToString() != "0")
                    {
                        // 优先通过ID匹配
                        targetRow = dataSource.FirstOrDefault(p => p.ID == savedDioPoint.ID);
                    }
                    
                    // 如果通过ID未找到或者ID为空，则通过其他属性匹配
                    if (targetRow == null)
                    {
                        targetRow = dataSource.FirstOrDefault(p => 
                            p.设备名称 == savedDioPoint.设备名称 &&
                            p.点位名称 == savedDioPoint.点位名称 &&
                            p.点位类型 == savedDioPoint.点位类型 &&
                            p.点位索引 == savedDioPoint.点位索引);
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