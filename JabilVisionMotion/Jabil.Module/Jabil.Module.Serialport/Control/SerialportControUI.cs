using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;
using Jabil.Module.Log.Lib;
using Jabil.Lib;
using Jabil.Module.Serialport.Common.Dtos;
using Jabil.Module.Serialport.Frm;
using Jabil.Module.Serialport.Lib;
using System.Text;

namespace Jabil.Module.Serialport.Control
{
    /// <summary>
    /// 串口控制UI控件
    /// 负责显示和管理串口的DI(数字输入)和DO(数字输出)点位
    /// </summary>
    public partial class SerialportControUI : UIUserControl
    {
        #region 常量定义
        private const int DefaultRowCount = 7;
        private const int DefaultColumnCount = 4;
        private const int UpdateIntervalMs = 300; // 状态更新间隔(毫秒)
        #endregion

        #region 控件集合
        private readonly List<DIO> _diControls = new List<DIO>();
        private readonly List<DIO> _doControls = new List<DIO>();
        #endregion

        #region 数据模型
        private readonly SerialportDto _serialportConfig;
        private readonly List<SerialportDIODto> _dioConfigList = new List<SerialportDIODto>();
        private string _outputMessage;
        #endregion

        #region 线程安全相关
        private static readonly object _lockObj = new object();
        private bool _isRunning = true;
        #endregion

