using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.ProductData.Frm;
using Jabil.Module.ProductData.Lib;
using Jabil.Module.Vision;
using JabilVisionMotion.Frm.UserPage;
using JabilVisionMotion.Lib;
using lctdevice;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using JabilVisionMotion.Edit.Process.TCPControlProcess;

namespace JabilVisionMotion.Edit.Process
{
    /// <summary>
    /// 状态判断流程处理类，继承自ProcessBase基类
    /// 核心功能：
    /// 1. 根据指令解析的点位类型（如"上料"、"分料"）和点位名称（如"前滑台结果读取"）
    /// 2. 执行对应的设备状态判断逻辑（如传感器信号读取、机械臂动作结果验证）
    /// 3. 处理异常情况（数据未清空、设备状态错误等）并记录日志
    /// 4. 协调Modbus通信、IO控制、轴运动等子模块的协同工作
    /// 
    /// 典型应用场景：
    /// - 上料滑台的物料检测结果读取与缓存
    /// - 分料机构的抛料控制与流水线联动
    /// - 前后工位的定位补偿计算（用于精密装配）
    /// 
    /// 指令格式规范："指令名称|点位类型|点位名称"
    /// 示例："StateJudge|上料|前滑台结果读取" 表示判断上料环节中前滑台的物料状态
    /// </summary>
    public class StateJudge : ProcessBase, ICloneable
    {
        #region 私有字段
        private string _commandName;       // 指令名称（固定为"StateJudge"，标识当前流程类型）
        private string _pointName;         // 点位名称（具体操作对象，如"抛料判断"、"穴位一"）
        private string _pointType;         // 点位类型（设备/工序分类，如"上料"、"分料"、"检测"）
        private readonly string _process;  // 原始指令字符串（完整的指令内容，用于解析和克隆）
        private readonly string _processTip; // 指令提示信息（用于日志输出，提高可追溯性）
        private CancellationToken _cancellationToken;  // 取消令牌（接收外部终止信号，支持流程中断）

        /// <summary>
        /// 设备类型专用锁字典，为不同设备提供独立的同步机制
        /// 实现原理：
        /// - key：设备类型字符串（如"左Epson"、"右Epson"）
        /// - value：对应设备的SemaphoreSlim信号量
        /// 作用：避免多线程并发操作同一设备时导致的状态不一致（如同时控制同一机械臂）
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _deviceLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();
        #endregion

        /// <summary>
        /// 初始化状态判断流程实例
        /// </summary>
        /// <param name="process">状态判断指令字符串（格式：指令名称|点位类型|点位名称）</param>
        /// <param name="processTip">指令提示信息（用于日志记录，描述该指令的业务场景）</param>
        public StateJudge(string process, string processTip) : base(process, processTip)
        {
            try
            {
                // 初始化流程状态为"等待"，包含指令上下文
                CurrentStatus = GeneralStatus.Wait;
                Message = $"等待执行 - 指令: {process}，提示: {processTip}";
                // 校验指令字符串和提示信息非空，为空则抛出异常
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                // 解析指令参数，提取点位类型和点位名称
                ParseCommand(process);
            }
            catch (Exception ex)
            {
                // 捕获初始化过程中的异常，包含指令信息
                Message = $"初始化失败 - 指令: {process}，错误: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return; // 初始化错误，退出构造函数
            }
        }

        /// <summary>
        /// 克隆方法，支持流程实例的复制（用于副流程缓存机制）
        /// 重要性：确保多实例并发执行时，每个实例的状态独立，避免相互干扰
        /// </summary>
        public override object Clone()
        {
            // 创建新实例并复制核心参数
            var clone = new StateJudge(_process, _processTip)
            {
                _commandName = this._commandName,
                _pointType = this._pointType,
                _pointName = this._pointName
            };
            // 初始化克隆实例的状态为"等待"，包含原始指令信息
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = $"克隆实例等待执行 - 原始指令: {_process}";

            return clone;
        }

