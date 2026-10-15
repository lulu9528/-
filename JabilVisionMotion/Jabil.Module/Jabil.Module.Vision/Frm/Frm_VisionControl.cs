using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Vision.Lib;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Vision.Frm
{
    /// <summary>
    /// 视觉控制窗体类，负责相机图像的采集、处理、显示以及用户交互
    /// 继承自Sunny.UI的UIPage，提供统一的UI风格
    /// </summary>
    public partial class Frm_VisionControl : UIPage
    {
        #region Constants
        /// <summary>
        /// 图像采集最大重试次数，当首次采集失败时尝试的次数
        /// </summary>
        private const int MaxGrabRetryCount = 2;
        /// <summary>
        /// 初始化延迟时间(毫秒)，用于硬件设备初始化后的稳定时间
        /// </summary>
        private const int InitializationDelay = 1000;
        /// <summary>
        /// 短延迟时间(毫秒)，用于操作之间的短暂等待
        /// </summary>
        private const int ShortDelay = 200;
        /// <summary>
        /// 图像文件过滤器，用于文件选择对话框中的文件类型筛选
        /// </summary>
        private const string ImageFilter = "图片文件(*.jpg;*.bmp;*.png;*.idb)|*.jpg;*.bmp;*.png;*.idb|所有文件(*.*)|*.*";
        #endregion

        #region Fields
        /// <summary>
        /// 窗体的单例实例，确保应用中只存在一个该窗体实例
        /// </summary>
        private static Frm_VisionControl _instance;
        /// <summary>
        /// 当前选中的检测点位类型
        /// </summary>
        private string _inspectionPointType;
        /// <summary>
        /// 当前选中的检测点位定义
        /// </summary>
        private string _inspectionPointDefinition;
        /// <summary>
        /// 当前选中的检测点位名称
        /// </summary>
        private string _inspectionPointName;
        /// <summary>
        /// 图像信息对象，存储当前处理的图像及其相关属性
        /// </summary>
        private ImageInfo _imageInfo = new ImageInfo();
        #endregion

        #region Properties
        /// <summary>
        /// 窗体单例访问属性，提供全局访问点，确保唯一实例
        /// </summary>
        public static Frm_VisionControl Instance
        {
            get
            {
                // 检查实例是否为空或已释放，如果是则创建新实例
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_VisionControl();
                return _instance;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// 初始化视觉控制窗体
        /// </summary>
        public Frm_VisionControl()
        {
            // 初始化窗体控件（由设计器自动生成）
            InitializeComponent();
            // 启用双缓冲和WM_PAINT优化，减少窗体闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            // 初始化事件处理器，绑定各类事件
            InitializeEventHandlers();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 初始化事件处理器，将控件事件与处理方法绑定
        /// </summary>
        private void InitializeEventHandlers()
        {
            // 工具条点击事件
            toolStrip1.ItemClicked += ToolStrip1_ItemClicked;
            // 点位类型下拉框选择变化事件
            cbb_点位类型.SelectedIndexChanged += CbbInspectionType_SelectedIndexChanged;
            // 点位定义下拉框选择变化事件
            cbb_点位定义.SelectedIndexChanged += CbbInspectionDefinition_SelectedIndexChanged;
            // 点位名称下拉框选择变化事件
            cbb_点位名称.SelectedIndexChanged += CbbInspectionName_SelectedIndexChanged;
        }
        #endregion

        #region UI Update Helpers
        /// <summary>
        /// 安全更新UI线程操作，确保UI操作在主线程执行
        /// </summary>
        /// <param name="action">要执行的UI操作委托</param>
        private void SafeUIUpdate(Action action)
        {
            // 检查当前线程是否为UI线程
            if (InvokeRequired)
                // 如果不是UI线程，调用Invoke在UI线程执行
                Invoke(action);
            else
                // 如果是UI线程，直接执行
                action();
        }

        /// <summary>
        /// 带异常处理的UI更新，在执行UI操作时禁用控件并处理可能的异常
        /// </summary>
        /// <param name="control">需要临时禁用的控件</param>
        /// <param name="action">要执行的操作委托</param>
        private void SafeUIUpdateWithExceptionHandling(Control control, Action action)
        {
            try
            {
                // 使用安全UI更新方法执行操作
                SafeUIUpdate(() =>
                {
                    // 执行前禁用控件，防止重复操作
                    control.Enabled = false;
                    // 执行实际操作
                    action.Invoke();
                    // 操作完成后重新启用控件
                    control.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                // 发生异常时确保控件处于启用状态
                control.Enabled = true;
                // 记录错误信息
                LogError(ex);
            }
        }
        #endregion

        #region Logging
        /// <summary>
        /// 记录错误日志并显示错误消息
        /// </summary>
        /// <param name="ex">异常信息对象</param>
        private void LogError(Exception ex)
        {
            // 保存错误日志到日志系统
            RLog.SaveError(ex);
            // 显示错误消息给用户
            Frm_MessageBox.Instance.MessageBoxShow($"操作失败: {ex.Message}", TipLevel.Error);
        }
        #endregion

        #region Camera Operations
        /// <summary>
        /// 相机采集图像的主方法，协调图像采集、重试和显示流程
        /// </summary>
        /// <param name="imageInfo">图像信息对象，用于存储采集到的图像</param>
        private void GrabImage(ImageInfo imageInfo)
        {
            try
            {
                // 清除当前显示的图像和记录
                SafeUIUpdate(() =>
                {
                    // 重置显示控件
                    cogRecordDisplay0 = null;
                    // 停止可能正在运行的实时显示
                    cogRecordDisplay0?.StopLiveDisplay();
                });

                // 尝试采集图像
                bool isImageGrabbed = TryGrabImage(imageInfo);

                // 如果首次采集失败，尝试重新初始化设备并再次采集
                if (!isImageGrabbed)
                {
                    isImageGrabbed = TryReinitializeAndGrab(imageInfo);
                }

                // 如果所有尝试都失败，提示用户检查设备
                if (!isImageGrabbed)
                {
                    Frm_MessageBox.Instance.MessageBoxShow(
                        $"相机 {imageInfo.绑定取像工具索引} 与工控机断开连接，请检查网线连接并重启软件！",
                        TipLevel.Warning);
                    return;
                }

                // 显示采集到的图像
                SafeUIUpdate(() =>
                {
                    cogRecordDisplay0.Image = imageInfo._CogImage;
                    // 调整图像大小以适应显示控件
                    cogRecordDisplay0.Fit();
                });
            }
            catch (Exception ex)
            {
                // 记录并显示错误信息
                LogError(ex);
            }
        }

        /// <summary>
        /// 尝试采集图像，包含重试机制
        /// </summary>
        /// <param name="imageInfo">图像信息对象，用于存储采集到的图像</param>
        /// <returns>是否采集成功</returns>
        private bool TryGrabImage(ImageInfo imageInfo)
        {
            // 首次采集图像
            VisionSystem.AcqFifoTools[imageInfo.绑定取像工具索引].Run();
            // 获取采集到的图像
            imageInfo._CogImage = VisionSystem.AcqFifoTools[imageInfo.绑定取像工具索引].OutputImage;

            // 如果首次采集失败，进行重试
            if (imageInfo._CogImage == null)
            {
                for (int i = 0; i < MaxGrabRetryCount; i++)
                {
                    // 再次尝试采集
                    VisionSystem.AcqFifoTools[imageInfo.绑定取像工具索引].Run();
                    imageInfo._CogImage = VisionSystem.AcqFifoTools[imageInfo.绑定取像工具索引].OutputImage;

                    // 如果采集成功，返回true
                    if (imageInfo._CogImage != null)
                        return true;
                }
                // 所有重试都失败，返回false
                return false;
            }
            // 首次采集成功，返回true
            return true;
        }

        /// <summary>
        /// 尝试重新初始化采集工具并采集图像，用于解决设备临时故障
        /// </summary>
        /// <param name="imageInfo">图像信息对象</param>
        /// <returns>是否成功采集到图像</returns>
        private bool TryReinitializeAndGrab(ImageInfo imageInfo)
        {
            // 使用using语句确保CogFrameGrabbers资源正确释放
            using (var frameGrabbers = new CogFrameGrabbers())
            {
                // 释放现有采集工具资源
                VisionSystem.AcqFifoTools[imageInfo.绑定取像工具索引].Dispose();
                // 等待设备释放完成
                Thread.Sleep(InitializationDelay);

                // 查找对应的取像工具配置信息
                var toolInfo = VisionSystem.Instance.ListVisionTool
                    .FirstOrDefault(t => t.检测工具类型 == "CogAcqFifoTool"
                                      && t.检测工具索引 == imageInfo.绑定取像工具索引);

                // 如果未找到工具配置，返回失败
                if (toolInfo == null) return false;

                // 构建取像工具配置文件(VPP)的路径
                string vppPath = Path.Combine(
                    Application.StartupPath,
                    "Config", "Project", "Vision", "Job",
                    Project.Instance.SystemParm.ProductName,
                    toolInfo.检测工具路径,
                    $"{toolInfo.检测工具名称}.vpp");

                // 重新加载取像工具配置
                VisionSystem.AcqFifoTools[toolInfo.检测工具索引] =
                    CogSerializer.LoadObjectFromFile(vppPath) as CogAcqFifoTool;

                // 等待工具加载完成
                Thread.Sleep(ShortDelay);

                // 再次尝试采集图像
                VisionSystem.AcqFifoTools[imageInfo.绑定取像工具索引].Run();
                imageInfo._CogImage = VisionSystem.AcqFifoTools[imageInfo.绑定取像工具索引].OutputImage;

                // 返回采集结果
                return imageInfo._CogImage != null;
            }
        }

        /// <summary>
        /// 切换相机实时显示状态（开始/停止实时显示）
        /// </summary>
        /// <param name="imageInfo">图像信息对象</param>
        private void ToggleLiveDisplay(ImageInfo imageInfo)
        {
            try
            {
                SafeUIUpdate(() =>
                {
                    // 如果正在实时显示，则停止
                    if (cogRecordDisplay0.LiveDisplayRunning)
                    {
                        cogRecordDisplay0.StopLiveDisplay();
                        cogRecordDisplay0.Fit(true);
                    }
                    else
                    {
                        // 清除当前图像
                        cogRecordDisplay0.Image = null;
                        cogRecordDisplay0.Record = null;
                        imageInfo._CogImage = null;

                        // 获取采集通道并启动实时显示
                        ICogAcqFifo fifo = VisionSystem.AcqFifoTools[imageInfo.绑定取像工具索引].Operator;
                        cogRecordDisplay0.StartLiveDisplay(fifo);
                        // 调整图像大小以适应显示控件
                        cogRecordDisplay0.Fit(true);
                    }
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        /// <summary>
        /// 从硬盘导入图像文件并显示
        /// </summary>
        /// <param name="imageInfo">图像信息对象，用于存储导入的图像</param>
        private void ImportImageFromDisk(ImageInfo imageInfo)
        {
            try
            {
                // 清除当前显示
                SafeUIUpdate(() =>
                {
                    cogRecordDisplay0 = null;
                });

                // 显示文件选择对话框，让用户选择图像文件
                if (VisionSystem.OpenFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 打开选中的图像文件
                    VisionSystem.ImageFileTool.Operator.Open(
                        VisionSystem.OpenFileDialog.FileName,
                        CogImageFileModeConstants.Read);

                    // 运行图像文件工具获取图像
                    VisionSystem.ImageFileTool.Run();
                    imageInfo._CogImage = VisionSystem.ImageFileTool.OutputImage;

                    // 在UI上显示导入的图像
                    SafeUIUpdate(() =>
                    {
                        cogRecordDisplay0.Image = imageInfo._CogImage;
                        cogRecordDisplay0.Fit(true);
                    });
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        /// <summary>
        /// 将当前显示的图像保存到硬盘
        /// </summary>
        /// <param name="imageInfo">图像信息对象，用于获取当前图像相关信息</param>
        private void SaveImageToDisk(ImageInfo imageInfo)
        {
            try
            {
                // 检查是否有图像可保存
                if (cogRecordDisplay0?.Image == null)
                {
                    Frm_MessageBox.Instance.MessageBoxShow(
                        $"相机 {imageInfo.绑定取像工具索引} 中没有图像可保存",
                        TipLevel.Warning);
                    return;
                }

                // 配置保存文件对话框
                VisionSystem.SaveFileDialog.Filter = ImageFilter;
                VisionSystem.SaveFileDialog.InitialDirectory =
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                // 显示保存文件对话框
                if (VisionSystem.SaveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = VisionSystem.SaveFileDialog.FileName;
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        // 设置要保存的图像
                        VisionSystem.ImageFileTool.InputImage = cogRecordDisplay0.Image;
                        // 打开文件进行写入
                        VisionSystem.ImageFileTool.Operator.Open(filePath, CogImageFileModeConstants.Write);
                        // 执行保存操作
                        VisionSystem.ImageFileTool.Run();
                        // 关闭文件
                        VisionSystem.ImageFileTool.Operator.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        /// <summary>
        /// 处理当前图像（应用视觉检测算法）并显示结果
        /// </summary>
        /// <param name="imageInfo">图像信息对象，包含要处理的图像和相关配置</param>
        private void ProcessImage(ImageInfo imageInfo)
        {
            try
            {
                // 检查是否有图像可处理
                if (imageInfo._CogImage == null)
                {
                    SafeUIUpdate(() =>
                    {
                        cogRecordDisplay0 = null;
                    });
                    Frm_MessageBox.Instance.MessageBoxShow(
                        $"相机 {imageInfo.绑定取像工具索引} 中没有图像可处理",
                        TipLevel.Warning);
                    return;
                }
                // 遍历系统中的视觉工具，匹配配置中指定的工具
                foreach (var tool in VisionSystem.Instance.ListVisionTool)
                {
                    // 绑定标定工具（用于图像坐标到物理坐标的转换）
                    if (tool.检测工具名称 == imageInfo.绑定标定工具名称)
                    {
                        switch (tool.检测工具类型)
                        {
                            case "CogCalibNPointToNPointTool":
                                // 设置标定工具的输入图像
                                VisionSystem.CalibNPointTools[imageInfo.绑定标定工具索引].InputImage = imageInfo._CogImage;
                                // 运行标定工具
                                VisionSystem.CalibNPointTools[imageInfo.绑定标定工具索引].Run();
                                // 将标定后的图像作为检测工具的输入
                                VisionSystem.ToolBlocks[imageInfo.绑定检测工具索引].Inputs["InputImage"].Value =
                                    VisionSystem.CalibNPointTools[imageInfo.绑定标定工具索引].OutputImage;
                                break;
                            case "CogCalibCheckerboardTool":
                                // 设置标定工具的输入图像
                                VisionSystem.CalibCheckerboardTools[imageInfo.绑定标定工具索引].InputImage = imageInfo._CogImage;
                                // 运行标定工具
                                VisionSystem.CalibCheckerboardTools[imageInfo.绑定标定工具索引].Run();
                                // 将标定后的图像作为检测工具的输入
                                VisionSystem.ToolBlocks[imageInfo.绑定检测工具索引].Inputs["InputImage"].Value =
                                    VisionSystem.CalibCheckerboardTools[imageInfo.绑定标定工具索引].OutputImage;
                                break;
                            default:                    
                                // 未配置标定工具，直接使用原始图像作为输入
                                VisionSystem.ToolBlocks[imageInfo.绑定检测工具索引].Inputs["InputImage"].Value = imageInfo._CogImage;
                                break;
                        }
                    }
                }
                // 运行检测工具块
                VisionSystem.ToolBlocks[imageInfo.绑定检测工具索引].Run();
                // 更新检测结果状态
                imageInfo.RunState = VisionSystem.ToolBlocks[imageInfo.绑定检测工具索引].RunStatus.Result
                    == CogToolResultConstants.Accept;

                // 创建检测结果记录
                ICogRecord record = VisionSystem.ToolBlocks[imageInfo.绑定检测工具索引].CreateLastRunRecord();
                // 在UI上显示检测结果
                SafeUIUpdate(() =>
                {
                    cogRecordDisplay0.Record = record;
                    cogRecordDisplay0.Image = imageInfo._CogImage;
                    cogRecordDisplay0.Fit(true);
                });
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 窗体加载事件处理方法，初始化界面数据
        /// </summary>
        private void Frm_VisionControl_Load(object sender, EventArgs e)
        {
            // 使用带异常处理的UI更新方法
            SafeUIUpdateWithExceptionHandling(this, () =>
            {
                // 加载期间禁用窗体，防止用户操作
                Instance.Enabled = false;

                // 初始化下拉框数据
                InitializeDropDowns();

                // 加载完成，启用窗体
                Instance.Enabled = true;
            });
        }

        /// <summary>
        /// 初始化所有下拉框数据，加载检测点位的类型、定义和名称
        /// </summary>
        private void InitializeDropDowns()
        {
            // 清空现有数据
            cbb_点位类型.Items.Clear();
            cbb_点位定义.Items.Clear();
            cbb_点位名称.Items.Clear();

            // 获取所有检测点位信息
            var inspectionItems = VisionSystem.Instance.ListVisionInspection;
            // 如果没有数据，直接返回
            if (inspectionItems?.Any() != true) return;

            // 填充点位类型（去重）
            var distinctTypes = inspectionItems.Select(item => item.检测点位类型).Distinct();
            cbb_点位类型.Items.AddRange(distinctTypes.ToArray());

            // 填充点位定义（去重）
            var distinctDefinitions = inspectionItems.Select(item => item.检测点位定义).Distinct();
            cbb_点位定义.Items.AddRange(distinctDefinitions.ToArray());

            // 填充点位名称（去重）
            var distinctNames = inspectionItems.Select(item => item.检测点位名称).Distinct();
            cbb_点位名称.Items.AddRange(distinctNames.ToArray());
        }

        /// <summary>
        /// 工具条项点击事件处理方法，根据点击的项执行相应操作
        /// </summary>
        private void ToolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            SafeUIUpdateWithExceptionHandling(toolStrip1, () =>
            {
                // 根据工具条项的名称执行相应操作
                switch (e.ClickedItem.Name)
                {
                    case "tsbtnGrab":
                        // 采集图像
                        GrabImage(_imageInfo);
                        break;
                    case "tsbtnLive":
                        // 切换实时显示
                        ToggleLiveDisplay(_imageInfo);
                        break;
                    case "tsbtnOpen":
                        // 导入图像
                        ImportImageFromDisk(_imageInfo);
                        break;
                    case "tsbtnSave":
                        // 保存图像
                        SaveImageToDisk(_imageInfo);
                        break;
                    case "tsbtnRun":
                        // 处理图像
                        ProcessImage(_imageInfo);
                        break;
                }
            });
        }

        /// <summary>
        /// 点位类型选择变化事件处理方法，更新相关的点位定义下拉框
        /// </summary>
        private void CbbInspectionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            SafeUIUpdateWithExceptionHandling(cbb_点位类型, () =>
            {
                // 检查是否选择了有效项
                if (cbb_点位类型.SelectedItem == null)
                {
                    Frm_MessageBox.Instance.MessageBoxShow("请先选择对应的点位类型！", TipLevel.Warning);
                    return;
                }

                // 保存选中的点位类型
                _inspectionPointType = cbb_点位类型.SelectedItem.ToString();
                // 更新点位定义下拉框
                UpdateDefinitionDropDown();
                // 未完成所有选择前，禁用工具条
                toolStrip1.Enabled = false;
            });
        }

        /// <summary>
        /// 点位定义选择变化事件处理方法，更新相关的点位名称下拉框
        /// </summary>
        private void CbbInspectionDefinition_SelectedIndexChanged(object sender, EventArgs e)
        {
            SafeUIUpdateWithExceptionHandling(cbb_点位定义, () =>
            {
                // 检查是否选择了有效项
                if (cbb_点位定义.SelectedItem == null)
                {
                    Frm_MessageBox.Instance.MessageBoxShow("请先选择对应的点位定义！", TipLevel.Warning);
                    return;
                }

                // 保存选中的点位定义
                _inspectionPointDefinition = cbb_点位定义.SelectedItem.ToString();
                // 更新点位名称下拉框
                UpdateNameDropDown();
                // 未完成所有选择前，禁用工具条
                toolStrip1.Enabled = false;
            });
        }

        /// <summary>
        /// 点位名称选择变化事件处理方法，更新图像信息并启用工具条
        /// </summary>
        private void CbbInspectionName_SelectedIndexChanged(object sender, EventArgs e)
        {
            SafeUIUpdateWithExceptionHandling(cbb_点位名称, () =>
            {
                // 检查是否选择了有效项
                if (cbb_点位名称.SelectedItem == null)
                {
                    Frm_MessageBox.Instance.MessageBoxShow("请先选择对应的点位名称！", TipLevel.Warning);
                    return;
                }

                // 保存选中的点位名称
                _inspectionPointName = cbb_点位名称.SelectedItem.ToString();
                // 根据选中的点位更新图像信息
                UpdateImageInfo();
                // 完成所有选择，启用工具条
                toolStrip1.Enabled = true;
            });
        }
        #endregion

        #region DropDown Helpers
        /// <summary>
        /// 更新点位定义下拉框，根据当前选中的点位类型筛选
        /// </summary>
        private void UpdateDefinitionDropDown()
        {
            // 清空现有数据
            cbb_点位定义.Items.Clear();
            cbb_点位名称.Items.Clear();

            // 根据当前点位类型筛选点位定义
            var definitions = VisionSystem.Instance.ListVisionInspection
                .Where(item => item.检测点位类型 == _inspectionPointType)
                .Select(item => item.检测点位定义)
                .Distinct();

            // 填充筛选后的点位定义
            cbb_点位定义.Items.AddRange(definitions.ToArray());
        }

        /// <summary>
        /// 更新点位名称下拉框，根据当前选中的点位类型和定义筛选
        /// </summary>
        private void UpdateNameDropDown()
        {
            // 清空现有数据
            cbb_点位名称.Items.Clear();

            // 根据当前点位类型和定义筛选点位名称
            var names = VisionSystem.Instance.ListVisionInspection
                .Where(item => item.检测点位类型 == _inspectionPointType
                            && item.检测点位定义 == _inspectionPointDefinition)
                .Select(item => item.检测点位名称)
                .Distinct();

            // 填充筛选后的点位名称
            cbb_点位名称.Items.AddRange(names.ToArray());
        }

        /// <summary>
        /// 根据选中的检测点位更新图像信息，包括绑定的工具索引等
        /// </summary>
        private void UpdateImageInfo()
        {
            // 查找选中的检测点位信息
            var inspectionItem = VisionSystem.Instance.ListVisionInspection
                .FirstOrDefault(item => item.检测点位类型 == _inspectionPointType
                                    && item.检测点位定义 == _inspectionPointDefinition
                                    && item.检测点位名称 == _inspectionPointName);

            // 如果未找到，直接返回
            if (inspectionItem == null) return;

            // 更新图像信息对象
            _imageInfo = new ImageInfo
            {
                检测点位名称 = inspectionItem.检测点位名称,
                检测点位类型 = inspectionItem.检测点位类型,
                检测点位定义 = inspectionItem.检测点位定义,
                检测点位索引 = inspectionItem.检测点位索引,
                绑定取像工具名称 = inspectionItem.绑定取像工具名称,
                绑定检测工具名称 = inspectionItem.绑定检测工具名称,
                绑定标定工具名称 = inspectionItem.绑定标定工具名称,
                产品颜色定义 = inspectionItem.产品颜色定义,
                取像工具曝光值 = inspectionItem.取像工具曝光值,
                取像工具对比度值 = inspectionItem.取像工具对比度值,
                _CogImage = null,
                _Record = null,
                JpgBitmap = null,
                Result = "ERR",
                RunState = false,
            };

            // 根据工具名称更新工具索引
            foreach (var tool in VisionSystem.Instance.ListVisionTool)
            {
                if (tool.检测工具名称 == _imageInfo.绑定取像工具名称)
                    _imageInfo.绑定取像工具索引 = tool.检测工具索引;

                if (tool.检测工具名称 == _imageInfo.绑定标定工具名称)
                    _imageInfo.绑定标定工具索引 = tool.检测工具索引;

                if (tool.检测工具名称 == _imageInfo.绑定检测工具名称)
                    _imageInfo.绑定检测工具索引 = tool.检测工具索引;
            }
        }
        #endregion
    }
}
