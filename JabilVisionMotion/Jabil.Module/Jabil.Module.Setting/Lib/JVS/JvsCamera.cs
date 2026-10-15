using System;
using System.IO;
using Python.Runtime;
using pyjvl2._C.libjvl;

namespace Jabil.Module.Setting.Lib.JVS
{
    /// <summary>
    /// JVS 取图工具封装（加载 workspace 导出的相机工具 .jvt，触发取图）。
    /// 调用格式与原框架 CogAcqFifoTool 对齐：Run() 取像一次，OutputImage 输出 JVS 图像。
    /// 相机连接/曝光/触发/多相机管理全部由 JVS 相机工具（CameraTool）内部处理，
    /// 本类不驱动任何硬件相机 SDK——现场存在什么相机无需考虑。
    /// 内部通过 pythonnet 执行内嵌 Python 脚本：dill 反序列化 .jvt → CameraTool →
    /// run() 触发取图 → 读 output_image pin（pyjvl2._C.libjvl.Image）。
    /// 注意：取图工具输出的 Image 所有权归属 JVS 工具，不得 Dispose；
    /// OutputImage 有效期截止到下一次 Run() / Close() 之前。
    /// </summary>
    public sealed class JvsCamera : IDisposable
    {
        /// <summary>
        /// 内嵌取图脚本（pythonnet 在独立模块命名空间执行）。
        /// jvs_load：dill 反序列化 .jvt 到全局 _CAMERA_TOOL（按路径幂等缓存，用于 Load 真实验证）；
        /// jvs_capture：触发取图 run() 并返回 output_image（工具未加载时内部补 jvs_load）。
        /// </summary>
        private const string CaptureScript =
            "import dill\r\n" +
            "_CAMERA_TOOL = None\r\n" +
            "_CAMERA_TOOL_PATH = None\r\n" +
            "def jvs_load(jvt_path):\r\n" +
            "    global _CAMERA_TOOL, _CAMERA_TOOL_PATH\r\n" +
            "    if _CAMERA_TOOL is None or _CAMERA_TOOL_PATH != jvt_path:\r\n" +
            "        with open(jvt_path, 'rb') as _f:\r\n" +
            "            _CAMERA_TOOL = dill.load(_f)\r\n" +
            "        _CAMERA_TOOL_PATH = jvt_path\r\n" +
            "    return True\r\n" +
            "def jvs_capture(jvt_path):\r\n" +
            "    global _CAMERA_TOOL, _CAMERA_TOOL_PATH\r\n" +
            "    if _CAMERA_TOOL is None or _CAMERA_TOOL_PATH != jvt_path:\r\n" +
            "        jvs_load(jvt_path)\r\n" +
            "    _CAMERA_TOOL.run()\r\n" +
            "    return _CAMERA_TOOL.get_output('output_image').value()\r\n";

        private readonly object _gate = new object();
        private PyModule _captureModule;
        private PyObject _loadFn;
        private PyObject _captureFn;
        private PyObject _lastCaptureResult;   // 防御持有：OutputImage 的 Python 引用在有效期内保持存活
        private string _toolPath;
        private bool _scriptReady;
        private bool _disposed;
        private Image _outputImage;
        private string _lastError;

        /// <summary>取图工具文件路径（.jvt 绝对路径，最近一次 Load 指定）。</summary>
        public string ToolPath { get { lock (_gate) { return _toolPath; } } }

        /// <summary>最近一次取像输出的 JVS 图像（所有权归取图工具，勿 Dispose；下一次 Run()/Close() 前有效）。</summary>
        public Image OutputImage
        {
            get { lock (_gate) { return _outputImage; } }
        }

        /// <summary>最后一次错误信息（加载/取像失败时记录）。</summary>
        public string LastError
        {
            get { lock (_gate) { return _lastError; } }
        }