        /// <summary>
        /// 解析状态判断指令参数
        /// 功能：从原始指令字符串中提取指令名称、点位类型和点位名称
        /// 格式要求：指令字符串需用"|"分隔为至少3个部分，例如"StateJudge|分料|抛料判断"
        /// </summary>
        private void ParseCommand(string process)
        {
            try
            {
                // 分割指令字符串，移除空项（避免因多余分隔符导致的解析错误）
                string[] commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                // 验证指令格式有效性（至少包含3个部分）
                if (commandParts.Length < 3)
                {
                    throw new ArgumentException($"指令格式错误，需满足'指令名称|点位类型|点位名称'，实际为: {process}");
                }

                // 赋值解析后的参数
                _commandName = commandParts[0];  // 指令名称（第1部分）
                _pointType = commandParts[1];    // 点位类型（第2部分）
                _pointName = commandParts[2];    // 点位名称（第3部分）

                // 验证关键参数非空（避免后续逻辑因空值导致异常）
                if (string.IsNullOrEmpty(_pointType))
                {
                    throw new ArgumentException("解析失败：点位类型不能为空");
                }

                if (string.IsNullOrEmpty(_pointName))
                {
                    throw new ArgumentException("解析失败：点位名称不能为空");
                }

                // 解析成功，更新消息
                Message = $"指令解析成功 - 类型: {_pointType}，名称: {_pointName}，原始指令: {process}";
            }
            catch (Exception ex)
            {
                // 捕获解析异常，包含具体错误原因
                Message = $"指令解析失败 - 指令: {process}，错误: {ex.Message}";
                CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return;
            }
        }


