using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ToolBlock;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Vision;
using Jabil.Module.Vision.Common.Dtos;
using Jabil.Module.Vision.Lib;
using JabilVisionMotion.Frm.UserPage;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jabil.Module.Setting.Lib.JVS;
using pyjvl2._C.libjvl;

namespace JabilVisionMotion.Edit.Process
{
    /// <summary>
    /// 相机定点拍照流程处理类，继承自ProcessBase基类
    /// 指令格式：Vision|点位定义|点位类型|点位名称|超时（秒）
    /// 示例：Vision|正反检测|下相机左|吸头一|5  （使用下相机左对"吸头一"进行正反检测，超时5秒）
    /// 核心功能：
    /// - 控制相机完成图像采集（支持重试机制）
    /// - 执行视觉检测（如正反检测等）
    /// - 处理检测结果并更新UI展示
    /// 依赖：Cognex视觉工具（如取像工具、检测工具、标定工具）
    /// </summary>
    public class VisionProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        /// <summary>最大拍照重试次数，防止因瞬时干扰导致的采集失败</summary>
        private const int MaxCaptureRetries = 3;
        /// <summary>拍照重试间隔（毫秒），避免高频重试导致的资源冲突</summary>
        private const int CaptureRetryIntervalMs = 1000;
        #endregion

        #region 私有字段
        private readonly string _process;        // 原始视觉指令字符串
        private readonly string _processTip;     // 指令提示信息（用于日志）
        private static readonly object _lock = new object(); // 线程锁，确保视觉操作线程安全

        // 指令解析结果    Vision|正向|前工位|定点拍照|5
        private string _commandName;            // 指令名称（固定为"Vision"）
        private string _detectionDefinition;    // 点位定义（如"正反检测"，表示检测类型）
        private string _detectionType;          // 点位类型（如"下相机左"，表示检测设备）
        private string _detectionName;          // 点位名称（如"吸头一"，表示检测对象）

        // 视觉参数
        private ImageInfo _imageInfo;           // 图像信息对象（存储采集参数、检测结果等）
        private VisionInspectionDto _visionInspection; // 视觉检测配置（从系统中查询的预设配置）

        private CogImage8Grey Image8Grey = new CogImage8Grey();

        // Cognex视觉工具（康耐视视觉库组件）
        private CogAcqFifoTool _cogAcqFifo;    // 取像工具（控制相机采集图像）
        private CogToolBlock _cogToolBlock;     // 检测工具块（执行具体视觉检测算法）
        private CogCalibNPointToNPointTool _cogCalibNPointToNPoint; // N点标定工具（用于坐标转换）
        private CogCalibCheckerboardTool _cogCalibCheckerboard; // 棋盘格标定工具（用于相机校准）

        // JVS 进程内嵌入（第二种方式：JvlDotNet）
        private JvsToolBlockRunner _jvsRunner;       // JVS 检测工具运行器（缓存于 JvsVisionBridge，装配一次）
        private string _jvsWorkspacePath;            // JVS .jvws 方案绝对路径
        private bool _jvsTakeToolBound;              // 取像工具是否为 JVS 模式（相机由 JVS 取图工具算子驱动）
        private string _jvsCaptureToolPath;          // JVS 取图工具文件（.jvt）绝对路径（VisionTool 表 JvsCameraTool 记录）
        private pyjvl2._C.libjvl.Image _jvsImage;  // 最近一次 JVS 相机取像输出（所有权归取图工具，勿 Dispose）
        private readonly Stopwatch _jvsElapsed = new Stopwatch(); // JVS 单次检测耗时统计
        #endregion

