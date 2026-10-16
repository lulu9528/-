using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using pyjvl2._C.libjvl;
using pyjvl2.workspace.framework.record;
using pyjvl2.workspace.framework.run_status;
using pyjvl2.workspace.framework.tool_base;
using pyjvl2.workspace.framework.tool_block;
using pyjvl2.workspace.framework.tool_pin;
using pyjvl2.workspace.framework.workspace;
using Python.Runtime;

namespace Jabil.Module.Setting.Lib.JVS
{
    /// <summary>
    /// 进程内 ToolBlock 运行器（JvlDotNet 嵌入方式）。
    /// 调用格式与 Cognex CogToolBlock 对齐：SetInput("pin", value)（对应 Inputs["pin"].Value）→
    /// Run()（对应 Run()）→ 索引器/GetOutput（对应 Outputs["pin"].Value）。
    /// </summary>
    /// <remarks>
    /// 昂贵的装配（打开 workspace、查找 tool block、解析 pin）只发生一次（见 <see cref="JvsToolBlockRunnerBuilder"/>），
    /// 运行期仅做按帧操作。ToolBlock 不可并发运行：内部用锁保护，重叠调用会串行化。
    /// 底层对象即 JvlDotNet 原生对象，需要更底层能力时通过 <see cref="ToolBlock"/> 访问。
    /// </remarks>
    public class JvsToolBlockRunner : IDisposable
    {
        private readonly object _runLock = new object();
        private readonly WorkSpace _workSpace;
        private readonly Dictionary<string, ToolPin> _inputPins = new Dictionary<string, ToolPin>(StringComparer.Ordinal);
        private readonly HashSet<string> _outputNames;
        private readonly List<string> _imageInputPinNames;
        private bool _disposed;

        internal JvsToolBlockRunner(
            WorkSpace workSpace,
            ToolBlock toolBlock,
            IEnumerable<string> outputNames,
            IEnumerable<string> imageInputPinNames,
            IList<string> sanityCheckIssues)
        {
            _workSpace = workSpace;
            ToolBlock = toolBlock;
            _outputNames = new HashSet<string>(outputNames, StringComparer.Ordinal);
            _imageInputPinNames = new List<string>(imageInputPinNames);
            SanityCheckIssues = sanityCheckIssues;
        }

        /// <summary>底层 ToolBlock 对象（WorkSpace.GetToolBlock 返回值）。</summary>
        public ToolBlock ToolBlock { get; private set; }

        /// <summary>图像输入 pin 名列表（声明顺序）。</summary>
        public IList<string> ImageInputPinNames
        {
            get { return _imageInputPinNames.AsReadOnly(); }
        }

        /// <summary>输出 pin 名列表。</summary>
        public IList<string> OutputNames
        {
            get { return _outputNames.ToList(); }
        }

        /// <summary>装配时 sanity_check 报告的异常项（空表示健康）。</summary>
        public IList<string> SanityCheckIssues { get; private set; }

        /// <summary>
        /// 最近一次运行产生的可视化记录（未启用 records 时为空）。
        /// Record 对象归属 ToolBlock，不要 Dispose；用 ToolBlock.clean_records() 释放。
        /// </summary>
        public IList<Record> Records
        {
            get
            {
                ThrowIfDisposed();
                return ToolBlock.records.ToList();
            }
        }

        /// <summary>
        /// 获取最近一次运行的可视化结果图（带标注叠加：JVS 记录图 image_with_shapes）。
        /// 仅当记录已开启（EnableRecords）且本次运行产生记录时返回图像；否则返回 null。
        /// 返回的 Bitmap 是独立拷贝（由 numpy tobytes 重建），调用方负责 Dispose。
        /// 说明：此方法在 _runLock 内取记录并转换，检测线程调用不会与 Run() 并发冲突。
        /// </summary>
        public System.Drawing.Bitmap GetLastResultBitmap()
        {
            ThrowIfDisposed();
            lock (_runLock)
            {
                try
                {
                    var records = ToolBlock.records.ToList();
                    for (int i = records.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            var img = records[i].image_with_shapes;
                            if (img != null && img.width > 0 && img.height > 0)
                            {
                                return ImageToBitmap(img);
                            }
                        }
                        catch (Exception)
                        {
                            // 个别记录可能没有可视化图像，跳过继续找更早的记录
                        }
                    }
                }
                catch (Exception)
                {
                    // records 访问异常（记录未开启等），返回 null
                }
                return null;
            }
        }

