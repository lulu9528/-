using System;
using Jabil.Vision.JvlDotNet;
using pythonnet.Runtime;

namespace Jabil.Module.Setting.Lib.JVS
{
    /// <summary>
    /// 启动/停止支撑 JVS 视觉工具的 Python 引擎（进程内嵌入，官方 JvlDotNet 方案第②种）。
    /// </summary>
    /// <remarks>
    /// 必须在任何 workspace 或工具被触碰之前恰好执行一次：先 Jvl.Initialize 启动引擎，
    /// 再 Codec.Register 注册 .NET/Python 类型转换器，最后才能创建第一个 WorkSpace。
    /// 重复调用无害（Jvl.Initialize 在引擎已运行时直接返回），线程安全。
    /// 参考官方 JvlRuntime 封装。
    /// </remarks>
    public static class JvsRuntime
    {
        private static readonly object Gate = new object();
        private static bool _codecRegistered;

        /// <summary>
        /// 初始化 Python 引擎并注册类型转换器。可多次调用。
        /// </summary>
        /// <param name="allowThreads">是否允许从多线程使用 Python 引擎（相机采集线程驱动视觉工具时需要）。默认 true。</param>
        public static void Initialize(bool allowThreads = true)
        {
            lock (Gate)
            {
                Jvl.Initialize(JvsPaths.PythonEnv, allowThreads);
                if (!_codecRegistered)
                {
                    Codec.Register();
                    _codecRegistered = true;
                }
            }
        }

        /// <summary>
        /// 引擎是否已初始化。
        /// </summary>
        public static bool IsInitialized
        {
            get { return Jvl.IsInitialized; }
        }

        /// <summary>
        /// 关闭 Python 引擎。引擎未启动时调用安全。
        /// </summary>
        public static void Shutdown()
        {
            lock (Gate)
            {
                if (!Jvl.IsInitialized)
                {
                    return;
                }
                try
                {
                    Jvl.Shutdown();
                    _codecRegistered = false;
                }
                catch (NotSupportedException)
                {
                    // .NET 8+ 上 BinaryFormatter 被禁用时 Jvl.Shutdown 可能抛 NotSupportedException；
                    // 进程即将退出，系统会回收所有资源，这里吞掉异常。
                }
            }
        }
    }
}
