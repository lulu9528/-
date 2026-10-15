using csLTDMC;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Module.IO;
using Jabil.Module.IO.Common.Dtos;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using JabilVisionMotion.Lib;
using lctdevice;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process.IOControlProcess
{
    /// <summary>
    /// IOTime流程处理类，继承自ProcessBase基类
    /// 核心功能：检测输入信号是否在指定时间内持续保持目标状态（高/低电平）
    /// </summary>
    public class IOTimeProcess : ProcessBase, ICloneable
    {
        #region 常量定义
        private const int SleepIntervalMs = 1;
        private const int DefaultTimeoutSeconds = 30;
        private const int M60PointGroupSize = 32;
        private const int SignalHigh = 1;
        private const int SignalLow = 0;
        #endregion

        #region 私有字段
        private readonly string _process;
        private readonly string _processTip;
        private static readonly SemaphoreSlim _ioSemaphore = new SemaphoreSlim(1, 1);

        private string _commandName;
        private string _pointType;
        private string _pointName;
        private int _signalValue;
        private bool _signalState;
        private int _signalDurationSeconds;
        private int _timeoutSeconds;
        private IOCardDIODto _ioCardDio;
        private IOCardDto _ioCard;
        private int _diNumber;
        private CancellationToken? _cancellationToken;
        #endregion

        public IOTimeProcess(string process, string processTip) : base(process, processTip)
        {
            try
            {
                CurrentStatus = GeneralStatus.Wait;
                Message = "Wait";

                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);
                BindIoParameters();
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
                return; // 确保设置错误状态后返回
            }
        }

        public void SetCancellationToken(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        private void ParseCommand(string process)
        {
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (commandParts.Length < 6)
            {
                HandleIoError($"指令格式错误: {process}，至少需要6个部分，格式为指令名称|IO点位类型|点位名称|信号值|信号持续时间|超时");
                return; // 设置错误后立即返回
            }

            _commandName = commandParts[0];
            _pointType = commandParts[1];
            _pointName = commandParts[2];

            if (!int.TryParse(commandParts[3], out _signalValue) || (_signalValue != SignalLow && _signalValue != SignalHigh))
            {
                HandleIoError($"信号值必须为0或1，当前值：{commandParts[3]}");
                return; // 设置错误后立即返回
            }

            _signalState = _signalValue == SignalHigh;

            if (!int.TryParse(commandParts[4], out _signalDurationSeconds) || _signalDurationSeconds < 0)
            {
                HandleIoError($"信号持续时间必须为非负整数，当前值：{commandParts[4]}");
                return; // 设置错误后立即返回
            }

            if (!int.TryParse(commandParts[5], out _timeoutSeconds) || _timeoutSeconds < -1)
            {
                _timeoutSeconds = DefaultTimeoutSeconds;
                HandleIoError($"超时值格式错误: {commandParts[5]}，使用默认值 {DefaultTimeoutSeconds} 秒");
                return; // 设置错误后立即返回
            }
        }

        private void BindIoParameters()
        {
            _ioCardDio = IOSystem.Instance.IOCardDios
                .FirstOrDefault(dio => dio.点位名称 == _pointName && dio.点位类型 == _pointType);

            if (_ioCardDio == null)
            {
                HandleIoError($"未找到点位名称[{_pointName}]的IO配置");
                return; // 设置错误后立即返回
            }

            _ioCard = IOSystem.Instance.IOCards
                .FirstOrDefault(card => card.卡名称 == _ioCardDio.卡名称);

            if (_ioCard == null)
            {
                HandleIoError($"未找到IO卡[{_ioCardDio.卡名称}]的配置");
                return; // 设置错误后立即返回
            }

            _diNumber = CalculateDiNumber();
            ProcessId = _ioCardDio.绑定代码;
        }

        private int CalculateDiNumber()
        {
            switch (_ioCard.卡类型)
            {
                case "PCIe-M60":
                    return (_ioCard.卡号索引 - 1) * M60PointGroupSize + _ioCardDio.点位索引 + 1;
                case "PCIe-EC8003":
                    return _ioCardDio.点位索引 + 1;
                default:
                    return _ioCardDio.点位索引;
            }
        }

        private void HandleIoError(string errorMessage)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                _process,
                $"--->>错误：{errorMessage}！！！！"
            );

            // 设置错误状态并立即返回
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
        }

        public override void Run()
        {
            try
            {
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";

                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                if (_ioCardDio == null || _ioCard == null)
                {
                    SetErrorStatus("IO配置未初始化完成");
                    return; // 设置错误后立即返回
                }

                if (_ioCardDio.点位类型 != "输入")
                {
                    SetErrorStatus($"不支持的点位类型--{_ioCardDio.点位类型}，仅支持输入类型");
                    return; // 设置错误后立即返回
                }

                ExecuteTimedInputOperation();
            }
            catch (OperationCanceledException)
            {
                SetErrorStatus("操作已被取消");
                return; // 设置错误后立即返回
            }
            catch (Exception ex)
            {
                CurrentStatus = GeneralStatus.Error;
                Message = ex.Message;
                RLog.SaveError(ex);
                return; // 设置错误后立即返回
            }
        }

        private void ExecuteTimedInputOperation()
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                _cancellationToken?.ThrowIfCancellationRequested();

                if (CheckEmergencyStop())
                    return;

                if (CheckTimeout(stopwatch))
                    return;

                if (!UpdateInputSignalState())
                {
                    return;
                }

                if (Controller.Instance.StationInfo.DIs[_diNumber].StandOn(_signalDurationSeconds))
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError";
                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Green,
                        "IOTime操作成功",
                        _pointName,
                        $"信号[{_pointName}]已持续{_signalDurationSeconds}秒保持目标值{_signalValue}"
                    );
                    break;
                }

                _cancellationToken?.WaitHandle.WaitOne(SleepIntervalMs);
            }

            stopwatch.Stop();
        }

        private bool UpdateInputSignalState()
        {
            bool currentState;

            try
            {
                _ioSemaphore.Wait(_cancellationToken ?? CancellationToken.None);

                switch (_ioCard.卡类型)
                {
                    case "7432":
                    case "7230":
                        var readResult = DASK.DI_ReadLine(
                            _ioCard.卡号索引,
                            1,
                            _ioCardDio.点位索引,
                            out ushort value7x3x
                        );
                        if (readResult != 0)
                        {
                            SetErrorStatus($"读取7230/7432系列IO卡失败，返回代码: {readResult}");
                            return false; // 设置错误后立即返回
                        }
                        currentState = value7x3x == _signalValue;
                        break;

                    case "PCIe-EC8003":
                        short valueEcIo = (short)(_signalState ? SignalHigh : SignalLow);
                        var readResult8003 = lctdevice.ecat_io.EC_Get_Digital_Chn_In(
                            1,
                            (short)_diNumber,
                            ref valueEcIo,
                            (short)_ioCard.卡号索引
                        );
                        if (readResult8003 != 0)
                        {
                            SetErrorStatus($"读取PCIe-EC8003系列IO卡失败，返回代码: {readResult8003}");
                            return false; // 设置错误后立即返回
                        }
                        currentState = valueEcIo == _signalValue;
                        break;

                    case "PCIe-M60":
                        short valueM60;
                        short ret = ecat_motion.M_Get_Digital_Chn_Input(
                            (short)_diNumber,
                            out valueM60,
                            0
                        );
                        if (ret != 0)
                        {
                            SetErrorStatus($"读取PCIe-M60系列IO卡失败，返回代码: {ret}");
                            return false; // 设置错误后立即返回
                        }
                        currentState = valueM60 == _signalValue;
                        break;

                    default:
                        SetErrorStatus($"未知IO卡类型--{_ioCard.卡类型}，不支持该类型的时间检测");
                        return false; // 设置错误后立即返回
                }

                Controller.Instance.StationInfo.DIs[_diNumber].State = currentState;
                return true;
            }
            catch (Exception ex)
            {
                SetErrorStatus($"读取输入信号失败: {ex.Message}");
                return false; // 设置错误后立即返回
            }
            finally
            {
                if (_ioSemaphore.CurrentCount == 0)
                {
                    _ioSemaphore.Release();
                }
            }
        }

        private bool CheckEmergencyStop()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                SetErrorStatus("机台急停");
                return true; // 设置错误后立即返回
            }
            return false;
        }

        private bool CheckTimeout(Stopwatch stopwatch)
        {
            if (_timeoutSeconds != -1 &&
                stopwatch.Elapsed.TotalSeconds > _timeoutSeconds)
            {
                SetErrorStatus($"IO超时！未在{_timeoutSeconds}秒内检测到信号持续{_signalDurationSeconds}秒");
                return true; // 设置错误后立即返回
            }
            return false;
        }

        private void SetErrorStatus(string errorMessage)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = errorMessage;
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "IOTime操作错误",
                _pointName,
                errorMessage
            );
            // 无需在此处返回，由调用方负责返回
        }

        public override object Clone()
        {
            var cloned = new IOTimeProcess(this._process, this._processTip);
            cloned._commandName = this._commandName;
            cloned._pointType = this._pointType;
            cloned._pointName = this._pointName;
            cloned._signalValue = this._signalValue;
            cloned._signalState = this._signalState;
            cloned._signalDurationSeconds = this._signalDurationSeconds;
            cloned._timeoutSeconds = this._timeoutSeconds;
            cloned._ioCardDio = this._ioCardDio;
            cloned._ioCard = this._ioCard;
            cloned._diNumber = this._diNumber;
            cloned.ProcessId = this.ProcessId;
            cloned.CurrentStatus = GeneralStatus.Wait;
            cloned.Message = "Wait";
            return cloned;
        }
    }
}