        /// <summary>
        /// 清理 ToolBlock 全部可视化记录（释放中间图像，防内存增长；下次 Run 会重建）。
        /// </summary>
        public void ClearRecords()
        {
            ThrowIfDisposed();
            lock (_runLock)
            {
                try
                {
                    ToolBlock.clean_records();
                }
                catch (Exception)
                {
                    // 清理失败不影响主流程
                }
            }
        }

        /// <summary>
        /// 将 JVS Image（image_with_shapes）转换为独立 Bitmap。
        /// 通过 pythonnet 调用 Python 侧 Image.to_mat()（C# 包装类型未暴露该方法），
        /// 通道序：OpenCV 惯例 BGR（3 通道）或灰度（1 通道），构建时做 BGR→RGB 交换。
        /// </summary>
        private static System.Drawing.Bitmap ImageToBitmap(pyjvl2._C.libjvl.Image img)
        {
            int w = img.width, h = img.height, channels = img.channels;
            if (w <= 0 || h <= 0 || channels <= 0)
            {
                return null;
            }

            try
            {
                using (Py.GIL())
                {
                    PyObject mat = img.InvokeMethod("to_mat"); // numpy.ndarray（H, W, C）
                    if (mat == null)
                    {
                        return null;
                    }
                    try
                    {
                        using (PyObject np = Py.Import("numpy"))
                        using (PyObject contig = np.InvokeMethod("ascontiguousarray", mat))
                        using (PyObject pyBytes = contig.InvokeMethod("tobytes"))
                        {
                            byte[] data = pyBytes.As<byte[]>();
                            if (data == null || data.Length < w * h)
                            {
                                return null;
                            }
                            return BuildBitmap(data, w, h, channels);
                        }
                    }
                    finally
                    {
                        mat.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 由像素字节构建 Bitmap（逐行拷贝以兼容 stride 对齐；3 通道做 BGR→RGB 交换）。
        /// </summary>
        private static System.Drawing.Bitmap BuildBitmap(byte[] data, int width, int height, int channels)
        {
            bool gray = channels == 1;
            System.Drawing.Bitmap bmp;
            if (gray)
            {
                bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                var palette = bmp.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }
                bmp.Palette = palette;
            }
            else
            {
                bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }

            int bytesPerPixel = gray ? 1 : 3;
            var rect = new System.Drawing.Rectangle(0, 0, width, height);
            System.Drawing.Imaging.BitmapData bd = null;
            try
            {
                bd = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
                int stride = bd.Stride;
                if (gray)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Marshal.Copy(data, y * width, bd.Scan0 + y * stride, width);
                    }
                }
                else
                {
                    // BGR → RGB 交换
                    byte[] row = new byte[width * 3];
                    for (int y = 0; y < height; y++)
                    {
                        int srcOff = y * width * 3;
                        Buffer.BlockCopy(data, srcOff, row, 0, row.Length);
                        for (int x = 0; x < width; x++)
                        {
                            int p = x * 3;
                            byte t = row[p];
                            row[p] = row[p + 2];
                            row[p + 2] = t;
                        }
                        Marshal.Copy(row, 0, bd.Scan0 + y * stride, row.Length);
                    }
                }
                return bmp;
            }
            catch (Exception)
            {
                bmp.Dispose();
                return null;
            }
            finally
            {
                if (bd != null)
                {
                    bmp.UnlockBits(bd);
                }
            }
        }

        /// <summary>
        /// 为输入 pin 赋值（对应 Cognex：toolBlock.Inputs["pin"].Value = value）。
        /// pin 已解析并缓存，避免每帧跨 Python 调用 GetInput。
        /// </summary>
        public void SetInput(string pinName, object value)
        {
            ThrowIfDisposed();
            ResolveInputPin(pinName).SetValue(value);
        }

        /// <summary>
        /// 运行 ToolBlock（对应 Cognex：toolBlock.Run()）。
        /// 内部串行化，防止并发 Run 互相破坏状态。
        /// </summary>
        public RunStatus Run()
        {
            ThrowIfDisposed();
            lock (_runLock)
            {
                ToolBlock.run();
                return ToolBlock.run_status;
            }
        }

        /// <summary>
        /// 便捷重载：单一图像输入 pin 时，赋值并运行。
        /// </summary>
        public RunStatus Run(Image image)
        {
            ThrowIfDisposed();
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            if (_imageInputPinNames.Count != 1)
            {
                throw new InvalidOperationException(
                    "ToolBlock \"" + ToolBlock.name + "\" 图像输入 pin 数量为 " + _imageInputPinNames.Count +
                    "，无法使用 Run(Image)，请改用 SetInput 显式赋值。");
            }
            SetInput(_imageInputPinNames[0], image);
            return Run();
        }

        /// <summary>
        /// 读取输出 pin 值（对应 Cognex：toolBlock.Outputs["pin"].Value）。
        /// </summary>
        public object this[string outputName]
        {
            get
            {
                ThrowIfDisposed();
                ValidateOutputName(outputName);
                return ToolBlock[outputName];
            }
        }

        /// <summary>
        /// 读取输出 pin 并转换为 T（兼容 Python 侧返回 PyObject 的场景）。
        /// </summary>
        public T GetOutput<T>(string outputName)
        {
            object raw = this[outputName];
            PyObject pythonObject = raw as PyObject;
            if (pythonObject != null)
            {
                return pythonObject.As<T>();
            }
            return (T)raw;
        }

        /// <summary>
        /// 释放由 Builder 打开的 Workspace（ToolBlock/pin/工具/记录均归属 Workspace，随其释放）。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _inputPins.Clear();
            if (_workSpace != null)
            {
                _workSpace.Dispose();
            }
        }