        /// <summary>
        /// 初始化SerialportControlUI实例
        /// </summary>
        /// <param name="config">串口配置信息</param>
        public SerialportControUI(SerialportDto config)
        {
            _serialportConfig = config ?? throw new ArgumentNullException(nameof(config));

            try
            {
                InitializeComponent();
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        private void SerialportControlUI_Load(object sender, EventArgs e)
        {
            try
            {
                LoadDioConfig();
                InitializeTableLayouts();
                CreateDioControls();
                StartSerialportMonitoring();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 加载DIO配置信息
        /// </summary>
        private void LoadDioConfig()
        {
            _dioConfigList.Clear();

            if (SerialportSystem.Instance?.SerialportDIOList == null) return;

            var matchedConfigs = SerialportSystem.Instance.SerialportDIOList
                .Where(dio => dio.COM口 == _serialportConfig.COM口)
                .ToList();

            _dioConfigList.AddRange(matchedConfigs);
        }

        /// <summary>
        /// 初始化表格布局
        /// </summary>
        private void InitializeTableLayouts()
        {
            // 根据串口配置确定行列数
            var (rowCount, columnCount) = GetLayoutDimensions();

            // 初始化DI表格
            InitializeTableLayout(tbl_DI, rowCount, columnCount);

            // 初始化DO表格
            InitializeTableLayout(tbl_DO, rowCount, columnCount);

            tbl_DI.Visible = true;
            tbl_DO.Visible = true;
        }

        /// <summary>
        /// 获取表格布局的行列数
        /// </summary>
        private (int rows, int columns) GetLayoutDimensions()
        {
            // 可根据不同串口配置返回不同的行列数
            return _serialportConfig.COM口 switch
            {
                "COM1" => (DefaultRowCount, DefaultColumnCount),
                _ => (DefaultRowCount, DefaultColumnCount)
            };
        }

        /// <summary>
        /// 初始化表格布局控件
        /// </summary>
        private void InitializeTableLayout(TableLayoutPanel table, int rowCount, int columnCount)
        {
            table.SuspendLayout();

            // 清除原有样式
            table.ColumnStyles.Clear();
            table.RowStyles.Clear();

            // 设置行列数
            table.RowCount = rowCount;
            table.ColumnCount = columnCount;

            // 添加行样式
            for (int i = 0; i < rowCount; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
            }

            // 添加列样式
            for (int i = 0; i < columnCount; i++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columnCount));
            }

            table.ResumeLayout(false);
        }

        /// <summary>
        /// 创建DIO控件并添加到表格
        /// </summary>
        private void CreateDioControls()
        {
            // 清除现有控件
            _diControls.Clear();
            _doControls.Clear();
            tbl_DI.Controls.Clear();
            tbl_DO.Controls.Clear();

            foreach (var dioConfig in _dioConfigList)
            {
                var dioControl = CreateDioControl(dioConfig);

                if (dioConfig.点位类型 == "输入")
                {
                    AddToTable(tbl_DI, dioControl, _diControls);
                }
                else if (dioConfig.点位类型 == "输出")
                {
                    dioControl.ControlClick += DoControl_Click;
                    AddToTable(tbl_DO, dioControl, _doControls);
                }
            }
        }

        /// <summary>
        /// 创建单个DIO控件
        /// </summary>
        private DIO CreateDioControl(SerialportDIODto config)
        {
            return new DIO
            {
                Dock = DockStyle.Fill,
                Enabled = config.点位类型 == "输出", // 只有输出类型可交互
                PointName = config.点位名称,
                MessageFormat = config.通讯格式定义
            };
        }

        /// <summary>
        /// 将控件添加到表格并记录到集合
        /// </summary>
        private void AddToTable(TableLayoutPanel table, DIO control, List<DIO> controlList)
        {
            table.Controls.Add(control);
            controlList.Add(control);
        }

        /// <summary>
        /// 启动串口监控任务
        /// </summary>
        private void StartSerialportMonitoring()
        {
            _isRunning = true;
            _ = MonitorSerialportAsync();
        }

        /// <summary>
        /// 异步监控串口状态
        /// </summary>
        private async Task MonitorSerialportAsync()
        {
            try
            {
                while (_isRunning)
                {
                    await Task.Delay(UpdateIntervalMs);

                    // 检查程序退出状态
                    if (Project.Instance?.GlobalVariables.IsProgramExiting == true ||
                        Project.Instance?.GlobalVariables.IsTestMode == true)
                    {
                        break;
                    }

                    // 线程安全地更新UI
                    if (IsHandleCreated)
                    {
                        Invoke(new Action(UpdateDioStates));
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 更新所有DIO控件状态
        /// </summary>
        private void UpdateDioStates()
        {
            lock (_lockObj)
            {
                UpdateDiStates();
                UpdateDoStates();
            }
        }

        /// <summary>
        /// 更新DI控件状态
        /// </summary>
        private void UpdateDiStates()
        {
            var serialPort = GetTargetSerialPort();
            if (serialPort == null) return;

            var receivedData = Encoding.ASCII.GetString(serialPort.ReceivedData)
                .Split(new[] { '@' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var diControl in _diControls)
            {
                if (!diControl.Created || Project.Instance?.GlobalVariables.IsProgramExiting == true)
                    break;

                diControl.PointState = receivedData.Any(data => diControl.MessageFormat.Contains(data));
            }
        }

        /// <summary>
        /// 更新DO控件状态
        /// </summary>
        private void UpdateDoStates()
        {
            var serialPort = GetTargetSerialPort();
            if (serialPort == null || string.IsNullOrEmpty(_outputMessage)) return;

            foreach (var doControl in _doControls)
            {
                if (!doControl.Created || Project.Instance?.GlobalVariables.IsProgramExiting == true)
                    break;

                doControl.PointState = doControl.MessageFormat.Contains(_outputMessage);
            }
        }

        /// <summary>
        /// 获取目标串口对象
        /// </summary>
        private SerialPortCommunicator GetTargetSerialPort()
        {
            return SerialportSystem.Instance?.SerialPorts?
                .FirstOrDefault(port => port.PortName == _serialportConfig.COM口);
        }

        /// <summary>
        /// DO控件点击事件处理
        /// </summary>
        private void DoControl_Click(object sender, EventArgs e)
        {
            if (sender is DIO DIO)
            {
                try
                {
                    DIO.Enabled = false;
                    SendDoCommand(DIO);
                }
                finally
                {
                    DIO.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 发送DO控制命令
        /// </summary>
        private void SendDoCommand(DIO doControl)
        {
            var targetPort = GetTargetSerialPort();
            if (targetPort == null) return;

            // 记录输出报文
            _outputMessage = doControl.MessageFormat;

            // 发送数据
            targetPort.SendData(_outputMessage, SerialPortCommunicator.SendFormat.String);

            // 记录日志
            RLog.SaveLog(LogType.ProductionData, LogLevel.Info, Color.Orange,
                $"输出--->{doControl.PointName}--->>>{_outputMessage}");
        }
    }
}
