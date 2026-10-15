using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;
using Jabil.Lib;
using Jabil.Module.Log.Lib;
using csLTDMC; // LTDMC系列IO卡驱动库
using Jabil.Module.IO.Common.Dtos;
using Jabil.Module.Permission;
using lctdevice; // 其他型号IO卡驱动库（如M60、EC_8003）
using System.Collections;

namespace Jabil.Module.IO.Control
{
    /// <summary>
    /// IO控制UI用户控件，负责IO卡的状态监控和手动控制
    /// 采用策略模式设计，支持多种类型IO卡，通过UI界面实时展示输入输出状态
    /// 并允许操作人员在手动模式下对输出点进行控制
    /// </summary>
    public partial class IOControlUI : UIUserControl
    {
        #region 常量定义
        /// <summary>每个分页面板中的行数，控制UI布局密度</summary>
        private const int RowCount = 6;
        /// <summary>每个分页面板中的列数，与行数共同决定每页显示的点位数量</summary>
        private const int ColumnCount = 2;
        /// <summary>IO状态更新间隔（毫秒），平衡实时性与系统资源占用</summary>
        private const int UpdateIntervalMs = 100;
        /// <summary>加载超时时间（毫秒）</summary>
        private const int LoadTimeoutMs = 1000000;
        /// <summary>分页标签文本常量，统一管理分页显示文本</summary>
        private const string FirstPageText = "第一页";
        private const string SecondPageText = "第二页";
        private const string ThirdPageText = "第三页";
        private const string FourthPageText = "第四页";
        private const string FifthPageText = "第五页";
        private const string SixthPageText = "第六页";
        private const string SeventhPageText = "第七页";
        private const string EighthPageText = "第八页";
        private const string NinthPageText = "第九页";
        private const string TenthPageText = "第十页";
        #endregion

        #region 字段
        /// <summary>
        /// 当前IO卡的元数据对象，包含卡的基本信息（如卡名称、卡号索引等）
        /// 构造函数初始化，确保不为null
        /// </summary>
        private readonly IOCardDto _ioCard;

        /// <summary>
        /// 数字输入点按钮列表，用于UI展示输入状态
        /// 输入点为只读，按钮不可交互，仅用于状态显示
        /// </summary>
        private readonly List<DIO> _digitalInputButtons = new List<DIO>();

        /// <summary>
        /// 数字输出点按钮列表，用于UI展示输出状态和接收用户操作
        /// 输出点可读写，按钮可交互，点击时触发状态切换
        /// </summary>
        private readonly List<DIO> _digitalOutputButtons = new List<DIO>();

        /// <summary>
        /// 当前IO卡的所有点位信息列表，包含输入和输出点的详细信息
        /// 从系统中加载，与当前卡名称匹配的点位集合
        /// </summary>
        private List<IOCardDIODto> _ioCardPoints = new List<IOCardDIODto>();

        /// <summary>
        /// 线程同步锁对象，用于保护多线程访问的共享资源
        /// 主要保护对系统状态变量（如测试模式、程序退出状态）的访问
        /// </summary>
        private readonly object _lockObj = new object();

        /// <summary>
        /// 取消令牌源，用于控制IO监控任务的优雅取消
        /// 当控件释放或需要停止监控时，通过此对象发送取消信号
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 卡类型到处理器的映射字典，实现策略模式的核心
        /// 键为CardType枚举值，值为对应的IIoCardHandler实现，
        /// 使得不同类型IO卡的操作可以通过统一接口调用
        /// </summary>
        private readonly Dictionary<CardType, IIoCardHandler> _cardHandlers;