        /// <summary>
        /// 执行状态判断逻辑（核心方法）
        /// 流程：
        /// 1. 测试模式判断（跳过实际操作，直接返回成功）
        /// 2. 根据点位类型（_pointType）分支处理不同工况（上料/分料/检测等）
        /// 3. 每个工况下根据点位名称（_pointName）执行具体逻辑
        /// 4. 处理成功/失败状态，并记录相应消息
        /// 异常处理：捕获所有异常，记录错误信息并设置错误状态
        /// </summary>
        public override void Run()
        {
            try
            {
                // 测试模式：用于调试和模拟运行，不执行实际设备操作
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = $"测试模式执行成功 - 类型: {_pointType}，名称: {_pointName}（跳过实际操作）";
                    return;
                }

                // 初始化运行状态，明确当前执行的操作
                CurrentStatus = GeneralStatus.Running;
                Message = $"正在执行 - 类型: {_pointType}，名称: {_pointName}（开始处理）";

                // 根据点位类型分支处理（主工况区分）
                switch (_pointType)
                {
                    #region LOMA
                    case "LOMA":
                        // 根据点位名称执行具体逻辑
                        OP70 now70;
                        OP70[] info = new OP70[] { };
                        string code;
                        
                        switch (_pointName)
                        {
                            case "飞拍开始":

                                // JVS 模式：飞拍相机由 JVS 取图工具算子驱动（.jvt 相机工具），无需订阅 Cognex 硬件取像事件
                                Controller.Instance.StationInfo.FlyVisionResult = 0;
                                break;

                            case "飞拍结束":
                                // JVS 模式：飞拍相机由 JVS 取图工具算子驱动，无 Cognex 硬件事件可取消
                                break;

                            case "Epson2是否需要上相机拍照":
                                if (Controller.Instance.StationInfo.Epson2X.Count>0&&
                                    Controller.Instance.StationInfo.Epson2Y.Count > 0&&
                                    Controller.Instance.StationInfo.Epson2R.Count > 0)
                                {
                                    //new TCPServerCommunicationProcess("TCPServer|输出|Epson2|发送位置给EPSON|3", "隐藏").Run();

                                    Controller.Instance.StationInfo.上相机是否需要拍照 = 0;
                                }
                                else
                                {
                                    //new VisionProcess("Vision|定位|定位相机|左上定位|5", "隐藏").Run();
                                    Controller.Instance.StationInfo.上相机是否需要拍照 = 1;
                                }
                                break;

                            case "发送Epson2下相机结果":
                                if (Controller.Instance.StationInfo.Epson2下相机拍照结果==1)
                                {
                                    new ModbusControlProcess.ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|左下相机拍照OK|1|3", "隐藏").Run();
                                }
                                else
                                {
                                    new ModbusControlProcess.ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|左下相机拍照NG|1|3", "隐藏").Run();
                                }
                                break;

                            case "Epson2允许刷新":
                                Controller.Instance.StationInfo.Epson2下相机是否刷新 = true;
                                break;
                            case "Epson2请求总结果允许刷新":
                                Controller.Instance.StationInfo.左皮带线是否刷新 = true;
                                break;



                            case "Epson1是否需要上相机拍照":
                                if (Controller.Instance.StationInfo.Epson1X.Count > 0 &&
                                    Controller.Instance.StationInfo.Epson1Y.Count > 0 &&
                                    Controller.Instance.StationInfo.Epson1R.Count > 0)
                                {
                                    Controller.Instance.StationInfo.上相机是否需要拍照 = 0;
                                    //Controller.Instance.StationInfo.供料器震动 = 0 ;
                                }
                                else
                                {
                                    Controller.Instance.StationInfo.上相机是否需要拍照 = 1;
                                    //Controller.Instance.StationInfo.供料器震动++ ;
                                }
                                break;

                            case "发送Epson1下相机结果":
                                if (Controller.Instance.StationInfo.Epson1下相机拍照结果 == 1)
                                {
                                    new ModbusControlProcess.ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|右下相机拍照OK|1|3", "隐藏").Run();
                                }
                                else
                                {
                                    new ModbusControlProcess.ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|右下相机拍照NG|1|3", "隐藏").Run();
                                }
                                break;

                            case "Epson1允许刷新":
                                Controller.Instance.StationInfo.Epson1下相机是否刷新 = true;
                                break;
                            case "Epson1请求总结果允许刷新":
                                Controller.Instance.StationInfo.右皮带线是否刷新 = true;
                                break;
                            case "清除Epson1上相机点位":
                                while (Controller.Instance.StationInfo.Epson1X.TryDequeue(out double xyr))
                                {

                                }

                                while (Controller.Instance.StationInfo.Epson1Y.TryDequeue(out double xyr))
                                {

                                }

                                while (Controller.Instance.StationInfo.Epson1R.TryDequeue(out double xyr))
                                {

                                }
                                break;


                            case "清除Epson2上相机点位":
                                while (Controller.Instance.StationInfo.Epson2X.TryDequeue(out double xyr))
                                {

                                }

                                while (Controller.Instance.StationInfo.Epson2Y.TryDequeue(out double xyr))
                                {

                                }

                                while (Controller.Instance.StationInfo.Epson2R.TryDequeue(out double xyr))
                                {

                                }
                                break;
                            case "输出飞拍定位结果":
                                if (Controller.Instance.StationInfo.FlyVisionResult==1)
                                {
                                    new ModbusControlProcess.ModbusPLCCommunicationProcess("ModbusPLC|输出|PLC|飞拍完成|1|3", "隐藏").Run();
                                }
                                else
                                {
                                    new ModbusControlProcess.ModbusPLCCommunicationProcess("ModbusPLC|输出|PLC|飞拍完成|2|3", "隐藏").Run();
                                }
                                break;

                            case "输出飞拍复检结果":
                                if (Controller.Instance.StationInfo.FlyVisionResult == 1)
                                {
                                    new ModbusControlProcess.ModbusPLCCommunicationProcess("ModbusPLC|输出|PLC|复检飞拍完成|1|-1", "隐藏").Run();
                                }
                                else
                                {
                                    new ModbusControlProcess.ModbusPLCCommunicationProcess("ModbusPLC|输出|PLC|复检飞拍完成|2|-1", "隐藏").Run();
                                }
                                break;
                            case "清除组装信号":
                                Controller.Instance.StationInfo.皮带线允许组装 = 0;
                                break;
                            case "呼叫Epson组装":
                                if (Controller.Instance.StationInfo.Epson1呼叫组装==1 && Controller.Instance.StationInfo.Epson2呼叫组装==1)
                                {
                                    new ModbusControlProcess.ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|Epson1流水线呼叫组装|1|3", "飞拍组装流程").Run();
                                }
                                else if (Controller.Instance.StationInfo.Epson1呼叫组装 == 1)
                                {
                                    new ModbusControlProcess.ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|Epson1流水线呼叫组装|1|3", "飞拍组装流程").Run();
                                }
                                else if (Controller.Instance.StationInfo.Epson2呼叫组装 == 1)
                                {
                                    new ModbusControlProcess.ModbusRobotCommunicationProcess("ModbusRobot|输出|Robot|Epson2流水线呼叫组装|1|3", "飞拍组装流程").Run();
                                }
                                break;
                            case "供料器震动":
                                if ((Controller.Instance.StationInfo.左上相机Mean+ Controller.Instance.StationInfo.右上相机Mean)/2>100)
                                {
                                    new ModbusControlProcess.ModbusPLCCommunicationProcess("ModbusPLC|输出|PLC|供料器|1|3", "隐藏").Run();
                                    Thread.Sleep(2000);
                                    new ModbusControlProcess.ModbusPLCCommunicationProcess("ModbusPLC|输出|PLC|供料器|0|3", "隐藏").Run();
                                }
                                break;



















                            case "产能":
                                #region 产能
                                int addOkCount = 4;
                                for (int i = 0; i < addOkCount; i++)
                                {
                                    YieldManager.Instance.AddSingleProduct(ProductResult.Ok);
                                }
                                Message = $"产能统计完成 - 新增{addOkCount}个合格产品（类型: {_pointType}）";
                                break;
                            #endregion

                            case "投入数":
                                #region 投入数
                                int addInputCount = 2;
                                for (int i = 0; i < addInputCount; i++)
                                {
                                    YieldManager.Instance.AddSingleProduct(ProductResult.投入数);
                                }
                                Message = $"投入数统计完成 - 新增{addInputCount}条投入记录（类型: {_pointType}）";
                                break;
                            #endregion

                            case "记录CT":
                                #region 记录CT
                                if (Controller.Instance.StationInfo.上次下料时间 != DateTime.MinValue)
                                {
                                    double ctSeconds = (DateTime.Now - Controller.Instance.StationInfo.上次下料时间).TotalSeconds;
                                    Frm_UtilizationRate.Instance._ctQueue.Enqueue(ctSeconds);
                                    Controller.Instance.StationInfo.CtTime = ctSeconds;
                                    Message = $"CT记录完成 - 耗时: {ctSeconds:F2}秒（上次下料时间: {Controller.Instance.StationInfo.上次下料时间:yyyy-MM-dd HH:mm:ss}）";
                                }
                                else
                                {
                                    Message = $"CT记录完成 - 首次记录（无历史下料时间）";
                                }
                                Controller.Instance.StationInfo.上次下料时间 = DateTime.Now;
                                break;
                            #endregion















                            default:
                                CurrentStatus = GeneralStatus.Error;
                                Message = $"{_pointType}-{_pointName}错误: 未知点位名称，请检查流程配置表";
                                return;
                        }
                        break;
                    #endregion
                    default:
                        CurrentStatus = GeneralStatus.Error;
                        Message = $"未知点位类型: {_pointType}，请检查流程配置表（点位名称: {_pointName}）";
                        return;
                }

                // 所有判断逻辑完成后，设置最终状态为成功
                CurrentStatus = GeneralStatus.NoError;
            }
            catch (Exception ex)
            {
                // 异常消息包含完整上下文
                this.Message = $"{_pointType}-{_pointName}执行异常: {ex.Message}（堆栈: {ex.StackTrace.Substring(0, Math.Min(ex.StackTrace.Length, 200))}）";
                this.CurrentStatus = GeneralStatus.Error;
                RLog.SaveError(ex);
                return;
            }
        }


