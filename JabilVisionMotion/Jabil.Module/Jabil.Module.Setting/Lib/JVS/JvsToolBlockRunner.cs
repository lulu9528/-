using System;
using System.Collections.Generic;
using System.Linq;
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
