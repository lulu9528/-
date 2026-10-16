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
    /// 依赖：JVS 视觉系统（相机工具算子取图 + 进程内 ToolBlock 检测）
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
                        throw new InvalidOperationException($"不支持的取像工具类型：{tool.检测工具类型}（仅支持 JvsCameraTool）");
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
                        throw new InvalidOperationException($"不支持的检测工具类型：{tool.检测工具类型}（仅支持 JvsWorkspaceTool）");
                    }
                }
            }

            // 验证关键工具是否绑定成功（取像和检测工具为必需）
            if (!_jvsTakeToolBound)
                throw new InvalidOperationException($"未找到取像工具: {_imageInfo.绑定取像工具名称}（未配置 JvsCameraTool）");

            if (_jvsRunner == null)
                throw new InvalidOperationException($"未找到检测工具: {_imageInfo.绑定检测工具名称}（未配置 JvsWorkspaceTool）");

            Message = $"[{DateTime.Now:HH:mm:ss}] 视觉工具绑定完成：所有必需工具已就绪（JVS 模式）";
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
                            ProcessCameraCapture("左上定位");
                            break;
                        case "Epson1左上定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理左上定位：使用左上定位相机";
                            ProcessCameraCapture("左上定位");
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
                            ProcessCameraCapture("右上定位");
                            break;
                        case "Epson1右上定位":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理左上定位：使用左上定位相机";
                            ProcessCameraCapture("右上定位");
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
                            ProcessCameraCapture("左下定位");
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
                            ProcessCameraCapture("右下定位");
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
                            ProcessCameraCapture("飞拍定位");
                            break;
                        case "飞拍复检":
                            Message = $"[{DateTime.Now:HH:mm:ss}] 准备处理飞拍定位：使用飞拍相机";
                            ProcessCameraCapture("飞拍复检");
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
        private void ProcessCameraCapture(string cameraType)
        {
            Message = $"[{DateTime.Now:HH:mm:ss}] 开始相机采集流程：相机类型={cameraType}，对象={_detectionName}";

            // ===== JVS 模式（进程内嵌入，唯一取图/检测通道）=====
            // 相机由 JVS workspace 相机工具算子（CameraTool/.jvt）取像，检测在进程内 ToolBlock 执行
            // 不再使用 Cognex 取像/检测/标定工具（VisionPro 已彻底移除）
            if (_jvsRunner == null)
            {
                SetErrorStatus("视觉检测未就绪：未装配 JVS 检测工具（JvsWorkspaceTool）");
                return;
            }

            if (!CaptureWithJvsCamera())
                return;

            if (!ProcessJvsImageDetection(cameraType, _detectionName))
                return;

            Message = $"[{DateTime.Now:HH:mm:ss}] JVS 检测完成：相机类型={cameraType}，检测点位={_detectionName}（结果已写入 StationInfo/数据页）";
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
                JvsVisionBridge.NotifyLastResult(Controller.Instance.StationInfo.JvsLastResultDisplay);
                MainPage.Instance?.AddVisionDataRow("ERR", detectionName, 0, elapsedMs, elapsedMs, (int)run.TotalMs, (int)run.ProcessingMs, run.Error);
                return false;
            }
            if (run.Points.Count == 0)
            {
                SetErrorStatus($"JVS 检测未返回有效检测点（输出 pin={mapping.OutputPin} 为空或格式不符；已自动重试一次）");
                Controller.Instance.StationInfo.JvsLastResultDisplay = $"ERR | 点位={detectionName} | 未返回有效检测点（重试后仍为空）";
                JvsVisionBridge.NotifyLastResult(Controller.Instance.StationInfo.JvsLastResultDisplay);
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
            JvsVisionBridge.NotifyLastResult(jvsDisplay); // 设置页/主界面状态面板刷新
            // 同步写入主界面数据页视觉数据表（耗时对比）
            MainPage.Instance?.AddVisionDataRow(
                _imageInfo.Result, detectionName, run.Points.Count,
                elapsedMs, elapsedMs, (int)run.TotalMs, (int)run.ProcessingMs,
                string.Join(";", run.Points.Select(p => $"{p.X:F3},{p.Y:F3},{p.R:F3}")));
            Message += $"，UI显示={Controller.Instance.StationInfo.JvsLastResultDisplay}";

            // ===== JVS 检测结果图推送主界面 6 宫格显示（非阻塞；显示失败不影响检测）=====
            // 取最近一次记录的带标注结果图（image_with_shapes）→ MainPage 对应相机格自动刷新
            try
            {
                var resultBitmap = _jvsRunner.GetLastResultBitmap();
                if (resultBitmap != null)
                {
                    string displayKey = string.IsNullOrWhiteSpace(_imageInfo.绑定取像工具名称)
                        ? detectionName
                        : _imageInfo.绑定取像工具名称;
                    MainPage.Instance?.RefreshJvsDisplay(displayKey, resultBitmap);
                }
                // 清理可视化记录（Bitmap 已独立拷贝），防止中间图像堆积导致内存增长
                _jvsRunner.ClearRecords();
            }
            catch (Exception displayEx)
            {
                RLog.SaveError(displayEx);
            }

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
        /// 设置错误状态（统一更新运行状态和消息）
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = $"[{DateTime.Now:HH:mm:ss}] 流程错误：{errorMessage}";
        }
    }

}