        public bool 前滑台第一焊接位(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"前滑台第一焊接位接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }
                result.二维码 = code;
                OP70 oP70 = new OP70();

                oP70.product_code = code;
                oP70.inbound_time = DateTime.Now;
                oP70.rfid = "1-1";
                oP70.zf = Controller.Instance.StationInfo.前焊接机振幅;
                oP70.bh = "1#";
                oP70.xdsdzz = result.相对深度总值 / 1000;
                oP70.cfyl = Controller.Instance.StationInfo.前焊接机触发压力;
                oP70.hjsj = result.焊接时间 / 1000;
                oP70.sd = result.速度;
                oP70.jdsdzz = result.绝对深度总值 / 1000;
                oP70.hjxdsd = result.焊接相对深度 / 1000;
                oP70.hjfzgl = result.焊接峰值功率;
                oP70.hjnl = result.焊接能量 / 10;
                oP70.zdhjyl = result.最大焊接压力;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);
                Controller.Instance.StationInfo.前滑台上传信息.Enqueue(oP70);
                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"前滑台第一焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }

        public bool 前滑台第三焊接位(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"前滑台第三焊接位接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }
                result.二维码 = code;
                OP70 oP70 = new OP70();
                oP70.product_code = code;
                oP70.inbound_time = DateTime.Now;
                oP70.rfid = "1-2";
                oP70.zf = Controller.Instance.StationInfo.前焊接机振幅;
                oP70.bh = "1#";
                oP70.xdsdzz = result.相对深度总值 / 1000;
                oP70.cfyl = Controller.Instance.StationInfo.前焊接机触发压力;
                oP70.hjsj = result.焊接时间 / 1000;
                oP70.sd = result.速度;
                oP70.jdsdzz = result.绝对深度总值 / 1000;
                oP70.hjxdsd = result.焊接相对深度 / 1000;
                oP70.hjfzgl = result.焊接峰值功率;
                oP70.hjnl = result.焊接能量 / 10;
                oP70.zdhjyl = result.最大焊接压力;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);
                Controller.Instance.StationInfo.前滑台上传信息.Enqueue(oP70);
                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"前滑台第三焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }

        public bool 后滑台第二焊接位(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"后滑台第二焊接位接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }

                result.二维码 = code;
                OP70 oP70 = new OP70();
                oP70.product_code = code;
                oP70.inbound_time = DateTime.Now;
                oP70.rfid = "2-1";
                oP70.zf = Controller.Instance.StationInfo.后焊接机振幅;
                oP70.bh = "2#";
                oP70.xdsdzz = result.相对深度总值 / 1000;
                oP70.cfyl = Controller.Instance.StationInfo.后焊接机触发压力;
                oP70.hjsj = result.焊接时间 / 1000;
                oP70.sd = result.速度;
                oP70.jdsdzz = result.绝对深度总值 / 1000;
                oP70.hjxdsd = result.焊接相对深度 / 1000;
                oP70.hjfzgl = result.焊接峰值功率;
                oP70.hjnl = result.焊接能量 / 10;
                oP70.zdhjyl = result.最大焊接压力;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);
                Controller.Instance.StationInfo.后滑台上传信息.Enqueue(oP70);
                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"后滑台第二焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }

        public bool 后滑台第四焊接位(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"后滑台第四焊接位接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }
                result.二维码 = code;
                OP70 oP70 = new OP70();
                oP70.product_code = code;
                oP70.inbound_time = DateTime.Now;
                oP70.rfid = "2-2";
                oP70.zf = Controller.Instance.StationInfo.后焊接机振幅;
                oP70.bh = "2#";
                oP70.xdsdzz = result.相对深度总值 / 1000;
                oP70.cfyl = Controller.Instance.StationInfo.后焊接机触发压力;
                oP70.hjsj = result.焊接时间 / 1000;
                oP70.sd = result.速度;
                oP70.jdsdzz = result.绝对深度总值 / 1000;
                oP70.hjxdsd = result.焊接相对深度 / 1000;
                oP70.hjfzgl = result.焊接峰值功率;
                oP70.hjnl = result.焊接能量 / 10;
                oP70.zdhjyl = result.最大焊接压力;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);
                Controller.Instance.StationInfo.后滑台上传信息.Enqueue(oP70);
                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"后滑台第四焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }

