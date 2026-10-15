using BasicClassLibrary;
using BasicClassLibrary.RexHelps;
using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.CalibFix;
using Jabil.Module.Log.Lib;
using Jabil.Lib;
using Sunny.UI;
using Jabil.Module.Log.Frm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Jabil.Module.Vision.Common.Dtos;

namespace Jabil.Module.Vision.Frm
{
    /// <summary>
    /// 视觉工具管理窗体
    /// 主要功能：
    /// 1. 管理Cognex VisionPro视觉工具（加载、配置、保存）
    /// 2. 图像回放与分析（支持单张/批量图像处理、分类）
    /// 3. 提供用户交互界面，用于视觉系统参数调整与测试
    /// </summary>
    public partial class Frm_VPTool : UIPage
    {
        #region 常量与字段定义
        // 视觉工具类型常量 - 用于区分不同类型的VisionPro工具
        private const string ToolTypeToolBlock = "CogToolBlockTool";           // 工具块工具
        private const string ToolTypeAcqFifo = "CogAcqFifoTool";               // 图像采集工具
        private const string ToolTypeCalibNPoint = "CogCalibNPointToNPointTool"; // 九点标定工具
        private const string ToolTypeCalibCheckerboard = "CogCalibCheckerboardTool"; // 棋盘格标定工具

        // 图像文件后缀常量 - 用于筛选支持的图像文件类型
        private static readonly HashSet<string> ImageExtensions = new HashSet<string> { ".bmp", ".jpg" };

        // 单例模式相关字段 - 确保整个应用中只有一个窗体实例
        private static Frm_VPTool _instance;
        private static readonly object _instanceLock = new object(); // 线程安全锁

        // 路径相关字段 - 视觉作业文件的基础路径
        private readonly string _visionJobBasePath;

        // 图像回放相关字段
        private List<string> _playbackImageFiles = new List<string>(); // 回放图像文件列表
        private string _playbackFolder = "";                           // 回放文件夹路径
        private string _currentPlaybackFile;                           // 当前播放的图像文件
        private bool _isPlaybackAllowed = false;                       // 是否允许回放
        private bool _isPlaybackActive = false;                        // 是否正在回放
        private int _playbackImageCount;                               // 回放图像总数
        private int _playbackImageIndex;                               // 当前回放图像索引
        private int _playbackDelay = 200;                              // 回放间隔(毫秒)
        private bool _isClassificationEnabled = false;                 // 是否启用图像分类(OK/NG)
        private bool _stopOnNg = false;                                // 是否在NG时停止回放
        private bool _playOriginalImage = false;                       // 是否使用原图跑图
        private bool _playNPointCalibImage = false;                    // 是否使用九点标定后图像跑图
        private bool _playCheckerboardImage = false;                   // 是否使用棋盘格标定后图像跑图
        private bool _lastCamResult = false;                           // 上一次检测结果(OK/NG)
        #endregion

