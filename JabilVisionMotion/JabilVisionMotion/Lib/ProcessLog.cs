using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using JabilVisionMotion.Frm.UserPage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Jabil.Module.Log.Control;

namespace JabilVisionMotion.Lib
{
    /// <summary>
    /// 流程日志记录辅助类<br/>
    /// 核心职责：统一管理流程日志的记录、展示和持久化，确保日志操作的一致性和可追溯性<br/>
    /// 核心功能：
    /// 1. 向UI组件实时输出流程日志（通过IProcessView接口适配不同视图）
    /// 2. 将日志按规范格式持久化到CSV文件（按日期和流程类型分类存储）
    /// 3. 提供日志去重、CSV格式转义等辅助功能，保证日志质量
    /// 设计特点：采用线程安全的单例模式，避免多线程环境下的日志混乱
    /// </summary>
    public class ProcessLog
    {
        #region 单例模式实现
        /// <summary>
        /// 线程安全的单例实例<br/>
        /// 使用Lazy延迟初始化，确保在首次访问时才创建实例，减少程序启动负载<br/>
        /// LazyThreadSafetyMode.ExecutionAndPublication：保证多线程环境下初始化方法只执行一次，避免实例重复创建
        /// </summary>
        private static readonly Lazy<ProcessLog> _instance = new Lazy<ProcessLog>(() => new ProcessLog(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局访问点<br/>
        /// 外部通过ProcessLog.Instance调用日志功能，确保全局日志操作的统一性
        /// </summary>
        public static ProcessLog Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数，防止外部实例化<br/>
        /// 确保单例模式的唯一性，避免多实例导致的日志记录冲突
        /// </summary>
        private ProcessLog() { }
        #endregion

        #region 私有字段
        /// <summary>
        /// 存储上次日志内容，用于过滤重复日志<br/>
        /// 避免UI界面和日志文件中出现大量重复信息，提升日志可读性
        /// </summary>
        private string _lastLogToPreventDuplication = "";

        /// <summary>
        /// 线程同步锁<br/>
        /// 确保日志文件写入操作的线程安全，避免多线程同时写入导致的文件损坏或内容混乱
        /// </summary>
        private readonly object _processLogLock = new object();
        #endregion

        #region 核心方法
        /// <summary>
        /// 保存日志到UI和文件系统（核心入口方法）<br/>
        /// 完整流程：参数校验→日志去重→UI实时展示→CSV文件持久化<br/>
        /// 支持按流程类型定向输出到对应的UI组件，实现日志的分类展示
        /// </summary>
        /// <param name="processTip">流程提示文本（用于匹配对应的日志处理器，如"左Epson取放料流程"）</param>
        /// <param name="color">日志显示颜色（如成功为绿色，错误为红色，用于UI区分）</param>
        /// <param name="bindingCode">绑定代码（流程的唯一标识，用于日志追溯）</param>
        /// <param name="processName">流程名称（如"左Epson取放料流程"，描述日志所属流程）</param>
        /// <param name="status">流程状态（如"运行中"、"完成"、"错误"，描述流程当前状态）</param>
        public void SaveLog(string processTip, Color color, string bindingCode, string processName, string status)
        {
            // 入参校验：流程提示文本不能为空（关键参数，用于定位日志处理器）
            if (string.IsNullOrEmpty(processTip))
                throw new ArgumentNullException(nameof(processTip));

            try
            {
                // 组合日志内容（用于去重判断和最终展示）
                var logContent = $"{processName}{status}";

                // 过滤重复日志：与上次日志内容完全一致则跳过（避免刷屏）
                if (_lastLogToPreventDuplication == logContent)
                    return;
                _lastLogToPreventDuplication = logContent; // 更新上次日志内容

                // 根据流程类型获取对应的日志处理器（包含UI更新器和文件处理器）
                if (TryGetLogProcessor(processTip, out var processor))
                {
                    // 在主线程更新UI（使用WFUtil确保跨线程操作安全，避免UI线程异常）
                    WFUtil.UpdateMainThreadUIAsync(ProcessMonitorPage.Instance, () =>
                    {
                        try
                        {
                            // 确定日志级别（根据颜色判断，可根据实际需求调整）
                            var logLevel = GetLogLevelByColor(color);

                            // 通过处理器更新UI（使用IProcessView接口确保对不同视图的兼容性）
                            processor.UpdateUiAction(LogType.Process, logLevel, color, bindingCode, logContent);
                        }
                        catch (InvalidCastException ex)
                        {
                            // 异常处理：视图未实现IProcessView接口（接口适配失败）
                            RLog.SaveError(new Exception($"视图未实现IProcessView接口: {ex.Message}", ex));
                            // 降级处理：使用默认日志记录（确保日志不丢失）
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, color, logContent);
                        }
                        catch (NullReferenceException ex)
                        {
                            // 异常处理：视图实例为null（UI组件未初始化）
                            RLog.SaveError(new Exception($"视图实例为null: {ex.Message}", ex));
                            // 降级处理：使用默认日志记录
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, color, logContent);
                        }
                    });

                    // 准备日志数据（生成CSV表头和一行日志内容）
                    var (header, data) = PrepareLogData(bindingCode, processName, status);
                    // 将日志数据保存到CSV文件（按流程类型分类）
                    SaveProcessData(header, data, "Process", processTip);
                }
                else
                {
                    // 未找到对应处理器时，使用默认日志记录（容错机制）
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, color, logContent);
                }
            }
            catch (Exception ex)
            {
                // 记录日志过程中的异常（确保日志系统自身的问题可追溯）
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 根据日志颜色确定日志级别
        /// </summary>
        /// <param name="color">日志显示颜色</param>
        /// <returns>对应的日志级别</returns>
        private LogLevel GetLogLevelByColor(Color color)
        {
            if (color == Color.Red)
                return LogLevel.Error;
            else if (color == Color.Yellow)
                return LogLevel.Warn;
            else
                return LogLevel.Info;
        }

        #endregion

        #region 日志数据处理（格式规范与内容准备）
        /// <summary>
        /// 准备日志数据（生成CSV表头和一行日志内容）<br/>
        /// 确保日志格式统一，包含必要的追溯信息（时间、绑定代码、流程信息等）
        /// </summary>
        /// <param name="bindingCode">绑定代码</param>
        /// <param name="processName">流程名称</param>
        /// <param name="status">流程状态</param>
        /// <returns>元组(表头字符串, 日志数据行字符串)</returns>
        private (string header, string data) PrepareLogData(string bindingCode, string processName, string status)
        {
            // 日志时间（精确到毫秒，满足工业场景下的精细追溯需求）
            var time = DateTime.Now.ToString("HH:mm:ss.fff");

            // 构建CSV表头和数据行（包含转义处理，确保CSV格式正确）
            return (
                "时间,绑定代码,流程信息,状态,\r\n",  // 表头：定义CSV列含义
                $"{time},{EscapeCsvField(bindingCode)},{EscapeCsvField(processName)},{EscapeCsvField(status)},\r\n"  // 数据行：对应的值
            );
        }

        /// <summary>
        /// 转义CSV字段中的特殊字符（确保CSV格式正确）<br/>
        /// 处理CSV格式的常见问题：逗号分隔符冲突、引号嵌套、换行符导致的行分裂等
        /// </summary>
        /// <param name="value">需要转义的字段值</param>
        /// <returns>转义后的字符串（符合CSV规范）</returns>
        private string EscapeCsvField(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // 若字段包含逗号、引号或换行符，需用双引号包裹（CSV标准格式要求）
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                // 双引号转义为两个双引号（CSV标准转义方式，避免引号闭合冲突）
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value; // 无特殊字符时直接返回原字符串
        }
        #endregion

        #region 日志文件存储（持久化逻辑）
        /// <summary>
        /// 将日志数据保存到CSV文件（按日期和流程类型分类）<br/>
        /// 存储策略：
        /// - 按日期分目录（便于按天检索日志）
        /// - 按小时分文件（避免单个文件过大，提升读写效率）
        /// - 按流程类型分文件夹（实现日志的分类管理）
        /// </summary>
        /// <param name="header">CSV表头（首次写入文件时添加）</param>
        /// <param name="data">日志数据行（单条日志内容）</param>
        /// <param name="parentDirectory">父目录名称（如"Process"，用于区分日志大类）</param>
        /// <param name="processName">流程名称（用于子目录分类，如"左Epson取放料流程"）</param>
        /// <returns>true：保存成功；false：失败</returns>
        private bool SaveProcessData(string header, string data, string parentDirectory, string processName)
        {
            try
            {
                // 加锁确保多线程写入安全（避免并发写入导致的文件损坏）
                lock (_processLogLock)
                {
                    // 日志日期计算：凌晨0-8点的日志归属前一天（符合工业生产班次习惯，如夜班日志归为前一天）
                    var logDate = DateTime.Now.Hour < 8
                        ? DateTime.Now.AddDays(-1)
                        : DateTime.Now;

                    // 构建日志文件路径：根目录/Config/Log/父目录/流程名称/日期/小时.csv
                    var basePath = Path.Combine(
                        Project.Instance.ParameterPath.DataPath,
                        "Config", "Log", parentDirectory, processName,
                        logDate.ToString("yyyy-MM-dd")  // 按日期分目录（如"2024-05-20"）
                    );

                    // 自动创建不存在的目录（确保日志能正常写入）
                    if (!Directory.Exists(basePath))
                    {
                        Directory.CreateDirectory(basePath);
                    }

                    // 按小时创建文件（避免单个文件过大，提升读写效率和检索速度）
                    var fileName = $"{logDate:yyyy年MM月dd日HH时}.csv";
                    var filePath = Path.Combine(basePath, fileName);

                    // 首次写入时添加表头（确保CSV文件有完整的列定义）
                    if (!File.Exists(filePath))
                    {
                        File.AppendAllText(filePath, header, Encoding.UTF8);
                    }

                    // 追加日志内容（使用UTF8编码确保中文正常显示）
                    File.AppendAllText(filePath, data, Encoding.UTF8);
                    return true;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录文件写入失败的异常
                return false;
            }
        }
        #endregion

        #region 日志处理器映射（流程与视图的关联）
        /// <summary>
        /// 根据流程提示文本获取对应的日志处理器<br/>
        /// 作用：建立流程类型与UI视图的映射关系，实现日志的定向展示（如"左Epson流程"日志显示在对应面板）
        /// </summary>
        /// <param name="processTip">流程提示文本（如"左Epson取放料流程"）</param>
        /// <param name="processor">输出参数：日志处理器（包含UI更新逻辑）</param>
        /// <returns>true：找到处理器；false：未找到</returns>
        private bool TryGetLogProcessor(string processTip, out LogProcessor processor)
        {
            // 使用字典映射替代冗长的switch-case，提高代码可维护性和扩展性
            // 键：流程提示文本；值：日志处理器（关联对应的UI视图）
            var processors = new Dictionary<string, LogProcessor>
        {
                            // 主流程日志映射（关联主流程视图）
                            { "飞拍组装流程", new LogProcessor(page => GetProcessView(page.processView_飞拍组装流程)) },
                            { "Epson1上相机流程", new LogProcessor(page => GetProcessView(page.processView_EPSON1上相机流程)) },
                            { "Epson1下相机流程", new LogProcessor(page => GetProcessView(page.processView_EPSON1下相机流程)) },
                            { "Epson1请求最终结果流程", new LogProcessor(page => GetProcessView(page.processView_EPSON1请求最终结果流程)) },
                            { "Epson2上相机流程", new LogProcessor(page => GetProcessView(page.processView_EPSON2上相机流程)) },
                            { "Epson2下相机流程", new LogProcessor(page => GetProcessView(page.processView_EPSON2下相机流程)) },
                            { "Epson2请求最终结果流程", new LogProcessor(page => GetProcessView(page.processView_EPSON2请求最终结果流程)) },

                            // 跳转顺序流程日志映射（关联副流程顺序监控视图）
                            { "飞拍组装流程跳转顺序", new LogProcessor(page => GetProcessView(page.processView_飞拍组装流程跳转顺序)) },
                            { "Epson1上相机流程跳转顺序", new LogProcessor(page => GetProcessView(page.processView_EPSON1上相机流程跳转顺序)) },
                            { "Epson1下相机流程跳转顺序", new LogProcessor(page => GetProcessView(page.processView_EPSON1下相机流程跳转顺序)) },
                            { "Epson1请求最终结果流程跳转顺序", new LogProcessor(page => GetProcessView(page.processView_EPSON1下相机流程跳转顺序)) },
                             { "Epson2上相机流程跳转顺序", new LogProcessor(page => GetProcessView(page.processView_EPSON2上相机流程跳转顺序)) },
                            { "Epson2下相机流程跳转顺序", new LogProcessor(page => GetProcessView(page.processView_EPSON2下相机流程跳转顺序)) },
                            { "Epson2请求最终结果流程跳转顺序", new LogProcessor(page => GetProcessView(page.processView_EPSON2请求最终结果流程跳转顺序)) },
                            
                            // 特殊流程日志映射
                            { "复位流程", new LogProcessor(page => GetProcessView(page.processView_复位流程)) }
            };

            return processors.TryGetValue(processTip, out processor);
        }

        /// <summary>
        /// 类型转换辅助方法，确保视图实现IProcessView接口<br/>
        /// 作用：提前发现类型不匹配问题，将潜在错误在开发/测试阶段暴露，减少运行时异常
        /// </summary>
        /// <param name="view">待转换的视图对象（ProcessView实例）</param>
        /// <returns>实现IProcessView接口的视图对象（确保支持日志输出方法）</returns>
        /// <exception cref="NullReferenceException">视图对象为null时抛出</exception>
        /// <exception cref="InvalidCastException">视图未实现接口时抛出</exception>
        private IProcessView GetProcessView(ProcessView view)
        {
            if (view == null)
            {
                throw new NullReferenceException("视图对象为null（可能UI组件未初始化）");
            }

            // 检查是否实现了IProcessView接口（确保视图支持OutputMsg方法）
            if (view is IProcessView processView)
            {
                return processView;
            }

            // 抛出明确的异常信息，便于开发人员定位接口实现问题
            throw new InvalidCastException($"视图对象类型 {view.GetType().Name} 未实现 IProcessView 接口，无法输出日志");
        }

        /// <summary>
        /// 日志处理器辅助类，封装UI更新逻辑<br/>
        /// 作用：关联流程类型与对应的UI视图，提供统一的UI更新方法，解耦流程与视图的直接依赖
        /// </summary>
        private class LogProcessor
        {
            /// <summary>
            /// 视图访问器，用于获取指定流程对应的IProcessView实例<br/>
            /// 通过委托延迟获取视图，避免初始化时视图未就绪的问题
            /// </summary>
            private readonly Func<ProcessMonitorPage, IProcessView> _viewAccessor;

            /// <summary>
            /// 构造函数<br/>
            /// 初始化视图访问器，确保能正确获取对应的日志视图
            /// </summary>
            /// <param name="viewAccessor">视图访问器（返回对应流程的IProcessView实例）</param>
            public LogProcessor(Func<ProcessMonitorPage, IProcessView> viewAccessor)
            {
                _viewAccessor = viewAccessor ?? throw new ArgumentNullException(nameof(viewAccessor));
            }

            /// <summary>
            /// 执行UI更新操作<br/>
            /// 调用视图的OutputMsg方法，将日志输出到对应的UI组件
            /// </summary>
            /// <param name="logType">日志类型（如Process、Operation等）</param>
            /// <param name="logLevel">日志级别（如Info、Warning、Error）</param>
            /// <param name="color">显示颜色</param>
            /// <param name="bindingCode">绑定代码</param>
            /// <param name="message">日志内容</param>
            public void UpdateUiAction(LogType logType, LogLevel logLevel, Color color, string bindingCode, string message)
            {
                // 获取对应的视图实例并输出日志
                var processView = _viewAccessor(ProcessMonitorPage.Instance);
                processView.OutputMsg(logType, logLevel, color, bindingCode, message);
            }
        }
        #endregion
    }
}