        public bool 前滑台第二焊接位(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"前滑台第二焊接位接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }
                result.二维码 = code;
                OP70 oP70 = new OP70();
                oP70.product_code = code;
                oP70.inbound_time = DateTime.Now;
                oP70.rfid = "1-1";
                oP70.zf = Controller.Instance.StationInfo.前焊接机振幅;
                oP70.bh = "1#";
                oP70.xdsdzz = result.相对深度总值 / 1000;
                oP70.cfyl = Controller.Instance.StationInfo.前焊接机触发压力;
                oP70.hjsj = result.焊接时间 / 1000;
                oP70.sd = result.速度;
                oP70.jdsdzz = result.绝对深度总值 / 1000;
                oP70.hjxdsd = result.焊接相对深度 / 1000;
                oP70.hjfzgl = result.焊接峰值功率;
                oP70.hjnl = result.焊接能量 / 10;
                oP70.zdhjyl = result.最大焊接压力;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);
                Controller.Instance.StationInfo.前滑台上传信息.Enqueue(oP70);
                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"前滑台第二焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }

        public bool 前滑台第四焊接位(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"前滑台第四焊接位接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }
                result.二维码 = code;
                OP70 oP70 = new OP70();
                oP70.product_code = code;
                oP70.inbound_time = DateTime.Now;
                oP70.rfid = "1-2";
                oP70.zf = Controller.Instance.StationInfo.前焊接机振幅;
                oP70.bh = "1#";
                oP70.xdsdzz = result.相对深度总值 / 1000;
                oP70.cfyl = Controller.Instance.StationInfo.前焊接机触发压力;
                oP70.hjsj = result.焊接时间 / 1000;
                oP70.sd = result.速度;
                oP70.jdsdzz = result.绝对深度总值 / 1000;
                oP70.hjxdsd = result.焊接相对深度 / 1000;
                oP70.hjfzgl = result.焊接峰值功率;
                oP70.hjnl = result.焊接能量 / 10;
                oP70.zdhjyl = result.最大焊接压力;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);
                Controller.Instance.StationInfo.前滑台上传信息.Enqueue(oP70);
                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"前滑台第四焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }

        public bool 后滑台第一焊接位(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"后滑台第一焊接位接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }

                result.二维码 = code;
                OP70 oP70 = new OP70();
                oP70.product_code = code;
                oP70.inbound_time = DateTime.Now;
                oP70.rfid = "2-1";
                oP70.zf = Controller.Instance.StationInfo.后焊接机振幅;
                oP70.bh = "2#";
                oP70.xdsdzz = result.相对深度总值 / 1000;
                oP70.cfyl = Controller.Instance.StationInfo.后焊接机触发压力;
                oP70.hjsj = result.焊接时间 / 1000;
                oP70.sd = result.速度;
                oP70.jdsdzz = result.绝对深度总值 / 1000;
                oP70.hjxdsd = result.焊接相对深度 / 1000;
                oP70.hjfzgl = result.焊接峰值功率;
                oP70.hjnl = result.焊接能量 / 10;
                oP70.zdhjyl = result.最大焊接压力;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);
                Controller.Instance.StationInfo.后滑台上传信息.Enqueue(oP70);
                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"后滑台第一焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }

        public bool 后滑台第三焊接位(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"后滑台第三焊接位接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }
                result.二维码 = code;
                OP70 oP70 = new OP70();
                oP70.product_code = code;
                oP70.inbound_time = DateTime.Now;
                oP70.rfid = "2-2";
                oP70.zf = Controller.Instance.StationInfo.后焊接机振幅;
                oP70.bh = "2#";
                oP70.xdsdzz = result.相对深度总值 / 1000;
                oP70.cfyl = Controller.Instance.StationInfo.后焊接机触发压力;
                oP70.hjsj = result.焊接时间 / 1000;
                oP70.sd = result.速度;
                oP70.jdsdzz = result.绝对深度总值 / 1000;
                oP70.hjxdsd = result.焊接相对深度 / 1000;
                oP70.hjfzgl = result.焊接峰值功率;
                oP70.hjnl = result.焊接能量 / 10;
                oP70.zdhjyl = result.最大焊接压力;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;


                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);
                Controller.Instance.StationInfo.后滑台上传信息.Enqueue(oP70);
                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"后滑台第三焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }




        public bool 前滑台焊接数据手动(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"前滑台接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }
                result.二维码 = code;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);

                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"前滑台第一焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }

        public bool 后滑台焊接数据手动(HttpClient httpClient, string uri, string code)
        {
            try
            {
                Post post = new Post(12345);
                string jsonData = JsonConvert.SerializeObject(post);
                ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(uri, content).Result;
                response.EnsureSuccessStatusCode();
                var responseString = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<获取最后焊接结果>(responseString);
                if (result.状态码 != 0)
                {
                    Message = $"后滑台接口返回错误 - 状态码: {result.状态码}，码值: {code}";
                    return false;
                }
                result.二维码 = code;

                result.相对深度总值 = result.相对深度总值 / 1000;
                result.绝对深度总值 = result.绝对深度总值 / 1000;
                result.焊接相对深度 = result.焊接相对深度 / 1000;
                result.焊接能量 = result.焊接能量 / 10;
                result.焊接时间 = result.焊接时间 / 1000;
                result.焊接绝对深度 = result.焊接绝对深度 / 1000;
                result.触发距离 = result.触发距离 / 1000;
                result.焊接日期和时间 = DateTime.Now;

                MainPage.Instance.AddDataRow(MainPage.Instance.dataGridView1, result);

                AppendToCsv(result, ProcessPath.dataPath + DateTime.Now.ToString("yyyyMMdd") + ".csv");
                return true;
            }
            catch (Exception ex)
            {
                Message = $"前滑台第一焊接位处理异常: {ex.Message}（码值: {code}）";
                return false;
            }
        }
        public static void AppendToCsv<T>(T data, string filePath, char separator = ',') where T : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            // 获取对象的所有公共属性
            PropertyInfo[] properties = typeof(T).GetProperties();
            if (properties.Length == 0)
                throw new InvalidOperationException("对象没有公共属性可序列化");

            bool fileExists = File.Exists(filePath);
            if (!fileExists)
            {
                Directory.CreateDirectory(ProcessPath.dataPath);
            }
            StringBuilder csvLine = new StringBuilder();

            using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                // 如果文件不存在，先写入标题行
                if (!fileExists)
                {
                    string header = string.Join(separator.ToString(),
                        Array.ConvertAll(properties, prop => prop.Name));
                    writer.WriteLine(header);
                }

                // 写入数据行
                string dataLine = string.Join(separator.ToString(),
                    Array.ConvertAll(properties, prop =>
                        FormatCsvValue(prop.GetValue(data)?.ToString() ?? "")));
                writer.WriteLine(dataLine);
            }
        }

        private static string FormatCsvValue(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
        /// <summary>
        /// 焊接机配方读取
        /// </summary>
        /// <param name="num">1前焊接机  2后焊接机</param>
        /// <returns></returns>

        private string GetUSWType(int num)
        {
            string sql = $"SELECT config_value FROM sys_config where config_key = 'USWType{num}'";
            return Jabil.Lib.SQLHelper.SugarSQLPgHelper.Instance.DB.Ado.GetString(sql);
        }

        private bool 设置激活的参数组(string uri,int sid,int recipeNo)
        {
            //Login login = new Login("ADMIN", "123456Aa#", "1234567890QWERTYUIOPASDFGHJKLZXC");
            SetStoreLogin setStoreLogin = new SetStoreLogin(sid,recipeNo);

            string jsonData = JsonConvert.SerializeObject(setStoreLogin);
            ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var _httpClient = new HttpClient();
            var response = _httpClient.PostAsync(uri, content).Result;
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().Result;
            var Store = JsonConvert.DeserializeObject<SetStoreLogout>(responseString);
            if (Store.StatusCode != 0)
            {
                return false;
            }
            return true;
        }
    }
}