        private ToolPin ResolveInputPin(string pinName)
        {
            if (string.IsNullOrEmpty(pinName))
            {
                throw new ArgumentException("输入 pin 名不能为空。", nameof(pinName));
            }
            ToolPin pin;
            if (_inputPins.TryGetValue(pinName, out pin))
            {
                return pin;
            }
            pin = ToolBlock.GetInput(pinName);
            if (pin == null)
            {
                throw new ArgumentException(
                    "ToolBlock \"" + ToolBlock.name + "\" 上不存在输入 pin \"" + pinName + "\"。" +
                    Environment.NewLine + "  可用输入 pin：" + Join(ToolBlock.input_names()),
                    nameof(pinName));
            }
            _inputPins[pinName] = pin;
            return pin;
        }

        private void ValidateOutputName(string outputName)
        {
            if (string.IsNullOrEmpty(outputName))
            {
                throw new ArgumentException("输出 pin 名不能为空。", nameof(outputName));
            }
            if (!_outputNames.Contains(outputName))
            {
                throw new ArgumentException(
                    "ToolBlock \"" + ToolBlock.name + "\" 上不存在输出 pin \"" + outputName + "\"。" +
                    Environment.NewLine + "  可用输出 pin：" + Join(_outputNames),
                    nameof(outputName));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(JvsToolBlockRunner).Name);
            }
        }

        private static string Join(IEnumerable<string> values)
        {
            return values == null ? "(无)" : string.Join(", ", values.ToArray());
        }
    }

    /// <summary>
    /// 装配 <see cref="JvsToolBlockRunner"/>：打开 workspace、选择 tool block、解析 pin。
    /// 装配非常昂贵（解析压缩归档并重建整个工具图），只应在启动阶段执行一次，严禁按帧调用。
    /// </summary>
    public sealed class JvsToolBlockRunnerBuilder
    {
        private readonly string _workspacePath;
        private readonly List<string> _inputPinNames = new List<string>();
        private string _toolBlockName;
        private bool _recordsEnabled;

        private JvsToolBlockRunnerBuilder(string workspacePath)
        {
            _workspacePath = workspacePath;
        }

        /// <summary>从 .jvws 文件路径开始装配。</summary>
        public static JvsToolBlockRunnerBuilder FromWorkspace(string workspacePath)
        {
            if (string.IsNullOrEmpty(workspacePath))
            {
                throw new ArgumentException("workspace 路径不能为空。", nameof(workspacePath));
            }
            return new JvsToolBlockRunnerBuilder(workspacePath);
        }

        /// <summary>选择 workspace 中的 tool block（名称区分大小写）。</summary>
        public JvsToolBlockRunnerBuilder UseToolBlock(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("tool block 名称不能为空。", nameof(name));
            }
            _toolBlockName = name;
            return this;
        }

        /// <summary>声明要赋值的输入 pin（可省略，SetInput 首次使用时会自动解析）。</summary>
        public JvsToolBlockRunnerBuilder WithInputPin(string pinName)
        {
            if (string.IsNullOrEmpty(pinName))
            {
                throw new ArgumentException("输入 pin 名不能为空。", nameof(pinName));
            }
            _inputPinNames.Add(pinName);
            return this;
        }

        /// <summary>
        /// 开启可视化记录（记录会保留中间图像，消耗时间与内存；仅界面需要显示时才开启）。
        /// </summary>
        public JvsToolBlockRunnerBuilder EnableRecords(bool enabled = true)
        {
            _recordsEnabled = enabled;
            return this;
        }

        /// <summary>打开 workspace 并产出运行器。</summary>
        public JvsToolBlockRunner Build()
        {
            WorkSpace workSpace;
            ToolBlock toolBlock;
            PrepareToolBlock(out workSpace, out toolBlock);

            try
            {
                return new JvsToolBlockRunner(
                    workSpace,
                    toolBlock,
                    toolBlock.output_names(),
                    FindImageInputPinNames(toolBlock),
                    toolBlock.sanity_check().ToList());
            }
            catch
            {
                workSpace.Dispose();
                throw;
            }
        }

        private void PrepareToolBlock(out WorkSpace workSpace, out ToolBlock toolBlock)
        {
            if (string.IsNullOrEmpty(_toolBlockName))
            {
                throw new InvalidOperationException(
                    "未选择 tool block。请在 Build 前调用 UseToolBlock。");
            }

            workSpace = new WorkSpace(_workspacePath);
            try
            {
                toolBlock = workSpace.GetToolBlock(_toolBlockName);
                if (toolBlock == null)
                {
                    throw new InvalidOperationException(
                        "workspace \"" + _workspacePath + "\" 中不存在 tool block \"" + _toolBlockName + "\"。" +
                        Environment.NewLine + "  可用 tool block：" + Join(workSpace.tool_block_names()) +
                        Environment.NewLine + "  名称区分大小写，请在 Jabil Vision Studio 中确认。");
                }

                foreach (string pinName in _inputPinNames)
                {
                    if (toolBlock.GetInput(pinName) == null)
                    {
                        throw new ArgumentException(
                            "ToolBlock \"" + toolBlock.name + "\" 上不存在输入 pin \"" + pinName + "\"。" +
                            Environment.NewLine + "  可用输入 pin：" + Join(toolBlock.input_names()));
                    }
                }

                if (_recordsEnabled)
                {
                    EnableToolRecords(toolBlock);
                }
            }
            catch
            {
                workSpace.Dispose();
                throw;
            }
        }

        private static IEnumerable<string> FindImageInputPinNames(ToolBlock toolBlock)
        {
            List<string> names = new List<string>();
            foreach (ToolPin pin in toolBlock.inputs.ToList())
            {
                if (pin.is_image())
                {
                    names.Add(pin.name);
                }
            }
            return names;
        }

        /// <summary>
        /// 递归开启 tool block 及其嵌套工具的记录开关（tool_block.record_enabled + 遍历 tools）。
        /// </summary>
        private static void EnableToolRecords(ToolBlock toolBlock)
        {
            toolBlock.record_enabled = true;
            foreach (ToolBase tool in toolBlock.tools.ToList())
            {
                ToolBlock nested = tool as ToolBlock;
                if (nested != null)
                {
                    EnableToolRecords(nested);
                }
                else
                {
                    try
                    {
                        tool.record_enabled = true;
                    }
                    catch (Exception)
                    {
                        // 部分工具类型不支持 record_enabled，忽略
                    }
                }
            }
        }

        private static string Join(IEnumerable<string> values)
        {
            return values == null ? "(无)" : string.Join(", ", values.ToArray());
        }
    }
}
