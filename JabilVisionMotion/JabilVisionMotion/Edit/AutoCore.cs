using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ToolBlock;
using csLTDMC;
using HslCommunication;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Lib.Parameter;
using Jabil.Module.IO;
using Jabil.Module.IO.Common.Dtos;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Motion;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Permission;
using Jabil.Module.Serialport;
using Jabil.Module.Serialport.Common.Dtos;
using Jabil.Module.Serialport.Lib;
using Jabil.Module.TCPIP;
using Jabil.Module.TCPIP.Common.Dtos;
using Jabil.Module.TCPIP.Lib;
using Jabil.Module.Vision;
using Jabil.Module.Vision.Common.Dtos;
using Jabil.Module.Vision.Lib;
using JabilVisionMotion.Edit;
using JabilVisionMotion.Frm.UserPage;
using JabilVisionMotion.Lib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static JabilVisionMotion.Lib.BuzzerLED;
using lctdevice;
using Jabil.Module.ProductData.Lib;
using Jabil.Module.ProductData.Frm;
using JabilVisionMotion.Frm;
using JabilVisionMotion.Frm.Dialog;
using System.Collections.Concurrent;
using JabilVisionMotion.Edit.Process;
using JabilVisionMotion.Edit.Process.TCPControlProcess;
using JabilVisionMotion.Edit.Process.AxisMotionControlProcess;
using JabilVisionMotion.Edit.Process.IOControlProcess;
using JabilVisionMotion.Edit.Process.ModbusControlProcess;

namespace JabilVisionMotion.Edit
{
    /// <summary>
    /// 自动化流程控制核心类
    /// 实现IDisposable接口以确保资源正确释放，负责解析、执行和管理自动化流程
    /// 支持并行任务处理、流程重复执行、紧急停止等核心功能
    /// </summary>
    public class AutoCore : IDisposable
    {
        #region 常量定义
        /// <summary>流程日志分隔符，用于在日志中区分不同的流程段</summary>
        private const string LogSeparator = "--->>--->>--->>-process split line--->>--->>--->>-";

        /// <summary>流程开始状态文本，用于日志记录流程启动事件</summary>
        private const string StartStatusText = " 状态:--Start";

        /// <summary>注释前缀，用于识别流程中的注释指令（将被跳过执行）</summary>
        private const string CommentPrefix = "//";

        /// <summary>错误指令日志文本，用于在日志中标记无效或未识别的指令</summary>
        private const string ErrorInstructionText = "--报错指令：";

        /// <summary>跳过指令日志文本，用于在日志中标记被忽略的注释指令</summary>
        private const string SkipInstructionText = "--不执行指令：";

        /// <summary>紧急停止日志文本，用于记录设备紧急停止事件</summary>
        private const string EmergencyStopText = "接收到来自机台的紧急停止信号！！！！ ";

        /// <summary>工位二指令错误文本，用于标记包含"工位二"的非法指令（业务限制）</summary>
        private const string StationTwoErrorText = "机台紧急停止，原因是因为运行流程中存在工位二的指令！！！ ";

        /// <summary>安全检查间隔（毫秒），定期检查设备状态确保操作安全</summary>
        private const int SecurityCheckIntervalMs = 10;

        /// <summary>任务清理间隔（毫秒），定期清理已完成的并行任务</summary>
        private const int TaskCleanupIntervalMs = 20;

        /// <summary>单个任务超时时间（毫秒），防止任务无限期阻塞</summary>
        private const int TaskTimeoutMs = 5000;
        #endregion

        #region 状态与配置
        /// <summary>
        /// 当前的运行状态
        /// 取值范围：Wait(等待)、Running(运行中)、NoError(完成无错误)、Error(错误)
        /// 提供外部访问接口，用于监控流程执行状态
        /// </summary>
        public GeneralStatus AutoRunStatus { get; private set; } = GeneralStatus.Wait;

        /// <summary>
        /// 是否重复执行流程的标志
        /// 由流程中的"Repeat"指令控制，为true时流程会循环执行
        /// </summary>
        private bool _isRepeat;

        /// <summary>
        /// 流程描述信息
        /// 用于日志记录的上下文标识，帮助区分不同的流程实例
        /// </summary>
        private readonly string _processTip;
        #endregion

        #region 流程数据
        /// <summary>
        /// 原始流程数据
        /// 存储从CSV文件加载的未解析原始数据，每行代表一组流程指令
        /// </summary>
        private readonly List<List<object>> _rawProcesses;

        /// <summary>
        /// 可执行流程对象列表
        /// 存储解析后的流程对象，按行组织，每行包含多个可并行执行的流程指令
        /// </summary>
        private readonly List<List<ProcessBase>> _executableProcesses = new List<List<ProcessBase>>();
        #endregion

        #region 线程与任务管理
        /// <summary>
        /// 轻量级任务同步信号量
        /// 用于保护对共享资源（如_runningTasks）的并发访问，替代传统lock以提高性能
        /// </summary>
        private readonly SemaphoreSlim _taskSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 当前运行的任务列表
        /// 跟踪正在执行的并行流程任务，用于等待所有任务完成和清理操作
        /// </summary>
        private readonly List<Task> _runningTasks = new List<Task>();

        /// <summary>
        /// 取消令牌源
        /// 用于发送取消信号，终止流程执行（如紧急停止、用户取消等场景）
        /// </summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// 总执行时间计时器
        /// 记录整个流程集的运行时间，用于性能监控和日志记录
        /// </summary>
        private readonly Stopwatch _totalStopwatch = new Stopwatch();

        /// <summary>
        /// 单流程执行计时器
        /// 记录单行流程的运行时间，用于单个步骤的性能监控
        /// </summary>
        private readonly Stopwatch _singleProcessStopwatch = new Stopwatch();

        /// <summary>
        /// 资源释放标志
        /// 用于IDisposable模式，确保资源只被释放一次
        /// </summary>
        private bool _disposed = false;
        #endregion

        #region 流程类型映射
        /// <summary>
        /// 流程类型创建器映射表
        /// 键：指令类型字符串（如"ABSMove"、"IO"）
        /// 值：创建对应流程对象的委托函数
        /// </summary>
        private readonly Dictionary<string, Func<string, string, ProcessBase>> _processCreators;
        #endregion

        /// <summary>
        /// 初始化流程控制类
        /// </summary>
        /// <param name="processPath">流程文件（CSV）的路径</param>
        /// <param name="processTip">流程描述信息</param>
        /// <exception cref="ArgumentNullException">当processTip为null时抛出</exception>
        public AutoCore(string processPath, string processTip)
        {
            _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));
            _processCreators = InitializeProcessCreators();