        /// <summary>
        /// 加载取图工具文件（.jvt，dill 序列化的 CameraTool 实例）。
        /// 幂等：同一路径只装配一次；路径变化或 Close 后重新装配，装配时真实反序列化验证 .jvt
        /// （损坏/版本不兼容在加载阶段暴露，而非首次取图）。
        /// 文件不存在/加载失败时返回 false。
        /// </summary>
        /// <param name="jvtPath">.jvt 工具文件绝对路径（如 D:\...\相机取图.jvt）。</param>
        public bool Load(string jvtPath)
        {
            lock (_gate)
            {
                if (string.IsNullOrWhiteSpace(jvtPath))
                {
                    _lastError = "取图工具文件路径为空（请在 JVS 设置页配置 .jvt 文件）。";
                    return false;
                }
                if (!File.Exists(jvtPath))
                {
                    _lastError = "取图工具文件不存在：" + jvtPath;
                    return false;
                }

                try
                {
                    if (!JvsRuntime.IsInitialized)
                    {
                        if (!JvsVisionBridge.Instance.InitializeEngine(out var engineError))
                        {
                            _lastError = engineError;
                            return false;
                        }
                    }

                    using (Py.GIL())
                    {
                        if (_disposed || !_scriptReady || _toolPath != jvtPath)
                        {
                            // 重建前先释放旧引用（路径切换/上次 Close 残留），避免 Python 对象泄漏；
                            // 同时消除 GetAttr 中途抛异常时「新模块+旧函数」的不一致隐患
                            ReleasePythonObjectsLocked();

                            _captureModule = PyModule.FromString("jvs_capture_mod", CaptureScript);
                            _loadFn = _captureModule.GetAttr("jvs_load");
                            _captureFn = _captureModule.GetAttr("jvs_capture");
                            _toolPath = jvtPath;
                            _scriptReady = true;
                            _disposed = false;

                            // 装配后真实反序列化验证 .jvt（工具实例缓存于脚本全局，仅首次/换路径执行）
                            using (PyObject pathArg = new PyString(_toolPath))
                            using (PyObject loadResult = _loadFn.Invoke(pathArg))
                            {
                                if (loadResult == null || loadResult.IsNone() || !loadResult.As<bool>())
                                {
                                    _lastError = "取图工具反序列化失败（.jvt 损坏或 dill 版本不兼容）。";
                                    ReleasePythonObjectsLocked(); // 复位，下次 Load 重新装配
                                    return false;
                                }
                            }
                        }
                    }

                    _lastError = null;
                    return true;
                }
                catch (Exception ex)
                {
                    _lastError = "取图工具加载失败：" + ex.Message;
                    try
                    {
                        if (JvsRuntime.IsInitialized)
                        {
                            using (Py.GIL()) { ReleasePythonObjectsLocked(); }
                        }
                    }
                    catch (Exception) { /* 释放失败不覆盖原始错误 */ }
                    return false;
                }
            }
        }

        /// <summary>
        /// 触发取图一次（对应原框架：cogAcqFifo.Run()）。
        /// 成功后 OutputImage 为 JVS 图像；失败时 OutputImage 为 null 且 LastError 有值。
        /// </summary>
        public bool Run()
        {
            lock (_gate)
            {
                if (_disposed || !_scriptReady || _captureFn == null)
                {
                    _lastError = "取图工具未加载（请先调用 Load 指定 .jvt 文件）。";
                    return false;
                }

                try
                {
                    using (Py.GIL())
                    {
                        // 上一帧结果引用先行释放（OutputImage 随之失效——契约：有效期至下一次 Run()）
                        if (_lastCaptureResult != null)
                        {
                            _lastCaptureResult.Dispose();
                            _lastCaptureResult = null;
                        }

                        // 参数必须为 Python str（pythonnet 3.x 的 FromManagedObject 对 C# String 包装
                        // 无法被 open() 识别，需显式 PyString）
                        using (PyObject pathArg = new PyString(_toolPath))
                        {
                            _lastCaptureResult = _captureFn.Invoke(pathArg);
                            if (_lastCaptureResult == null || _lastCaptureResult.IsNone())
                            {
                                if (_lastCaptureResult != null)
                                {
                                    _lastCaptureResult.Dispose();
                                    _lastCaptureResult = null;
                                }
                                _lastError = "取图脚本无返回（未拿到图像）。";
                                _outputImage = null;
                                return false;
                            }
                            _outputImage = _lastCaptureResult.As<Image>();
                        }
                    }

                    if (_outputImage == null || _outputImage.width <= 0 || _outputImage.height <= 0)
                    {
                        _lastError = "取图输出为空图像（相机未出图，请检查 JVS 相机工具与相机连接状态）。";
                        _outputImage = null;
                        return false;
                    }

                    _lastError = null;
                    return true;
                }
                catch (Exception ex)
                {
                    _lastError = "取图异常：" + ex.Message;
                    _outputImage = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// 关闭并释放（程序退出/方案切换时调用）。
        /// 释放 Python 引用与取图结果；此后可再次 Load 重新装配（方案切换场景）。
        /// 不释放工具输出的 Image（所有权在 JVS 工具侧）。
        /// </summary>
        public void Close()
        {
            lock (_gate)
            {
                _disposed = true;
                _outputImage = null;
                try
                {
                    if (JvsRuntime.IsInitialized)
                    {
                        // PyObject 销毁要求持有 GIL（CPython 引用计数），释放段显式获取
                        using (Py.GIL())
                        {
                            ReleasePythonObjectsLocked();
                        }
                    }
                    else
                    {
                        ReleasePythonObjectsLocked();
                    }
                }
                catch (Exception)
                {
                    // 释放失败不阻塞退出
                }
            }
        }

        /// <summary>释放 Python 引用（必须在 _gate 锁内调用；PyObject 销毁要求 GIL，调用方确保 GIL 环境）。
        /// 同时清 _outputImage：其底层 Python 对象随 _lastCaptureResult 释放，C# 包装同步置空避免悬空窗口。</summary>
        private void ReleasePythonObjectsLocked()
        {
            _outputImage = null; // 与 Close 的清引用保持一致：底层对象已释放，包装同步失效
            if (_lastCaptureResult != null) { _lastCaptureResult.Dispose(); _lastCaptureResult = null; }
            if (_captureFn != null) { _captureFn.Dispose(); _captureFn = null; }
            if (_loadFn != null) { _loadFn.Dispose(); _loadFn = null; }
            if (_captureModule != null) { _captureModule.Dispose(); _captureModule = null; }
            _scriptReady = false;
            _toolPath = null;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
