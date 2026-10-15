using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Sunny.UI;
using Jabil.Module.Log.Lib;
using Jabil.Lib;
using Jabil.Module.TCPIP.Common.Dtos;
using Jabil.Module.TCPIP.Lib;
using Jabil.Module.Permission;
using System.Linq;

namespace Jabil.Module.TCPIP.Control
{
    /// <summary>
    /// TCPIP设备控制界面，用于展示和操作DIO点位
    /// </summary>
    public partial class TcpIpControlUI : UIUserControl
    {
        #region 常量定义
        private const int RowCount = 6;                  // 每行显示的控件数量
        private const int ColumnCount = 2;               // 每列显示的控件数量
        private const int UpdateIntervalMs = 300;        // 状态更新间隔(毫秒)
        private const string FirstPageText = "第一页";
        private const string SecondPageText = "第二页";
        private const string ThirdPageText = "第三页";
        private const string FourthPageText = "第四页";
        private const string FifthPageText = "第五页";
        #endregion

        #region 私有字段
        private readonly TcpIpCommunicationDto _deviceConfig;  // 设备配置信息
        private List<TcpIpCommunicationDioDto> _dioPoints;      // DIO点位列表
        private List<DIO> _inputDioControls;             // 输入DIO控件列表
        private List<DIO> _outputDioControls;            // 输出DIO控件列表
        private string _outputMessage;                          // 输出报文缓存
        private readonly object _lockObj = new object();        // 线程同步锁
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化TCPIP控制界面
        /// </summary>
        /// <param name="deviceConfig">设备配置信息</param>
        public TcpIpControlUI(TcpIpCommunicationDto deviceConfig)
        {
            _deviceConfig = deviceConfig ?? throw new ArgumentNullException(nameof(deviceConfig));

            try
            {
                InitializeComponent();
                InitializeControlStyles();

                // 初始化集合
                _dioPoints = new List<TcpIpCommunicationDioDto>();
                _inputDioControls = new List<DIO>();
                _outputDioControls = new List<DIO>();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 设置控件样式以优化绘制性能
        /// </summary>
        private void InitializeControlStyles()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();
        }

        /// <summary>
        /// 加载控件时初始化界面
        /// </summary>
        private void TcpIpControlUI_Load(object sender, EventArgs e)
        {
            try
            {
                LoadDioPoints();
                LoadDioControls();
                StartTcpIpMonitoring();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 加载DIO点位数据
        /// </summary>
        private void LoadDioPoints()
        {
            if (TcpIpSystem.Instance.TcpIpCommunicationDios == null) return;

            _dioPoints = TcpIpSystem.Instance.TcpIpCommunicationDios
                .Where(p => p.点位名称 == _deviceConfig.设备名称)
                .ToList();
        }

        /// <summary>
        /// 加载DIO控件到界面
        /// </summary>
        private void LoadDioControls()
        {
            if (_dioPoints.Count == 0) return;

            // 加载输入DIO控件
            var inputPoints = _dioPoints.Where(p => p.点位类型 == "输入").ToList();
            LoadDioControlsToTabControl(inputPoints, uiTabControl_Inputs, false, out _inputDioControls);

            // 加载输出DIO控件
            var outputPoints = _dioPoints.Where(p => p.点位类型 == "输出").ToList();
            LoadDioControlsToTabControl(outputPoints, uiTabControl_Outputs, true, out _outputDioControls);
        }

        /// <summary>
        /// 通用方法：将DIO控件加载到TabControl
        /// </summary>
        /// <param name="points">DIO点位列表</param>
        /// <param name="tabControl">目标TabControl</param>
        /// <param name="isEnabled">控件是否启用</param>
        /// <param name="dioControls">输出的DIO控件列表</param>
        private void LoadDioControlsToTabControl(List<TcpIpCommunicationDioDto> points,
                                                UITabControl tabControl,
                                                bool isEnabled,
                                                out List<DIO> dioControls)
        {
            dioControls = new List<DIO>();
            if (points.Count == 0) return;

            // 计算需要的页数
            int totalPages = (points.Count - 1) / (RowCount * ColumnCount) + 1;
            var panels = CreateTableLayoutPanels(totalPages);

            // 填充控件
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var dioControl = CreateDioControl(point, isEnabled);

                if (isEnabled)
                {
                    dioControl.ControlClick += OutputDio_Click;
                }

                // 计算所在页索引
                int pageIndex = i / (RowCount * ColumnCount);
                panels[pageIndex].Controls.Add(dioControl);
                dioControls.Add(dioControl);
            }

            // 添加面板到TabControl
            AddPanelsToTabControl(panels, tabControl);
        }

        /// <summary>
        /// 创建DIO控件
        /// </summary>
        private DIO CreateDioControl(TcpIpCommunicationDioDto point, bool isEnabled)
        {
            return new DIO
            {
                Dock = DockStyle.Fill,
                Enabled = isEnabled,
                PointName = point.点位名称,
                MessageFormat = point.通讯格式定义,
                PointState = false
            };
        }

        /// <summary>
        /// 创建表格布局面板
        /// </summary>
        private UITableLayoutPanel[] CreateTableLayoutPanels(int count)
        {
            var panels = new UITableLayoutPanel[count];

            for (int i = 0; i < count; i++)
            {
                var panel = new UITableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = RowCount,
                    ColumnCount = ColumnCount
                };

                // 配置行样式
                for (int k = 0; k < RowCount; k++)
                {
                    panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / RowCount));
                }

                // 配置列样式
                for (int k = 0; k < ColumnCount; k++)
                {
                    panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / ColumnCount));
                }

