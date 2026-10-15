using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using JabilVisionMotion.Edit.Process.IOControlProcess;
using JabilVisionMotion.Frm;
using JabilVisionMotion.Frm.Dialog;
using JabilVisionMotion.Frm.UserPage;
using JabilVisionMotion.Lib;
using lctdevice;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process
{
    /// <summary>
    /// 临时变量流程处理类，继承自ProcessBase基类
    /// 核心功能：处理系统临时变量的读写操作，支持输入/输出两种类型，包含超时控制和急停响应机制
    /// 指令格式：TemporaryVariables|点位类型(输入/输出)|点位名称|信号值|超时时间(秒，-1表示不超时)
    /// 支持的点位：
    /// - 输入：下料盘状态、左供料抽屉灯状态、右供料抽屉灯状态
    /// - 输出：左供料抽屉灯状态、右供料抽屉灯状态
    /// 设计特点：使用字典映射点位与处理方法，减少条件判断；统一封装超时和急停检查逻辑
    /// </summary>
    public class TemporaryVariablesProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        private const int CheckIntervalMs = 1;  // 状态检查间隔(毫秒)，降低CPU占用
        #endregion

        #region 私有字段
        private readonly string _process;       // 原始指令字符串
        private readonly string _processTip;    // 指令提示信息（用于日志）
        private string _commandName;            // 指令名称（固定为"TemporaryVariables"）
        private string _pointType;              // 点位类型（"输入"或"输出"）
        private string _pointName;              // 点位名称（如"下料盘状态"）
        private int _signalValue;               // 信号值（用于设置输出或判断输入条件）
        private int _timeoutSeconds;            // 超时时间（秒，-1表示不超时）

        // 输入点位处理器映射表：键为点位名称，值为处理方法（返回bool表示是否满足条件）
        private readonly Dictionary<string, Func<bool>> _inputHandlers;
        // 输出点位处理器映射表：键为点位名称，值为处理方法（无返回值）
        private readonly Dictionary<string, Action> _outputHandlers;
        #endregion

        /// <summary>
        /// 初始化临时变量流程
        /// </summary>
        /// <param name="process">临时变量指令字符串（如"TemporaryVariables|输入|下料盘状态|1|10"）</param>
        /// <param name="processTip">指令提示信息（用于日志记录）</param>
        public TemporaryVariablesProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                // 初始化输入点位处理器映射表（绑定点位名称与对应的处理方法）
                //_inputHandlers = new Dictionary<string, Func<bool>>
                _inputHandlers = new Dictionary<string, Func<bool>>
            {
                    {"Epson1请求下相机拍照",()=> Controller.Instance.StationInfo.Epson1下相机请求拍照== _signalValue },
                    {"Epson1皮带线请求结果",()=>Controller.Instance.StationInfo.Epson1皮带线请求结果==_signalValue && Controller.Instance.StationInfo.皮带线允许组装==_signalValue },

                    {"Epson2请求下相机拍照",()=> Controller.Instance.StationInfo.Epson2下相机请求拍照== _signalValue },
                    {"Epson2皮带线请求结果",()=>Controller.Instance.StationInfo.Epson2皮带线请求结果==_signalValue && Controller.Instance.StationInfo.皮带线允许组装==_signalValue },

                     {"流水线组装完成",()=>(Controller.Instance.StationInfo.Epson1流水线组装完成==_signalValue ||Controller.Instance.StationInfo.Epson2流水线组装完成==_signalValue) },
                    {"Epson1或Epson2准备好组装",()=>(Controller.Instance.StationInfo.Epson1呼叫组装==_signalValue ||Controller.Instance.StationInfo.Epson2呼叫组装==_signalValue) },

                    {"飞拍流程选择",()=>飞拍流程选择()==_signalValue },
                     //{"搬运轴是否运动中", ()=> Controller.Instance.StationInfo.搬运轴是否运动中 == _signalValue },
                     {"前滑台穴位选择", ()=>   前滑台穴位选择() == _signalValue},
                     {"后滑台穴位选择", ()=>   后滑台穴位选择() == _signalValue },
                     
                     {"前覆膜机覆膜完成", ()=> Controller.Instance.StationInfo.前覆膜机覆膜完成 == _signalValue },
                     {"后覆膜机覆膜完成", ()=> Controller.Instance.StationInfo.后覆膜机覆膜完成 == _signalValue },

                     {"前覆膜机允许覆膜", ()=> Controller.Instance.StationInfo.前覆膜机允许覆膜 == _signalValue },
                     {"后覆膜机允许覆膜", ()=> Controller.Instance.StationInfo.后覆膜机允许覆膜 == _signalValue },

                     {"搬运轴在前滑台取料完成", ()=> Controller.Instance.StationInfo.搬运轴在前滑台取料完成 == _signalValue },
                     {"搬运轴在后滑台取料完成", ()=> Controller.Instance.StationInfo.搬运轴在后滑台取料完成 == _signalValue },

                     {"搬运轴流程选择", ()=>  _signalValue == 搬运轴_流程选择() },

                     {"呼叫搬运轴来前滑台取料", ()=> Controller.Instance.StationInfo.呼叫搬运轴来前滑台取料 == _signalValue },
                     {"呼叫搬运轴来后滑台取料", ()=> Controller.Instance.StationInfo.呼叫搬运轴来后滑台取料 == _signalValue },

                     {"允许进入治具选择流程", ()=> Controller.Instance.StationInfo.允许进入治具选择流程 == _signalValue },

                     {"手动流程选择等待", ()=>  Project.Instance.GlobalVariables.手动流程选择等待 == _signalValue },
            };

                // 初始化输出点位处理器映射表
                _outputHandlers = new Dictionary<string, Action>
            {
                     //{"搬运轴是否运动中", ()=> Controller.Instance.StationInfo.搬运轴是否运动中 = _signalValue },

                     {"呼叫搬运轴来前滑台取料", ()=> Controller.Instance.StationInfo.呼叫搬运轴来前滑台取料 = _signalValue },
                     {"呼叫搬运轴来后滑台取料", ()=> Controller.Instance.StationInfo.呼叫搬运轴来后滑台取料 = _signalValue },


                     {"搬运轴在前滑台取料完成", ()=> Controller.Instance.StationInfo.搬运轴在前滑台取料完成 = _signalValue },
                     {"搬运轴在后滑台取料完成", ()=> Controller.Instance.StationInfo.搬运轴在后滑台取料完成 = _signalValue },


                     {"前覆膜机允许覆膜", ()=> Controller.Instance.StationInfo.前覆膜机允许覆膜 = _signalValue },
                     {"后覆膜机允许覆膜", ()=> Controller.Instance.StationInfo.后覆膜机允许覆膜 = _signalValue },

                     {"前覆膜机覆膜完成", ()=> Controller.Instance.StationInfo.前覆膜机覆膜完成 = _signalValue },
                     {"后覆膜机覆膜完成", ()=> Controller.Instance.StationInfo.后覆膜机覆膜完成 = _signalValue },


                      {"搬运轴流程选择", ()=>Controller.Instance.StationInfo.搬运轴流程选择 = _signalValue },

                };

                ParseCommand(process);  // 解析指令参数

                CurrentStatus = GeneralStatus.Wait; // 初始状态：等待
                Message = "Wait";
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error; // 初始化失败：错误状态
                Message = ex.Message;               // 记录异常信息
                RLog.SaveError(ex);                 // 保存错误日志
                return; // 初始化错误时立即返回
            }
        }


        private int 搬运轴_流程选择()
        {

            //Controller.Instance.是否屏蔽滑台 == 0  不屏蔽   Controller.Instance.是否屏蔽滑台 == 1屏蔽前滑台  Controller.Instance.是否屏蔽滑台 == 2屏蔽后滑台
            if (Controller.Instance.StationInfo.呼叫搬运轴来前滑台取料 == 1 || Controller.Instance.StationInfo.呼叫搬运轴来后滑台取料 == 1)
            {
                if (Controller.Instance.StationInfo.呼叫搬运轴来前滑台取料 == 1 && Controller.Instance.StationInfo.呼叫搬运轴来后滑台取料 == 0 && Controller.Instance.是否屏蔽滑台==0)
                {
                    Controller.Instance.StationInfo.搬运轴流程选择 = 1;
                }
                else if (Controller.Instance.StationInfo.呼叫搬运轴来前滑台取料 == 0 && Controller.Instance.StationInfo.呼叫搬运轴来后滑台取料 == 1 && Controller.Instance.是否屏蔽滑台 == 0)
                {
                    Controller.Instance.StationInfo.搬运轴流程选择 = 2;
                }
                else if (Controller.Instance.StationInfo.呼叫搬运轴来前滑台取料 == 1 && Controller.Instance.StationInfo.呼叫搬运轴来后滑台取料 == 1 && Controller.Instance.是否屏蔽滑台 == 0)
                {
                    Controller.Instance.StationInfo.搬运轴流程选择 = 1;
                }
                else if (Controller.Instance.StationInfo.呼叫搬运轴来前滑台取料 == 1 && Controller.Instance.StationInfo.呼叫搬运轴来后滑台取料 == 0 && Controller.Instance.是否屏蔽滑台 == 2)
                {
                    //前滑台单轴
                    Controller.Instance.StationInfo.搬运轴流程选择 = 3;
                }
                else if (Controller.Instance.StationInfo.呼叫搬运轴来前滑台取料 == 0 && Controller.Instance.StationInfo.呼叫搬运轴来后滑台取料 == 1 && Controller.Instance.是否屏蔽滑台 == 1)
                {
                    //后滑台单轴
                    Controller.Instance.StationInfo.搬运轴流程选择 = 4;
                }
                else
                {
                    
                    Controller.Instance.StationInfo.搬运轴流程选择 = 0;
                }
                return 1;
            }

            return 0;
        }


        private int 后滑台穴位选择()
        {
            if (Controller.Instance.后滑台呼叫放料 == 1 || Controller.Instance.后滑台呼叫放料 == 2)
            {
                return 1;
            }

            return 0;
        }


        private int 前滑台穴位选择()
        {
            if (Controller.Instance.前滑台呼叫放料 == 1 || Controller.Instance.前滑台呼叫放料 == 2)
            {
                return 1;
            }

            return 0;
        }



        private int 飞拍流程选择()
        {
            if (Controller.Instance.飞拍功能选择==1&& Controller.Instance.飞拍功能选择 == 2)
            {
                if (Controller.Instance.飞拍功能选择 == 1)
                {
                    Controller.Instance.In飞拍功能选择 = 1;
                }
                else if (Controller.Instance.飞拍功能选择 == 2)
                {
                    Controller.Instance.In飞拍功能选择 = 2;
                }
                return 1;
            }
            return 0;

        }

        /// <summary>
        /// 克隆当前对象，创建一个新的TemporaryVariablesProcess实例并复制所有状态
        /// </summary>
        /// <returns>克隆后的对象</returns>
        public override object Clone()
        {
            // 创建新实例
            var clone = new TemporaryVariablesProcess(_process, _processTip)
            {
                // 复制所有字段值
                _commandName = this._commandName,
                _pointType = this._pointType,
                _pointName = this._pointName,
                _signalValue = this._signalValue,
                _timeoutSeconds = this._timeoutSeconds,
                CurrentStatus = this.CurrentStatus,
                Message = this.Message
            };
            // 初始化克隆实例的状态为"等待"
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";

            return clone;
        }

        /// <summary>
        /// 解析临时变量指令参数，验证格式并提取关键信息
        /// </summary>
        /// <param name="process">待解析的指令字符串</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式（至少5个部分）
            if (commandParts.Length < 5)
            {
                HandleError($"指令格式错误: {process}，至少需要5个部分，格式为TemporaryVariables|点位类型|点位名称|信号值|超时时间");
                return;
            }

            // 提取指令各部分参数
            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _pointName = commandParts[2];

            // 验证信号值是否为整数
            if (!int.TryParse(commandParts[3], out _signalValue))
            {
                HandleError($"信号值格式错误: {commandParts[3]}，必须为整数");
                return;
            }

            // 验证超时时间是否为整数
            if (!int.TryParse(commandParts[4], out _timeoutSeconds))
            {
                HandleError($"超时时间格式错误: {commandParts[4]}，必须为整数");
                return;
            }

            // 验证点位类型是否合法（仅支持"输入"和"输出"）
            if (_pointType != "输入" && _pointType != "输出")
            {
                HandleError($"不支持的点位类型: {_pointType}，仅支持'输入'和'输出'");
                return;
            }

            // 验证点位名称是否支持（根据类型检查对应的处理器映射表）
            if ((_pointType == "输入" && !_inputHandlers.ContainsKey(_pointName)) ||
                (_pointType == "输出" && !_outputHandlers.ContainsKey(_pointName)))
            {
                HandleError($"不支持的点位名称: {_pointName}，点位类型: {_pointType}");
                return;
            }
        }

        /// <summary>
        /// 执行临时变量处理逻辑（核心方法）
        /// 根据点位类型（输入/输出）执行对应的读写操作，并处理超时和急停
        /// </summary>
        public override void Run()
        {
            try
            {
                // 测试模式：直接返回成功（不执行实际操作）
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 更新运行状态
                CurrentStatus = GeneralStatus.Running;
                Message = $"Running-- 处理[{_pointType}]类型的[{_pointName}]，信号值: {_signalValue}，超时: {_timeoutSeconds}秒";

                // 根据点位类型执行对应操作
                switch (_pointType)
                {
                    case "输入":
                        ExecuteInputProcessing();  // 处理输入类型点位
                        break;
                    case "输出":
                        ExecuteOutputProcessing(); // 处理输出类型点位
                        break;
                    default:
                        SetErrorStatus($"未知点位类型--{_pointType}");
                        return;
                }

                // 操作完成且未出错：设置成功状态
                if (CurrentStatus != GeneralStatus.Error)
                {
                    CurrentStatus = GeneralStatus.NoError;
                }
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error; // 执行失败：错误状态
                Message = ex.Message;               // 记录异常信息
                RLog.SaveError(ex);                 // 保存错误日志
                return; // 异常导致错误状态时返回
            }
        }

        #region 输入处理逻辑（读取并等待条件满足）

        /// <summary>
        /// 执行输入类型点位处理
        /// 循环检查点位状态，直到满足条件、超时或触发急停
        /// </summary>
        private void ExecuteInputProcessing()
        {
            var startTime = DateTime.Now; // 记录开始时间，用于超时计算

            // 循环检查直到满足条件
            while (true)
            {
                // 检查急停状态（急停时终止并报错）
                if (CheckEmergencyStop())
                {
                    return;
                }

                // 检查是否超时（超时终止并报错）
                if (CheckTimeout(startTime))
                {
                    return;
                }
                // 执行对应输入点位的处理方法，判断是否满足条件
                if (_inputHandlers.TryGetValue(_pointName, out var handler) && handler.Invoke())
                {
                    // 满足条件：更新状态消息并退出循环
                    Message = $"NoError--点位名称：{_pointName}-->>信号值：{_signalValue}";
                    break;
                }

                // 短暂休眠，降低CPU占用
                Thread.Sleep(CheckIntervalMs);
            }
        }

        #endregion

        #region 输出处理逻辑（设置指定点位的值）

        /// <summary>
        /// 执行输出类型点位处理
        /// 直接调用对应点位的处理方法，设置信号值
        /// </summary>
        private void ExecuteOutputProcessing()
        {
            // 检查错误状态
            if (CurrentStatus == GeneralStatus.Error)
                return;

            if (_outputHandlers.TryGetValue(_pointName, out var handler))
            {
                // 执行输出处理方法
                handler.Invoke();
                // 更新状态消息
                Message = $"NoError--点位名称：{_pointName}-->>信号值：{_signalValue}";
            }
            else
            {
                SetErrorStatus($"未知输出点位名称--{_pointName}");
                return;
            }
        }

        /// <summary>
        /// 处理"左供料抽屉灯状态"输出点位
        /// 设置左供料抽屉灯的状态为指定信号值
        /// </summary>
        private void HandleOutputLeftFeederLightStatus()
        {
            // 检查错误状态
            if (CurrentStatus == GeneralStatus.Error)
                return;

            //SysParm.Instance.BuzzerLed.LeftFeederDrawerStatus = _signalValue;
        }

        /// <summary>
        /// 处理"右供料抽屉灯状态"输出点位
        /// 设置右供料抽屉灯的状态为指定信号值
        /// </summary>
        private void HandleOutputRightFeederLightStatus()
        {
            // 检查错误状态
            if (CurrentStatus == GeneralStatus.Error)
                return;

            //SysParm.Instance.BuzzerLed.RightFeederDrawerStatus = _signalValue;
        }

        #endregion

        #region 通用辅助方法

        /// <summary>
        /// 检查急停状态
        /// </summary>
        /// <returns>true表示触发急停，false表示正常</returns>
        private bool CheckEmergencyStop()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                SetErrorStatus("机台急停");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否超时
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <returns>true表示超时，false表示未超时</returns>
        private bool CheckTimeout(DateTime startTime)
        {
            // 超时时间不为-1且已超过设定时间
            if (_timeoutSeconds != -1 &&
                DateTime.Now.Subtract(startTime).TotalSeconds > _timeoutSeconds)
            {
                SetErrorStatus($"超时！！   报警信息：{Message}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置错误状态（统一更新运行状态和消息）
        /// </summary>
        /// <param name="errorMessage">错误描述信息</param>
        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }

        /// <summary>
        /// 处理错误，设置机台紧急停止状态并记录日志
        /// </summary>
        /// <param name="errorMessage">错误信息描述</param>
        /// <exception cref="InvalidOperationException">始终抛出此异常，通知上层处理错误</exception>
        private void HandleError(string errorMessage)
        {
            // 设置机台为紧急停止状态
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

            // 记录紧急错误日志
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _process,
                $"--->>错误：{errorMessage}！！！！"
            );
            CurrentStatus = GeneralStatus.Error;
            // 抛出异常，终止当前运动流程
            throw new InvalidOperationException(errorMessage);
        }

        #endregion
    }
}