        /// <summary>
        /// 标记是否已完成异步加载，防止重复加载
        /// </summary>
        private bool _isLoaded = false;
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化IOControlUI控件
        /// </summary>
        /// <param name="item">IO卡元数据对象，包含卡的基本信息，不能为空</param>
        /// <exception cref="ArgumentNullException">当item为null时抛出，确保初始化参数有效</exception>
        public IOControlUI(IOCardDto item)
        {
            try
            {
                // 验证输入参数，确保不为null，提前暴露错误
                _ioCard = item ?? throw new ArgumentNullException(nameof(item));
                InitializeComponent(); // 初始化设计器生成的控件

                // 启用双缓冲和禁止擦除背景，减少UI刷新时的闪烁现象
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

                // 初始化卡处理器字典，注册所有支持的IO卡类型及其对应的处理器
                // 策略模式：通过字典实现"类型-处理器"的映射，新增卡类型只需添加新的处理器并注册
                _cardHandlers = new Dictionary<CardType, IIoCardHandler>
                {
                    { CardType.Card3400, new Card3400Handler() },
                    { CardType.Card3600, new Card3600Handler() },
                    { CardType.Card7230, new Card7230Handler() },
                    { CardType.Card7432, new Card7432Handler() },
                    { CardType.CardM60, new CardM60Handler() },
                    { CardType.CardEC8003, new CardEC8003Handler() }
                };
            }
            catch (Exception ex)
            {
                // 记录初始化过程中的异常，便于问题排查
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 加载事件
        /// <summary>
        /// 控件加载事件处理方法，是控件初始化的入口
        /// 执行权限更新、点位加载、UI控件初始化和启动IO监控任务
        /// 采用try-catch块捕获异常，确保控件加载失败时不崩溃
        /// </summary>
        private async void IOControlUI_Load(object sender, EventArgs e)
        {
            // 防止重复加载
            if (_isLoaded)
                return;

            try
            {
                // 显示加载指示器
                ShowLoadingIndicator();

                // 创建加载超时的取消令牌
                using (var cts = new CancellationTokenSource(LoadTimeoutMs))
                {
                    try
                    {
                        // 并行执行可同时进行的初始化操作
                        var permissionTask = UpdatePermissionAsync(cts.Token);
                        var pointsTask = LoadIOPointsAsync(cts.Token);

                        // 等待所有并行任务完成
                        await Task.WhenAll(permissionTask, pointsTask).ConfigureAwait(true);

                        // 初始化控件（需要在UI线程执行）
                        await InitializeControlsAsync(cts.Token).ConfigureAwait(true);

                        // 启动IO监控
                        StartIoMonitoring();

                        _isLoaded = true;
                    }
                    catch (OperationCanceledException)
                    {
                        ShowErrorMessage("加载超时，请重试");
                        RLog.SaveLog(LogType.Error, LogLevel.Info, Color.Red, "IO控件加载超时");
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录加载过程中的异常，包含堆栈信息便于调试
                RLog.SaveError(ex);
                ShowErrorMessage($"加载失败: {ex.Message}");
            }
            finally
            {
                // 隐藏加载指示器
                HideLoadingIndicator();
            }
        }
        #endregion

        #region 异步加载辅助方法
        /// <summary>
        /// 显示加载指示器，提示用户正在加载
        /// </summary>
        private void ShowLoadingIndicator()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ShowLoadingIndicator));
                return;
            }

            // 创建并显示加载提示
            var loadingLabel = new UILabel
            {
                Text = "正在加载IO控件，请稍候...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold)
            };
            loadingLabel.Name = "loadingIndicator";

            // 清除现有控件并显示加载提示
            Controls.Clear();
            Controls.Add(loadingLabel);
        }

