using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ToolBlock;
using Jabil.Module.Vision.Common.Dtos;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Vision.Frm
{
    /// <summary>
    /// 视觉系统配置页面
    /// 功能：管理视觉检测工具和检测点位的配置信息
    /// 包括添加、修改、删除工具和检测点位，以及相关文件操作
    /// </summary>
    public partial class Frm_VPPSetting : UIPage
    {
        #region 常量与路径定义
        /// <summary>
        /// 配置文件相对路径
        /// </summary>
        private const string BaseConfigRelativePath = "Config/Project/Vision/Job/Test/Program";

        /// <summary>
        /// 工具类型常量定义
        /// </summary>
        private const string ToolAcqFifo = "CogAcqFifoTool";
        private const string ToolCalibNPoint = "CogCalibNPointToNPointTool";
        private const string ToolCalibCheckerboard = "CogCalibCheckerboardTool";
        private const string ToolBlock = "CogToolBlockTool";

        /// <summary>
        /// 配置文件绝对路径
        /// </summary>
        private string BaseConfigPath => Path.Combine(Application.StartupPath, BaseConfigRelativePath);
        #endregion

        #region 单例模式实现
        /// <summary>
        /// 窗体单例实例
        /// </summary>
        private static Frm_VPPSetting _instance;

        /// <summary>
        /// 线程同步锁
        /// </summary>
        private static readonly object _syncLock = new object();

        /// <summary>
        /// 单例实例访问属性
        /// 采用双重检查锁定确保线程安全
        /// </summary>
        public static Frm_VPPSetting Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    lock (_syncLock)
                    {
                        if (_instance == null || _instance.IsDisposed)
                        {
                            // 确保在UI线程创建实例
                            _instance = InvokeOnUiThread(() => new Frm_VPPSetting());
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 私有字段
        /// <summary>
        /// 工具类型与实例的映射字典
        /// 用于替代switch语句，提高可维护性
        /// </summary>
        private readonly Dictionary<string, Func<object>> _toolTypeMappings;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 构造函数
        /// </summary>
        public Frm_VPPSetting()
        {
            InitializeComponent();
            _toolTypeMappings = InitToolTypeMappings();
            // 异步初始化数据网格，避免UI阻塞
            _ = InitDataGridViewsAsync();
        }

        /// <summary>
        /// 初始化工具类型映射
        /// </summary>
        private Dictionary<string, Func<object>> InitToolTypeMappings()
        {
            return new Dictionary<string, Func<object>>
            {
                { ToolAcqFifo, () => new CogAcqFifoTool() },
                { ToolCalibNPoint, () => new CogCalibNPointToNPointTool() },
                { ToolCalibCheckerboard, () => new CogCalibCheckerboardTool() },
                { ToolBlock, () => new CogToolBlock() }
            };
        }

        /// <summary>
        /// 异步初始化数据网格视图
        /// </summary>
        private async Task InitDataGridViewsAsync()
        {
            try
            {
                // 安全访问VisionSystem实例
                if (VisionSystem.Instance != null)
                {
                    await VisionSystem.Instance.RefreshInspectionGridViewAsync(DataGridView_VisionInspection, 150)
                        .ConfigureAwait(true);
                    await VisionSystem.Instance.RefreshToolGridViewAsync(DataGridView_VisionTool, 240)
                        .ConfigureAwait(true);
                }
                else
                {
                    ShowWarning("视觉系统实例未初始化，数据加载失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"数据网格初始化失败：{ex.Message}");
            }
        }
        #endregion

        #region 路径与文件辅助方法
        /// <summary>
        /// 获取工具类型对应的目录路径
        /// </summary>
        /// <param name="toolType">工具类型</param>
        /// <returns>工具目录路径</returns>
        private string GetToolDirectory(string toolType)
        {
            if (string.IsNullOrWhiteSpace(toolType))
                throw new ArgumentException("工具类型不能为空", nameof(toolType));

            return Path.Combine(BaseConfigPath, toolType);
        }

        /// <summary>
        /// 获取工具文件路径
        /// </summary>
        /// <param name="toolType">工具类型</param>
        /// <param name="toolName">工具名称</param>
        /// <returns>工具文件完整路径</returns>
        private string GetToolFilePath(string toolType, string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                throw new ArgumentException("工具名称不能为空", nameof(toolName));

            return Path.Combine(GetToolDirectory(toolType), $"{toolName}.vpp");
        }

        /// <summary>
        /// 验证VisionSystem实例是否有效
        /// </summary>
        /// <returns>有效返回true，否则返回false并显示错误</returns>
        private bool ValidateVisionSystemInstance()
        {
            if (VisionSystem.Instance == null)
            {
                ShowError("视觉系统实例未初始化");
                return false;
            }

            if (VisionSystem.Instance.ListVisionTool == null)
            {
                ShowError("视觉工具列表未初始化");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取DataGridView选中行的工具信息
        /// </summary>
        private bool TryGetSelectedToolInfo(DataGridView dataGridView, out string toolName, out string toolType, out int index)
        {
            toolName = null;
            toolType = null;
            index = -1;

            if (dataGridView.SelectedRows.Count == 0)
            {
                ShowWarning("请选择要操作的工具");
                return false;
            }

            var selectedRow = dataGridView.SelectedRows[0];

            // 使用列名访问，避免硬编码索引导致的维护问题
            toolName = selectedRow.Cells[nameof(VisionToolDto.检测工具名称)]?.Value?.ToString();
            toolType = selectedRow.Cells[nameof(VisionToolDto.检测工具类型)]?.Value?.ToString();

            if (!int.TryParse(selectedRow.Cells[nameof(VisionToolDto.检测工具索引)]?.Value?.ToString(), out index))
            {
                ShowError("工具索引格式错误");
                return false;
            }

            if (string.IsNullOrWhiteSpace(toolName) || string.IsNullOrWhiteSpace(toolType))
            {
                ShowError("工具信息不完整");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取DataGridView选中行的检测点位信息
        /// </summary>
        private bool TryGetSelectedInspectionInfo(out int index)
        {
            index = -1;

            if (DataGridView_VisionInspection.SelectedRows.Count == 0)
            {
                ShowWarning("请选择要操作的检测点位");
                return false;
            }

            var selectedRow = DataGridView_VisionInspection.SelectedRows[0];
            if (!int.TryParse(selectedRow.Cells[nameof(VisionInspectionDto.检测点位索引)]?.Value?.ToString(), out index))
            {
                ShowError("检测点位索引格式错误");
                return false;
            }

            return true;
        }
        #endregion

        #region UI辅助方法
        /// <summary>
        /// 在UI线程执行操作
        /// </summary>
        private static T InvokeOnUiThread<T>(Func<T> action)
        {
            if (Application.OpenForms.Count > 0 && Application.OpenForms[0].InvokeRequired)
            {
                return (T)Application.OpenForms[0].Invoke(action);
            }
            return action();
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        private void ShowError(string message) => UIMessageBox.ShowError2(message);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        private void ShowWarning(string message) => UIMessageBox.ShowWarning(message);

        /// <summary>
        /// 显示信息消息
        /// </summary>
        private void ShowInfo(string message, int delay = 3) => UIMessageBox.ShowInfo2(message, false, delay);
        #endregion

        #region 事件处理
        private void DataGridView_VisionTool_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // 空实现保持
        }

        private async void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (!(sender is ToolStripMenuItem menuItem))
                {
                    ShowError("无效的菜单操作");
                    return;
                }

                // 使用字典委托替代switch，提高可维护性
                var actionMap = new Dictionary<string, Func<Task>>
                {
                    { "添加", HandleAddToolAsync },
                    { "修改", HandleModifyToolAsync },
                    { "删除", HandleDeleteToolAsync }
                };

                if (actionMap.TryGetValue(menuItem.Text, out var action))
                {
                    await action().ConfigureAwait(true);
                }
                else
                {
                    ShowWarning("未实现的操作");
                }
            }
            catch (Exception ex)
            {
                ShowError($"操作失败：{ex.Message}");
            }
        }

        private async void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!(sender is ToolStripMenuItem menuItem))
                {
                    ShowError("无效的菜单操作");
                    return;
                }

                var actionMap = new Dictionary<string, Func<Task>>
                {
                    { "添加", HandleAddInspectionAsync },
                    { "修改", HandleModifyInspectionAsync },
                    { "删除", HandleDeleteInspectionAsync }
                };

                if (actionMap.TryGetValue(menuItem.Text, out var action))
                {
                    await action().ConfigureAwait(true);
                }
                else
                {
                    ShowWarning("未实现的操作");
                }
            }
            catch (Exception ex)
            {
                ShowError($"操作失败：{ex.Message}");
            }
        }

        private void uiContextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {
            // 空实现保持
        }
        #endregion

        #region 工具操作
        /// <summary>
        /// 处理添加工具操作
        /// </summary>
        private async Task HandleAddToolAsync()
        {
            try
            {
                var editForm = Frm_VpToolEdit.Instance;
                if (editForm == null)
                {
                    ShowError("工具编辑窗体初始化失败");
                    return;
                }

                editForm.Text = "添加工具";
                editForm.Refresh();

                if (editForm.ShowDialog() != DialogResult.OK)
                    return;

                if (!ValidateVisionSystemInstance())
                    return;

                // 检查工具是否已存在
                var existingTool = VisionSystem.Instance.ListVisionTool
                    .FirstOrDefault(item => item != null
                        && item.检测工具类型 == editForm.ToolType
                        && item.检测工具名称 == editForm.ToolName);

                if (existingTool != null)
                {
                    ShowError("工具名称已存在！");
                    return;
                }

                // 创建工具目录
                string toolDir = GetToolDirectory(editForm.ToolType);
                try
                {
                    if (!Directory.Exists(toolDir))
                    {
                        Directory.CreateDirectory(toolDir);
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"创建工具目录失败：{ex.Message}");
                    return;
                }

                // 创建新工具DTO
                var newTool = new VisionToolDto
                {
                    检测工具名称 = editForm.ToolName,
                    检测工具类型 = editForm.ToolType,
                    检测工具索引 = VisionSystem.Instance.ListVisionTool
                        .Count(t => t != null && t.检测工具类型 == editForm.ToolType),
                    检测工具路径 = Path.Combine("Program", editForm.ToolType)
                };

                // 保存工具并刷新界面（等待异步操作完成）
                await VisionSystem.Instance.AddOrUpdateVisionToolAsync(newTool)
                    .ConfigureAwait(true);
                await VisionSystem.Instance.RefreshToolGridViewAsync(DataGridView_VisionTool, 240)
                    .ConfigureAwait(true);
                SaveToolToFile(newTool, toolDir);

                ShowInfo("添加成功！");
            }
            catch (Exception ex)
            {
                ShowError($"添加工具失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 处理修改工具操作
        /// </summary>
        private async Task HandleModifyToolAsync()
        {
            try
            {
                if (!TryGetSelectedToolInfo(DataGridView_VisionTool, out string toolName, out string toolType, out int index))
                    return;

                if (!ValidateVisionSystemInstance())
                    return;

                // 查找对应的工具DTO
                var dto = VisionSystem.Instance.ListVisionTool
                    .FirstOrDefault(item => item != null
                        && item.检测工具类型 == toolType
                        && item.检测工具索引 == index);

                if (dto == null)
                {
                    ShowWarning("未找到对应工具");
                    return;
                }

                // 显示编辑窗体
                var editForm = Frm_VpToolEdit.Instance;
                editForm.Text = "修改工具";
                editForm.Refresh();

                if (editForm.ShowDialog() != DialogResult.OK)
                    return;

                // 处理工具文件重命名
                string toolDir = GetToolDirectory(editForm.ToolType);
                Directory.CreateDirectory(toolDir);

                var visionTool = VisionSystem.Instance.ListVisionTool
                    .FirstOrDefault(item => item != null
                        && item.检测工具类型 == editForm.ToolType
                        && item.检测工具索引 == index);

                if (visionTool == null)
                    return;

                if (visionTool.检测工具名称 != editForm.ToolName)
                {
                    string oldPath = GetToolFilePath(toolType, visionTool.检测工具名称);
                    string newPath = GetToolFilePath(toolType, editForm.ToolName);

                    if (File.Exists(oldPath))
                    {
                        try
                        {
                            // 处理文件已存在的情况
                            if (File.Exists(newPath))
                            {
                                File.Delete(newPath);
                            }
                            File.Move(oldPath, newPath);
                        }
                        catch (Exception ex)
                        {
                            ShowError($"文件重命名失败：{ex.Message}");
                            return;
                        }
                    }

                    visionTool.检测工具名称 = editForm.ToolName;
                    await VisionSystem.Instance.AddOrUpdateVisionToolAsync(visionTool)
                        .ConfigureAwait(true);
                    await VisionSystem.Instance.RefreshToolGridViewAsync(DataGridView_VisionTool, 240)
                        .ConfigureAwait(true);
                    ShowInfo("修改成功！", 5);
                }
            }
            catch (Exception ex)
            {
                ShowError($"修改工具失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 处理删除工具操作
        /// </summary>
        private async Task HandleDeleteToolAsync()
        {
            if (DataGridView_VisionTool.SelectedRows.Count == 0 || DataGridView_VisionTool.SelectedRows[0].Index < 0)
            {
                ShowWarning("请先选中要删除的工具");
                return;
            }

            if (!UIMessageBox.Show("确认删除当前选中工具？", "提示", UIStyle.Blue, UIMessageBoxButtons.OKCancel))
                return;

            if (!ValidateVisionSystemInstance())
                return;

            foreach (DataGridViewRow selectedRow in DataGridView_VisionTool.SelectedRows)
            {
                string toolName = selectedRow.Cells[nameof(VisionToolDto.检测工具名称)]?.Value?.ToString();
                string toolType = selectedRow.Cells[nameof(VisionToolDto.检测工具类型)]?.Value?.ToString();

                if (string.IsNullOrWhiteSpace(toolName) || string.IsNullOrWhiteSpace(toolType))
                {
                    ShowError("选中的工具信息不完整");
                    continue;
                }

                // 查找要删除的工具
                var toolsToDelete = VisionSystem.Instance.ListVisionTool
                    .Where(t => t != null
                        && t.检测工具名称 == toolName
                        && t.检测工具类型 == toolType)
                    .ToList();

                if (toolsToDelete.Count == 0)
                {
                    ShowWarning("未找到要删除的工具");
                    continue;
                }

                // 删除工具记录
                await VisionSystem.Instance.DeleteVisionToolsAsync(toolsToDelete)
                    .ConfigureAwait(true);

                // 删除工具文件
                string toolPath = GetToolFilePath(toolType, toolName);
                if (File.Exists(toolPath))
                {
                    try
                    {
                        File.Delete(toolPath);
                    }
                    catch (Exception ex)
                    {
                        ShowError($"文件删除失败：{ex.Message}");
                    }
                }
            }

            // 刷新数据
            await VisionSystem.Instance.RefreshToolGridViewAsync(DataGridView_VisionTool, 240)
                .ConfigureAwait(true);
            ShowInfo("删除成功", 5);
        }

        /// <summary>
        /// 将工具保存到文件
        /// </summary>
        /// <param name="tool">工具DTO</param>
        /// <param name="directory">保存目录</param>
        private void SaveToolToFile(VisionToolDto tool, string directory)
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool), "工具信息不能为空");

            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("保存目录不能为空", nameof(directory));

            // 注意：BinaryFormatter在.NET 5+中已过时，存在安全风险
            // 如需替换，可考虑使用CogSerializer的其他序列化方式
            var formatterType = typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter);
            string savePath = GetToolFilePath(tool.检测工具类型, tool.检测工具名称);

            // 使用字典映射替代switch语句，提高可维护性
            if (_toolTypeMappings.TryGetValue(tool.检测工具类型, out var toolCreator))
            {
                object toolInstance = toolCreator();

                if (!File.Exists(savePath) && toolInstance != null)
                {
                    try
                    {
                        CogSerializer.SaveObjectToFile(toolInstance, savePath, formatterType,
                                                     CogSerializationOptionsConstants.Results);
                    }
                    catch (Exception ex)
                    {
                        ShowError($"工具保存失败：{ex.Message}");
                    }
                }
            }
            else
            {
                throw new NotSupportedException($"不支持的工具类型：{tool.检测工具类型}");
            }
        }
        #endregion

        #region 检测点位操作
        /// <summary>
        /// 处理添加检测点位操作
        /// </summary>
        private async Task HandleAddInspectionAsync()
        {
            var editForm = Frm_InspectionEdit.Instance;
            if (editForm == null)
            {
                ShowError("检测编辑窗体初始化失败");
                return;
            }

            editForm.Text = "添加Inspection";
            editForm.RefreshData();

            if (editForm.ShowDialog() != DialogResult.OK)
                return;

            if (VisionSystem.Instance != null)
            {
                await VisionSystem.Instance.AddOrUpdateVisionInspectionAsync(editForm.InspectionDto)
                    .ConfigureAwait(true);
                await VisionSystem.Instance.RefreshInspectionGridViewAsync(DataGridView_VisionInspection, 240)
                    .ConfigureAwait(true);
                ShowInfo("添加成功", 5);
            }
            else
            {
                ShowError("视觉系统实例未初始化，无法保存数据");
            }
        }

        /// <summary>
        /// 处理修改检测点位操作
        /// </summary>
        private async Task HandleModifyInspectionAsync()
        {
            if (!TryGetSelectedInspectionInfo(out int index))
                return;

            if (VisionSystem.Instance == null || VisionSystem.Instance.ListVisionInspection == null)
            {
                ShowError("视觉系统实例未初始化");
                return;
            }

            var dto = VisionSystem.Instance.ListVisionInspection
                .FirstOrDefault(item => item != null && item.检测点位索引 == index);

            if (dto == null)
            {
                ShowWarning("未找到对应的检测点位");
                return;
            }

            var editForm = Frm_InspectionEdit.Instance;
            editForm.Text = "修改Inspection";
            editForm.RefreshData(dto);

            if (editForm.ShowDialog() != DialogResult.OK)
                return;

            await VisionSystem.Instance.AddOrUpdateVisionInspectionAsync(editForm.InspectionDto)
                .ConfigureAwait(true);
            await VisionSystem.Instance.RefreshInspectionGridViewAsync(DataGridView_VisionInspection, 240)
                .ConfigureAwait(true);
            ShowInfo("修改成功", 5);
        }

        /// <summary>
        /// 处理删除检测点位操作
        /// </summary>
        private async Task HandleDeleteInspectionAsync()
        {
            if (!UIMessageBox.Show("确认删除当前选中项？", "提示", UIStyle.Blue, UIMessageBoxButtons.OKCancel))
                return;

            if (VisionSystem.Instance == null || VisionSystem.Instance.ListVisionInspection == null)
            {
                ShowError("视觉系统实例未初始化");
                return;
            }

            foreach (DataGridViewRow row in DataGridView_VisionInspection.SelectedRows)
            {
                if (!int.TryParse(row.Cells[nameof(VisionInspectionDto.检测点位索引)]?.Value?.ToString(), out int selectedIndex))
                    continue;

                var toDelete = VisionSystem.Instance.ListVisionInspection
                    .Where(item => item != null && item.检测点位索引 == selectedIndex);

                await VisionSystem.Instance.DeleteVisionInspectionsAsync(toDelete)
                    .ConfigureAwait(true);
            }

            await VisionSystem.Instance.RefreshInspectionGridViewAsync(DataGridView_VisionInspection, 240)
                .ConfigureAwait(true);
            ShowInfo("删除成功", 5);
        }
        #endregion
    }
}