                panels[i] = panel;
            }

            return panels;
        }

        /// <summary>
        /// 将面板添加到TabControl
        /// </summary>
        private void AddPanelsToTabControl(UITableLayoutPanel[] panels, UITabControl tabControl)
        {
            tabControl.Controls.Clear();

            for (int i = 0; i < panels.Length; i++)
            {
                var tabPage = new TabPage
                {
                    BackColor = Color.Transparent,
                    Font = new Font("微软雅黑", 12F, FontStyle.Bold, GraphicsUnit.Point, 134),
                    Name = $"tabPage_{i}",
                    Size = new Size(450, 628),
                    TabIndex = i,
                    Text = GetPageText(i),
                    Controls = { panels[i] }
                };

                tabControl.Controls.Add(tabPage);
            }
        }

        /// <summary>
        /// 获取分页文本
        /// </summary>
        private string GetPageText(int pageIndex)
        {
            return pageIndex switch
            {
                0 => FirstPageText,
                1 => SecondPageText,
                2 => ThirdPageText,
                3 => FourthPageText,
                4 => FifthPageText,
                _ => $"第{pageIndex + 1}页"
            };
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 输出DIO控件点击事件
        /// </summary>
        private void OutputDio_Click(object sender, EventArgs e)
        {
            if (sender is DIO DIO)
            {

                try
                {
                    // 切换到UI线程执行
                    Invoke(new Action(() => ProcessOutputDioClick(DIO)));
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex);
                }

            }

        }

        /// <summary>
        /// 处理输出DIO点击逻辑
        /// </summary>
        private void ProcessOutputDioClick(DIO dioControl)
        {
            // 临时禁用控件防止重复点击
            dioControl.Enabled = false;

            try
            {
                // 检查运行模式
                if (MenuProxy.Instance.MachineRunModel == MachineMode.Auto)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange,
                                "自动模式下不允许手动操作，请切换到手动后重试！");
                    return;
                }

                // 查找对应的DIO点位
                var targetPoint = _dioPoints.FirstOrDefault(p =>
                    p.点位名称 == dioControl.PointName && p.点位类型 == "输出");

                if (targetPoint == null) return;

                // 发送数据到TCP服务器
                SendDataToTcpServer(dioControl.MessageFormat);

                // 记录操作日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange,
                            $"输出--->{dioControl.PointName}--->>>{dioControl.MessageFormat}");
            }
            finally
            {
                // 恢复控件状态
                dioControl.Enabled = true;
            }
        }
        #endregion

        #region TCPIP通信
        /// <summary>
        /// 启动TCPIP监控线程
        /// </summary>
        private void StartTcpIpMonitoring()
        {
            _ = Task.Run(async () => await MonitorTcpIpStatusAsync(),
                         CancellationToken.None);
        }

        /// <summary>
        /// 监控TCPIP状态并更新界面
        /// </summary>
        private async Task MonitorTcpIpStatusAsync()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(UpdateIntervalMs);

                    lock (_lockObj)
                    {
                        // 程序退出时终止循环
                        if (Project.Instance.GlobalVariables.IsProgramExiting)
                            break;

                        // 确保控件已创建句柄
                        if (!IsHandleCreated) continue;

                        // 在UI线程更新界面
                        WFUtil.UpdateMainThreadUIAsync(this, UpdateDioStates);
                    }
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex);
                    await Task.Delay(UpdateIntervalMs);
                }
            }
        }

        /// <summary>
        /// 更新DIO控件状态
        /// </summary>
        private void UpdateDioStates()
        {
            UpdateInputDioStates();
            UpdateOutputDioStates();
        }

        /// <summary>
        /// 更新输入DIO状态
        /// </summary>
        private void UpdateInputDioStates()
        {
            if (_inputDioControls.Count == 0) return;

            var server = GetTargetTcpServer();
            if (server == null || server._clients.Count == 0) return;

            string receivedData = server.ReceiveString;
            if (string.IsNullOrEmpty(receivedData)) return;

            // 解析接收的数据
            string[] dataParts = receivedData.Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
            if (dataParts.Length == 0) return;

            foreach (var dioControl in _inputDioControls)
            {
                if (Project.Instance.GlobalVariables.IsProgramExiting) break;
                if (!dioControl.Created) continue;

                dioControl.PointState = dioControl.MessageFormat.Contains(dataParts[0]);
            }
        }

        /// <summary>
        /// 更新输出DIO状态
        /// </summary>
        private void UpdateOutputDioStates()
        {
            if (_outputDioControls.Count == 0 || string.IsNullOrEmpty(_outputMessage)) return;

            var server = GetTargetTcpServer();
            if (server == null || server._clients.Count == 0) return;

            foreach (var dioControl in _outputDioControls)
            {
                if (Project.Instance.GlobalVariables.IsProgramExiting) break;
                if (!dioControl.Created) continue;

                dioControl.PointState = dioControl.MessageFormat.Contains(_outputMessage);
            }
        }

        /// <summary>
        /// 发送数据到TCP服务器
        /// </summary>
        private void SendDataToTcpServer(string message)
        {
            var server = GetTargetTcpServer();
            if (server == null || server._clients.Count == 0) return;

            server.SendData(System.Text.Encoding.Default.GetBytes(message));
            _outputMessage = message;
        }

        /// <summary>
        /// 获取目标TCP服务器
        /// </summary>
        private TCPServer GetTargetTcpServer()
        {
            return TcpIpSystem.Instance.TcpServers
                ?.FirstOrDefault(s => s.Name == _deviceConfig.设备名称);
        }
        #endregion
    }
}