        /// <summary>
        /// 初始化相机定点拍照流程
        /// </summary>
        /// <param name="process">视觉指令字符串（如"Vision|正反检测|下相机左|吸头一|5"）</param>
        /// <param name="processTip">指令提示信息（用于日志显示）</param>
        public VisionProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait;
                Message = $"[{DateTime.Now:HH:mm:ss}] 初始化视觉流程：等待执行（指令：{process}）"; // 初始消息包含指令详情
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);      // 解析指令参数
                BindVisionParameters();     // 绑定视觉配置和工具
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = $"[{DateTime.Now:HH:mm:ss}] 初始化失败：{ex.Message}（指令：{process}）"; // 错误消息包含指令上下文
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 克隆方法，支持多实例并发时的状态独立性
        /// </summary>
        public override object Clone()
        {
            var clone = new VisionProcess(_process, _processTip)
            {
                _commandName = this._commandName,
                _detectionDefinition = this._detectionDefinition,
                _detectionType = this._detectionType,
                _detectionName = this._detectionName,
                _visionInspection = this._visionInspection,
                _imageInfo = this._imageInfo,
                _cogAcqFifo = this._cogAcqFifo,
                _cogToolBlock = this._cogToolBlock,
                _cogCalibNPointToNPoint = this._cogCalibNPointToNPoint,
                _cogCalibCheckerboard = this._cogCalibCheckerboard,
                Message = this.Message + "（已克隆实例）" // 克隆消息附加标识
            };

            return clone;
        }

        /// <summary>
        /// 解析视觉指令参数，提取检测相关的配置信息
        /// </summary>
        /// <param name="process">待解析的视觉指令字符串</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式（至少5个部分）
            if (commandParts.Length < 5)
            {
                throw new ArgumentException($"指令格式错误: 需满足'指令名称|点位定义|点位类型|点位名称|超时'，实际为{process}");
            }

            // 提取指令各部分信息
            _commandName = commandParts[0];
            _detectionDefinition = commandParts[1]; // 点位定义（如"正反检测"）
            _detectionType = commandParts[2];       // 点位类型（如"下相机左"）
            _detectionName = commandParts[3];       // 点位名称（如"吸头一"）
                                                    // 注：超时参数在基类处理，此处未单独存储

            Message = $"[{DateTime.Now:HH:mm:ss}] 指令解析成功：指令名称={_commandName}，检测类型={_detectionType}，检测对象={_detectionName}";
        }

        /// <summary>
        /// 绑定视觉检测参数（从系统配置中查询预设的检测配置，并初始化图像信息和工具）
        /// </summary>
        private void BindVisionParameters()
        {
            Message = $"[{DateTime.Now:HH:mm:ss}] 开始绑定视觉配置：类型={_detectionType}，定义={_detectionDefinition}，名称={_detectionName}";

            // 从系统中查找匹配的视觉检测配置（根据类型、定义、名称）
            _visionInspection = VisionSystem.Instance.ListVisionInspection
                .FirstOrDefault(vi =>
                    vi.检测点位类型 == _detectionType &&
                    vi.检测点位定义 == _detectionDefinition &&
                    vi.检测点位名称 == _detectionName);

            // 未找到配置时处理错误
            if (_visionInspection == null)
            {
                HandleVisionError($"未找到匹配的视觉配置 [类型:{_detectionType}, 定义:{_detectionDefinition}, 名称:{_detectionName}]");
                return;
            }

            ProcessId = _visionInspection.绑定代码;
            Message = $"[{DateTime.Now:HH:mm:ss}] 配置绑定成功：流程ID={ProcessId}，取像工具={_visionInspection.绑定取像工具名称}，检测工具={_visionInspection.绑定检测工具名称}";

            // 初始化图像信息对象（存储采集和检测的关键参数）
            _imageInfo = new ImageInfo
            {
                检测点位名称 = _visionInspection.检测点位名称,
                检测点位类型 = _visionInspection.检测点位类型,
                检测点位定义 = _visionInspection.检测点位定义,
                检测点位索引 = _visionInspection.检测点位索引,
                绑定取像工具名称 = _visionInspection.绑定取像工具名称,
                绑定检测工具名称 = _visionInspection.绑定检测工具名称,
                绑定标定工具名称 = _visionInspection.绑定标定工具名称,
                产品颜色定义 = _visionInspection.产品颜色定义,
                取像工具曝光值 = _visionInspection.取像工具曝光值,
                取像工具对比度值 = _visionInspection.取像工具对比度值,
                _CogImage = null,
                _Record = null,
                JpgBitmap = null,
                Result = "ERR",
                RunState = false
            };

            // 绑定具体的视觉工具（取像、检测、标定工具）
            BindVisionTools();
        }

        /// <summary>
        /// 绑定视觉处理工具（根据配置中的工具名称匹配系统中的Cognex工具实例）
        /// </summary>
        private void BindVisionTools()
        {
            Message = $"[{DateTime.Now:HH:mm:ss}] 开始绑定视觉工具：取像工具={_imageInfo.绑定取像工具名称}，检测工具={_imageInfo.绑定检测工具名称}";

            // 遍历系统中的视觉工具，匹配配置中指定的工具
            foreach (var tool in VisionSystem.Instance.ListVisionTool)
            {
                // 绑定取像工具（控制相机采集图像）
                if (tool.检测工具名称 == _imageInfo.绑定取像工具名称)
                {
                    _imageInfo.绑定取像工具索引 = tool.检测工具索引;
                    if (tool.检测工具类型 == "JvsCameraTool")
                    {
                        // JVS 模式：取图由 JVS workspace 相机工具算子驱动（.jvt，CameraTool），
                        // 点胶机进程不驱动任何硬件相机 SDK，不加载 Cognex 取像工具
                        _jvsTakeToolBound = true;
                        _jvsCaptureToolPath = tool.检测工具路径;
                        Message += $", 取像工具索引={_imageInfo.绑定取像工具索引}（JVS 取图工具={_jvsCaptureToolPath}）";
                    }
                    else
                    {
                        _cogAcqFifo = VisionSystem.AcqFifoTools[_imageInfo.绑定取像工具索引];
                        Message += $", 取像工具索引={_imageInfo.绑定取像工具索引}";
                    }
                }

                // 绑定标定工具（用于图像坐标到物理坐标的转换）
                if (tool.检测工具名称 == _imageInfo.绑定标定工具名称)
                {
                    _imageInfo.绑定标定工具索引 = tool.检测工具索引;
                    // 根据工具类型匹配具体的标定工具实例
                    switch (tool.检测工具类型)
                    {
                        case "CogCalibNPointToNPointTool":
                            _cogCalibNPointToNPoint = VisionSystem.CalibNPointTools[_imageInfo.绑定标定工具索引];
                            Message += $", N点标定工具索引={_imageInfo.绑定标定工具索引}";
                            break;
                        case "CogCalibCheckerboardTool":
                            _cogCalibCheckerboard = VisionSystem.CalibCheckerboardTools[_imageInfo.绑定标定工具索引];
                            Message += $", 棋盘格标定工具索引={_imageInfo.绑定标定工具索引}";
                            break;
                    }
                }

                // 绑定检测工具块（执行具体的视觉检测算法，如正反判断）
                if (tool.检测工具名称 == _imageInfo.绑定检测工具名称)
                {
                    _imageInfo.绑定检测工具索引 = tool.检测工具索引;
                    if (tool.检测工具类型 == "JvsWorkspaceTool")
                    {
                        // JVS 模式：从 JvsVisionBridge 获取进程内 ToolBlock 运行器（装配一次，缓存复用）
                        _jvsWorkspacePath = tool.检测工具路径;
                        _jvsRunner = JvsVisionBridge.Instance.GetRunner(
                            tool.检测工具名称, tool.检测工具路径, out var jvsError);
                        if (_jvsRunner == null)
                            throw new InvalidOperationException(
                                $"JVS 检测工具装配失败: {_imageInfo.绑定检测工具名称}（{jvsError}）");
                        Message += $", 检测工具索引={_imageInfo.绑定检测工具索引}（JVS 进程内）";
                    }
                    else
                    {
                        _cogToolBlock = VisionSystem.ToolBlocks[_imageInfo.绑定检测工具索引];
                        Message += $", 检测工具索引={_imageInfo.绑定检测工具索引}";
                    }
                }
            }

            // 验证关键工具是否绑定成功（取像和检测工具为必需）
            if (!_jvsTakeToolBound && _cogAcqFifo == null)
                throw new InvalidOperationException($"未找到取像工具: {_imageInfo.绑定取像工具名称}");

            if (_jvsRunner == null && _cogToolBlock == null)
                throw new InvalidOperationException($"未找到检测工具: {_imageInfo.绑定检测工具名称}");

            Message = $"[{DateTime.Now:HH:mm:ss}] 视觉工具绑定完成：所有必需工具已就绪（JVS 模式={( _jvsRunner != null)}）";
        }

        /// <summary>
        /// 处理视觉配置错误（配置缺失时触发紧急停止并记录日志）
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void HandleVisionError(string errorMessage)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            Message = $"[{DateTime.Now:HH:mm:ss}] 配置错误（触发紧急停止）：{errorMessage}";
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _process,
                $"--->>错误：{errorMessage}！！！！"
            );
        }

        /// <summary>
        /// 执行相机拍照流程（核心方法）
        /// 协调图像采集、视觉检测和结果展示的完整流程
        /// </summary>
        public override void Run()
        {
            try
            {
                lock (_lock)
                {
                    CurrentStatus = GeneralStatus.Running;
                    Message = $"[{DateTime.Now:HH:mm:ss}] 开始执行视觉流程：检测类型={_detectionType}，对象={_detectionName}";

                    // 测试模式：直接返回成功（不执行实际采集和检测）
                    if (Project.Instance.GlobalVariables.IsTestMode)
                    {
                        CurrentStatus = GeneralStatus.NoError;
                        Message = $"[{DateTime.Now:HH:mm:ss}] 测试模式：流程执行成功（跳过实际检测）";
                        return;
                    }

                    // 验证配置是否初始化完成
                    if (_imageInfo.检测点位名称 == null || _visionInspection == null)
                    {
                        SetErrorStatus("视觉配置未初始化完成（检测点位名称或配置为空）");
                        return;
                    }

                    // 根据点位定义执行对应的检测流程
                    switch (_detectionDefinition)
                    {
                        case "定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 开始执行定位流程：定义={_detectionDefinition}，对象={_detectionName}";
                            Execute_EPSON相机定位();
                            break;
                        default:
                            SetErrorStatus($"不支持的点位定义: {_detectionDefinition}（当前仅支持'扫码'）");
                            return;
                    }

                    // 根据检测状态设置最终结果
                    if (_imageInfo.RunState)
                    {
                        CurrentStatus = GeneralStatus.NoError;
                        Message = $"[{DateTime.Now:HH:mm:ss}] 视觉流程执行成功：检测结果={_imageInfo.Result}，对象={_detectionName}";
                    }
                    else
                    {
                        CurrentStatus = GeneralStatus.Error;
                        Message = $"[{DateTime.Now:HH:mm:ss}] 视觉流程执行失败：检测状态未通过，对象={_detectionName}";
                    }
                }
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = $"[{DateTime.Now:HH:mm:ss}] 视觉流程异常终止：{ex.Message}（堆栈：{ex.StackTrace.Substring(0, 100)}...）";
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 执行扫码检测流程（具体检测类型的实现）
        /// 根据取像工具名称区分不同相机
        /// </summary>
        private void Execute_EPSON相机定位()
        {
            Message = $"[{DateTime.Now:HH:mm:ss}] 进入上相机定位流程：取像工具={_imageInfo.绑定取像工具名称}，对象={_detectionName}";

            // 根据绑定的取像工具名称，调用对应相机的采集和检测逻辑
            switch (_imageInfo.绑定取像工具名称)
            {
                case "左上相机":
                    // 处理扫码相机的采集和显示
                    switch (_detectionName)
                    {
                        case "Epson2左上定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理左上定位：使用左上定位相机";
                            ProcessCameraCapture("左上定位",
                                MainPage.Instance.cogRecordDisplay2,
                                (display, record) => UpdateDisplay(display, record));
                            break;
                        case "Epson1左上定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理左上定位：使用左上定位相机";
                            ProcessCameraCapture("左上定位",
                                MainPage.Instance.cogRecordDisplay2,
                                (display, record) => UpdateDisplay(display, record));
                            break;
                        default:
                            SetErrorStatus($"不支持的检测点位名称: {_detectionName}（扫码相机支持的对象：扫码检测1、扫码检测2）");
                            break;
                    }
                    break;
                case "右上相机":
                    // 处理扫码相机的采集和显示
                    switch (_detectionName)
                    {
                        case "Epson2右上定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理左上定位：使用左上定位相机";
                            ProcessCameraCapture("右上定位",
                                MainPage.Instance.cogRecordDisplay4,
                                (display, record) => UpdateDisplay(display, record));
                            break;
                        case "Epson1右上定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理左上定位：使用左上定位相机";
                            ProcessCameraCapture("右上定位",
                                MainPage.Instance.cogRecordDisplay4,
                                (display, record) => UpdateDisplay(display, record));
                            break;
                        default:
                            SetErrorStatus($"不支持的检测点位名称: {_detectionName}（扫码相机支持的对象：扫码检测1、扫码检测2）");
                            break;
                    }
                    break;
                case "左下相机":
                    switch (_detectionName)
                    {
                        case "左下定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理左下定位：使用左下定位相机";
                            ProcessCameraCapture("左下定位",
                                MainPage.Instance.cogRecordDisplay3,
                                (display, record) => UpdateDisplay(display, record));
                            break;
                        default:
                            SetErrorStatus($"不支持的检测点位名称: {_detectionName}（扫码相机支持的对象：扫码检测1、扫码检测2）");
                            break;
                    }
                    break;
                case "右下相机":
                    switch (_detectionName)
                    {
                        case "右下定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理左下定位：使用左下定位相机";
                            ProcessCameraCapture("右下定位",
                                MainPage.Instance.cogRecordDisplay5,
                                (display, record) => UpdateDisplay(display, record));
                            break;
                        default:
                            SetErrorStatus($"不支持的检测点位名称: {_detectionName}（扫码相机支持的对象：扫码检测1、扫码检测2）");
                            break;
                    }
                    break;
                case "飞拍相机":
                    switch (_detectionName)
                    {
                        case "飞拍定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理飞拍定位：使用飞拍相机";
                            ProcessCameraCapture("飞拍定位",
                                MainPage.Instance.cogRecordDisplay1,
                                (display, record) => UpdateDisplay(display, record));
                            break;
                        case "飞拍复检":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理飞拍定位：使用飞拍相机";
                            ProcessCameraCapture("飞拍复检",
                                MainPage.Instance.cogRecordDisplay1,
                                (display, record) => UpdateDisplay(display, record));
                            break;
                        default:
                            SetErrorStatus($"不支持的检测点位名称: {_detectionName}（扫码相机支持的对象：扫码检测1、扫码检测2）");
                            break;
                    }
                    break;
                default:
                    SetErrorStatus($"不支持的取像工具: {_imageInfo.绑定取像工具名称}（当前支持：扫码相机、LED_相机右）");
                    break;
            }
        }


        /// <summary>
        /// 处理相机图像采集与检测的通用逻辑
        /// 包含参数配置、图像采集、检测执行和结果显示
        /// </summary>
        /// <param name="cameraType">相机类型（如下相机左）</param>
        /// <param name="displayAccessor">显示控件访问器（用于获取UI显示控件）</param>
        /// <param name="displayUpdater">显示更新器（用于更新UI显示内容）</param>
        private void ProcessCameraCapture(string cameraType,
                                         CogRecordDisplay displayAccessor,
                                         Action<object, object> displayUpdater)
        {
            Message = $"[{DateTime.Now:HH:mm:ss}] 开始相机采集流程：相机类型={cameraType}，对象={_detectionName}";

            // ===== JVS 模式（进程内嵌入）：相机由 JVS 相机 API 取像，检测在进程内 ToolBlock 执行 =====
            if (_jvsRunner != null)
            {
                if (!CaptureWithJvsCamera())
                    return;

                if (!ProcessJvsImageDetection(cameraType, _detectionName))
                    return;

                // 更新UI显示检测结果（JVS 模式显示最近一次 JVS 检测结果文本）
                UpdateVisionDisplay(displayAccessor, displayUpdater);
                Message = $"[{DateTime.Now:HH:mm:ss}] UI显示更新完成：相机类型={cameraType}，JVS 检测完成";
                return;
            }

            if (cameraType == "左上定位" || cameraType == "左下定位"|| cameraType == "右上定位" || cameraType == "右下定位")
            {
                // 配置相机参数（曝光、对比度）并采集图像（带重试）
                ConfigureAndCaptureImage();

                // 验证图像采集结果
                if (_cogAcqFifo.RunStatus.Result != CogToolResultConstants.Accept ||
                    _cogAcqFifo.OutputImage == null)
                {
                    SetErrorStatus($"取像工具 {_imageInfo.绑定取像工具名称} 取图失败（重试{MaxCaptureRetries}次后仍无有效图像）");
                    return;
                }
                else
                {
                    Image8Grey = _cogAcqFifo.OutputImage as CogImage8Grey;
                    Message = $"[{DateTime.Now:HH:mm:ss}] 图像采集成功：取像工具={_imageInfo.绑定取像工具名称}，图像类型={Image8Grey?.GetType().Name}";
                }
            }
            else
            {
                Image8Grey = Controller.Instance.StationInfo.image8Greys;
                Message = $"[{DateTime.Now:HH:mm:ss}] 使用预存图像：相机类型={cameraType}，图像来源=StationInfo";
            }

            // 执行图像检测
            if (!ProcessImageDetection(cameraType, _detectionName))
                return;

            // 更新UI显示检测结果
            UpdateVisionDisplay(displayAccessor, displayUpdater);
            UpdateVisionDisplay(MainPage.Instance.cogRecordDisplay_结果图保存, displayUpdater);
            Message = $"[{DateTime.Now:HH:mm:ss}] UI显示更新完成：相机类型={cameraType}，显示控件={displayAccessor?.GetType().Name}";
        }


        /// <summary>
        /// 配置相机参数并采集图像（带重试机制）
        /// 确保图像采集的可靠性
        /// </summary>
        private void ConfigureAndCaptureImage()
        {
            // 设置相机采集参数（曝光值和对比度，从配置中获取）
            _cogAcqFifo.Operator.OwnedExposureParams.Exposure = _imageInfo.取像工具曝光值;
            _cogAcqFifo.Operator.OwnedContrastParams.Contrast = _imageInfo.取像工具对比度值;
            Message = $"[{DateTime.Now:HH:mm:ss}] 相机参数配置完成：曝光值={_imageInfo.取像工具曝光值}，对比度={_imageInfo.取像工具对比度值}";

            // 采集图像（带重试逻辑，最多重试MaxCaptureRetries次）
            bool captureSuccess = false;
            for (int i = 0; i < MaxCaptureRetries; i++)
            {
                _cogAcqFifo.Run();
                if (_cogAcqFifo.OutputImage != null)
                {
                    captureSuccess = true;
                    Message = $"[{DateTime.Now:HH:mm:ss}] 第{i + 1}次采集成功：取像工具={_imageInfo.绑定取像工具名称}";
                    break;
                }
                Message = $"[{DateTime.Now:HH:mm:ss}] 第{i + 1}次采集失败：取像工具无输出图像，将在{CaptureRetryIntervalMs}ms后重试";
                Thread.Sleep(CaptureRetryIntervalMs); // 重试间隔
            }

            if (!captureSuccess)
            {
                Message = $"[{DateTime.Now:HH:mm:ss}] 所有重试均失败：取像工具={_imageInfo.绑定取像工具名称}，重试次数={MaxCaptureRetries}";
            }
        }

        /// <summary>
        /// JVS 相机取像（进程内，带重试）
        /// 取图由 JVS workspace 相机工具算子驱动：加载 .jvt（CameraTool）→ run() → output_image
        /// 调用格式对齐原框架 cogAcqFifo.Run() 语义，成功后 OutputImage 即 JVS Image
        /// </summary>
        private bool CaptureWithJvsCamera()
        {
            var camera = JvsVisionBridge.Instance.Camera;
            var mapping = JvsVisionBridge.Instance.Mapping;

            // 取图工具文件路径：优先 VisionTool 表 JvsCameraTool 记录（绑定取像工具），兜底映射配置
            string toolPath = _jvsCaptureToolPath;
            if (string.IsNullOrWhiteSpace(toolPath))
                toolPath = mapping.CaptureToolPath;

            if (!camera.Load(toolPath))
            {
                SetErrorStatus($"JVS 取图工具加载失败：{camera.LastError}");
                return false;
            }

            Message = $"[{DateTime.Now:HH:mm:ss}] JVS 相机取像：工具={Path.GetFileName(toolPath)}";

            bool captureSuccess = false;
            for (int i = 0; i < MaxCaptureRetries; i++)
            {
                if (camera.Run())
                {
                    _jvsImage = camera.OutputImage;
                    captureSuccess = true;
                    Message += $"，第{i + 1}次采集成功（图像={_jvsImage?.width}x{_jvsImage?.height}）";
                    break;
                }
                Message += $"，第{i + 1}次采集失败：{camera.LastError}，将在{CaptureRetryIntervalMs}ms后重试";
                Thread.Sleep(CaptureRetryIntervalMs);
            }

            if (!captureSuccess)
            {
                SetErrorStatus($"JVS 相机取像失败（重试{MaxCaptureRetries}次后仍无有效图像）：{camera.LastError}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// JVS 进程内检测执行（第二种方式：JvlDotNet）
        /// 调用格式与原框架对齐：相机 Run()→OutputImage（JVS Image）→ runner.SetInput(输入pin, 图像)
        /// → runner.Run() → 读输出 pin（data）→ JvsDataParser 解析为点数组 → 写 StationInfo/UI
        /// </summary>
        /// <param name="cameraType">相机类型（仅用于日志）</param>
        /// <param name="detectionName">检测点位名称（决定结果写入哪个容器）</param>
        /// <returns>是否检测成功</returns>
        private bool ProcessJvsImageDetection(string cameraType, string detectionName)
        {
            Message = $"[{DateTime.Now:HH:mm:ss}] 开始 JVS 检测（进程内）：相机类型={cameraType}，对象={detectionName}";

            if (_jvsImage == null)
            {
                SetErrorStatus("JVS 检测输入图像为空（取像失败或未取像）");
                return false;
            }

            var mapping = JvsVisionBridge.Instance.Mapping;

            _jvsElapsed.Restart();

            // 输入 pin 赋值（对齐原框架：_cogToolBlock.Inputs["InputImage"].Value = Image8Grey）
            try
            {
                _jvsRunner.SetInput(mapping.InputPin, _jvsImage);
            }
            catch (Exception ex)
            {
                SetErrorStatus($"JVS 输入 pin 赋值失败（pin={mapping.InputPin}）：{ex.Message}");
                return false;
            }

            // 运行 ToolBlock（对齐原框架：_cogToolBlock.Run()）
            var runStatus = _jvsRunner.Run();
            var run = new JvsRunResult();
            run.TotalMs = runStatus.total_time * 1000.0;
            run.ProcessingMs = runStatus.processing_time * 1000.0;
            run.Success = runStatus.success;

            // 冷启动保护：JVS 引擎冷启动/长时间空闲后前 1~2 次 run 可能 data 为空
            // （run_status.success=true 但 pts=0），实测复跑即恢复。自动重试一次，仍空才判失败。
            if (run.Success)
            {
                object raw = _jvsRunner[mapping.OutputPin];
                run.RawData = raw == null ? null : raw.ToString();
                run.Points = JvsDataParser.Parse(raw, mapping);
                if (run.Points.Count == 0)
                {
                    Message += "，首帧空结果，等待后自动重试一次";
                    Thread.Sleep(400);
                    runStatus = _jvsRunner.Run();
                    run.TotalMs = runStatus.total_time * 1000.0;
                    run.ProcessingMs = runStatus.processing_time * 1000.0;
                    run.Success = runStatus.success;
                    raw = _jvsRunner[mapping.OutputPin];
                    run.RawData = raw == null ? null : raw.ToString();
                    run.Points = JvsDataParser.Parse(raw, mapping);
                }
            }
            else
            {
                run.Error = $"run_status.success=false（total={run.TotalMs:F0}ms）";
            }

            _jvsElapsed.Stop();
            int elapsedMs = (int)_jvsElapsed.ElapsedMilliseconds;

            if (!run.Success)
            {
                SetErrorStatus($"JVS 检测失败：{run.Error}");
                Controller.Instance.StationInfo.JvsLastResultDisplay = $"ERR | 点位={detectionName} | JVS 检测失败：{run.Error}";
                MainPage.Instance?.AddVisionDataRow("ERR", detectionName, 0, elapsedMs, elapsedMs, (int)run.TotalMs, (int)run.ProcessingMs, run.Error);
                return false;
            }
            if (run.Points.Count == 0)
            {
                SetErrorStatus($"JVS 检测未返回有效检测点（输出 pin={mapping.OutputPin} 为空或格式不符；已自动重试一次）");
                Controller.Instance.StationInfo.JvsLastResultDisplay = $"ERR | 点位={detectionName} | 未返回有效检测点（重试后仍为空）";
                MainPage.Instance?.AddVisionDataRow("ERR", detectionName, 0, elapsedMs, elapsedMs, (int)run.TotalMs, (int)run.ProcessingMs, "未返回有效检测点（重试后仍为空）");
                return false;
            }

            Message += $"，JVS处理={run.ProcessingMs:F0}ms，检测到{run.Points.Count}个点（data={run.RawData}）";
            _imageInfo.RunState = true;
            _imageInfo.Result = "OK";

            // ===== UI 显示：点胶机侧显示 OK + X/Y/R 数值（原图过渡期由 JVS 界面兜底）=====
            string jvsDisplay =
                $"{_imageInfo.Result} | 点位={detectionName} | 点数={run.Points.Count} | " +
                string.Join(";", run.Points.Select(p => $"{p.X:F3},{p.Y:F3},{p.R:F3}"));
            Controller.Instance.StationInfo.JvsLastResultDisplay = jvsDisplay;
            // 同步写入主界面数据页视觉数据表（耗时对比）
            MainPage.Instance?.AddVisionDataRow(
                _imageInfo.Result, detectionName, run.Points.Count,
                elapsedMs, elapsedMs, (int)run.TotalMs, (int)run.ProcessingMs,
                string.Join(";", run.Points.Select(p => $"{p.X:F3},{p.Y:F3},{p.R:F3}")));
            Message += $"，UI显示={Controller.Instance.StationInfo.JvsLastResultDisplay}";

            // 结果写入 StationInfo（按检测点位名称）
            return ApplyJvsResult(detectionName, run.Points);
        }

        /// <summary>
        /// 将 JVS 检测点数组写入 StationInfo（与 Cog 版 ProcessImageDetection 的 switch 语义对齐）
        /// - Epson 定位类：每个点入队 Epson1/Epson2 的 X/Y/R 队列（与 Cog 版遍历 Results 多行一致）
        /// - 下相机定位类（左下/右下）：取首点，按请求位置号走标准位/固定补偿/旋转中心补偿计算
        /// </summary>
        private bool ApplyJvsResult(string detectionName, List<JvsPoint> points)
        {
            var st = Controller.Instance.StationInfo;
            switch (detectionName)
            {
                case "Epson2左上定位":
                case "Epson2右上定位":
                    foreach (var p in points)
                    {
                        st.Epson2X.Enqueue(Math.Round(p.X, 3));
                        st.Epson2Y.Enqueue(Math.Round(p.Y, 3));
                        st.Epson2R.Enqueue(Math.Round(p.R, 3));
                    }
                    break;
                case "Epson1左上定位":
                case "Epson1右上定位":
                    foreach (var p in points)
                    {
                        st.Epson1X.Enqueue(Math.Round(p.X, 3));
                        st.Epson1Y.Enqueue(Math.Round(p.Y, 3));
                        st.Epson1R.Enqueue(Math.Round(p.R, 3));
                    }
                    break;
                case "左下定位":
                    return ApplyJvsDownLocation("左", points, st.Epson2下相机请求位置号);
                case "右下定位":
                    return ApplyJvsDownLocation("右", points, st.Epson1下相机请求位置号);
                default:
                    SetErrorStatus($"JVS 模式暂不支持检测点位：{detectionName}");
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 下相机定位补偿计算（JVS 版，与 Cog 版 ProcessImageDetection 左下/右下定位逻辑一致）
        /// 取 points 首点作为拍照数据，按请求位置号（1-6）取标准位/固定补偿/旋转中心
        /// </summary>
        /// <param name="side">"左" 或 "右"</param>
        /// <param name="points">JVS 检测点数组</param>
        /// <param name="requestPos">请求位置号（1-6）</param>
        private bool ApplyJvsDownLocation(string side, List<JvsPoint> points, int requestPos)
        {
            var st = Controller.Instance.StationInfo;
            if (requestPos < 1 || requestPos > 6)
            {
                SetErrorStatus($"下相机{side}定位请求位置号无效：{requestPos}");
                return false;
            }
            if (points.Count == 0)
            {
                SetErrorStatus($"下相机{side}定位：JVS 未返回检测点");
                return false;
            }

            // JVS 方案输出多点时先取首点（多点映射待现场确认后扩展）
            var p = points[0];
            if (points.Count > 1)
                Message += $"，检测到{points.Count}个点，下相机定位先取首点（多点映射待确认）";

            var 下相机标准位 = side == "左" ? GetLeftDownStandard(requestPos) : GetRightDownStandard(requestPos);
            var 下相机拍照数据 = new CoordinateData(p.X, p.Y, p.R);
            var 固定补偿 = side == "左" ? GetLeftDownCompensation(requestPos) : GetRightDownCompensation(requestPos);
            var 旋转中心 = side == "左" ? GetLeftDownRotationCenter(requestPos) : GetRightDownRotationCenter(requestPos);

            var calculate = st.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

            if (side == "左")
            {
                st.downCamX左[requestPos - 1] = calculate.X * (-1);
                st.downCamY左[requestPos - 1] = calculate.Y * (-1);
                st.downCamR左[requestPos - 1] = calculate.Rotation * (-1);
                st.Epson2下相机拍照结果 = 1;
                st.Epson2下相机请求位置号 = 0;
            }
            else
            {
                st.downCamX右[requestPos - 1] = calculate.X * (-1);
                st.downCamY右[requestPos - 1] = calculate.Y * (-1);
                st.downCamR右[requestPos - 1] = calculate.Rotation * (-1);
                // 与 Cog 版一致：右相机带角度偏差判断（>5° 记结果 2）
                if (Math.Abs(下相机标准位.Rotation - 下相机拍照数据.Rotation) > 5)
                    st.Epson1下相机拍照结果 = 2;
                else
                    st.Epson1下相机拍照结果 = 1;
                st.Epson1下相机请求位置号 = 0;
            }
            return true;
        }

        #region 下相机定位补偿参数（左/右 × 位置号 1-6，与 Cog 版取值一致）
        private static CoordinateData GetLeftDownStandard(int n) => n switch
        {
            1 => new CoordinateData(Project.Instance.postion左Epson.X1, Project.Instance.postion左Epson.Y1, Project.Instance.postion左Epson.R1),
            2 => new CoordinateData(Project.Instance.postion左Epson.X2, Project.Instance.postion左Epson.Y2, Project.Instance.postion左Epson.R2),
            3 => new CoordinateData(Project.Instance.postion左Epson.X3, Project.Instance.postion左Epson.Y3, Project.Instance.postion左Epson.R3),
            4 => new CoordinateData(Project.Instance.postion左Epson.X4, Project.Instance.postion左Epson.Y4, Project.Instance.postion左Epson.R4),
            5 => new CoordinateData(Project.Instance.postion左Epson.X5, Project.Instance.postion左Epson.Y5, Project.Instance.postion左Epson.R5),
            6 => new CoordinateData(Project.Instance.postion左Epson.X6, Project.Instance.postion左Epson.Y6, Project.Instance.postion左Epson.R6),
            _ => throw new ArgumentOutOfRangeException(nameof(n))
        };

        private static CoordinateData GetLeftDownCompensation(int n) => n switch
        {
            1 => new CoordinateData(Project.Instance.postion左Epson.X21, Project.Instance.postion左Epson.Y21, Project.Instance.postion左Epson.R21),
            2 => new CoordinateData(Project.Instance.postion左Epson.X22, Project.Instance.postion左Epson.Y22, Project.Instance.postion左Epson.R22),
            3 => new CoordinateData(Project.Instance.postion左Epson.X23, Project.Instance.postion左Epson.Y23, Project.Instance.postion左Epson.R23),
            4 => new CoordinateData(Project.Instance.postion左Epson.X24, Project.Instance.postion左Epson.Y24, Project.Instance.postion左Epson.R24),
            5 => new CoordinateData(Project.Instance.postion左Epson.X25, Project.Instance.postion左Epson.Y25, Project.Instance.postion左Epson.R25),
            6 => new CoordinateData(Project.Instance.postion左Epson.X26, Project.Instance.postion左Epson.Y26, Project.Instance.postion左Epson.R26),
            _ => throw new ArgumentOutOfRangeException(nameof(n))
        };

        private static CoordinateData GetLeftDownRotationCenter(int n) => n switch
        {
            1 => new CoordinateData { X = Project.Instance.postion左Epson.X11, Y = Project.Instance.postion左Epson.Y11 },
            2 => new CoordinateData { X = Project.Instance.postion左Epson.X12, Y = Project.Instance.postion左Epson.Y12 },
            3 => new CoordinateData { X = Project.Instance.postion左Epson.X13, Y = Project.Instance.postion左Epson.Y13 },
            4 => new CoordinateData { X = Project.Instance.postion左Epson.X14, Y = Project.Instance.postion左Epson.Y14 },
            5 => new CoordinateData { X = Project.Instance.postion左Epson.X15, Y = Project.Instance.postion左Epson.Y15 },
            6 => new CoordinateData { X = Project.Instance.postion左Epson.X16, Y = Project.Instance.postion左Epson.Y16 },
            _ => throw new ArgumentOutOfRangeException(nameof(n))
        };

        private static CoordinateData GetRightDownStandard(int n) => n switch
        {
            1 => new CoordinateData(Project.Instance.postion右Epson.X1, Project.Instance.postion右Epson.Y1, Project.Instance.postion右Epson.R1),
            2 => new CoordinateData(Project.Instance.postion右Epson.X2, Project.Instance.postion右Epson.Y2, Project.Instance.postion右Epson.R2),
            3 => new CoordinateData(Project.Instance.postion右Epson.X3, Project.Instance.postion右Epson.Y3, Project.Instance.postion右Epson.R3),
            4 => new CoordinateData(Project.Instance.postion右Epson.X4, Project.Instance.postion右Epson.Y4, Project.Instance.postion右Epson.R4),
            5 => new CoordinateData(Project.Instance.postion右Epson.X5, Project.Instance.postion右Epson.Y5, Project.Instance.postion右Epson.R5),
            6 => new CoordinateData(Project.Instance.postion右Epson.X6, Project.Instance.postion右Epson.Y6, Project.Instance.postion右Epson.R6),
            _ => throw new ArgumentOutOfRangeException(nameof(n))
        };

        private static CoordinateData GetRightDownCompensation(int n) => n switch
        {
            1 => new CoordinateData(Project.Instance.postion右Epson.X21, Project.Instance.postion右Epson.Y21, Project.Instance.postion右Epson.R21),
            2 => new CoordinateData(Project.Instance.postion右Epson.X22, Project.Instance.postion右Epson.Y22, Project.Instance.postion右Epson.R22),
            3 => new CoordinateData(Project.Instance.postion右Epson.X23, Project.Instance.postion右Epson.Y23, Project.Instance.postion右Epson.R23),
            4 => new CoordinateData(Project.Instance.postion右Epson.X24, Project.Instance.postion右Epson.Y24, Project.Instance.postion右Epson.R24),
            5 => new CoordinateData(Project.Instance.postion右Epson.X25, Project.Instance.postion右Epson.Y25, Project.Instance.postion右Epson.R25),
            6 => new CoordinateData(Project.Instance.postion右Epson.X26, Project.Instance.postion右Epson.Y26, Project.Instance.postion右Epson.R26),
            _ => throw new ArgumentOutOfRangeException(nameof(n))
        };

        private static CoordinateData GetRightDownRotationCenter(int n) => n switch
        {
            1 => new CoordinateData { X = Project.Instance.postion右Epson.X11, Y = Project.Instance.postion右Epson.Y11 },
            2 => new CoordinateData { X = Project.Instance.postion右Epson.X12, Y = Project.Instance.postion右Epson.Y12 },
            3 => new CoordinateData { X = Project.Instance.postion右Epson.X13, Y = Project.Instance.postion右Epson.Y13 },
            4 => new CoordinateData { X = Project.Instance.postion右Epson.X14, Y = Project.Instance.postion右Epson.Y14 },
            5 => new CoordinateData { X = Project.Instance.postion右Epson.X15, Y = Project.Instance.postion右Epson.Y15 },
            6 => new CoordinateData { X = Project.Instance.postion右Epson.X16, Y = Project.Instance.postion右Epson.Y16 },
            _ => throw new ArgumentOutOfRangeException(nameof(n))
        };
        #endregion

        /// <summary>
        /// 执行图像检测处理（核心检测逻辑）
        /// 将采集的图像输入检测工具，执行算法并判断结果
        /// </summary>
        /// <param name="cameraType">相机类型（用于验证检测类型匹配）</param>
        /// <returns>是否检测成功</returns>
        private bool ProcessImageDetection(string cameraType, string detectionName)
        {
            Message = $"[{DateTime.Now:HH:mm:ss}] 开始图像检测：相机类型={cameraType}，检测对象={detectionName}";

            if (cameraType == "左上定位" || cameraType == "左下定位" || cameraType == "右上定位" || cameraType == "右下定位")
            {
                // 将采集的图像输入检测工具块
                _cogToolBlock.Inputs["InputImage"].Value = Image8Grey;

                if (_cogToolBlock.Inputs["InputImage"].Value == null)
                {
                    SetErrorStatus("检测工具输入图像为空（可能采集失败或图像转换错误）");
                    return false;
                }

                // 执行检测算法
                _imageInfo._CogImage = Image8Grey; // 保存原始图像
                _cogToolBlock.Run(); // 运行检测工具块
                Message = $"[{DateTime.Now:HH:mm:ss}] 检测工具执行完成：工具状态={_cogToolBlock.RunStatus.Result}";

                // 更新检测状态（根据工具返回结果判断是否成功）
                _imageInfo.RunState = _cogToolBlock.RunStatus.Result == CogToolResultConstants.Accept;
                _imageInfo.RunState = true;


                CoordinateData 下相机标准位 = new CoordinateData();

                CoordinateData 下相机拍照数据 = new CoordinateData();

                CoordinateData 固定补偿 = new CoordinateData();

                CoordinateData 旋转中心 = new CoordinateData();

                CoordinateData CalculateResult = new CoordinateData();


                switch (detectionName)
                {
                    case "Epson2左上定位":
                        string result = _cogToolBlock.Outputs["Results"].Value.ToString();
                        Controller.Instance.StationInfo.左上相机Mean=Convert.ToDouble(_cogToolBlock.Outputs["Mean"].Value);

                        if (result != null)
                        {
                            string[] lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            for (int i = 0; i < lines.Length; i++)
                            {
                                string[] parts = lines[i].Split(',');
                                Controller.Instance.StationInfo.Epson2X.Enqueue(Math.Round(double.Parse(parts[0], CultureInfo.InvariantCulture), 3));
                                Controller.Instance.StationInfo.Epson2Y.Enqueue(Math.Round(double.Parse(parts[1], CultureInfo.InvariantCulture), 3));
                                Controller.Instance.StationInfo.Epson2R.Enqueue(Math.Round(double.Parse(parts[2], CultureInfo.InvariantCulture), 3));
                            }
                        }
                        break;
                    case "Epson2右上定位":
                        string result_R = _cogToolBlock.Outputs["Results"].Value.ToString();
                        Controller.Instance.StationInfo.右上相机Mean = Convert.ToDouble(_cogToolBlock.Outputs["Mean"].Value);

                        if (result_R != null)
                        {
                            string[] lines = result_R.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            for (int i = 0; i < lines.Length; i++)
                            {
                                string[] parts = lines[i].Split(',');
                                Controller.Instance.StationInfo.Epson2X.Enqueue(Math.Round(double.Parse(parts[0], CultureInfo.InvariantCulture), 3));
                                Controller.Instance.StationInfo.Epson2Y.Enqueue(Math.Round(double.Parse(parts[1], CultureInfo.InvariantCulture), 3));
                                Controller.Instance.StationInfo.Epson2R.Enqueue(Math.Round(double.Parse(parts[2], CultureInfo.InvariantCulture), 3));
                            }
                        }
                        break;

                    case "Epson1左上定位":
                        string result1 = _cogToolBlock.Outputs["Results"].Value.ToString();
                        Controller.Instance.StationInfo.左上相机Mean = Convert.ToDouble(_cogToolBlock.Outputs["Mean"].Value);
                        if (result1 != null)
                        {
                            string[] lines = result1.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            for (int i = 0; i < lines.Length; i++)
                            {
                                string[] parts = lines[i].Split(',');
                                Controller.Instance.StationInfo.Epson1X.Enqueue(Math.Round(double.Parse(parts[0], CultureInfo.InvariantCulture), 3));
                                Controller.Instance.StationInfo.Epson1Y.Enqueue(Math.Round(double.Parse(parts[1], CultureInfo.InvariantCulture), 3));
                                Controller.Instance.StationInfo.Epson1R.Enqueue(Math.Round(double.Parse(parts[2], CultureInfo.InvariantCulture), 3));
                            }
                        }
                        break;
                    case "Epson1右上定位":
                        string result2 = _cogToolBlock.Outputs["Results"].Value.ToString();
                        Controller.Instance.StationInfo.右上相机Mean = Convert.ToDouble(_cogToolBlock.Outputs["Mean"].Value);
                        if (result2 != null)
                        {
                            string[] lines = result2.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            for (int i = 0; i < lines.Length; i++)
                            {
                                string[] parts = lines[i].Split(',');
                                Controller.Instance.StationInfo.Epson1X.Enqueue(Math.Round(double.Parse(parts[0], CultureInfo.InvariantCulture), 3));
                                Controller.Instance.StationInfo.Epson1Y.Enqueue(Math.Round(double.Parse(parts[1], CultureInfo.InvariantCulture), 3));
                                Controller.Instance.StationInfo.Epson1R.Enqueue(Math.Round(double.Parse(parts[2], CultureInfo.InvariantCulture), 3));
                            }
                        }
                        break;


                    case "左下定位":
                        if (Controller.Instance.StationInfo.Epson2下相机请求位置号 == 1)
                        {
                            下相机标准位.X = Project.Instance.postion左Epson.X1;
                            下相机标准位.Y = Project.Instance.postion左Epson.Y1;
                            下相机标准位.Rotation = Project.Instance.postion左Epson.R1;


                            下相机拍照数据.X = Convert.ToDouble(_cogToolBlock.Outputs["X"].Value); ;
                            下相机拍照数据.Y = Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value);
                            下相机拍照数据.Rotation = Convert.ToDouble(_cogToolBlock.Outputs["R"].Value);


                            固定补偿.X = Project.Instance.postion左Epson.X21;
                            固定补偿.Y = Project.Instance.postion左Epson.Y21;
                            固定补偿.Rotation = Project.Instance.postion左Epson.R21;


                            旋转中心.X = Project.Instance.postion左Epson.X11;
                            旋转中心.Y = Project.Instance.postion左Epson.Y11;


                            CalculateResult= Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX左[0] = CalculateResult.X*(-1);
                            Controller.Instance.StationInfo.downCamY左[0] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR左[0] = CalculateResult.Rotation * (-1);


                        }
                        else if (Controller.Instance.StationInfo.Epson2下相机请求位置号 == 2)
                        {

                            下相机标准位.X = Project.Instance.postion左Epson.X2;
                            下相机标准位.Y = Project.Instance.postion左Epson.Y2;
                            下相机标准位.Rotation = Project.Instance.postion左Epson.R2;


                            下相机拍照数据.X = Convert.ToDouble(_cogToolBlock.Outputs["X"].Value); ;
                            下相机拍照数据.Y = Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value);
                            下相机拍照数据.Rotation = Convert.ToDouble(_cogToolBlock.Outputs["R"].Value);


                            固定补偿.X = Project.Instance.postion左Epson.X22;
                            固定补偿.Y = Project.Instance.postion左Epson.Y22;
                            固定补偿.Rotation = Project.Instance.postion左Epson.R22;


                            旋转中心.X = Project.Instance.postion左Epson.X12;
                            旋转中心.Y = Project.Instance.postion左Epson.Y12;


                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX左[1] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY左[1] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR左[1] = CalculateResult.Rotation * (-1);

                        }
                        else if (Controller.Instance.StationInfo.Epson2下相机请求位置号 == 3)
                        {
                            下相机标准位.X = Project.Instance.postion左Epson.X3;
                            下相机标准位.Y = Project.Instance.postion左Epson.Y3;
                            下相机标准位.Rotation = Project.Instance.postion左Epson.R3;


                            下相机拍照数据.X = Convert.ToDouble(_cogToolBlock.Outputs["X"].Value); ;
                            下相机拍照数据.Y = Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value);
                            下相机拍照数据.Rotation = Convert.ToDouble(_cogToolBlock.Outputs["R"].Value);


                            固定补偿.X = Project.Instance.postion左Epson.X23;
                            固定补偿.Y = Project.Instance.postion左Epson.Y23;
                            固定补偿.Rotation = Project.Instance.postion左Epson.R23;


                            旋转中心.X = Project.Instance.postion左Epson.X13;
                            旋转中心.Y = Project.Instance.postion左Epson.Y13;


                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX左[2] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY左[2] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR左[2] = CalculateResult.Rotation * (-1);

                        }
                        else if (Controller.Instance.StationInfo.Epson2下相机请求位置号 == 4)
                        {
                            下相机标准位.X = Project.Instance.postion左Epson.X4;
                            下相机标准位.Y = Project.Instance.postion左Epson.Y4;
                            下相机标准位.Rotation = Project.Instance.postion左Epson.R4;


                            下相机拍照数据.X = Convert.ToDouble(_cogToolBlock.Outputs["X"].Value); ;
                            下相机拍照数据.Y = Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value);
                            下相机拍照数据.Rotation = Convert.ToDouble(_cogToolBlock.Outputs["R"].Value);


                            固定补偿.X = Project.Instance.postion左Epson.X24;
                            固定补偿.Y = Project.Instance.postion左Epson.Y24;
                            固定补偿.Rotation = Project.Instance.postion左Epson.R24;


                            旋转中心.X = Project.Instance.postion左Epson.X14;
                            旋转中心.Y = Project.Instance.postion左Epson.Y14;


                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX左[3] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY左[3] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR左[3] = CalculateResult.Rotation * (-1);
                        }
                        else if (Controller.Instance.StationInfo.Epson2下相机请求位置号 == 5)
                        {
                            下相机标准位.X = Project.Instance.postion左Epson.X5;
                            下相机标准位.Y = Project.Instance.postion左Epson.Y5;
                            下相机标准位.Rotation = Project.Instance.postion左Epson.R5;


                            下相机拍照数据.X = Convert.ToDouble(_cogToolBlock.Outputs["X"].Value); ;
                            下相机拍照数据.Y = Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value);
                            下相机拍照数据.Rotation = Convert.ToDouble(_cogToolBlock.Outputs["R"].Value);


                            固定补偿.X = Project.Instance.postion左Epson.X25;
                            固定补偿.Y = Project.Instance.postion左Epson.Y25;
                            固定补偿.Rotation = Project.Instance.postion左Epson.R25;


                            旋转中心.X = Project.Instance.postion左Epson.X15;
                            旋转中心.Y = Project.Instance.postion左Epson.Y15;


                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX左[4] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY左[4] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR左[4] = CalculateResult.Rotation * (-1);




                        }
                        else if (Controller.Instance.StationInfo.Epson2下相机请求位置号 == 6)
                        {
                            下相机标准位.X = Project.Instance.postion左Epson.X6;
                            下相机标准位.Y = Project.Instance.postion左Epson.Y6;
                            下相机标准位.Rotation = Project.Instance.postion左Epson.R6;


                            下相机拍照数据.X = Convert.ToDouble(_cogToolBlock.Outputs["X"].Value); ;
                            下相机拍照数据.Y = Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value);
                            下相机拍照数据.Rotation = Convert.ToDouble(_cogToolBlock.Outputs["R"].Value);


                            固定补偿.X = Project.Instance.postion左Epson.X26;
                            固定补偿.Y = Project.Instance.postion左Epson.Y26;
                            固定补偿.Rotation = Project.Instance.postion左Epson.R26;


                            旋转中心.X = Project.Instance.postion左Epson.X16;
                            旋转中心.Y = Project.Instance.postion左Epson.Y16;


                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX左[5] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY左[5] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR左[5] = CalculateResult.Rotation * (-1);
                        }
                        Controller.Instance.StationInfo.Epson2下相机拍照结果 = 1;
                        Controller.Instance.StationInfo.Epson2下相机请求位置号 = 0;
                        break;


                    case "右下定位":
                        Controller.Instance.StationInfo.Epson1下相机拍照结果 = 1;
                        if (Controller.Instance.StationInfo.Epson1下相机请求位置号 == 1)
                        {
                            下相机标准位.X = Project.Instance.postion右Epson.X1;
                            下相机标准位.Y = Project.Instance.postion右Epson.Y1;
                            下相机标准位.Rotation = Project.Instance.postion右Epson.R1;


                            下相机拍照数据.X =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["X"].Value),3);
                            下相机拍照数据.Y =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value),3);
                            下相机拍照数据.Rotation =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["R"].Value),3);



                            if (Math.Abs(下相机标准位.Rotation- 下相机拍照数据.Rotation)>5)
                            {
                                Controller.Instance.StationInfo.Epson1下相机拍照结果 = 2;
                            }

                            固定补偿.X = Project.Instance.postion右Epson.X21;
                            固定补偿.Y = Project.Instance.postion右Epson.Y21;
                            固定补偿.Rotation = Project.Instance.postion右Epson.R21;


                            旋转中心.X = Project.Instance.postion右Epson.X11;
                            旋转中心.Y = Project.Instance.postion右Epson.Y11;


                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX右[0] = CalculateResult.X*(-1);
                            Controller.Instance.StationInfo.downCamY右[0] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR右[0] = CalculateResult.Rotation * (-1);



                        }
                        else if (Controller.Instance.StationInfo.Epson1下相机请求位置号 == 2)
                        {

                            下相机标准位.X = Project.Instance.postion右Epson.X2;
                            下相机标准位.Y = Project.Instance.postion右Epson.Y2;
                            下相机标准位.Rotation = Project.Instance.postion右Epson.R2;


                            下相机拍照数据.X =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["X"].Value),3);
                            下相机拍照数据.Y = Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value), 3);
                            下相机拍照数据.Rotation =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["R"].Value),3);


                            固定补偿.X = Project.Instance.postion右Epson.X22;
                            固定补偿.Y = Project.Instance.postion右Epson.Y22;
                            固定补偿.Rotation = Project.Instance.postion右Epson.R22;

                            if (Math.Abs(下相机标准位.Rotation - 下相机拍照数据.Rotation) > 5)
                            {
                                Controller.Instance.StationInfo.Epson1下相机拍照结果 = 2;
                            }



                            旋转中心.X = Project.Instance.postion右Epson.X12;
                            旋转中心.Y = Project.Instance.postion右Epson.Y12;


                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX右[1] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY右[1] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR右[1] = CalculateResult.Rotation * (-1);

                        }
                        else if (Controller.Instance.StationInfo.Epson1下相机请求位置号 == 3)
                        {
                            下相机标准位.X = Project.Instance.postion右Epson.X3;
                            下相机标准位.Y = Project.Instance.postion右Epson.Y3;
                            下相机标准位.Rotation = Project.Instance.postion右Epson.R3;


                            下相机拍照数据.X =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["X"].Value),3) ;
                            下相机拍照数据.Y =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value),3);
                            下相机拍照数据.Rotation =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["R"].Value));


                            固定补偿.X = Project.Instance.postion右Epson.X23;
                            固定补偿.Y = Project.Instance.postion右Epson.Y23;
                            固定补偿.Rotation = Project.Instance.postion右Epson.R23;


                            旋转中心.X = Project.Instance.postion右Epson.X13;
                            旋转中心.Y = Project.Instance.postion右Epson.Y13;

                            if (Math.Abs(下相机标准位.Rotation - 下相机拍照数据.Rotation) > 5)
                            {
                                Controller.Instance.StationInfo.Epson1下相机拍照结果 = 2;
                            }



                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX右[2] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY右[2] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR右[2] = CalculateResult.Rotation * (-1);
                        }
                        else if (Controller.Instance.StationInfo.Epson1下相机请求位置号 == 4)
                        {
                            下相机标准位.X = Project.Instance.postion右Epson.X4;
                            下相机标准位.Y = Project.Instance.postion右Epson.Y4;
                            下相机标准位.Rotation = Project.Instance.postion右Epson.R4;


                            下相机拍照数据.X = Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["X"].Value), 3);
                            下相机拍照数据.Y =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value),3);
                            下相机拍照数据.Rotation =Math.Round( Convert.ToDouble(_cogToolBlock.Outputs["R"].Value),3);


                            固定补偿.X = Project.Instance.postion右Epson.X24;
                            固定补偿.Y = Project.Instance.postion右Epson.Y24;
                            固定补偿.Rotation = Project.Instance.postion右Epson.R24;


                            旋转中心.X = Project.Instance.postion右Epson.X14;
                            旋转中心.Y = Project.Instance.postion右Epson.Y14;


                            if (Math.Abs(下相机标准位.Rotation - 下相机拍照数据.Rotation) > 5)
                            {
                                Controller.Instance.StationInfo.Epson1下相机拍照结果 = 2;
                            }



                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX右[3] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY右[3] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR右[3] = CalculateResult.Rotation * (-1);

                        }
                        else if (Controller.Instance.StationInfo.Epson1下相机请求位置号 == 5)
                        {
                            下相机标准位.X = Project.Instance.postion右Epson.X5;
                            下相机标准位.Y = Project.Instance.postion右Epson.Y5;
                            下相机标准位.Rotation = Project.Instance.postion右Epson.R5;


                            下相机拍照数据.X =Math.Round( Convert.ToDouble(_cogToolBlock.Outputs["X"].Value),3) ;
                            下相机拍照数据.Y =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value),3);
                            下相机拍照数据.Rotation =Math.Round( Convert.ToDouble(_cogToolBlock.Outputs["R"].Value),3);


                            固定补偿.X = Project.Instance.postion右Epson.X25;
                            固定补偿.Y = Project.Instance.postion右Epson.Y25;
                            固定补偿.Rotation = Project.Instance.postion右Epson.R25;


                            旋转中心.X = Project.Instance.postion右Epson.X15;
                            旋转中心.Y = Project.Instance.postion右Epson.Y15;


                            if (Math.Abs(下相机标准位.Rotation - 下相机拍照数据.Rotation) > 5)
                            {
                                Controller.Instance.StationInfo.Epson1下相机拍照结果 = 2;
                            }



                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX右[4] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY右[4] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR右[4] = CalculateResult.Rotation * (-1);

                        }
                        else if (Controller.Instance.StationInfo.Epson1下相机请求位置号 == 6)
                        {
                            下相机标准位.X = Project.Instance.postion右Epson.X6;
                            下相机标准位.Y = Project.Instance.postion右Epson.Y6;
                            下相机标准位.Rotation = Project.Instance.postion右Epson.R6;


                            下相机拍照数据.X =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["X"].Value),3) ;
                            下相机拍照数据.Y =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value),3);
                            下相机拍照数据.Rotation =Math.Round(Convert.ToDouble(_cogToolBlock.Outputs["R"].Value),3);


                            固定补偿.X = Project.Instance.postion右Epson.X26;
                            固定补偿.Y = Project.Instance.postion右Epson.Y26;
                            固定补偿.Rotation = Project.Instance.postion右Epson.R26;


                            旋转中心.X = Project.Instance.postion右Epson.X16;
                            旋转中心.Y = Project.Instance.postion右Epson.Y16;

                            if (Math.Abs(下相机标准位.Rotation - 下相机拍照数据.Rotation) > 5)
                            {
                                Controller.Instance.StationInfo.Epson1下相机拍照结果 = 2;
                            }



                            CalculateResult = Controller.Instance.StationInfo.Calculate(下相机标准位, 下相机拍照数据, 固定补偿, 旋转中心);

                            Controller.Instance.StationInfo.downCamX右[5] = CalculateResult.X * (-1);
                            Controller.Instance.StationInfo.downCamY右[5] = CalculateResult.Y * (-1);
                            Controller.Instance.StationInfo.downCamR右[5] = CalculateResult.Rotation * (-1);
                        }
                       
                        Controller.Instance.StationInfo.Epson1下相机请求位置号 = 0;
                        break;
                    default:
                        break;
                }

            }
            else
            {
                if (VisionSystem.Instance.List_飞拍图片.Count!=6)
                {
                    Message = $"[{DateTime.Now:HH:mm:ss}]:飞拍拍照次数NG!!";
                    VisionSystem.Instance.List_飞拍图片.Clear();
                    Controller.Instance.StationInfo.FlyVisionResult = 2;
                    _imageInfo.RunState = true;
                }
                else
                {
                    for (int i = 0; i < VisionSystem.Instance.List_飞拍图片.Count; i++)
                    {
                        _cogToolBlock.Inputs["InputImage"].Value = VisionSystem.Instance.List_飞拍图片[i];
                        // 执行检测算法
                        _imageInfo._CogImage = VisionSystem.Instance.List_飞拍图片[i];; // 保存原始图像
                       
                        _cogToolBlock.Run();

                        _imageInfo.RunState = _cogToolBlock.RunStatus.Result == CogToolResultConstants.Accept;
                        _imageInfo.RunState = true;


                        switch (detectionName)
                        {
                            case "飞拍定位":

                                Controller.Instance.StationInfo.FlyVisionX[i] = Convert.ToDouble(_cogToolBlock.Outputs["X"].Value);
                                Controller.Instance.StationInfo.FlyVisionY[i] = Convert.ToDouble(_cogToolBlock.Outputs["Y"].Value);
                                //Controller.Instance.StationInfo.FlyVisionR[i] = Convert.ToDouble(_cogToolBlock.Outputs["R"].Value);

                                break;
                            case "飞拍复检":
                                Controller.Instance.StationInfo.FlyVisionCheckResult[i] = Convert.ToBoolean(_cogToolBlock.Outputs["Result"].Value);
                                break;
                            default:
                                break;
                        }

                        string path1 = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".jpg";
                        CogImageSaver.SaveCogImageToFile(_imageInfo._CogImage, path1, "OK", detectionName);



                    }
                    Controller.Instance.StationInfo.FlyVisionResult = 1;
                    VisionSystem.Instance.List_飞拍图片.Clear();
                    return true;
                }

            }




            // 记录扫码结果


           // string path = DateTime.Now.ToString("yyyyMMddHHmmssfff") +".jpg";
            //int codeCount = (int)_cogToolBlock.Outputs["Count"].Value;
            //if (codeCount != 1)
            //{
            //    SetErrorStatus($"二维码读取错误：检测到{codeCount}个码（预期1个），工具输出Count={codeCount}");
            //    CogImageSaver.SaveCogImageToFile(Image8Grey, path, "NG", detectionName);
            //    CogImageSaver.SaveCogRecordToFile(_cogToolBlock.CreateLastRunRecord().SubRecords[0], path, "NG", detectionName);
            //    return false;
            //}
            string path = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".jpg";
            CogImageSaver.SaveCogImageToFile(Image8Grey,path,"OK", detectionName);
            //CogImageSaver.SaveCogRecordToFile(_cogToolBlock.CreateLastRunRecord().SubRecords[0], path, "OK", detectionName);

            return true;
        }

        /// <summary>
        /// 更新视觉检测结果显示（根据检测点位名称更新对应UI控件）
        /// </summary>
        /// <param name="displayAccessor">显示控件访问器</param>
        /// <param name="displayUpdater">显示更新器</param>
        private void UpdateVisionDisplay(CogRecordDisplay displayAccessor, Action<object, object> displayUpdater)
        {
            // 创建检测结果记录（用于UI展示）
            var record = _cogToolBlock.CreateLastRunRecord().SubRecords[0];
            Message = $"[{DateTime.Now:HH:mm:ss}] 准备更新显示";

            displayAccessor.Record = record;
            displayAccessor.AutoFit = true;
            Message = $"[{DateTime.Now:HH:mm:ss}] 显示控件更新完成：Record已设置，AutoFit=True";
        }

        /// <summary>
        /// 跨线程更新显示控件（UI控件必须在主线程更新）
        /// 通过Invoke方法确保UI操作在主线程执行
        /// </summary>
        /// <param name="displayAccessor">显示控件访问器</param>
        /// <param name="displayUpdater">显示更新器</param>
        /// <param name="record">检测结果记录</param>
        private void InvokeDisplayUpdate(Func<object> displayAccessor,
                                        Action<object, object> displayUpdater,
                                        object record)
        {
            var display = displayAccessor.Invoke();
            Message = $"[{DateTime.Now:HH:mm:ss}] 开始跨线程更新显示：控件类型={display?.GetType().Name}";

            display.GetType().GetMethod("Invoke").Invoke(display, new object[]
            {
                new Action(() =>
                {
                    displayUpdater.Invoke(display, record);
                    display.GetType().GetProperty("AutoFit").SetValue(display, true);
                    Message = $"[{DateTime.Now:HH:mm:ss}] 跨线程显示更新完成：控件已刷新";
                })
            });
        }

        /// <summary>
        /// 具体更新显示控件的内容（设置检测结果记录）
        /// </summary>
        /// <param name="display">显示控件</param>
        /// <param name="record">检测结果记录</param>
        private void UpdateDisplay(object display, object record)
        {
            display.GetType().GetProperty("Record").SetValue(display, record);
            Message = $"[{DateTime.Now:HH:mm:ss}] 显示控件内容更新：已设置Record属性";
        }

        /// <summary>
        /// 设置错误状态（统一更新运行状态和消息）
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = $"[{DateTime.Now:HH:mm:ss}] 流程错误：{errorMessage}";
        }
    }




    //codeInfo = DateTime.Now.ToString("HH_mm_ss_fff");


    public static class CogImageSaver
    {
        // 定义两种类型的文件夹名称
        private const string IMAGE_TYPE_FOLDER = "CogImages";
        private const string RECORD_TYPE_FOLDER = "CogRecords";

        /// <summary>
        /// 保存Cognex图像到文件系统
        /// 按检测名称分类保存图像，异步执行避免阻塞主线程
        /// </summary>
        //public static void SaveCogImageToFile(ICogImage image, string name, string result, string detectionName)
        //{
        //    SaveCogImageToFile(image, name, result, detectionName, null);
        //}

        /// <summary>
        /// 保存Cognex图像到文件系统
        /// 按检测名称和信息码信息分类保存图像，异步执行避免阻塞主线程
        /// </summary>
        public static void SaveCogImageToFile(ICogImage image, string name, string result,
                                      string detectionName, string codeInfo = null)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (image == null)
                    {
                        return;
                    }

                    if (!ShouldSaveImage(result))
                        return;

                    // 构建CogImage专用路径，包含类型文件夹
                    string dir = BuildSaveDirectory(detectionName, result, IMAGE_TYPE_FOLDER);
                    EnsureDirectoryExists(dir);
                    //if (codeInfo == null)
                    //{
                    //    codeInfo = DateTime.Now.ToShortDateString();
                    //}
                    string filePath = Path.Combine(dir, name);
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        return;
                    }


                    var tool = new CogImageFileTool();
                    tool.InputImage = image;
                    tool.Operator.Open(filePath, CogImageFileModeConstants.Write);//加载存图所选的存储路径
                    tool.Run();//存图工具运行
                    tool.Operator.Close();//存图工具关闭        var tool = new CogImageFileTool();
                    //using (var bitmap = image.ToBitmap())
                    //{
                    //    bitmap.Save(filePath);
                    //}
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex, $"保存ICogImage图像失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 保存Cognex记录中的图像到文件系统
        /// 从ICogRecord中提取图像，按检测名称分类保存
        /// </summary>
        //public static void SaveCogRecordToFile(ICogRecord cogRecord, string name, string result, string detectionName)
        //{
        //    SaveCogRecordToFile(cogRecord, name, result, detectionName, null);
        //}

        /// <summary>
        /// 保存Cognex记录中的图像到文件系统
        /// 从ICogRecord中提取图像，按检测名称和信息码信息分类保存
        /// </summary>
        public static void SaveCogRecordToFile(ICogRecord cogRecord, string name, string result,
                                             string detectionName, string codeInfo = null)
        {
            // 提前检查必要参数，避免不必要的线程创建
            if (cogRecord == null || string.IsNullOrWhiteSpace(name) || !ShouldSaveImage(result))
            {
                return;
            }
            // 构建保存路径
            string dir = BuildSaveDirectory(detectionName, result, RECORD_TYPE_FOLDER);
            EnsureDirectoryExists(dir);
            string filePath = Path.Combine(dir, name);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            // 获取窗体实例并操作控件
            //var cogRecordDisplay = MainPage.Instance.cogRecordDisplay_结果图保存;
            var cogRecordDisplay = MainPage.Instance.cogRecordDisplay5;
            if (cogRecordDisplay == null)
            {
                return;
            }

            // 使用Invoke确保在UI线程操作控件
            cogRecordDisplay.Invoke(new Action(() =>
            {
                SaveImageToFile(cogRecordDisplay, cogRecord, filePath, codeInfo);
            }));

        }

        /// <summary>
        /// 将图像保存到文件，增加错误处理和资源管理
        /// </summary>
        private static void SaveImageToFile(CogRecordDisplay display, ICogRecord record, string filePath, string codeInfo)
        {
            if (display == null || record == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            // 检查目标目录是否存在
            //string directory = Path.GetDirectoryName(filePath);
            //if (!Directory.Exists(directory))
            //{
            //    try
            //    {
            //        Directory.CreateDirectory(directory);
            //    }
            //    catch (Exception ex)
            //    {
            //        RLog.SaveError(ex, $"创建目录失败: {directory}");
            //        return;
            //    }
            //}

            // 检查磁盘空间
            if (!HasEnoughDiskSpace(filePath, 10)) // 检查是否有10MB可用空间
            {
                return;
            }

            Bitmap bitmap = null;
            try
            {
                display.Record = record;
                display.AutoFit = true; // 自动调整图像大小以适应显示控件

                // 尝试创建较小尺寸的位图（如果原始尺寸过大）
                bitmap = display.CreateContentBitmap(CogDisplayContentBitmapConstants.Custom) as Bitmap;
                if (bitmap == null)
                {
                    // 备选方案：创建缩小版本的图像
                    // 如果有获取原始尺寸的方法，可以先检查再决定是否缩小
                    // bitmap = CreateResizedBitmap(display, record);
                    return;
                }

                // 保存前检查位图尺寸是否合理
                if (IsBitmapSizeExcessive(bitmap))
                {
                    return;
                }
                //if (codeInfo == null)
                //{
                //    codeInfo = DateTime.Now.ToShortDateString();
                //}



                // 使用using语句确保资源释放
                using (var tempBitmap = new Bitmap(bitmap))
                {
                    tempBitmap.Save(filePath);
                }
            }
            catch (OutOfMemoryException ex)
            {
                RLog.SaveError(ex, $"内存不足，无法处理图像: {ex.Message}");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, $"保存图像到文件时出错: {ex.Message}");
            }
            finally
            {
                // 确保释放资源
                if (bitmap != null)
                {
                    bitmap.Dispose();
                    bitmap = null;
                }
                // 重置显示的记录，避免资源占用
                display.Record = null;
            }
        }

        // 辅助方法：检查磁盘空间
        private static bool HasEnoughDiskSpace(string path, long requiredMB)
        {
            try
            {
                DriveInfo drive = new DriveInfo(Path.GetPathRoot(path));
                return drive.AvailableFreeSpace >= requiredMB * 1024 * 1024;
            }
            catch
            {
                return false;
            }
        }

        // 辅助方法：检查位图尺寸是否过大
        private static bool IsBitmapSizeExcessive(Bitmap bitmap)
        {
            // 根据实际情况调整阈值
            const long maxPixels = 50000000; // 5000万像素
            return (long)bitmap.Width * bitmap.Height > maxPixels;
        }

        /// <summary>
        /// 检查是否应该根据系统配置和检测结果保存图像
        /// </summary>
        private static bool ShouldSaveImage(string result)
        {
            return !((!Project.Instance.visionParm.SaveOkImages && result == "OK") ||
                    (!Project.Instance.visionParm.SaveNgImages && result == "NG"));
        }

        /// <summary>
        /// 构建保存图像的目录路径，增加类型区分文件夹
        /// </summary>
        private static string BuildSaveDirectory(string detectionName, string result, string typeFolder)
        {
            var pathParts = new List<string>
        {
            Jabil.Lib.Parameter.ParameterPath.SaveImagePath,
            typeFolder,  // 添加类型区分文件夹
            DateTime.Now.ToString("yyyy-MM-dd"),
            detectionName ?? "未知检测名称"
        };

            //if (!string.IsNullOrEmpty(codeInfo))
            //{
            //    pathParts.Add(codeInfo);
            //}

            pathParts.Add(result ?? "未知结果");

            return Path.Combine(pathParts.ToArray());
        }

        /// <summary>
        /// 确保目录存在，如果不存在则创建
        /// </summary>
        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}