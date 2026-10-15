using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Jabil.Module.Setting.Lib.JVS
{
    /// <summary>
    /// JVS 进程内嵌入门面（第二种方式：JvlDotNet）。
    /// 职责：
    /// - Python 引擎生命周期（JvlRuntime）
    /// - ToolBlock 运行器装配与缓存（按检测工具名称）
    /// - 相机实例（延迟打开，Run 时自动 Open）
    /// - 映射配置访问
    /// 由 SysParm 在启动阶段调用 <see cref="InitializeEngine(out string)"/>；
    /// VisionProcess 在检测时通过 <see cref="GetRunner(string, string, out string)"/> 获取运行器。
    /// </summary>
    public sealed class JvsVisionBridge : IDisposable
    {
        private static readonly Lazy<JvsVisionBridge> LazyInstance =
            new Lazy<JvsVisionBridge>(() => new JvsVisionBridge());

        private readonly object _sync = new object();
        private readonly Dictionary<string, JvsToolBlockRunner> _runners =
            new Dictionary<string, JvsToolBlockRunner>(StringComparer.Ordinal);

        /// <summary>全局单例。</summary>
        public static JvsVisionBridge Instance
        {
            get { return LazyInstance.Value; }
        }

        /// <summary>映射配置（全局，可被设置页 Reload 刷新）。</summary>
        public JvsWorkspaceMapping Mapping
        {
            get { return JvsWorkspaceMapping.Instance; }
        }

        /// <summary>相机实例（未打开时内部延迟打开）。</summary>
        public JvsCamera Camera { get; } = new JvsCamera();

        /// <summary>Python 引擎是否已初始化。</summary>
        public bool IsEngineInitialized
        {
            get { return JvsRuntime.IsInitialized; }
        }

        /// <summary>
        /// 初始化 Python 引擎（幂等）。必须在任何 workspace 装配前调用。
        /// </summary>
        public bool InitializeEngine(out string error)
        {
            error = null;
            try
            {
                JvsRuntime.Initialize();
                return true;
            }
            catch (Exception ex)
            {
                error = "JVS Python 引擎初始化失败：" + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 按检测工具名称获取 ToolBlock 运行器；未装配时按 workspacePath 装配并缓存。
        /// </summary>
        /// <param name="toolName">检测工具名称（VisionSystem 工具记录）。</param>
        /// <param name="workspacePath">.jvws 方案绝对路径。</param>
        /// <param name="error">失败原因。</param>
        /// <returns>运行器；失败时返回 null。</returns>
        public JvsToolBlockRunner GetRunner(string toolName, string workspacePath, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(workspacePath))
            {
                error = $"JVS 工具 {toolName} 未配置 .jvws 路径（检测工具路径字段）";
                return null;
            }
            if (!File.Exists(workspacePath))
            {
                error = $"JVS 方案文件不存在：{workspacePath}";
                return null;
            }

            lock (_sync)
            {
                JvsToolBlockRunner runner;
                if (_runners.TryGetValue(toolName, out runner))
                {
                    return runner;
                }

                try
                {
                    var mapping = Mapping;
                    runner = JvsToolBlockRunnerBuilder
                        .FromWorkspace(workspacePath)
                        .UseToolBlock(mapping.ToolBlock)
                        .WithInputPin(mapping.InputPin)
                        .Build();
                    _runners[toolName] = runner;
                    return runner;
                }
                catch (Exception ex)
                {
                    error = $"JVS 方案装配失败（{workspacePath}）：{ex.Message}";
                    return null;
                }
            }
        }

        /// <summary>
        /// 释放所有运行器、相机与 Python 引擎（程序退出时调用）。
        /// </summary>
        public void Shutdown()
        {
            lock (_sync)
            {
                foreach (var runner in _runners.Values)
                {
                    try { runner.Dispose(); } catch (Exception) { }
                }
                _runners.Clear();

                try { Camera.Close(); } catch (Exception) { }

                try { JvsRuntime.Shutdown(); } catch (Exception) { }
            }
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