        /// <summary>
        /// 隐藏加载指示器
        /// </summary>
        private void HideLoadingIndicator()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(HideLoadingIndicator));
                return;
            }

            // 移除加载提示，恢复原有控件
            var loadingLabel = Controls["loadingIndicator"];
            if (loadingLabel != null)
            {
                Controls.Remove(loadingLabel);
                // 重新添加原有控件
                Controls.Add(uiTabControl1);
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ShowErrorMessage), message);
                return;
            }

            var errorLabel = new UILabel
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                ForeColor = Color.Red
            };

            Controls.Clear();
            Controls.Add(errorLabel);
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 异步加载当前IO卡的所有点位信息
        /// 从系统全局的IO点位列表中，筛选出与当前卡名称匹配的点位
        /// 为后续UI控件初始化提供数据基础
        /// </summary>
        private async Task LoadIOPointsAsync(CancellationToken cancellationToken)
        {
            // 使用Task.Run将可能耗时的操作移至后台线程
            await Task.Run(() =>
            {
                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();

                // 检查系统点位列表是否为空，避免空引用异常
                if (IOSystem.Instance.IOCardDios == null) return;

                // 筛选出与当前卡名称匹配的点位，建立卡与点位的关联
                _ioCardPoints = IOSystem.Instance.IOCardDios
                    .Where(point => point.卡名称 == _ioCard.卡名称)
                    .ToList();

                // 再次检查取消请求
                cancellationToken.ThrowIfCancellationRequested();
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步初始化所有UI控件的入口方法
        /// 根据加载的点位信息，分别初始化输入和输出控件
        /// 若没有点位信息，则不进行初始化
        /// </summary>
        private async Task InitializeControlsAsync(CancellationToken cancellationToken)
        {
            // 如果没有点位信息，则不初始化控件，避免无效操作
            if (!_ioCardPoints.Any()) return;
            // 确保在UI线程执行控件初始化
            if (InvokeRequired)
            {
                await Task.Run(() => Invoke(new Action(async () => await InitializeControlsAsync(cancellationToken))));
                return;
            }
            // 并行初始化输入和输出控件
            var inputTask = Task.Run(() => InitializeDigitalInputControls(), cancellationToken);
            var outputTask = Task.Run(() => InitializeDigitalOutputControls(), cancellationToken);

            // 等待两个初始化任务完成
            await Task.WhenAll(inputTask, outputTask).ConfigureAwait(true);
        }

        /// <summary>
        /// 初始化数字输入控件
        /// 根据输入点位数量创建分页面板和按钮，并添加到输入标签页
        /// 输入按钮不可交互，仅用于展示状态
        /// </summary>
        private void InitializeDigitalInputControls()
        {
            // 确保在UI线程执行
            if (InvokeRequired)
            {
                Invoke(new Action(InitializeDigitalInputControls));
                return;
            }

            // 筛选出所有输入类型的点位
            var inputPoints = _ioCardPoints.Where(p => p.点位类型 == "输入").ToList();
            if (!inputPoints.Any()) return; // 无输入点则退出

            // 计算需要的面板数量（每页显示RowCount*ColumnCount个点位）
            int panelCount = (inputPoints.Count - 1) / (RowCount * ColumnCount) + 1;
            var inputPanels = CreateControlPanels(panelCount);

            int pointIndex = 0;
            foreach (var point in inputPoints)
            {
                var dioButton = CreateDIOButton(point, isEnabled: false);
                _digitalInputButtons.Add(dioButton);

                int panelIndex = pointIndex / (RowCount * ColumnCount);
                inputPanels[panelIndex].Controls.Add(dioButton);
                SetDIOButtonPosition(inputPanels[panelIndex], dioButton, pointIndex);

                pointIndex++;
            }

            AddPanelsToTabControl(inputPanels, uiTabControl_输入);
        }

        /// <summary>
        /// 初始化数字输出控件
        /// 根据输出点位数量创建分页面板和按钮，并添加到输出标签页
        /// 输出按钮可交互，点击时触发状态切换
        /// </summary>
        private void InitializeDigitalOutputControls()
        {
            // 确保在UI线程执行
            if (InvokeRequired)
            {
                Invoke(new Action(InitializeDigitalOutputControls));
                return;
            }

            // 筛选出所有输出类型的点位
            var outputPoints = _ioCardPoints.Where(p => p.点位类型 == "输出").ToList();
            if (!outputPoints.Any()) return;

            // 计算需要的面板数量
            int panelCount = (outputPoints.Count - 1) / (RowCount * ColumnCount) + 1;
            var outputPanels = CreateControlPanels(panelCount);

            int pointIndex = 0;
            foreach (var point in outputPoints)
            {
                var dioButton = CreateDIOButton(point, isEnabled: true);
                dioButton.PointClick += DigitalOutput_Click;
                _digitalOutputButtons.Add(dioButton);

                int panelIndex = pointIndex / (RowCount * ColumnCount);
                outputPanels[panelIndex].Controls.Add(dioButton);
                SetDIOButtonPosition(outputPanels[panelIndex], dioButton, pointIndex);

                pointIndex++;
            }

            AddPanelsToTabControl(outputPanels, uiTabControl_输出);
        }

        /// <summary>
        /// 设置DIO按钮在表格面板中的行列位置
        /// 确保按钮在面板中有序排列
        /// </summary>
        private void SetDIOButtonPosition(UITableLayoutPanel panel, DIO button, int pointIndex)
        {
            int localIndex = pointIndex % (RowCount * ColumnCount);
            int row = localIndex / ColumnCount;
            int col = localIndex % ColumnCount;

            panel.SetRow(button, row);
            panel.SetColumn(button, col);
        }

        /// <summary>
        /// 创建指定数量的控件面板
        /// 每个面板采用表格布局，包含指定的行和列
        /// </summary>
        private UITableLayoutPanel[] CreateControlPanels(int count)
        {
            var panels = new UITableLayoutPanel[count];

            for (int i = 0; i < count; i++)
            {
                panels[i] = new UITableLayoutPanel
                {
                    RowCount = RowCount,
                    ColumnCount = ColumnCount,
                    Dock = DockStyle.Fill
                };

                panels[i].ColumnStyles.Clear();
                panels[i].RowStyles.Clear();

                // 配置行样式
                for (int k = 0; k < RowCount; k++)
                {
                    panels[i].RowStyles.Add(new RowStyle(SizeType.Percent, 10));
                }
                panels[i].RowStyles.Add(new RowStyle(SizeType.Absolute, 2));

                // 配置列样式
                for (int k = 0; k < ColumnCount; k++)
                {
                    panels[i].ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
                }
                panels[i].ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 2));
            }

            return panels;
        }

        /// <summary>
        /// 创建DIO按钮控件，封装DIO按钮的创建逻辑
        /// </summary>
        private DIO CreateDIOButton(IOCardDIODto point, bool isEnabled)
        {
            return new DIO
            {
                Dock = DockStyle.Fill,
                Enabled = isEnabled,
                PointName = point.点位名称,
                PointIndex = point.点位索引,
                Margin = new Padding(5)
            };
        }

        /// <summary>
        /// 将创建的面板添加到标签控件，实现分页显示
        /// </summary>
        private void AddPanelsToTabControl(UITableLayoutPanel[] panels, UITabControl tabControl)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<UITableLayoutPanel[], UITabControl>(AddPanelsToTabControl), panels, tabControl);
                return;
            }

            for (int i = 0; i < panels.Length; i++)
            {
                var tabPage = new TabPage
                {
                    BackColor = Color.Transparent,
                    Font = new Font("微软雅黑", 12F, FontStyle.Bold, GraphicsUnit.Point, 134),
                    Name = $"tabPage{i}",
                    Size = new Size(450, 628),
                    TabIndex = i,
                    Text = GetPageText(i),
                    Controls = { panels[i] }
                };

                tabControl.Controls.Add(tabPage);
            }
        }

        /// <summary>
        /// 根据索引获取标签页文本
        /// </summary>
        private string GetPageText(int index)
        {
            return index switch
            {
                0 => FirstPageText,
                1 => SecondPageText,
                2 => ThirdPageText,
                3 => FourthPageText,
                4 => FifthPageText,
                5 => SixthPageText,
                6 => SeventhPageText,
                7 => EighthPageText,
                8 => NinthPageText,
                9 => TenthPageText,
                _ => $"第{index + 1}页"
            };
        }
        #endregion

        #region IO监控
        /// <summary>
        /// 启动IO状态监控任务
        /// </summary>
        private void StartIoMonitoring()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // 在后台线程启动监控任务
            _ = Task.Run(async () => await MonitorIoStatus(token), token);
        }

        /// <summary>
        /// IO状态监控异步方法，核心监控逻辑
        /// </summary>
        private async Task MonitorIoStatus(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // 等待指定的更新间隔，可被取消
                    await Task.Delay(UpdateIntervalMs, cancellationToken).ConfigureAwait(false);

                    if (ShouldStopMonitoring())
                        break;

                    var cardType = GetCardType(_ioCard.卡名称);
                    if (_cardHandlers.TryGetValue(cardType, out var handler))
                    {
                        // 异步读取IO状态
                        var ioStatus = await Task.Run(() =>
                            handler.ReadIoStatus(_ioCard.卡号索引, _digitalInputButtons.Count, _digitalOutputButtons.Count),
                            cancellationToken).ConfigureAwait(false);

                        // 更新UI显示
                        UpdateUiWithIoStatus(ioStatus);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不记录异常
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 检查是否应该停止监控
        /// </summary>
        private bool ShouldStopMonitoring()
        {
            lock (_lockObj)
            {
                return Project.Instance.GlobalVariables.IsTestMode ||
                       Project.Instance.GlobalVariables.IsProgramExiting;
            }
        }

        /// <summary>
        /// 使用IO状态更新UI，统一入口
        /// </summary>
        private void UpdateUiWithIoStatus(IoStatus status)
        {
            if (!IsHandleCreated || status == null) return;

            UpdateDigitalInputUI(status.DigitalInputs);
            UpdateDigitalOutputUI(status.DigitalOutputs);
        }

        /// <summary>
        /// 更新数字输入状态UI
        /// </summary>
        private void UpdateDigitalInputUI(List<bool> inputStatuses)
        {
            if (_digitalInputButtons.Count == 0 || inputStatuses.Count == 0) return;

            for (int i = 0; i < _digitalInputButtons.Count && i < inputStatuses.Count; i++)
            {
                var button = _digitalInputButtons[i];
                var status = inputStatuses[i];
                UpdateControlUI(button, () => button.PointState = status ? 1 : 0);
            }
        }

        /// <summary>
        /// 更新数字输出状态UI
        /// </summary>
        private void UpdateDigitalOutputUI(List<bool> outputStatuses)
        {
            if (_digitalOutputButtons.Count == 0 || outputStatuses.Count == 0) return;

            for (int i = 0; i < _digitalOutputButtons.Count && i < outputStatuses.Count; i++)
            {
                var button = _digitalOutputButtons[i];
                var status = outputStatuses[i];
                UpdateControlUI(button, () => button.PointState = status ? 1 : 0);
            }
        }

        /// <summary>
        /// 在UI线程更新控件，确保线程安全
        /// </summary>
        private void UpdateControlUI(DIO control, Action action)
        {
            if (control.InvokeRequired)
            {
                // 使用异步Invoke避免死锁
                _ = control.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        RLog.SaveError(ex);
                    }
                }));
            }
            else
            {
                action.Invoke();
            }
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 数字输出按钮点击事件处理
        /// </summary>
        private void DigitalOutput_Click(object sender, EventArgs e)
        {
            try
            {
                var dioControl = sender as DIO;
                if (dioControl == null) return;

                // 异步处理点击操作
                _ = ProcessDigitalOutputClickAsync(dioControl);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 异步处理数字输出点击操作
        /// </summary>
        private async Task ProcessDigitalOutputClickAsync(DIO dioControl)
        {
            // 暂时禁用按钮防止重复操作
            dioControl.Enabled = false;

            try
            {
                // 检查是否处于自动模式
                if (MenuProxy.Instance.MachineRunModel == MachineMode.Auto)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, "自动模式下不允许手动操作，请切换到手动后重试！");
                    return;
                }

                // 查找当前按钮对应的点位信息
                var point = await Task.Run(() =>
                    _ioCardPoints.FirstOrDefault(p => p.点位名称 == dioControl.PointName)
                ).ConfigureAwait(true);

                if (point == null)
                {
                    RLog.SaveLog(LogType.Error, LogLevel.Info, Color.Red, $"未找到点位信息：{dioControl.PointName}");
                    return;
                }

                // 获取当前卡类型并查找对应的处理器
                var cardType = GetCardType(_ioCard.卡名称);
                if (_cardHandlers.TryGetValue(cardType, out var handler))
                {
                    bool value = dioControl.PointState == 1;

                    // 异步执行写入操作
                    await Task.Run(() =>
                        handler.WriteDigitalOutput(_ioCard.卡号索引, point.点位索引, value)
                    ).ConfigureAwait(true);

                    // 记录操作日志
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange,
                        $"操作：{_ioCard.卡名称}, {dioControl.PointName}--》当前状态：{dioControl.PointState}");
                }
                else
                {
                    RLog.SaveLog(LogType.Error, LogLevel.Info, Color.Red, $"不支持的卡类型：{cardType}");
                }
            }
            finally
            {
                // 恢复按钮状态
                dioControl.Enabled = true;
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 根据卡名称获取卡类型
        /// </summary>
        private CardType GetCardType(string cardName)
        {
            if (string.IsNullOrEmpty(cardName)) return CardType.Unknown;

            if (cardName.IndexOf("3400", StringComparison.OrdinalIgnoreCase) >= 0)
                return CardType.Card3400;
            if (cardName.IndexOf("3600", StringComparison.OrdinalIgnoreCase) >= 0)
                return CardType.Card3600;
            if (cardName.IndexOf("7230", StringComparison.OrdinalIgnoreCase) >= 0)
                return CardType.Card7230;
            if (cardName.IndexOf("7432", StringComparison.OrdinalIgnoreCase) >= 0)
                return CardType.Card7432;
            if (cardName.IndexOf("M60", StringComparison.OrdinalIgnoreCase) >= 0)
                return CardType.CardM60;
            if (cardName.IndexOf("EC_8003", StringComparison.OrdinalIgnoreCase) >= 0)
                return CardType.CardEC8003;

            return CardType.Unknown;
        }

        /// <summary>
        /// 异步更新权限设置
        /// </summary>
        private async Task UpdatePermissionAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                // 实际项目中应根据具体权限项进行控制
                // this.toolStripButton_IOControl.Visible = PermissionSystem.CheckPermission("手动/IO/监控", out _);
            }, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region 卡处理器实现
        /// <summary>
        /// 3400型号IO卡的处理器
        /// </summary>
        private class Card3400Handler : IIoCardHandler
        {
            public IoStatus ReadIoStatus(ushort cardIndex, int inputCount, int outputCount)
            {
                return ReadLTDMCCardStatus(cardIndex);
            }

            public void WriteDigitalOutput(ushort cardIndex, ushort pointIndex, bool value)
            {
                LTDMC.dmc_write_outbit(cardIndex, pointIndex, value ? (ushort)1 : (ushort)0);
            }
        }

        /// <summary>
        /// 3600型号IO卡的处理器
        /// </summary>
        private class Card3600Handler : IIoCardHandler
        {
            public IoStatus ReadIoStatus(ushort cardIndex, int inputCount, int outputCount)
            {
                return ReadLTDMCCardStatus(cardIndex);
            }

            public void WriteDigitalOutput(ushort cardIndex, ushort pointIndex, bool value)
            {
                LTDMC.dmc_write_outbit(cardIndex, pointIndex, value ? (ushort)1 : (ushort)0);
            }
        }

        /// <summary>
        /// 7230型号IO卡的处理器
        /// </summary>
        private class Card7230Handler : IIoCardHandler
        {
            public IoStatus ReadIoStatus(ushort cardIndex, int inputCount, int outputCount)
            {
                return ReadDASKCardStatus(cardIndex);
            }

            public void WriteDigitalOutput(ushort cardIndex, ushort pointIndex, bool value)
            {
                DASK.DO_WriteLine(0, cardIndex, pointIndex, value ? (ushort)1 : (ushort)0);
            }
        }

        /// <summary>
        /// 7432型号IO卡的处理器
        /// </summary>
        private class Card7432Handler : IIoCardHandler
        {
            public IoStatus ReadIoStatus(ushort cardIndex, int inputCount, int outputCount)
            {
                return ReadDASKCardStatus(cardIndex);
            }

            public void WriteDigitalOutput(ushort cardIndex, ushort pointIndex, bool value)
            {
                DASK.DO_WriteLine(0, cardIndex, pointIndex, value ? (ushort)1 : (ushort)0);
            }
        }

        /// <summary>
        /// M60型号IO卡的处理器
        /// </summary>
        private class CardM60Handler : IIoCardHandler
        {
            public IoStatus ReadIoStatus(ushort cardIndex, int inputCount, int outputCount)
            {
                var status = new IoStatus();

                int chnBegin = (cardIndex - 1) * 32 + 1;
                uint diValue = 0;
                ecat_motion.M_Get_Digital_Port_Input((short)chnBegin, ref diValue, 0);
                status.DigitalInputs = ConvertToBoolList(diValue);

                uint doValue = 0;
                ecat_motion.M_Get_Digital_Port_Output((short)chnBegin, ref doValue, 0);
                status.DigitalOutputs = ConvertToBoolList(doValue);
                return status;
            }

            public void WriteDigitalOutput(ushort cardIndex, ushort pointIndex, bool value)
            {
                short m60Chn = (short)(32 * (cardIndex - 1) + pointIndex + 1);
                ecat_motion.M_Set_Digital_Chn_Output(m60Chn,
                   value ? (short)0 : (short)1, 0);
            }
        }

        /// <summary>
        /// EC8003型号IO卡的处理器
        /// </summary>
        private class CardEC8003Handler : IIoCardHandler
        {
            public IoStatus ReadIoStatus(ushort cardIndex, int inputCount, int outputCount)
            {
                var status = new IoStatus();

                var diBuffer = new byte[inputCount];
                int diCount = 0;
                ecat_io.EC_Get_Digital_In(ref diBuffer[0], ref diCount, (short)cardIndex);
                status.DigitalInputs = diBuffer.Take(diCount).Select(b => Convert.ToBoolean(b)).ToList();

                var doBuffer = new byte[outputCount];
                int doCount = 0;
                ecat_io.EC_Get_Digital_Out(ref doBuffer[0], ref doCount, (short)cardIndex);
                status.DigitalOutputs = doBuffer.Take(doCount).Select(b => Convert.ToBoolean(b)).ToList();

                return status;
            }

            public void WriteDigitalOutput(ushort cardIndex, ushort pointIndex, bool value)
            {
                ecat_io.EC_Set_Digital_Chn_Out(
                    1,
                    (short)(pointIndex + 1),
                    value ? (short)1 : (short)0,
                    (short)cardIndex
                );
            }
        }

        #region 卡处理辅助方法
        private static IoStatus ReadLTDMCCardStatus(ushort cardIndex)
        {
            var status = new IoStatus();

            uint inputValue = LTDMC.dmc_read_inport(cardIndex, 0);
            status.DigitalInputs = ConvertToBoolList(inputValue).Select(b => !b).ToList();

            uint outputValue = LTDMC.dmc_read_outport(cardIndex, 0);
            status.DigitalOutputs = ConvertToBoolList(outputValue).Select(b => !b).ToList();

            return status;
        }

        private static IoStatus ReadDASKCardStatus(ushort cardIndex)
        {
            var status = new IoStatus();

            DASK.DI_ReadPort(0, cardIndex, out uint inputValue);
            status.DigitalInputs = ConvertToBoolList(inputValue);

            DASK.DO_ReadPort(0, cardIndex, out uint outputValue);
            status.DigitalOutputs = ConvertToBoolList(outputValue);

            return status;
        }

        private static List<bool> ConvertToBoolList(uint value)
        {
            return new BitArray(BitConverter.GetBytes(value))
                .OfType<bool>()
                .ToList();
        }
        #endregion
        #endregion
    }

    /// <summary>
    /// 卡类型枚举
    /// </summary>
    public enum CardType
    {
        /// <summary>未知类型的卡，默认值</summary>
        Unknown,
        /// <summary>3400型号的IO卡</summary>
        [Description("3400")]
        Card3400,
        /// <summary>3600型号的IO卡</summary>
        [Description("3600")]
        Card3600,
        /// <summary>7230型号的IO卡</summary>
        [Description("7230")]
        Card7230,
        /// <summary>7432型号的IO卡</summary>
        [Description("7432")]
        Card7432,
        /// <summary>M60型号的IO卡</summary>
        [Description("M60")]
        CardM60,
        /// <summary>EC_8003型号的IO卡</summary>
        [Description("EC_8003")]
        CardEC8003
    }

    /// <summary>
    /// IO状态模型类
    /// </summary>
    public class IoStatus
    {
        /// <summary>卡名称</summary>
        public string CardName { get; set; }
        /// <summary>数字输入点状态列表</summary>
        public List<bool> DigitalInputs { get; set; } = new List<bool>();
        /// <summary>数字输出点状态列表</summary>
        public List<bool> DigitalOutputs { get; set; } = new List<bool>();
    }

    /// <summary>
    /// IO卡操作接口
    /// </summary>
    public interface IIoCardHandler
    {
        /// <summary>
        /// 读取IO卡的输入输出状态
        /// </summary>
        IoStatus ReadIoStatus(ushort cardIndex, int inputCount, int outputCount);

        /// <summary>
        /// 写入数字输出点状态
        /// </summary>
        void WriteDigitalOutput(ushort cardIndex, ushort pointIndex, bool value);
    }
}