            try
            {
                _rawProcesses = Project.Instance.GetCsvData(processPath, true) ?? new List<List<object>>();
                ParseProcesses();
            }
            catch (Exception ex)
            {
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 初始化流程类型创建器映射表
        /// </summary>
        private Dictionary<string, Func<string, string, ProcessBase>> InitializeProcessCreators()
        {
            return new Dictionary<string, Func<string, string, ProcessBase>>
        {
            { "Repeat", (data, tip) => { _isRepeat = true; return null; } },
            { "ProcessJump", (data, tip) => new ProcessJump(data, tip) },
            { "ProcessRun", (data, tip) => new ProcessRun(data, tip) },
        };
        }

        /// <summary>
        /// 解析原始流程数据为可执行流程对象
        /// </summary>
        private void ParseProcesses()
        {
            foreach (var processRow in _rawProcesses)
            {
                var executableRow = new List<ProcessBase>();

                for (int colIndex = 1; colIndex < processRow.Count; colIndex++)
                {
                    var processData = processRow[colIndex]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(processData)) continue;

                    if (processData.IndexOf("工位二", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        HandleStationTwoError(processData);
                        return;
                    }

                    var processKey = processData.Split('|')[0];
                    HandleProcessType(processKey, processData, executableRow);
                }

                _executableProcesses.Add(executableRow);
            }
        }

        /// <summary>
        /// 处理包含工位二的错误指令
        /// </summary>
        private void HandleStationTwoError(string processData)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                processData,
                StationTwoErrorText
            );
        }

        /// <summary>
        /// 根据流程类型处理并创建对应流程对象
        /// </summary>
        private void HandleProcessType(string processKey, string processData, List<ProcessBase> executableRow)
        {
            if (string.Equals(processKey, "Repeat", StringComparison.OrdinalIgnoreCase))
            {
                _isRepeat = true;
                return;
            }

            if (_processCreators.TryGetValue(processKey, out var creator))
            {
                executableRow.Add(creator(processData, _processTip));
            }
            else
            {
                HandleUnknownProcess(processKey, processData);
            }
        }

        /// <summary>
        /// 处理未知或注释的流程指令
        /// </summary>
        private void HandleUnknownProcess(string processKey, string processData)
        {
            if (processKey.Contains(CommentPrefix))
            {
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.YellowGreen,
                    "注释",
                    _processTip,
                    $"{SkipInstructionText}{processData}"
                );
            }
            else
            {
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Red,
                    "错误指令",
                    _processTip,
                    $"{ErrorInstructionText}{processData}"
                );
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            }
        }

        /// <summary>
        /// 启动流程执行
        /// </summary>
        public void Run()
        {
            AutoRunStatus = GeneralStatus.Running;
            _ = Task.Run(ExecuteProcessLoop, _cts.Token);
        }

        /// <summary>
        /// 流程执行主循环
        /// </summary>
        private async Task ExecuteProcessLoop()
        {
            try
            {
                do
                {
                    _totalStopwatch.Restart();
                    await ExecuteAllProcessRows();
                    _totalStopwatch.Stop();
                    LogTotalExecutionTime();
                }
                while (_isRepeat && !_cts.IsCancellationRequested);

                if (!_cts.IsCancellationRequested)
                {
                    AutoRunStatus = MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped
                        ? GeneralStatus.Error
                        : GeneralStatus.NoError;
                }
            }
            catch (OperationCanceledException)
            {
                AutoRunStatus = GeneralStatus.Error;
            }
            catch (Exception ex)
            {
                AutoRunStatus = GeneralStatus.Error;
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 执行所有行的流程
        /// </summary>
        private async Task ExecuteAllProcessRows()
        {
            for (int rowIndex = 0; rowIndex < _executableProcesses.Count; rowIndex++)
            {
                if (!await CheckSecurityRestrictions())
                {
                    return;
                }

                var processRow = _executableProcesses[rowIndex];
                if (!await ProcessSingleRow(processRow, rowIndex))
                {
                    rowIndex--;
                    await HandleRowError();
                }
            }
        }

        /// <summary>
        /// 处理单行流程
        /// </summary>
        private async Task<bool> ProcessSingleRow(List<ProcessBase> processRow, int rowIndex)
        {
            _singleProcessStopwatch.Restart();
            bool hasError = false;

            await StartRowTasks(processRow);
            await WaitForRowTasksCompletion();
            hasError = CheckRowExecutionResults(processRow);

            _singleProcessStopwatch.Stop();
            return !hasError;
        }

        /// <summary>
        /// 启动行内所有流程任务（优化：添加任务超时）
        /// </summary>
        private async Task StartRowTasks(List<ProcessBase> processRow)
        {
            await _taskSemaphore.WaitAsync(_cts.Token);
            try
            {
                _runningTasks.Clear();
            }
            finally
            {
                _taskSemaphore.Release();
            }

            foreach (var process in processRow)
            {
                if (_cts.IsCancellationRequested) break;

                LogProcessStart(process);

                // 为每个任务添加独立超时，避免单个任务阻塞整体流程
                using (var taskCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                {
                    taskCts.CancelAfter(TaskTimeoutMs); // 超时自动取消
                    var task = Task.Run(() => ExecuteProcess(process), taskCts.Token)
                        .ContinueWith(t =>
                        {
                            // 超时/取消时标记流程失败
                            if (t.IsCanceled) process.Fail("任务超时或被取消");
                            else if (t.IsFaulted) process.Fail($"任务异常: {t.Exception?.InnerException?.Message}");
                        }, TaskContinuationOptions.ExecuteSynchronously);

                    await _taskSemaphore.WaitAsync(_cts.Token);
                    try
                    {
                        _runningTasks.Add(task);
                    }
                    finally
                    {
                        _taskSemaphore.Release();
                    }
                }
            }
        }

        /// <summary>
        /// 执行单个流程（优化：确保资源释放）
        /// </summary>
        private void ExecuteProcess(ProcessBase process)
        {
            // 若ProcessBase实现了IDisposable，确保任务完成后释放资源
            using (process as IDisposable)
            {
                try
                {
                    if (_cts.IsCancellationRequested)
                    {
                        process.Fail("流程已被取消");
                        return;
                    }

                    process.Run();
                }
                catch (Exception ex)
                {
                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Red,
                        "流程异常",
                        process.ProcessName,
                        $"异常类型: {ex.GetType().Name}, 消息: {ex.Message}"
                    );
                    process.CurrentStatus = GeneralStatus.Error;
                }
            }
        }

        /// <summary>
        /// 等待行内所有任务完成（核心优化：减少内存占用）
        /// </summary>
        private async Task WaitForRowTasksCompletion()
        {
            while (true)
            {
                // 1. 优先响应取消信号（补充第一个版本的优点）
                if (_cts.IsCancellationRequested)
                    return;

                // 2. 检查安全限制
                if (!await CheckSecurityRestrictions())
                    return;

                List<Task> tasksCopy = null;
                await _taskSemaphore.WaitAsync(_cts.Token);
                try
                {
                    if (_runningTasks.Count == 0)
                        break;
                    // 先提前清理一次已完成任务，减少后续等待的任务量（关键优化）
                    _runningTasks.RemoveAll(t => t.IsCompleted);
                    if (_runningTasks.Count == 0)
                        break;
                    tasksCopy = new List<Task>(_runningTasks);
                }
                finally
                {
                    _taskSemaphore.Release();
                }

                // 3. 等待任一任务完成（而非等待所有或超时），兼顾及时响应和减少锁竞争
                await Task.WhenAny(tasksCopy);

                // 4. 任务完成后立即清理（避免堆积）
                await _taskSemaphore.WaitAsync(_cts.Token);
                try
                {
                    _runningTasks.RemoveAll(t => t.IsCompleted);
                }
                finally
                {
                    _taskSemaphore.Release();
                }
            }
        }
        /// <summary>
        /// 检查行执行结果
        /// </summary>
        private bool CheckRowExecutionResults(List<ProcessBase> processRow)
        {
            if (_cts.IsCancellationRequested)
            {
                return true;
            }

            bool hasError = true;
            int completedCount = 0;
            if (processRow.Count == 0)
            {
                return false;
            }

            foreach (var process in processRow)
            {
                switch (process.CurrentStatus)
                {
                    case GeneralStatus.NoError:
                        hasError = false;
                        completedCount++;
                        LogProcessCompletion(process);
                        break;
                    case GeneralStatus.Error:
                        hasError = true;
                        completedCount++;
                        LogProcessError(process);
                        return hasError;
                }
            }

            if (completedCount >= processRow.Count)
            {
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Black,
                    "流程分隔符",
                    LogSeparator,
                    ""
                );
            }

            return hasError;
        }

        /// <summary>
        /// 处理行执行错误
        /// </summary>
        private async Task HandleRowError()
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                Cancel();
                return;
            }

            MenuProxy.Instance.MachineRunStatus = MachineRunStatus.Alarming;
            StatusUpdater.TriggerWarning(WarningColor.Orange);
            await Task.Delay(100, _cts.Token);
        }

        /// <summary>
        /// 检查安全限制
        /// </summary>
        private async Task<bool> CheckSecurityRestrictions()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                return await HandleEmergencyStop();
            }

            if (_cts.IsCancellationRequested)
            {
                return false;
            }

            while (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming
                   || MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Paused)
            {
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                {
                    return await HandleEmergencyStop();
                }

                if (_cts.IsCancellationRequested)
                {
                    return false;
                }

                await Task.Delay(SecurityCheckIntervalMs, _cts.Token);
            }

            while (MenuProxy.Instance.MachineRunModel == MachineMode.Manual
                   && MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Running)
            {
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                {
                    return await HandleEmergencyStop();
                }

                if (_cts.IsCancellationRequested)
                {
                    return false;
                }

                await Task.Delay(SecurityCheckIntervalMs, _cts.Token);
            }

            return true;
        }

        /// <summary>
        /// 处理紧急停止
        /// </summary>
        private async Task<bool> HandleEmergencyStop()
        {
            AutoRunStatus = GeneralStatus.Error;
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                EmergencyStopText,
                ""
            );
            _cts.Cancel();
            await Task.CompletedTask;
            return false;
        }

        /// <summary>
        /// 记录流程开始日志
        /// </summary>
        private void LogProcessStart(ProcessBase process)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Black,
                process.ProcessId,
                process.ProcessName,
                StartStatusText
            );
        }

        /// <summary>
        /// 记录流程完成日志
        /// </summary>
        private void LogProcessCompletion(ProcessBase process)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Black,
                process.ProcessId,
                process.ProcessName,
                $" 状态:--{process.Message}--单流程作业耗时：{(_singleProcessStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 记录流程错误日志
        /// </summary>
        private void LogProcessError(ProcessBase process)
        {
            Project.Instance.GlobalVariables.AlarmMessage = $"{process.ProcessName} ,状态:--{process.Message}";
            Project.Instance.GlobalVariables.AlarmId = process.ProcessId;

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.IndianRed,
                process.ProcessId,
                process.ProcessName,
                $"--报警耗时：{(_singleProcessStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 记录总执行时间日志
        /// </summary>
        private void LogTotalExecutionTime()
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.AliceBlue,
                "流程集耗时",
                _processTip,
                $"  耗时 ：  {(_totalStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 取消所有正在执行的任务
        /// </summary>
        public void Cancel()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                AutoRunStatus = GeneralStatus.Error;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 资源释放的实际实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _taskSemaphore?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~AutoCore()
        {
            Dispose(false);
        }
    }

    public class AutoCore_手动 : IDisposable
    {
        #region 常量定义
        /// <summary>流程日志分隔符，用于区分不同流程段</summary>
        private const string LogSeparator = "--->>--->>--->>-process split line--->>--->>--->>-";
        /// <summary>流程开始状态文本，用于日志记录</summary>
        private const string StartStatusText = " 状态:--Start";
        /// <summary>注释前缀，用于识别流程中的注释指令</summary>
        private const string CommentPrefix = "//";
        /// <summary>错误指令日志文本，用于标记无效指令</summary>
        private const string ErrorInstructionText = "--报错指令：";
        /// <summary>跳过指令日志文本，用于标记注释或忽略的指令</summary>
        private const string SkipInstructionText = "--不执行指令：";
        /// <summary>紧急停止日志文本，用于记录紧急停止事件</summary>
        private const string EmergencyStopText = "接收到来自机台的紧急停止信号！！！！ ";
        /// <summary>工位二指令错误文本，用于标记包含工位二的非法指令</summary>
        private const string StationTwoErrorText = "机台紧急停止，原因是因为运行流程中存在工位二的指令！！！ ";
        /// <summary>安全检查间隔（毫秒）</summary>
        private const int SecurityCheckIntervalMs = 10;
        /// <summary>任务清理间隔（毫秒）</summary>
        private const int TaskCleanupIntervalMs = 20;
        #endregion

        #region 状态与配置
        /// <summary>当前的运行状态（等待/运行中/无错误/错误）</summary>
        public GeneralStatus AutoRunStatus { get; private set; } = GeneralStatus.Wait;
        /// <summary>是否重复执行流程（由流程中的Repeat指令控制）</summary>
        private bool _isRepeat;
        /// <summary>流程描述信息，用于日志记录的上下文标识</summary>
        private readonly string _processTip;
        #endregion

        #region 流程数据
        /// <summary>原始流程数据（从CSV加载的未解析数据）</summary>
        private readonly List<List<object>> _rawProcesses;
        /// <summary>可执行流程对象列表（解析后的流程，按行组织，每行包含多个并行执行的流程）</summary>
        private readonly List<List<ProcessBase>> _executableProcesses = new List<List<ProcessBase>>();
        #endregion

        #region 线程与任务管理
        /// <summary>轻量级任务同步信号量，替代lock提高性能</summary>
        private readonly SemaphoreSlim _taskSemaphore = new SemaphoreSlim(1, 1);
        /// <summary>当前运行的任务列表，用于跟踪并行执行的流程</summary>
        private readonly List<Task> _runningTasks = new List<Task>();
        /// <summary>取消令牌源，用于终止流程执行（如紧急停止时）</summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        /// <summary>总执行时间计时器，记录整个流程集的运行时间</summary>
        private readonly Stopwatch _totalStopwatch = new Stopwatch();
        /// <summary>单流程执行计时器，记录单行流程的运行时间</summary>
        private readonly Stopwatch _singleProcessStopwatch = new Stopwatch();
        /// <summary>是否已释放资源</summary>
        private bool _disposed = false;
        #endregion

        #region 流程类型映射
        /// <summary>
        /// 流程类型创建器映射表
        /// 键：指令类型（如"ABSMove"、"IO"）；值：创建对应流程对象的委托
        /// 用于替代冗长的switch语句，提高扩展性
        /// </summary>
        private readonly Dictionary<string, Func<string, string, ProcessBase>> _processCreators;
        #endregion

        /// <summary>
        /// 初始化模式流程控制类
        /// </summary>
        /// <param name="processPath">流程文件（CSV）的路径</param>
        /// <param name="processTip">流程描述信息（用于日志上下文）</param>
        public AutoCore_手动(string processPath, string processTip)
        {
            _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));
            // 初始化流程类型映射表（支持的指令类型）
            _processCreators = InitializeProcessCreators();

            try
            {
                // 从CSV加载原始流程数据（若加载失败则使用空列表）
                _rawProcesses = Project.Instance.GetCsvData(processPath, true) ?? new List<List<object>>();
                // 解析原始数据为可执行的流程对象
                ParseProcesses();
            }
            catch (Exception ex)
            {
                // 初始化失败时触发紧急停止，并记录错误
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 初始化流程类型创建器映射表
        /// 注册模式支持的所有流程指令及其对应的创建方法
        /// </summary>
        private Dictionary<string, Func<string, string, ProcessBase>> InitializeProcessCreators()
        {
            return new Dictionary<string, Func<string, string, ProcessBase>>
        {
            { "ABSMove", (data, tip) => new ABSMoveProcess(data, tip) },
            { "ABSMoveJump", (data, tip) => new ABSMoveJumpProcess(data, tip) },
            { "ABSMoveNoWait", (data, tip) => new ABSMoveNoWaitProcess(data, tip) },
            { "CheckMoveDone", (data, tip) => new CheckMoveDoneProcess(data, tip) },
            { "ELPMoveNoWait", (data, tip) => new ELPMoveNoWaitProcess(data, tip) },
            { "CheckELPDone", (data, tip) => new CheckELPDoneProcess(data, tip) },
            { "RELMove", (data, tip) => new RELMoveProcess(data, tip) },
            { "HomeMove", (data, tip) => new AxisHomeProcess(data, tip) },
            { "Delay", (data, tip) => new TimeDelay(data, tip) },
            { "IO", (data, tip) => new IOProcess(data, tip) },
            { "IOTime", (data, tip) => new IOTimeProcess(data, tip) },
            { "ELNMove", (data, tip) => new ELNMoveProcess(data, tip) },
            { "ELPMove", (data, tip) => new ELPMoveProcess(data, tip) },
            { "ProcessRun", (data, tip) => new ProcessRun(data, tip) },
            { "StateJudge", (data, tip) => new StateJudge(data, tip) },
            { "Vision", (data, tip) => new VisionProcess(data, tip) },
            { "TCPServer", (data, tip) => new TCPServerCommunicationProcess(data, tip) },
            { "TCPClient", (data, tip) => new TCPClientCommunicationProcess(data, tip) },
            { "ModbusPLC", (data, tip) => new ModbusPLCCommunicationProcess(data, tip) },
            { "ModbusRobot", (data, tip) => new ModbusRobotCommunicationProcess(data, tip) },
            { "临时变量", (data, tip) => new TemporaryVariablesProcess(data, tip) },
            { "Serialport", (data, tip) => new SerialportProcess(data, tip) } // 副流程特有：串口通信指令
        };
        }

        /// <summary>
        /// 解析原始流程数据为可执行流程对象
        /// 扩展基础解析逻辑，增加工位二指令检查，支持更多设备控制指令
        /// </summary>
        private void ParseProcesses()
        {
            foreach (var processRow in _rawProcesses)
            {
                var executableRow = new List<ProcessBase>();

                // 从第二列开始解析（第一列通常是流程名称）
                for (int colIndex = 1; colIndex < processRow.Count; colIndex++)
                {
                    var processData = processRow[colIndex]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(processData)) continue;

                    // 模式禁止执行包含"工位二"的指令（业务限制）
                    if (processData.IndexOf("工位二", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        HandleStationTwoError(processData);
                        return;
                    }

                    // 提取指令类型（按"|"分割后的第一个部分）
                    var processKey = processData.Split('|')[0];
                    // 根据指令类型创建流程对象
                    HandleProcessType(processKey, processData, executableRow);
                }

                _executableProcesses.Add(executableRow);
            }
        }

        /// <summary>
        /// 处理包含工位二的错误指令
        /// 触发紧急停止并记录错误日志（模式特有限制）
        /// </summary>
        /// <param name="processData">包含工位二的指令内容</param>
        private void HandleStationTwoError(string processData)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                processData,
                StationTwoErrorText
            );
        }

        /// <summary>
        /// 根据流程类型处理并创建对应流程对象
        /// 利用映射表创建流程对象，简化逻辑并提高扩展性
        /// </summary>
        /// <param name="processKey">指令类型标识</param>
        /// <param name="processData">完整指令内容</param>
        /// <param name="executableRow">当前行的可执行流程列表（添加结果）</param>
        private void HandleProcessType(string processKey, string processData, List<ProcessBase> executableRow)
        {
            if (string.Equals(processKey, "Repeat", StringComparison.OrdinalIgnoreCase))
            {
                // 标记流程需要重复执行
                _isRepeat = true;
                return;
            }

            // 从映射表中获取对应的流程创建器
            if (_processCreators.TryGetValue(processKey, out var creator))
            {
                // 创建流程对象并添加到行列表
                executableRow.Add(creator(processData, _processTip));
            }
            else
            {
                // 处理未知指令
                HandleUnknownProcess(processKey, processData);
            }
        }

        /// <summary>
        /// 处理未知或注释的流程指令
        /// 与基础类逻辑一致，但针对模式指令进行适配
        /// </summary>
        /// <param name="processKey">指令类型标识</param>
        /// <param name="processData">完整指令内容</param>
        private void HandleUnknownProcess(string processKey, string processData)
        {
            if (processKey.Contains(CommentPrefix))
            {
                // 注释指令：记录跳过日志
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.YellowGreen,
                    "注释",
                    _processTip,
                    $"{SkipInstructionText}{processData}"
                );
            }
            else
            {
                // 未知指令：记录错误日志并触发紧急停止
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Red,
                    "错误指令",
                    _processTip,
                    $"{ErrorInstructionText}{processData}"
                );
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            }
        }

        /// <summary>
        /// 启动模式流程执行
        /// 逻辑与基础类一致，适用于操作场景
        /// </summary>
        public void Run()
        {
            AutoRunStatus = GeneralStatus.Running;
            // 使用Task.Run而非直接调用，避免长时间操作阻塞UI线程
            _ = Task.Run(ExecuteProcessLoop, _cts.Token);
        }

        /// <summary>
        /// 模式流程执行主循环
        /// 继承基础循环逻辑，针对模式优化
        /// </summary>
        private async Task ExecuteProcessLoop()
        {
            try
            {
                do
                {
                    _totalStopwatch.Restart();
                    await ExecuteAllProcessRows();
                    _totalStopwatch.Stop();

                    LogTotalExecutionTime();
                }
                while (_isRepeat && !_cts.IsCancellationRequested);

                if (!_cts.IsCancellationRequested)
                {
                    AutoRunStatus = MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped
                        ? GeneralStatus.Error
                        : GeneralStatus.NoError;
                }
            }
            catch (OperationCanceledException)
            {
                // 预期的取消操作，不记录错误
                AutoRunStatus = GeneralStatus.Error;
            }
            catch (Exception ex)
            {
                AutoRunStatus = GeneralStatus.Error;
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 执行所有行的流程
        /// 与基础类逻辑一致，支持模式下的并行流程执行
        /// </summary>
        private async Task ExecuteAllProcessRows()
        {
            for (int rowIndex = 0; rowIndex < _executableProcesses.Count; rowIndex++)
            {
                if (!await CheckSecurityRestrictions())
                {
                    return;
                }

                var processRow = _executableProcesses[rowIndex];
                if (!await ProcessSingleRow(processRow, rowIndex))
                {
                    rowIndex--; // 重试当前行
                    await HandleRowError();
                }
            }
        }

        /// <summary>
        /// 处理单行流程
        /// 与基础类逻辑一致，支持模式下的并行任务管理
        /// </summary>
        private async Task<bool> ProcessSingleRow(List<ProcessBase> processRow, int rowIndex)
        {
            _singleProcessStopwatch.Restart();
            bool hasError = false;

            await StartRowTasks(processRow);
            await WaitForRowTasksCompletion();
            hasError = CheckRowExecutionResults(processRow);

            _singleProcessStopwatch.Stop();
            return !hasError;
        }

        /// <summary>
        /// 启动行内所有流程任务
        /// 优化：使用SemaphoreSlim替代lock，减少线程阻塞
        /// </summary>
        private async Task StartRowTasks(List<ProcessBase> processRow)
        {
            await _taskSemaphore.WaitAsync(_cts.Token);
            try
            {
                _runningTasks.Clear();
            }
            finally
            {
                _taskSemaphore.Release();
            }

            foreach (var process in processRow)
            {
                if (_cts.IsCancellationRequested) break;

                LogProcessStart(process);
                var task = Task.Run(() => ExecuteProcess(process), _cts.Token);

                await _taskSemaphore.WaitAsync(_cts.Token);
                try
                {
                    _runningTasks.Add(task);
                }
                finally
                {
                    _taskSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// 执行单个流程
        /// 增加详细异常捕获，提高错误可追溯性
        /// </summary>
        private void ExecuteProcess(ProcessBase process)
        {
            try
            {
                if (_cts.IsCancellationRequested)
                {
                    process.Fail("流程已被取消");
                    return;
                }

                process.Run();
            }
            catch (Exception ex)
            {
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Red,
                    "流程异常",
                    process.ProcessName,
                    $"异常类型: {ex.GetType().Name}, 消息: {ex.Message}, 堆栈: {ex.StackTrace}"
                );
                process.CurrentStatus = GeneralStatus.Error;
            }
        }

        /// <summary>
        /// 等待行内所有任务完成
        /// 优化：定期清理已完成任务，减少列表操作开销
        /// </summary>
        private async Task WaitForRowTasksCompletion()
        {
            while (true)
            {
                // 1. 优先响应取消信号（补充第一个版本的优点）
                if (_cts.IsCancellationRequested)
                    return;

                // 2. 检查安全限制
                if (!await CheckSecurityRestrictions())
                    return;

                List<Task> tasksCopy = null;
                await _taskSemaphore.WaitAsync(_cts.Token);
                try
                {
                    if (_runningTasks.Count == 0)
                        break;
                    // 先提前清理一次已完成任务，减少后续等待的任务量（关键优化）
                    _runningTasks.RemoveAll(t => t.IsCompleted);
                    if (_runningTasks.Count == 0)
                        break;
                    tasksCopy = new List<Task>(_runningTasks);
                }
                finally
                {
                    _taskSemaphore.Release();
                }

                // 3. 等待任一任务完成（而非等待所有或超时），兼顾及时响应和减少锁竞争
                await Task.WhenAny(tasksCopy);

                // 4. 任务完成后立即清理（避免堆积）
                await _taskSemaphore.WaitAsync(_cts.Token);
                try
                {
                    _runningTasks.RemoveAll(t => t.IsCompleted);
                }
                finally
                {
                    _taskSemaphore.Release();
                }
            }
        }
        /// <summary>
        /// 检查行执行结果
        /// 优化：增加对取消状态的检查
        /// </summary>
        private bool CheckRowExecutionResults(List<ProcessBase> processRow)
        {
            if (_cts.IsCancellationRequested)
            {
                return true; // 取消状态视为有错误
            }

            bool hasError = true;
            int completedCount = 0;
            if (processRow.Count == 0)
            {
                return false;
            }

            foreach (var process in processRow)
            {
                switch (process.CurrentStatus)
                {
                    case GeneralStatus.NoError:
                        hasError = false;
                        completedCount++;
                        LogProcessCompletion(process);
                        break;
                    case GeneralStatus.Error:
                        hasError = true;
                        completedCount++;
                        LogProcessError(process);
                        return hasError;
                }
            }

            if (completedCount >= processRow.Count)
            {
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Black,
                    "流程分隔符",
                    LogSeparator,
                    ""
                );
            }

            return hasError;
        }

        /// <summary>
        /// 处理行执行错误
        /// 优化：增加对取消状态的响应
        /// </summary>
        private async Task HandleRowError()
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                Cancel();
                return;
            }

            MenuProxy.Instance.MachineRunStatus = MachineRunStatus.Alarming;
            StatusUpdater.TriggerWarning(WarningColor.Orange);
            await Task.Delay(100, _cts.Token);
        }

        /// <summary>
        /// 检查安全限制（模式特有）
        /// 扩展基础安全检查，增加联机模式的特殊限制
        /// </summary>
        private async Task<bool> CheckSecurityRestrictions()
        {
            // 紧急停止检查
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                return await HandleEmergencyStop();
            }

            // 检查是否已取消
            if (_cts.IsCancellationRequested)
            {
                return false;
            }

            // 等待报警或暂停状态解除
            while (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming
                   || MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Paused)
            {
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                {
                    return await HandleEmergencyStop();
                }

                if (_cts.IsCancellationRequested)
                {
                    return false;
                }

                await Task.Delay(SecurityCheckIntervalMs, _cts.Token);
            }

            // 模式特有：联机运行时的额外限制
            while (MenuProxy.Instance.MachineRunModel == MachineMode.Manual
                   && MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Homing)
            {
                // 联机模式下若触发紧急停止，直接终止
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                {
                    return await HandleEmergencyStop();
                }

                if (_cts.IsCancellationRequested)
                {
                    return false;
                }

                await Task.Delay(SecurityCheckIntervalMs, _cts.Token);
            }

            return true;
        }

        /// <summary>
        /// 处理紧急停止（模式适配）
        /// 封装紧急停止逻辑，避免代码重复
        /// </summary>
        private async Task<bool> HandleEmergencyStop()
        {
            AutoRunStatus = GeneralStatus.Error;
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                EmergencyStopText,
                ""
            );
            _cts.Cancel();
            await Task.CompletedTask;
            return false;
        }

        /// <summary>
        /// 记录流程开始日志（模式适配）
        /// </summary>
        private void LogProcessStart(ProcessBase process)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Black,
                process.ProcessId,
                process.ProcessName,
                StartStatusText
            );
        }

        /// <summary>
        /// 记录流程完成日志（模式适配）
        /// </summary>
        private void LogProcessCompletion(ProcessBase process)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Black,
                process.ProcessId,
                process.ProcessName,
                $" 状态:--{process.Message}--单流程作业耗时：{(_singleProcessStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 记录流程错误日志（模式适配）
        /// </summary>
        private void LogProcessError(ProcessBase process)
        {
            Project.Instance.GlobalVariables.AlarmMessage = $"{process.ProcessName} ,状态:--{process.Message}";
            Project.Instance.GlobalVariables.AlarmId = process.ProcessId;

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.IndianRed,
                process.ProcessId,
                process.ProcessName,
                $"--报警耗时：{(_singleProcessStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 记录总执行时间日志（模式适配）
        /// </summary>
        private void LogTotalExecutionTime()
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.AliceBlue,
                "流程集耗时",
                _processTip,
                $"  耗时 ：  {(_totalStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 取消所有正在执行的任务（模式适配）
        /// </summary>
        public void Cancel()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                AutoRunStatus = GeneralStatus.Error;
            }
        }

        /// <summary>
        /// 释放资源，避免内存泄漏
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 资源释放的实际实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 释放托管资源
                _cts?.Cancel();
                _cts?.Dispose();
                _taskSemaphore?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// 析构函数，确保资源释放
        /// </summary>
        ~AutoCore_手动()
        {
            Dispose(false);
        }
    }

    public class AutoCore_副流程 : IDisposable
    {
        #region 常量定义
        /// <summary>流程日志分隔符，用于区分不同流程段</summary>
        private const string LogSeparator = "--->>--->>--->>-process split line--->>--->>--->>-";
        /// <summary>流程开始状态文本，用于日志记录</summary>
        private const string StartStatusText = " 状态:--Start";
        /// <summary>注释前缀，用于识别流程中的注释指令</summary>
        private const string CommentPrefix = "//";
        /// <summary>错误指令日志文本，用于标记无效指令</summary>
        private const string ErrorInstructionText = "--报错指令：";
        /// <summary>跳过指令日志文本，用于标记注释或忽略的指令</summary>
        private const string SkipInstructionText = "--不执行指令：";
        /// <summary>紧急停止日志文本，用于记录紧急停止事件</summary>
        private const string EmergencyStopText = "接收到来自机台的紧急停止信号！！！！ ";
        /// <summary>工位二指令错误文本，用于标记包含工位二的非法指令</summary>
        private const string StationTwoErrorText = "机台紧急停止，原因是因为运行流程中存在工位二的指令！！！ ";
        /// <summary>安全检查间隔（毫秒）</summary>
        private const int SecurityCheckIntervalMs = 10;
        /// <summary>任务清理间隔（毫秒）</summary>
        private const int TaskCleanupIntervalMs = 20;
        /// <summary>单个任务超时时间（毫秒），防止任务无限期阻塞</summary>
        private const int TaskTimeoutMs = 5000;
        #endregion

        #region 状态与配置
        /// <summary>当前的运行状态（等待/运行中/无错误/错误）</summary>
        public GeneralStatus AutoRunStatus { get; private set; } = GeneralStatus.Wait;
        /// <summary>是否重复执行流程（由流程中的Repeat指令控制）</summary>
        private bool _isRepeat;
        /// <summary>流程描述信息，用于日志记录的上下文标识</summary>
        private readonly string _processTip;
        #endregion

        #region 流程数据
        /// <summary>原始流程数据（从CSV加载的未解析数据）</summary>
        private readonly List<List<object>> _rawProcesses;
        /// <summary>可执行流程对象列表（解析后的流程，按行组织，每行包含多个并行执行的流程）</summary>
        private readonly List<List<ProcessBase>> _executableProcesses = new List<List<ProcessBase>>();
        #endregion

        #region 线程与任务管理
        /// <summary>轻量级任务同步信号量，替代lock提高性能</summary>
        private readonly SemaphoreSlim _taskSemaphore = new SemaphoreSlim(1, 1);
        /// <summary>当前运行的任务列表，用于跟踪并行执行的流程</summary>
        private readonly List<Task> _runningTasks = new List<Task>();
        /// <summary>取消令牌源，用于终止流程执行（如紧急停止时）</summary>
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        /// <summary>总执行时间计时器，记录整个流程集的运行时间</summary>
        private readonly Stopwatch _totalStopwatch = new Stopwatch();
        /// <summary>单流程执行计时器，记录单行流程的运行时间</summary>
        private readonly Stopwatch _singleProcessStopwatch = new Stopwatch();
        /// <summary>是否已释放资源</summary>
        private bool _disposed = false;
        #endregion

        #region 流程类型映射
        /// <summary>
        /// 流程类型创建器映射表
        /// 键：指令类型（如"ABSMove"、"IO"）；值：创建对应流程对象的委托
        /// 用于替代冗长的switch语句，提高扩展性
        /// </summary>
        private readonly Dictionary<string, Func<string, string, ProcessBase>> _processCreators;
        #endregion

        /// <summary>
        /// 初始化模式流程控制类
        /// </summary>
        /// <param name="processPath">流程文件（CSV）的路径</param>
        /// <param name="processTip">流程描述信息（用于日志上下文）</param>
        public AutoCore_副流程(string processPath, string processTip)
        {
            _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));
            _processCreators = InitializeProcessCreators();

            try
            {
                _rawProcesses = Project.Instance.GetCsvData(processPath, true) ?? new List<List<object>>();
                ParseProcesses();
            }
            catch (Exception ex)
            {
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 初始化流程类型创建器映射表
        /// 注册模式支持的所有流程指令及其对应的创建方法
        /// </summary>
        private Dictionary<string, Func<string, string, ProcessBase>> InitializeProcessCreators()
        {
            return new Dictionary<string, Func<string, string, ProcessBase>>
        {
            { "ABSMove", (data, tip) => new ABSMoveProcess(data, tip) },
            { "ABSMoveJump", (data, tip) => new ABSMoveJumpProcess (data, tip) },
            { "ABSMoveNoWait", (data, tip) => new ABSMoveNoWaitProcess(data, tip) },
            { "CheckMoveDone", (data, tip) => new CheckMoveDoneProcess(data, tip) },
            { "ELPMoveNoWait", (data, tip) => new ELPMoveNoWaitProcess(data, tip) },
            { "CheckELPDone", (data, tip) => new CheckELPDoneProcess(data, tip) },
            { "RELMove", (data, tip) => new RELMoveProcess(data, tip) },
            { "HomeMove", (data, tip) => new AxisHomeProcess(data, tip) },
            { "Delay", (data, tip) => new TimeDelay(data, tip) },
            { "IO", (data, tip) => new IOProcess(data, tip) },
            { "IOTime", (data, tip) => new IOTimeProcess(data, tip) },
            { "ELNMove", (data, tip) => new ELNMoveProcess(data, tip) },
            { "ELPMove", (data, tip) => new ELPMoveProcess(data, tip) },
            { "ProcessRun", (data, tip) => new ProcessRun(data, tip) },
            { "StateJudge", (data, tip) => new StateJudge(data, tip) },
            { "Vision", (data, tip) => new VisionProcess(data, tip) },
            { "TCPServer", (data, tip) => new TCPServerCommunicationProcess(data, tip) },
            { "TCPClient", (data, tip) => new TCPClientCommunicationProcess(data, tip) },
            { "ModbusPLC", (data, tip) => new ModbusPLCCommunicationProcess(data, tip) },
            { "ModbusRobot", (data, tip) => new ModbusRobotCommunicationProcess(data, tip) },
            { "临时变量", (data, tip) => new TemporaryVariablesProcess(data, tip) },
            { "Serialport", (data, tip) => new SerialportProcess(data, tip) } // 副流程特有：串口通信指令
        };
        }

        /// <summary>
        /// 解析原始流程数据为可执行流程对象
        /// 扩展基础解析逻辑，增加工位二指令检查，支持更多设备控制指令
        /// </summary>
        private void ParseProcesses()
        {
            foreach (var processRow in _rawProcesses)
            {
                var executableRow = new List<ProcessBase>();

                for (int colIndex = 1; colIndex < processRow.Count; colIndex++)
                {
                    var processData = processRow[colIndex]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(processData)) continue;

                    if (processData.IndexOf("工位二", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        HandleStationTwoError(processData);
                        return;
                    }

                    var processKey = processData.Split('|')[0];
                    HandleProcessType(processKey, processData, executableRow);
                }

                _executableProcesses.Add(executableRow);
            }
        }

        /// <summary>
        /// 处理包含工位二的错误指令
        /// 触发紧急停止并记录错误日志（模式特有限制）
        /// </summary>
        private void HandleStationTwoError(string processData)
        {
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                processData,
                StationTwoErrorText
            );
        }

        /// <summary>
        /// 根据流程类型处理并创建对应流程对象
        /// 利用映射表创建流程对象，简化逻辑并提高扩展性
        /// </summary>
        private void HandleProcessType(string processKey, string processData, List<ProcessBase> executableRow)
        {
            if (string.Equals(processKey, "Repeat", StringComparison.OrdinalIgnoreCase))
            {
                _isRepeat = true;
                return;
            }

            if (_processCreators.TryGetValue(processKey, out var creator))
            {
                executableRow.Add(creator(processData, _processTip));
            }
            else
            {
                HandleUnknownProcess(processKey, processData);
            }
        }

        /// <summary>
        /// 处理未知或注释的流程指令
        /// 与基础类逻辑一致，但针对模式指令进行适配
        /// </summary>
        private void HandleUnknownProcess(string processKey, string processData)
        {
            if (processKey.Contains(CommentPrefix))
            {
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.YellowGreen,
                    "注释",
                    _processTip,
                    $"{SkipInstructionText}{processData}"
                );
            }
            else
            {
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Red,
                    "错误指令",
                    _processTip,
                    $"{ErrorInstructionText}{processData}"
                );
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
            }
        }

        /// <summary>
        /// 启动模式流程执行
        /// 逻辑与基础类一致，适用于操作场景
        /// </summary>
        public void Run()
        {
            AutoRunStatus = GeneralStatus.Running;
            _ = Task.Run(ExecuteProcessLoop, _cts.Token);
        }

        /// <summary>
        /// 模式流程执行主循环
        /// 继承基础循环逻辑，针对模式优化
        /// </summary>
        private async Task ExecuteProcessLoop()
        {
            try
            {
                do
                {
                    _totalStopwatch.Restart();
                    await ExecuteAllProcessRows();
                    _totalStopwatch.Stop();

                    LogTotalExecutionTime();
                }
                while (_isRepeat && !_cts.IsCancellationRequested);

                if (!_cts.IsCancellationRequested)
                {
                    AutoRunStatus = MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped
                        ? GeneralStatus.Error
                        : GeneralStatus.NoError;
                }
            }
            catch (OperationCanceledException)
            {
                AutoRunStatus = GeneralStatus.Error;
            }
            catch (Exception ex)
            {
                AutoRunStatus = GeneralStatus.Error;
                Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 执行所有行的流程
        /// 与基础类逻辑一致，支持模式下的并行流程执行
        /// </summary>
        private async Task ExecuteAllProcessRows()
        {
            for (int rowIndex = 0; rowIndex < _executableProcesses.Count; rowIndex++)
            {
                if (!await CheckSecurityRestrictions())
                {
                    return;
                }

                var processRow = _executableProcesses[rowIndex];
                if (!await ProcessSingleRow(processRow, rowIndex))
                {
                    rowIndex--;
                    await HandleRowError();
                }
            }
        }

        /// <summary>
        /// 处理单行流程
        /// 与基础类逻辑一致，支持模式下的并行任务管理
        /// </summary>
        private async Task<bool> ProcessSingleRow(List<ProcessBase> processRow, int rowIndex)
        {
            _singleProcessStopwatch.Restart();
            bool hasError = false;

            await StartRowTasks(processRow);
            await WaitForRowTasksCompletion();
            hasError = CheckRowExecutionResults(processRow);

            _singleProcessStopwatch.Stop();
            return !hasError;
        }

        /// <summary>
        /// 启动行内所有流程任务（优化：添加任务超时控制）
        /// </summary>
        private async Task StartRowTasks(List<ProcessBase> processRow)
        {
            await _taskSemaphore.WaitAsync(_cts.Token);
            try
            {
                _runningTasks.Clear();
            }
            finally
            {
                _taskSemaphore.Release();
            }

            foreach (var process in processRow)
            {
                if (_cts.IsCancellationRequested) break;

                LogProcessStart(process);

                // 为每个任务添加独立超时，避免单个任务阻塞整体流程
                using (var taskCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token))
                {
                    taskCts.CancelAfter(TaskTimeoutMs); // 超时自动取消
                    var task = Task.Run(() => ExecuteProcess(process), taskCts.Token)
                        .ContinueWith(t =>
                        {
                            // 超时/取消时标记流程失败，确保状态正确
                            if (t.IsCanceled) process.Fail("任务超时或被取消");
                            else if (t.IsFaulted) process.Fail($"任务异常: {t.Exception?.InnerException?.Message}");
                        }, TaskContinuationOptions.ExecuteSynchronously);

                    await _taskSemaphore.WaitAsync(_cts.Token);
                    try
                    {
                        _runningTasks.Add(task);
                    }
                    finally
                    {
                        _taskSemaphore.Release();
                    }
                }
            }
        }

        /// <summary>
        /// 执行单个流程（优化：确保资源释放）
        /// </summary>
        private void ExecuteProcess(ProcessBase process)
        {
            // 若ProcessBase实现了IDisposable，确保任务完成后释放资源
            using (process as IDisposable)
            {
                try
                {
                    if (_cts.IsCancellationRequested)
                    {
                        process.Fail("流程已被取消");
                        return;
                    }

                    process.Run();
                }
                catch (Exception ex)
                {
                    ProcessLog.Instance.SaveLog(
                        _processTip,
                        Color.Red,
                        "流程异常",
                        process.ProcessName,
                        $"异常类型: {ex.GetType().Name}, 消息: {ex.Message}"
                    );
                    process.CurrentStatus = GeneralStatus.Error;
                }
            }
        }

        /// <summary>
        /// 等待行内所有任务完成（核心优化：解决内存增长问题）
        /// </summary>
        private async Task WaitForRowTasksCompletion()
        {
            while (true)
            {
                // 1. 优先响应取消信号（补充第一个版本的优点）
                if (_cts.IsCancellationRequested)
                    return;

                // 2. 检查安全限制
                if (!await CheckSecurityRestrictions())
                    return;

                List<Task> tasksCopy = null;
                await _taskSemaphore.WaitAsync(_cts.Token);
                try
                {
                    if (_runningTasks.Count == 0)
                        break;
                    // 先提前清理一次已完成任务，减少后续等待的任务量（关键优化）
                    _runningTasks.RemoveAll(t => t.IsCompleted);
                    if (_runningTasks.Count == 0)
                        break;
                    tasksCopy = new List<Task>(_runningTasks);
                }
                finally
                {
                    _taskSemaphore.Release();
                }

                // 3. 等待任一任务完成（而非等待所有或超时），兼顾及时响应和减少锁竞争
                await Task.WhenAny(tasksCopy);

                // 4. 任务完成后立即清理（避免堆积）
                await _taskSemaphore.WaitAsync(_cts.Token);
                try
                {
                    _runningTasks.RemoveAll(t => t.IsCompleted);
                }
                finally
                {
                    _taskSemaphore.Release();
                }
            }
        }
        /// <summary>
        /// 检查行执行结果
        /// 优化：增加对取消状态的检查
        /// </summary>
        private bool CheckRowExecutionResults(List<ProcessBase> processRow)
        {
            if (_cts.IsCancellationRequested)
            {
                return true; // 取消状态视为有错误
            }

            bool hasError = true;
            int completedCount = 0;
            if (processRow.Count == 0)
            {
                return false;
            }

            foreach (var process in processRow)
            {
                switch (process.CurrentStatus)
                {
                    case GeneralStatus.NoError:
                        hasError = false;
                        completedCount++;
                        LogProcessCompletion(process);
                        break;
                    case GeneralStatus.Error:
                        hasError = true;
                        completedCount++;
                        LogProcessError(process);
                        return hasError;
                }
            }

            if (completedCount >= processRow.Count)
            {
                ProcessLog.Instance.SaveLog(
                    _processTip,
                    Color.Black,
                    "流程分隔符",
                    LogSeparator,
                    ""
                );
            }

            return hasError;
        }

        /// <summary>
        /// 处理行执行错误
        /// 优化：增加对取消状态的响应
        /// </summary>
        private async Task HandleRowError()
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                Cancel();
                return;
            }

            MenuProxy.Instance.MachineRunStatus = MachineRunStatus.Alarming;
            StatusUpdater.TriggerWarning(WarningColor.Orange);
            await Task.Delay(100, _cts.Token);
        }

        /// <summary>
        /// 检查安全限制（模式特有）
        /// 扩展基础安全检查，增加联机模式的特殊限制
        /// </summary>
        private async Task<bool> CheckSecurityRestrictions()
        {
            if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
            {
                return await HandleEmergencyStop();
            }

            if (_cts.IsCancellationRequested)
            {
                return false;
            }

            while (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Alarming
                   || MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Paused)
            {
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                {
                    return await HandleEmergencyStop();
                }

                if (_cts.IsCancellationRequested)
                {
                    return false;
                }

                await Task.Delay(SecurityCheckIntervalMs, _cts.Token);
            }

            while (MenuProxy.Instance.MachineRunModel == MachineMode.Manual
                   && MenuProxy.Instance.MachineRunStatus == MachineRunStatus.Running)
            {
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                {
                    return await HandleEmergencyStop();
                }

                if (_cts.IsCancellationRequested)
                {
                    return false;
                }

                await Task.Delay(SecurityCheckIntervalMs, _cts.Token);
            }

            return true;
        }

        /// <summary>
        /// 处理紧急停止（模式适配）
        /// 封装紧急停止逻辑，避免代码重复
        /// </summary>
        private async Task<bool> HandleEmergencyStop()
        {
            AutoRunStatus = GeneralStatus.Error;
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Red,
                "紧急停止",
                EmergencyStopText,
                ""
            );
            _cts.Cancel();
            await Task.CompletedTask;
            return false;
        }

        /// <summary>
        /// 记录流程开始日志（模式适配）
        /// </summary>
        private void LogProcessStart(ProcessBase process)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Black,
                process.ProcessId,
                process.ProcessName,
                StartStatusText
            );
        }

        /// <summary>
        /// 记录流程完成日志（模式适配）
        /// </summary>
        private void LogProcessCompletion(ProcessBase process)
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.Black,
                process.ProcessId,
                process.ProcessName,
                $" 状态:--{process.Message}--单流程作业耗时：{(_singleProcessStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 记录流程错误日志（模式适配）
        /// </summary>
        private void LogProcessError(ProcessBase process)
        {
            Project.Instance.GlobalVariables.AlarmMessage = $"{process.ProcessName} ,状态:--{process.Message}";
            Project.Instance.GlobalVariables.AlarmId = process.ProcessId;

            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.IndianRed,
                process.ProcessId,
                process.ProcessName,
                $"--报警耗时：{(_singleProcessStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 记录总执行时间日志（模式适配）
        /// </summary>
        private void LogTotalExecutionTime()
        {
            ProcessLog.Instance.SaveLog(
                _processTip,
                Color.AliceBlue,
                "流程集耗时",
                _processTip,
                $"  耗时 ：  {(_totalStopwatch.ElapsedMilliseconds / 1000.0f):F3} s"
            );
        }

        /// <summary>
        /// 取消所有正在执行的任务（模式适配）
        /// </summary>
        public void Cancel()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                AutoRunStatus = GeneralStatus.Error;
            }
        }

        /// <summary>
        /// 释放资源，避免内存泄漏
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 资源释放的实际实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _taskSemaphore?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// 析构函数，确保资源释放
        /// </summary>
        ~AutoCore_副流程()
        {
            Dispose(false);
        }
    }
}