        #region 单例属性
        /// <summary>
        /// 窗体单例实例
        /// 采用双重检查锁定(Double-Checked Locking)实现线程安全的单例模式
        /// 确保在多线程环境下不会创建多个窗体实例
        /// </summary>
        public static Frm_VPTool Instance
        {
            get
            {
                // 第一次检查，避免每次获取实例都加锁
                if (_instance == null || _instance.IsDisposed)
                {
                    // 加锁确保线程安全
                    lock (_instanceLock)
                    {
                        // 第二次检查，防止在等待锁的过程中实例已被创建
                        if (_instance == null || _instance.IsDisposed)
                        {
                            _instance = new Frm_VPTool();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 构造函数
        /// 初始化UI组件和基础路径
        /// 设置双缓冲以减少绘图闪烁
        /// </summary>
        public Frm_VPTool()
        {
            InitializeComponent();
            // 启用双缓冲和WM_PAINT优化，减少界面闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // 初始化视觉作业基础路径
            _visionJobBasePath = Path.Combine(
                Application.StartupPath,
                "Config",
                "Project",
                "Vision",
                "Job"
            );
        }

        /// <summary>
        /// 窗体加载事件处理程序
        /// 初始化工具下拉列表，加载可用的视觉工具
        /// </summary>
        private void Frm_VPTool_Load(object sender, EventArgs e)
        {
            try
            {
                // 启用跑图功能面板
                uiTableLayoutPanel_跑图.Enabled = true;

                // 清空所有工具下拉列表
                ClearToolComboBoxes();

                // 从视觉系统加载工具并填充到下拉列表
                LoadVisionToolsToCombos();
            }
            catch (Exception ex)
            {
                // 记录加载过程中的异常
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 工具管理与保存
        /// <summary>
        /// 保存视觉工具配置
        /// 根据用户选中的工具类型和名称，将对应的VisionPro工具配置保存为VPP文件
        /// </summary>
        private void SaveVppTool(object sender, EventArgs e)
        {
            // 将发送者转换为UIButton
            var button = sender as UIButton;
            if (button == null) return;

            try
            {
                // 禁用按钮防止重复点击
                DisableControl(button);

                // 验证产品名称是否为空
                if (string.IsNullOrEmpty(Project.Instance.SystemParm.ProductName))
                {
                    return;
                }

                // 构建产品路径
                var productPath = Path.Combine(_visionJobBasePath, Project.Instance.SystemParm.ProductName);
                // 检查路径是否存在，若存在则询问是否覆盖
                if (Directory.Exists(productPath) && !ConfirmOverwrite())
                {
                    return;
                }

                // 根据选中的工具类型保存配置
                SaveSelectedToolConfiguration();
            }
            catch (Exception ex)
            {
                // 记录异常日志
                RLog.SaveError(ex);
            }
            finally
            {
                // 重新启用按钮
                EnableControl(button);
            }
        }

        /// <summary>
        /// 保存选中的工具配置
        /// 遍历所有视觉工具，根据当前选中的工具类型和名称，保存对应的配置
        /// </summary>
        private void SaveSelectedToolConfiguration()
        {
            // 遍历所有视觉工具
            foreach (var tool in VisionSystem.Instance.ListVisionTool)
            {
                // 保存CogToolBlockTool类型的工具
                if (cbbCogToolBlockTool.Text == tool.检测工具名称 && tool.检测工具类型 == ToolTypeToolBlock)
                {
                    SaveTool(VisionSystem.ToolBlocks[tool.检测工具索引], tool, cbbCogToolBlockTool.Text);
                }
                // 保存CogAcqFifoTool类型的工具
                else if (cbbCogAcqFifoTool.Text == tool.检测工具名称 && tool.检测工具类型 == ToolTypeAcqFifo)
                {
                    SaveTool(VisionSystem.AcqFifoTools[tool.检测工具索引], tool, cbbCogAcqFifoTool.Text);
                }
                // 保存CogCalibNPointToNPointTool类型的工具
                else if (cbbCogCalibNPointToNPointTool.Text == tool.检测工具名称 && tool.检测工具类型 == ToolTypeCalibNPoint)
                {
                    SaveTool(VisionSystem.CalibNPointTools[tool.检测工具索引], tool, cbbCogCalibNPointToNPointTool.Text);
                }
                // 保存CogCalibCheckerboardTool类型的工具
                else if (cbbCogCalibCheckerboardTool.Text == tool.检测工具名称 && tool.检测工具类型 == ToolTypeCalibCheckerboard)
                {
                    SaveTool(VisionSystem.CalibCheckerboardTools[tool.检测工具索引], tool, cbbCogCalibCheckerboardTool.Text);
                }
            }
        }

        /// <summary>
        /// 保存单个工具配置到文件
        /// </summary>
        /// <param name="toolObject">要保存的工具对象</param>
        /// <param name="toolInfo">工具信息对象，包含路径等元数据</param>
        /// <param name="toolName">工具名称，用于生成文件名</param>
        private void SaveTool(object toolObject, VisionToolDto toolInfo, string toolName)
        {
            // 工具对象为空则直接返回
            if (toolObject == null) return;

            // 构建工具保存路径
            var toolPath = Path.Combine(
                _visionJobBasePath,
                Project.Instance.SystemParm.ProductName,
                toolInfo.检测工具路径
            );

            // 确保目录存在，不存在则创建
            Directory.CreateDirectory(toolPath);

            // 构建完整的文件路径
            var filePath = Path.Combine(toolPath, $"{toolName}.vpp");
            // 使用VisionPro的序列化工具保存对象到文件
            CogSerializer.SaveObjectToFile(
                toolObject,
                filePath,
                typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter),
                CogSerializationOptionsConstants.Results
            );
        }

        /// <summary>
        /// 确认是否覆盖现有文件
        /// 弹出确认对话框，询问用户是否覆盖已存在的文件
        /// </summary>
        /// <returns>如果用户确认覆盖则返回true，否则返回false</returns>
        private bool ConfirmOverwrite()
        {
            // 设置确认框信息
            Frm_ConfirmBox.Instance.lbl_Message.Text = "\r\n 该型号已存在,是否覆盖原来的文件？";
            Frm_ConfirmBox.Instance.Visible = false;
            Frm_ConfirmBox.Instance.TopMost = true;
            // 显示确认对话框
            Frm_ConfirmBox.Instance.ShowDialog();

            // 返回用户选择结果
            return Frm_ConfirmBox.Instance.Result == ConfirmBoxResult.Yes;
        }
        #endregion

        #region 工具下拉列表处理
        /// <summary>
        /// 清空所有工具下拉列表
        /// 重置所有工具类型对应的下拉选择框
        /// </summary>
        private void ClearToolComboBoxes()
        {
            cbbCogToolBlockTool.Items.Clear();
            cbbCogAcqFifoTool.Items.Clear();
            cbbCogCalibNPointToNPointTool.Items.Clear();
            cbbCogCalibCheckerboardTool.Items.Clear();
        }

        /// <summary>
        /// 加载视觉工具到对应的下拉列表
        /// 遍历所有视觉工具，根据工具类型添加到对应的下拉列表中
        /// </summary>
        private void LoadVisionToolsToCombos()
        {
            foreach (var tool in VisionSystem.Instance.ListVisionTool)
            {
                AddToolToCombo(tool.检测工具名称, tool.检测工具类型);
            }
        }

        /// <summary>
        /// 将工具添加到对应的下拉列表
        /// 根据工具类型，将工具名称添加到相应的下拉选择框中
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="toolType">工具类型</param>
        private void AddToolToCombo(string toolName, string toolType)
        {
            switch (toolType)
            {
                case ToolTypeToolBlock:
                    AddToComboIfNotExists(cbbCogToolBlockTool, toolName);
                    break;
                case ToolTypeAcqFifo:
                    AddToComboIfNotExists(cbbCogAcqFifoTool, toolName);
                    break;
                case ToolTypeCalibNPoint:
                    AddToComboIfNotExists(cbbCogCalibNPointToNPointTool, toolName);
                    break;
                case ToolTypeCalibCheckerboard:
                    AddToComboIfNotExists(cbbCogCalibCheckerboardTool, toolName);
                    break;
            }
        }

        /// <summary>
        /// 如果下拉列表中不存在该项，则添加
        /// 避免下拉列表中出现重复的工具名称
        /// </summary>
        private void AddToComboIfNotExists(UIComboBox combo, string item)
        {
            if (!combo.Items.Contains(item))
            {
                combo.Items.Add(item);
            }
        }

        /// <summary>
        /// CogToolBlockTool下拉列表选择变更事件
        /// 当用户选择不同的工具块时，更新编辑控件并运行该工具
        /// </summary>
        private void cbbCogToolBlockTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateToolSubject(cbbCogToolBlockTool.Text, ToolTypeToolBlock,
                index => VisionSystem.ToolBlocks[index],
                tool => { cogToolBlockEditV21.Subject = (CogToolBlock)tool; cogToolBlockEditV21.Subject.Run(); });
        }

        /// <summary>
        /// CogAcqFifoTool下拉列表选择变更事件
        /// 当用户选择不同的图像采集工具时，更新编辑控件并运行该工具
        /// </summary>
        private void cbbCogAcqFifoTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateToolSubject(cbbCogAcqFifoTool.Text, ToolTypeAcqFifo,
                index => VisionSystem.AcqFifoTools[index],
                tool => { CogAcqFifoEditV21.Subject = (CogAcqFifoTool)tool; CogAcqFifoEditV21.Subject.Run(); });
        }

        /// <summary>
        /// CogCalibNPointToNPointTool下拉列表选择变更事件
        /// 当用户选择不同的九点标定工具时，更新编辑控件并运行该工具
        /// </summary>
        private void cbbCogCalibNPointToNPointTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateToolSubject(cbbCogCalibNPointToNPointTool.Text, ToolTypeCalibNPoint,
                index => VisionSystem.CalibNPointTools[index],
                tool => { cogCalibNPointToNPointEditV22.Subject = (CogCalibNPointToNPointTool)tool; cogCalibNPointToNPointEditV22.Subject.Run(); });
        }

        /// <summary>
        /// CogCalibCheckerboardTool下拉列表选择变更事件
        /// 当用户选择不同的棋盘格标定工具时，更新编辑控件并运行该工具
        /// </summary>
        private void cbbCogCalibCheckerboardTool_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateToolSubject(cbbCogCalibCheckerboardTool.Text, ToolTypeCalibCheckerboard,
                index => VisionSystem.CalibCheckerboardTools[index],
                tool => { cogCalibCheckerboardEditV21.Subject = (CogCalibCheckerboardTool)tool; cogCalibCheckerboardEditV21.Subject.Run(); });
        }

        /// <summary>
        /// 通用方法：更新工具主题并运行
        /// 封装了工具选择变更的通用逻辑，减少代码重复
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="toolType">工具类型</param>
        /// <param name="toolGetter">获取工具的方法，根据索引返回工具对象</param>
        /// <param name="toolSetter">设置工具并运行的方法，将工具对象绑定到编辑控件</param>
        private void UpdateToolSubject(string toolName, string toolType,
                                     Func<int, object> toolGetter,
                                     Action<object> toolSetter)
        {
            // 根据工具类型获取对应的下拉列表
            var combo = GetComboBoxByType(toolType);
            if (combo == null) return;

            try
            {
                // 禁用下拉列表防止操作冲突
                DisableControl(combo);

                // 在UI线程中执行更新操作
                UIThreadInvoke(() =>
                {
                    // 遍历所有视觉工具，找到匹配的工具
                    foreach (var tool in VisionSystem.Instance.ListVisionTool)
                    {
                        if (tool.检测工具名称 == toolName && tool.检测工具类型 == toolType)
                        {
                            // 获取工具对象
                            var toolObject = toolGetter(tool.检测工具索引);
                            if (toolObject != null)
                            {
                                // 设置工具到编辑控件并运行
                                toolSetter(toolObject);
                            }
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // 记录异常
                RLog.SaveError(ex);
            }
            finally
            {
                // 重新启用下拉列表
                EnableControl(combo);
            }
        }

        /// <summary>
        /// 根据工具类型获取对应的下拉列表
        /// 使用switch表达式实现简洁的类型到控件的映射
        /// </summary>
        private UIComboBox GetComboBoxByType(string toolType)
        {
            return toolType switch
            {
                ToolTypeToolBlock => cbbCogToolBlockTool,
                ToolTypeAcqFifo => cbbCogAcqFifoTool,
                ToolTypeCalibNPoint => cbbCogCalibNPointToNPointTool,
                ToolTypeCalibCheckerboard => cbbCogCalibCheckerboardTool,
                _ => null
            };
        }

        /// <summary>
        /// 刷新工具下拉列表
        /// 当用户点击下拉列表时，重新加载工具列表以确保数据最新
        /// </summary>
        private void UIComboBox_Click(object sender, EventArgs e)
        {
            try
            {
                // 在UI线程中刷新工具列表
                UIThreadInvoke(() =>
                {
                    ClearToolComboBoxes();
                    LoadVisionToolsToCombos();
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 图像回放功能
        /// <summary>
        /// 获取指定路径下的所有图像文件
        /// 筛选出指定后缀名的图像文件，用于回放功能
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <returns>图像文件路径列表</returns>
        private List<string> GetImageFiles(string path)
        {
            var imageFiles = new List<string>();
            // 路径不存在则返回空列表
            if (!Directory.Exists(path)) return imageFiles;

            try
            {
                // 遍历文件夹中的所有文件
                foreach (var file in Directory.GetFiles(path))
                {
                    // 获取文件后缀并转换为小写
                    var ext = Path.GetExtension(file)?.ToLower();
                    // 检查是否为支持的图像类型
                    if (!string.IsNullOrEmpty(ext) && ImageExtensions.Contains(ext))
                    {
                        imageFiles.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录获取文件列表时的异常
                RLog.SaveError(ex);
            }

            return imageFiles;
        }

        /// <summary>
        /// 打开并运行指定图像
        /// 加载图像文件，根据当前跑图模式处理图像，并更新状态显示
        /// </summary>
        private void OpenAndRunImage(string filePath, string folderPath)
        {
            try
            {
                // 在UI线程中执行图像处理
                UIThreadInvoke(() =>
                {
                    // 打开图像文件
                    VisionSystem.ImageFileTool.Operator.Open(filePath, CogImageFileModeConstants.Read);
                    VisionSystem.ImageFileTool.Run();
                    var image = VisionSystem.ImageFileTool.OutputImage;

                    // 根据跑图模式处理图像
                    ProcessImageByPlaybackMode(image);

                    // 更新状态栏显示
                    UpdatePlaybackStatus(filePath);

                    // 如果启用分类，处理图像分类
                    if (_isClassificationEnabled)
                    {
                        ClassifyImage(filePath, folderPath);
                    }
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 根据跑图模式处理图像
        /// 根据用户选择的跑图模式（原图/九点标定后/棋盘格标定后），使用不同的方式处理图像
        /// </summary>
        private void ProcessImageByPlaybackMode(ICogImage image)
        {
            if (_playOriginalImage)
            {
                RunOriginalImageMode(image);
            }
            else if (_playNPointCalibImage)
            {
                RunNPointCalibMode(image);
            }
            else if (_playCheckerboardImage)
            {
                RunCheckerboardMode(image);
            }
        }

        /// <summary>
        /// 原图跑图模式
        /// 直接使用原始图像作为输入运行工具块
        /// </summary>
        private void RunOriginalImageMode(ICogImage image)
        {
            cogToolBlockEditV21.Subject.Inputs["InputImage"].Value = image;
            cogToolBlockEditV21.Subject.Run();
        }

        /// <summary>
        /// 九点标定输出图跑图模式
        /// 先使用九点标定工具处理图像，再将处理后的图像作为输入运行工具块
        /// </summary>
        private void RunNPointCalibMode(ICogImage image)
        {
            cogCalibNPointToNPointEditV22.Subject.InputImage = image;
            cogCalibNPointToNPointEditV22.Subject.Run();
            cogToolBlockEditV21.Subject.Inputs["InputImage"].Value = cogCalibNPointToNPointEditV22.Subject.OutputImage;
            cogToolBlockEditV21.Subject.Run();
        }

        /// <summary>
        /// 棋盘格输出图跑图模式
        /// 先使用棋盘格标定工具处理图像，再将处理后的图像作为输入运行工具块
        /// </summary>
        private void RunCheckerboardMode(ICogImage image)
        {
            cogCalibCheckerboardEditV21.Subject.InputImage = image;
            cogCalibCheckerboardEditV21.Subject.Run();
            cogToolBlockEditV21.Subject.Inputs["InputImage"].Value = cogCalibCheckerboardEditV21.Subject.OutputImage;
            cogToolBlockEditV21.Subject.Run();
        }

        /// <summary>
        /// 更新回放状态显示
        /// 在状态栏显示当前图像名称和播放进度
        /// </summary>
        private void UpdatePlaybackStatus(string filePath)
        {
            toolStripStatusLabel图像名称.Text =
                $"图像名称：{Path.GetFileName(filePath)} {(_playbackImageIndex + 1)}/{_playbackImageCount}";
        }

        /// <summary>
        /// 分类图像（OK/NG）
        /// 根据工具运行结果，将图像分类到OK或NG文件夹
        /// </summary>
        private void ClassifyImage(string filePath, string folderPath)
        {
            var fileName = Path.GetFileName(filePath);
            // 判断检测结果（Accept为OK，否则为NG）
            _lastCamResult = cogToolBlockEditV21.Subject.RunStatus.Result == CogToolResultConstants.Accept;

            // 确定目标文件夹（OK或NG）
            var targetFolder = _lastCamResult
                ? Path.Combine(folderPath, "OK Images")
                : Path.Combine(folderPath, "NG Images");

            // 创建目标文件夹（如果不存在）
            Directory.CreateDirectory(targetFolder);

            // 移动文件到对应分类文件夹
            var targetPath = Path.Combine(targetFolder, fileName);
            // 仅在目标文件不存在时移动，避免覆盖
            if (!File.Exists(targetPath))
            {
                File.Move(filePath, targetPath);
            }
        }

        /// <summary>
        /// 播放第一张图像
        /// 将回放索引设置为0，显示第一张图像
        /// </summary>
        private void btnPlayBackFirst_Click(object sender, EventArgs e)
        {
            ProcessPlaybackAction(() => _playbackImageIndex = 0, sender as UISymbolButton);
        }

        /// <summary>
        /// 播放上一张图像
        /// 将回放索引减1，显示上一张图像（如果当前不是第一张）
        /// </summary>
        private void btnPlayBackPrev_Click(object sender, EventArgs e)
        {
            ProcessPlaybackAction(() =>
            {
                if (_playbackImageIndex > 0)
                    _playbackImageIndex--;
            }, sender as UISymbolButton);
        }

        /// <summary>
        /// 播放下一张图像
        /// 将回放索引加1，显示下一张图像（如果当前不是最后一张）
        /// </summary>
        private void btnPlayBackNext_Click(object sender, EventArgs e)
        {
            ProcessPlaybackAction(() =>
            {
                if (_playbackImageIndex < _playbackImageCount - 1)
                    _playbackImageIndex++;
            }, sender as UISymbolButton);
        }

        /// <summary>
        /// 播放最后一张图像
        /// 将回放索引设置为最后一张，显示最后一张图像
        /// </summary>
        private void btnPlayBackLast_Click(object sender, EventArgs e)
        {
            ProcessPlaybackAction(() => _playbackImageIndex = _playbackImageCount - 1, sender as UISymbolButton);
        }

        /// <summary>
        /// 处理回放操作
        /// 封装了图像回放的通用逻辑，包括索引更新、图像加载和按钮状态更新
        /// </summary>
        /// <param name="indexAction">用于更新索引的操作</param>
        /// <param name="button">触发该操作的按钮</param>
        private void ProcessPlaybackAction(Action indexAction, UIButton button)
        {
            // 按钮为空或没有图像文件则返回
            if (button == null || _playbackImageFiles.Count == 0) return;

            try
            {
                // 禁用按钮防止重复操作
                DisableControl(button);
                // 执行索引更新操作
                indexAction.Invoke();

                // 检查索引是否有效
                if (_playbackImageIndex >= 0 && _playbackImageIndex < _playbackImageFiles.Count)
                {
                    // 获取当前图像文件并处理
                    _currentPlaybackFile = _playbackImageFiles[_playbackImageIndex];
                    OpenAndRunImage(_currentPlaybackFile, _playbackFolder);
                    // 更新回放按钮状态
                    UpdatePlaybackButtonsState();
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                // 重新启用按钮
                EnableControl(button);
            }
        }

        /// <summary>
        /// 选择回放文件夹
        /// 打开文件夹选择对话框，让用户选择包含图像的文件夹，并初始化回放参数
        /// </summary>
        private void BindingNavigatorAddNewItem_Click(object sender, EventArgs e)
        {
            var button = sender as UISymbolButton;
            if (button == null) return;

            try
            {
                // 禁用按钮所在的工具栏防止重复操作
                DisableControl(button);

                // 显示文件夹选择对话框
                if (FolderBrowserDialog_Classify.ShowDialog() != DialogResult.OK)
                    return;

                // 更新回放文件夹路径
                _playbackFolder = FolderBrowserDialog_Classify.SelectedPath;
                // 获取该文件夹中的所有图像文件
                _playbackImageFiles = GetImageFiles(_playbackFolder);
                _playbackImageCount = _playbackImageFiles.Count;
                // 重置索引为第一张
                _playbackImageIndex = 0;

                // 如果启用分类，初始化OK和NG文件夹
                if (_isClassificationEnabled)
                {
                    Directory.CreateDirectory(Path.Combine(_playbackFolder, "OK Images"));
                    Directory.CreateDirectory(Path.Combine(_playbackFolder, "NG Images"));
                }

                // 更新回放按钮状态
                UpdatePlaybackButtonsState();

                // 打开并显示第一张图像
                if (_playbackImageCount > 0)
                {
                    _currentPlaybackFile = _playbackImageFiles[0];
                    OpenAndRunImage(_currentPlaybackFile, _playbackFolder);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                // 重新启用工具栏
                EnableControl(button);
            }
        }

        /// <summary>
        /// 播放/暂停回放
        /// 切换回放状态，开始或停止自动播放图像序列
        /// </summary>
        private void btnPlayBackPlay_Click(object sender, EventArgs e)
        {
            var button = sender as UISymbolButton;
            if (button == null) return;

            try
            {
                // 禁用按钮防止重复点击
                DisableControl(button);

                // 根据当前文本判断是播放还是暂停
                if (button.Text == "播放")
                {
                    StartPlayback(button);
                }
                else
                {
                    StopPlayback(button);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                // 重新启用按钮
                EnableControl(button);
            }
        }

        /// <summary>
        /// 开始回放
        /// 设置按钮状态为暂停，启动定时器开始自动播放图像
        /// </summary>
        private void StartPlayback(UISymbolButton button)
        {
            button.Text = "暂停";
            button.Symbol = 61516; // 暂停图标
            _isPlaybackActive = true; // 标记为正在回放
            TimerPlayBack.Interval = _playbackDelay; // 设置播放间隔
            TimerPlayBack.Enabled = true; // 启动定时器

            // 禁用导航按钮，防止手动干预
            DisablePlaybackNavigationButtons();
        }

        /// <summary>
        /// 停止回放
        /// 设置按钮状态为播放，停止定时器结束自动播放
        /// </summary>
        private void StopPlayback(UISymbolButton button)
        {
            button.Text = "播放";
            button.Symbol = 61515; // 播放图标
            _isPlaybackActive = false; // 标记为停止回放
            TimerPlayBack.Enabled = false; // 停止定时器

            // 启用导航按钮，允许手动操作
            EnablePlaybackNavigationButtons();
        }

        /// <summary>
        /// 回放定时器事件
        /// 定时切换到下一张图像，实现自动播放功能
        /// </summary>
        private void TimerPlayBack_Tick(object sender, EventArgs e)
        {
            try
            {
                // 先禁用定时器，避免重入
                TimerPlayBack.Enabled = false;

                // 不在播放状态或没有图像则返回
                if (!_isPlaybackActive || _playbackImageCount == 0)
                    return;

                // 检查是否需要在NG时停止
                if (_stopOnNg && !_lastCamResult)
                {
                    StopPlayback(btnPlayBackPlay);
                    return;
                }

                // 移动到下一张图像
                if (_playbackImageIndex < _playbackImageCount - 1)
                {
                    _playbackImageIndex++;
                    _currentPlaybackFile = _playbackImageFiles[_playbackImageIndex];
                    OpenAndRunImage(_currentPlaybackFile, _playbackFolder);
                    // 启用定时器继续播放
                    TimerPlayBack.Enabled = true;
                }
                else
                {
                    // 已到达最后一张，停止播放
                    StopPlayback(btnPlayBackPlay);
                    UpdatePlaybackButtonsState();
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 更新回放按钮状态
        /// 根据当前播放位置，更新各个导航按钮的启用/禁用状态
        /// </summary>
        private void UpdatePlaybackButtonsState()
        {
            // 没有图像时禁用所有按钮
            if (_playbackImageCount == 0)
            {
                SetPlaybackButtonsEnabled(false);
                return;
            }

            // 根据当前索引更新按钮状态
            btnPlayBackFirst.Enabled = _playbackImageIndex > 0;
            btnPlayBackPrev.Enabled = _playbackImageIndex > 0;
            btnPlayBackNext.Enabled = _playbackImageIndex < _playbackImageCount - 1;
            btnPlayBackLast.Enabled = _playbackImageIndex < _playbackImageCount - 1;
            btnPlayBackPlay.Enabled = true;
        }

        /// <summary>
        /// 设置所有回放导航按钮的启用状态
        /// 批量设置所有回放相关按钮的启用/禁用状态
        /// </summary>
        private void SetPlaybackButtonsEnabled(bool enabled)
        {
            btnPlayBackFirst.Enabled = enabled;
            btnPlayBackPrev.Enabled = enabled;
            btnPlayBackNext.Enabled = enabled;
            btnPlayBackLast.Enabled = enabled;
            btnPlayBackPlay.Enabled = enabled;
        }

        /// <summary>
        /// 禁用回放导航按钮
        /// 在自动播放时禁用所有手动导航按钮
        /// </summary>
        private void DisablePlaybackNavigationButtons()
        {
            btnPlayBackFirst.Enabled = false;
            btnPlayBackPrev.Enabled = false;
            btnPlayBackNext.Enabled = false;
            btnPlayBackLast.Enabled = false;
        }

        /// <summary>
        /// 启用回放导航按钮
        /// 在停止自动播放时启用所有手动导航按钮
        /// </summary>
        private void EnablePlaybackNavigationButtons()
        {
            if (_playbackImageCount == 0) return;

            btnPlayBackFirst.Enabled = true;
            btnPlayBackPrev.Enabled = true;
            btnPlayBackNext.Enabled = true;
            btnPlayBackLast.Enabled = true;
        }
        #endregion

        #region 跑图设置
        /// <summary>
        /// 图像分类复选框变更事件
        /// 更新图像分类启用状态
        /// </summary>
        private void CheckBox_Classify_CheckedChanged(object sender, EventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            try
            {
                DisableControl(checkBox);
                _isClassificationEnabled = checkBox.Checked;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                EnableControl(checkBox);
            }
        }

        /// <summary>
        /// NG时停止跑图复选框变更事件
        /// 更新NG时是否停止回放的状态
        /// </summary>
        private void CheckBox_是否NG时跑图停止_CheckedChanged(object sender, EventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            try
            {
                DisableControl(checkBox);
                _stopOnNg = checkBox.Checked;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                EnableControl(checkBox);
            }
        }

        /// <summary>
        /// 原图跑图单选框变更事件
        /// 更新是否使用原图跑图的状态
        /// </summary>
        private void radioButton_原图跑图_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as UIRadioButton;
            if (radio == null) return;

            try
            {
                DisableControl(radio);
                _playOriginalImage = radio.Checked;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                EnableControl(radio);
            }
        }

        /// <summary>
        /// 九点标定输出图跑图单选框变更事件
        /// 更新是否使用九点标定后图像跑图的状态
        /// </summary>
        private void radioButton_9点标定输出图跑图_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio == null) return;

            try
            {
                DisableControl(radio);
                _playNPointCalibImage = radio.Checked;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                EnableControl(radio);
            }
        }

        /// <summary>
        /// 棋盘格输出图跑图单选框变更事件
        /// 更新是否使用棋盘格标定后图像跑图的状态
        /// </summary>
        private void radioButton_CheckBoard输出图跑图_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio == null) return;

            try
            {
                DisableControl(radio);
                _playCheckerboardImage = radio.Checked;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
            finally
            {
                EnableControl(radio);
            }
        }
        #endregion

        #region 图像操作
        /// <summary>
        /// 打开单张图像并运行
        /// 打开文件选择对话框，让用户选择单张图像，并使用该图像运行工具
        /// </summary>
        private void uiBtn_OpenRunImage_Click(object sender, EventArgs e)
        {
            try
            {
                // 显示文件选择对话框
                if (VisionSystem.OpenFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 打开选中的图像文件
                    VisionSystem.ImageFileTool.Operator.Open(
                        VisionSystem.OpenFileDialog.FileName,
                        CogImageFileModeConstants.Read
                    );
                    VisionSystem.ImageFileTool.Run();

                    // 获取图像并运行工具块
                    var image = VisionSystem.ImageFileTool.OutputImage;
                    cogToolBlockEditV21.Subject.Inputs["InputImage"].Value = image;
                    cogToolBlockEditV21.Subject.Run();
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 抓取图像并运行
        /// 从图像采集工具获取实时图像，并使用该图像运行工具块
        /// </summary>
        private void uiBtn_GrabRunImage_Click(object sender, EventArgs e)
        {
            try
            {
                // 运行图像采集工具获取图像
                CogAcqFifoEditV21.Subject.Run();
                // 使用采集到的图像运行工具块
                cogToolBlockEditV21.Subject.Inputs["InputImage"].Value = CogAcqFifoEditV21.Subject.OutputImage;
                cogToolBlockEditV21.Subject.Run();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 在UI线程执行操作
        /// 确保UI操作在主线程执行，避免跨线程操作异常
        /// </summary>
        private void UIThreadInvoke(Action action)
        {
            if (InvokeRequired)
            {
                // 如果当前不在UI线程，则调用Invoke在UI线程执行
                Invoke(action);
            }
            else
            {
                // 已经在UI线程，直接执行
                action.Invoke();
            }
        }

        /// <summary>
        /// 禁用控件
        /// 在UI线程中禁用指定控件，防止操作冲突
        /// </summary>
        private void DisableControl(Control control)
        {
            if (control != null && !control.IsDisposed)
            {
                UIThreadInvoke(() => control.Enabled = false);
            }
        }

        /// <summary>
        /// 启用控件
        /// 在UI线程中启用指定控件，允许用户操作
        /// </summary>
        private void EnableControl(Control control)
        {
            if (control != null && !control.IsDisposed)
            {
                UIThreadInvoke(() => control.Enabled = true);
            }
        }
        #endregion
    }
}
