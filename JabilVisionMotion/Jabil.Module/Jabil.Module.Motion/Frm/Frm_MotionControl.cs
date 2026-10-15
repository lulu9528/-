using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Lib;
using Jabil.Module.Motion.Service;
using Jabil.Module.Permission;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Motion.Frm
{
    /// <summary>
    /// 运动控制页面类
    /// 继承自UIPage（SunnyUI框架的页面基类）
    /// 功能：实现对运动轴的控制与监控，包括点位设置、手动/自动移动、速度调节、回原点等操作
    /// </summary>
    public partial class Frm_MotionControl : UIPage, IDisposable
    {
        #region 常量定义
        // 速度比例的显示格式字符串，用于UI展示（例如："速度比例：+100%"）
        private const string SpeedRatioFormat = "速度比例：+{0}%";
        // 快速步径的可选值集合，用于快速选择移动步长
        private readonly double[] _quickStepOptions = { 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5 };
        // 预设速度比例常量（百分比）
        private const int LowSpeedRatio = 10;    // 低速比例
        private const int MediumSpeedRatio = 50; // 中速比例
        private const int HighSpeedRatio = 100;  // 高速比例
        // 回原点状态检查间隔（毫秒）
        private const int HomeCheckInterval = 100;
        // 最大移动速度限制（百分比）
        private int MaxSpeedLimit = 100;
        #endregion

        #region 字段与属性
        // 存储当前筛选出的点位信息列表（根据点位类型和定义筛选）
        private readonly List<MotionPointDto> _listAxisPoint = new List<MotionPointDto>();
        // 当前选中的点位数据对象，用于获取/设置点位参数
        private MotionPointDto _currentMotionPoint;
        // 存储各轴（X/Y/Z/U/R）的参数信息字典，键为轴类型名称，值为轴参数对象
        private readonly Dictionary<string, MotionAxisDto> _axisParameters = new Dictionary<string, MotionAxisDto>
        {
            { "X轴", new MotionAxisDto() },
            { "Y轴", new MotionAxisDto() },
            { "Z轴", new MotionAxisDto() },
            { "U轴", new MotionAxisDto() },
            { "R轴", new MotionAxisDto() }
        };
        // 当前选中的轴参数对象，用于单轴操作（如使能、回原点）
        private MotionAxisDto _selectedAxisParam;
        // 选中的轴移动步径值（单位根据实际场景定义，如mm）
        public double SelectedAxisStep { get; set; } = 1;
        // 选中的轴移动速度值（单位根据实际场景定义，如mm/s）
        public double SelectedAxisSpeed { get; set; } = 5;
        // 回原点操作状态标记，用于判断是否正在执行回原点
        private bool _isHomed = false;
        // 运动状态监控定时器
        private readonly System.Threading.Timer _motionStatusTimer;
        // 同步锁对象，用于线程安全操作
        private readonly object _lockObj = new object();
        // 运动操作取消令牌源，用于取消长时间运行的操作
        private CancellationTokenSource _cancellationTokenSource;

        private readonly ToolTip _toolTip = new ToolTip();

        // 单例模式实现：保证页面全局唯一实例
        private static readonly Lazy<Frm_MotionControl> _lazyInstance = new Lazy<Frm_MotionControl>(() => new Frm_MotionControl());
        public static Frm_MotionControl Instance => _lazyInstance.Value;
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 构造函数：初始化页面组件与基础设置
        /// </summary>
        public Frm_MotionControl()
        {
            InitializeComponent(); // 初始化设计器生成的组件（如按钮、输入框等）
            // 启用双缓冲和WM_PAINT绘制优化，减少UI刷新时的闪烁
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            // 初始化定时器
            timer_Home.Interval = HomeCheckInterval;
            _motionStatusTimer = new System.Threading.Timer(UpdateMotionStatus, null, 100, 500);
            InitializeQuickStepList(); // 初始化快速步径选择列表
            InitializeEvents(); // 集中绑定所有控件事件
        }

        /// <summary>
        /// 初始化快速步径列表（UI下拉框数据填充）
        /// </summary>
        private void InitializeQuickStepList()
        {
            uiListBox_快速步径.Items.Clear(); // 清空现有项
            // 将预设的步径值添加到列表中
            foreach (var step in _quickStepOptions)
            {
                uiListBox_快速步径.Items.Add(step.ToString());
            }
            // 默认选中步径1
            uiListBox_快速步径.SelectedItem = "1";
        }

        /// <summary>
        /// 集中初始化所有控件的事件绑定
        /// 好处：便于事件管理与维护，避免分散在代码各处
        /// </summary>
        private void InitializeEvents()
        {
            // 页面生命周期事件
            Load += Frm_MotionControl_Load; // 页面加载时触发
            FormClosed += Frm_Motion_FormClosed; // 页面关闭时触发

            // 点位相关控件事件
            uiComboBox_选中点位名称.SelectedIndexChanged += uiComboBox_选中点位名称_SelectedIndexChanged; // 选中点位变化
            uiListBox_点位类型.SelectedIndexChanged += uiListBox_点位类型_SelectedIndexChanged; // 点位类型选择变化
            uiListBox_点位定义.SelectedIndexChanged += uiListBox_点位定义_SelectedIndexChanged; // 点位定义选择变化
            uiDataGridView_点位资讯.CellMouseClick += uiDataGridView_点位资讯_CellMouseClick; // 点位表格点击

            // 功能按钮事件
            uiButton_获取.Click += uiButton_获取_Click; // "获取"按钮（获取当前位置）
            uiButton_设置.Click += uiButton_设置_Click; // "设置"按钮（保存点位参数）
            uiButton_移动.Click += uiButton_移动_Click; // "移动"按钮（执行轴移动）
            uiButton_急停.Click += uiButton_急停_Click; // "急停"按钮（停止当前运动）
            uiButton_使能.Click += uiButton_使能_Click; // "使能"按钮（切换轴使能状态）
            uiButton_回原点.Click += uiButton_回原点_Click; // "回原点"按钮（执行回原点操作）
            uiButton_报警复位.Click += uiButton_报警复位_Click; // "报警复位"按钮

            // 单选按钮事件（速度/位置切换、速度预设）
            uiRadioButton_Value.CheckedChanged += uiRadioButton_Value_CheckedChanged; // 位置模式选择
            uiRadioButton_Speed.CheckedChanged += uiRadioButton_Speed_CheckedChanged; // 速度模式选择
            uiRadioButton_低速.CheckedChanged += uiRadioButton_速度预设_CheckedChanged; // 低速预设
            uiRadioButton_中速.CheckedChanged += uiRadioButton_速度预设_CheckedChanged; // 中速预设
            uiRadioButton_高速.CheckedChanged += uiRadioButton_速度预设_CheckedChanged; // 高速预设
            uiRadioButton_速度自定义.CheckedChanged += uiRadioButton_速度自定义_CheckedChanged; // 自定义速度

            // 数值输入控件事件
            uiDoubleUpDown_步径.ValueChanged += uiDoubleUpDown_步径_ValueChanged; // 步径输入变化
            uiDoubleUpDown_速度.ValueChanged += uiDoubleUpDown_速度_ValueChanged; // 速度输入变化
            uiTrackBar_速度比例.ValueChanged += uiTrackBar_速度比例_ValueChanged; // 速度比例滑块变化

            // 轴选择与步径选择事件
            uiListBox_选中运动轴.SelectedIndexChanged += uiListBox_选中运动轴_SelectedIndexChanged; // 选中轴变化
            uiListBox_快速步径.SelectedIndexChanged += uiListBox_快速步径_SelectedIndexChanged; // 快速步径选择变化

            // 选项卡切换事件
            uiTabControl_功能选择.SelectedIndexChanged += uiTabControl_功能选择_SelectedIndexChanged; // 切换"轴运动控制"和"轴状态"标签页

            // 定时器事件（用于回原点过程监控）
            timer_Home.Tick += timer_Home_Tick;

            // 轴移动按钮事件（使用lambda表达式统一绑定到HandleAxisMovement方法）
            uiImageButton_X轴_正向移动.Click += (s, e) => HandleAxisMovement(s, "X轴", true);
            uiImageButton_X轴_负向移动.Click += (s, e) => HandleAxisMovement(s, "X轴", false);
            uiImageButton_Y轴_正向移动.Click += (s, e) => HandleAxisMovement(s, "Y轴", true);
            uiImageButton_Y轴_负向移动.Click += (s, e) => HandleAxisMovement(s, "Y轴", false);
            uiImageButton_Z轴_正向移动.Click += (s, e) => HandleAxisMovement(s, "Z轴", true);
            uiImageButton_Z轴_负向移动.Click += (s, e) => HandleAxisMovement(s, "Z轴", false);
            uiImageButton_U轴_正向移动.Click += (s, e) => HandleAxisMovement(s, "U轴", true);
            uiImageButton_U轴_负向移动.Click += (s, e) => HandleAxisMovement(s, "U轴", false);
            uiImageButton_R轴_正向移动.Click += (s, e) => HandleAxisMovement(s, "R轴", true);
            uiImageButton_R轴_负向移动.Click += (s, e) => HandleAxisMovement(s, "R轴", false);

            // 轴位置和速度输入框变化事件（统一绑定到UpdateAxisValue方法）
            uiDoubleUpDown_X_Value.ValueChanged += (s, e) => UpdateAxisValue(s, "X轴", true);
            uiDoubleUpDown_X_Speed.ValueChanged += (s, e) => UpdateAxisValue(s, "X轴", false);
            uiDoubleUpDown_Y_Value.ValueChanged += (s, e) => UpdateAxisValue(s, "Y轴", true);
            uiDoubleUpDown_Y_Speed.ValueChanged += (s, e) => UpdateAxisValue(s, "Y轴", false);
            uiDoubleUpDown_Z_Value.ValueChanged += (s, e) => UpdateAxisValue(s, "Z轴", true);
            uiDoubleUpDown_Z_Speed.ValueChanged += (s, e) => UpdateAxisValue(s, "Z轴", false);
            uiDoubleUpDown_U_Value.ValueChanged += (s, e) => UpdateAxisValue(s, "U轴", true);
            uiDoubleUpDown_U_Speed.ValueChanged += (s, e) => UpdateAxisValue(s, "U轴", false);
            uiDoubleUpDown_R_Value.ValueChanged += (s, e) => UpdateAxisValue(s, "R轴", true);
            uiDoubleUpDown_R_Speed.ValueChanged += (s, e) => UpdateAxisValue(s, "R轴", false);
        }
        #endregion

        #region 页面加载与初始化
        /// <summary>
        /// 页面加载事件处理：加载运动控制所需的基础数据
        /// 确保UI操作在主线程执行
        /// </summary>
        private void Frm_MotionControl_Load(object sender, EventArgs e)
        {
            try
            {
                // 跨线程检查：如果当前线程不是UI线程，通过Invoke切换到UI线程执行
                if (InvokeRequired)
                {
                    Invoke(new Action(LoadMotionControlData));
                }
                else
                {
                    LoadMotionControlData();
                }

                // 初始化速度比例
                UpdateSpeedRatio(MediumSpeedRatio);
                // 默认选中位置模式
                uiRadioButton_Value.Checked = true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录异常日志
                ShowErrorMessage("页面加载失败", ex.Message);
            }
        }

        /// <summary>
        /// 加载运动控制核心数据
        /// 包括：运动轴列表、运动状态面板、点位类型列表
        /// </summary>
        private void LoadMotionControlData()
        {
            Enabled = false; // 加载期间禁用页面交互
            try
            {
                // 检查运动系统是否初始化
                if (MotionSystem.Instance == null || MotionSystem.Instance.MotionController == null)
                {
                    throw new InvalidOperationException("运动系统未初始化或控制器连接失败");
                }

                LoadMotionAxisList(); // 加载可控制的运动轴列表
                LoadMotionStatusPanel(); // 加载运动状态显示面板
                LoadPointTypeList(); // 加载点位类型列表
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowErrorMessage("数据加载失败", ex.Message);
            }
            finally
            {
                Enabled = true; // 无论加载成功与否，恢复页面交互
            }
        }

        /// <summary>
        /// 加载运动轴列表（从运动系统获取并显示到UI）
        /// </summary>
        private void LoadMotionAxisList()
        {
            uiListBox_选中运动轴.Items.Clear(); // 清空现有轴列表
            // 从运动系统实例中获取轴列表，过滤空名称和"未绑定"的轴
            if (MotionSystem.Instance?.MotionAxes != null && MotionSystem.Instance.MotionAxes.Any())
            {
                foreach (var item in MotionSystem.Instance.MotionAxes)
                {
                    if (!string.IsNullOrEmpty(item.轴名称) && !uiListBox_选中运动轴.Items.Contains(item.轴名称) && item.轴名称 != "未绑定")
                    {
                        uiListBox_选中运动轴.Items.Add(item.轴名称);
                    }
                }
            }
            else
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, "未检测到可用的运动轴");
            }
        }

        /// <summary>
        /// 加载运动状态面板（嵌入Frm_MotionStatus状态页面）
        /// </summary>
        private void LoadMotionStatusPanel()
        {
            uiPanel_MotionStatus_轴运动控制.Visible = false; // 先隐藏面板
            uiPanel_MotionStatus_轴运动控制.Controls.Clear(); // 清空面板内容
            uiPanel_MotionStatus_轴运动控制.Dock = DockStyle.Fill; // 设置面板填充父容器

            try
            {
                // 获取运动状态页面单例，设置为子控件并显示
                var motionStatus = Frm_MotionStatus.Instance;
                motionStatus.TopLevel = false; // 设为非顶级窗口（可嵌入其他控件）
                motionStatus.Dock = DockStyle.Top; // 顶部停靠
                motionStatus.AutoScaleMode = AutoScaleMode.None; // 禁用自动缩放
                motionStatus.Show(); // 显示状态页面

                uiPanel_MotionStatus_轴运动控制.Controls.Add(motionStatus); // 添加到面板
                uiPanel_MotionStatus_轴运动控制.Visible = true; // 显示面板
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                uiPanel_MotionStatus_轴运动控制.Controls.Add(new Label
                {
                    Text = "运动状态面板加载失败",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                uiPanel_MotionStatus_轴运动控制.Visible = true;
            }
        }

        /// <summary>
        /// 加载点位类型列表（从运动系统获取所有 distinct 的点位类型）
        /// </summary>
        private void LoadPointTypeList()
        {
            uiListBox_点位类型.Items.Clear(); // 清空点位类型列表
            uiListBox_点位定义.Items.Clear(); // 清空点位定义列表

            // 从运动系统获取点位列表，提取不重复的点位类型
            if (MotionSystem.Instance?.MotionPoints != null && MotionSystem.Instance.MotionPoints.Any())
            {
                var pointTypes = MotionSystem.Instance.MotionPoints
                    .Select(p => p.点位类型)
                    .Distinct()
                    .Where(t => !string.IsNullOrEmpty(t)); // 过滤空类型

                foreach (var type in pointTypes)
                {
                    uiListBox_点位类型.Items.Add(type);
                }
            }
            else
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Gray, "未检测到点位数据");
            }
        }
        #endregion

        #region 点位相关事件处理
        /// <summary>
        /// 选中点位名称变化事件：更新当前点位信息并显示对应的轴控制面板
        /// </summary>
        private void uiComboBox_选中点位名称_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 通过通用方法处理控件操作（包含线程检查和异常处理）
            HandleOperation(sender, () =>
            {
                // 隐藏所有轴的控制面板和移动按钮
                HideAllAxisPanels();
                HideAllAxisButtons();

                if (uiComboBox_选中点位名称.SelectedItem == null) return; // 未选中点位则返回

                // 获取当前选中的点位数据
                _currentMotionPoint = GetSelectedMotionPoint();
                if (_currentMotionPoint == null) return;

                // 初始化并显示各轴的控制面板（仅显示已绑定的轴）
                InitializeAxisPanel("X轴", _currentMotionPoint.X轴绑定轴,
                    uiDoubleUpDown_X_Value, uiDoubleUpDown_X_Speed, groupBox_X轴,
                    _currentMotionPoint.X轴目标位置, _currentMotionPoint.X轴运行速度);

                InitializeAxisPanel("Y轴", _currentMotionPoint.Y轴绑定轴,
                    uiDoubleUpDown_Y_Value, uiDoubleUpDown_Y_Speed, groupBox_Y轴,
                    _currentMotionPoint.Y轴目标位置, _currentMotionPoint.Y轴运行速度);

                InitializeAxisPanel("Z轴", _currentMotionPoint.Z轴绑定轴,
                    uiDoubleUpDown_Z_Value, uiDoubleUpDown_Z_Speed, groupBox_Z轴,
                    _currentMotionPoint.Z轴目标位置, _currentMotionPoint.Z轴运行速度);

                InitializeAxisPanel("U轴", _currentMotionPoint.U轴绑定轴,
                    uiDoubleUpDown_U_Value, uiDoubleUpDown_U_Speed, groupBox_U轴,
                    _currentMotionPoint.U轴目标位置, _currentMotionPoint.U轴运行速度);

                InitializeAxisPanel("R轴", _currentMotionPoint.R轴绑定轴,
                    uiDoubleUpDown_R_Value, uiDoubleUpDown_R_Speed, groupBox_R轴,
                    _currentMotionPoint.R轴目标位置, _currentMotionPoint.R轴运行速度);

                uiListBox_选中运动轴.SelectedItem = null; // 清除选中的运动轴
            });
        }

        /// <summary>
        /// 初始化单个轴的控制面板
        /// </summary>
        /// <param name="axisType">轴类型（如"X轴"）</param>
        /// <param name="boundAxis">绑定的实际轴名称</param>
        /// <param name="valueControl">位置输入控件</param>
        /// <param name="speedControl">速度输入控件</param>
        /// <param name="panel">轴对应的分组面板</param>
        /// <param name="targetValue">目标位置值</param>
        /// <param name="speedValue">运行速度值</param>
        private void InitializeAxisPanel(string axisType, string boundAxis,
            UIDoubleUpDown valueControl, UIDoubleUpDown speedControl,
            GroupBox panel, double targetValue, double speedValue)
        {
            // 轴未绑定则隐藏面板
            if (string.IsNullOrEmpty(boundAxis) || boundAxis == "未绑定")
            {
                panel.Visible = false;
                return;
            }

            // 查找绑定轴的参数信息
            var axisParam = MotionSystem.Instance?.MotionAxes?.FirstOrDefault(a => a.轴名称 == boundAxis);
            if (axisParam == null) return;

            // 更新轴参数字典
            _axisParameters[axisType] = axisParam;

            // 设置控件初始值
            valueControl.Value = targetValue;
            valueControl.Enabled = uiRadioButton_Value.Checked;
            speedControl.Value = speedValue;
            speedControl.Enabled = uiRadioButton_Speed.Checked;

            // 显示面板并更新标题，显示对应的移动按钮
            panel.Visible = true;
            panel.Text = $"{axisType} ({axisParam.轴名称})"; // 面板标题显示轴类型和实际名称
            ShowAxisButtons(axisType, true);
        }

        /// <summary>
        /// 点位类型选择变化事件：加载该类型下的所有点位定义
        /// </summary>
        private void uiListBox_点位类型_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                if (uiListBox_点位类型.SelectedItem == null) return;

                // 清空关联的列表和表格
                uiListBox_点位定义.Items.Clear();
                uiComboBox_选中点位名称.Items.Clear();
                uiDataGridView_点位资讯.Rows.Clear();

                // 获取选中的点位类型
                string selectedType = uiListBox_点位类型.SelectedItem.ToString();

                // 加载该类型下的所有点位定义（去重）
                if (MotionSystem.Instance?.MotionPoints != null)
                {
                    var pointDefinitions = MotionSystem.Instance.MotionPoints
                        .Where(p => p.点位类型 == selectedType)
                        .Select(p => p.点位定义)
                        .Distinct()
                        .Where(d => !string.IsNullOrEmpty(d));

                    foreach (var definition in pointDefinitions)
                    {
                        uiListBox_点位定义.Items.Add(definition);
                    }
                }

                // 隐藏所有轴面板，清除选中点位
                HideAllAxisPanels();
                uiComboBox_选中点位名称.SelectedItem = null;
            });
        }

        /// <summary>
        /// 点位定义选择变化事件：加载该定义下的所有点位数据，并设置绑定轴排序
        /// </summary>
        private void uiListBox_点位定义_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                // 未选择类型或定义则返回
                if (uiListBox_点位定义.SelectedItem == null || uiListBox_点位类型.SelectedItem == null)
                {
                    // 清除绑定轴列表，恢复默认排序
                    Frm_MotionStatus.Instance.SetSelectedPointBindingAxes(null);
                    return;
                }

                // 清空数据列表和UI控件
                _listAxisPoint.Clear();
                uiDataGridView_点位资讯.Rows.Clear();
                uiComboBox_选中点位名称.Items.Clear();

                // 获取选中的类型和定义
                string selectedType = uiListBox_点位类型.SelectedItem.ToString();
                string selectedDefinition = uiListBox_点位定义.SelectedItem.ToString();

                // 加载符合条件的点位数据
                if (MotionSystem.Instance?.MotionPoints != null)
                {
                    _listAxisPoint.AddRange(MotionSystem.Instance.MotionPoints
                        .Where(p => p.点位类型 == selectedType && p.点位定义 == selectedDefinition));
                }

                // 收集当前点位定义下所有绑定的轴
                List<string> allBindingAxes = new List<string>();

                // 填充数据网格和下拉列表
                if (_listAxisPoint.Any())
                {
                    SetupPointDataGridView(); // 初始化表格列标题

                    // 循环添加点位数据到表格和下拉框，并收集轴信息
                    for (int i = 0; i < _listAxisPoint.Count; i++)
                    {
                        var point = _listAxisPoint[i];
                        AddPointToGridView(i, point); // 添加到表格
                        uiComboBox_选中点位名称.Items.Add(point.点位名称); // 添加到下拉框

                        // 提取该点位绑定的轴
                        CollectBindingAxes(point, allBindingAxes);
                    }

                    uiComboBox_选中点位名称.SelectedIndex = 0; // 默认选中第一个点位

                    // 设置绑定轴列表，用于排序（去重处理）
                    Frm_MotionStatus.Instance.SetSelectedPointBindingAxes(allBindingAxes.Distinct().ToList());
                }
                else
                {
                    // 没有点位数据时，清除绑定轴排序
                    Frm_MotionStatus.Instance.SetSelectedPointBindingAxes(null);
                }

                uiRadioButton_Value.Checked = true; // 默认选中位置模式
            });
        }

        /// <summary>
        /// 收集点位绑定的所有轴
        /// </summary>
        private void CollectBindingAxes(MotionPointDto point, List<string> axesList)
        {
            AddAxesFromBinding(point.X轴绑定轴, axesList);
            AddAxesFromBinding(point.Y轴绑定轴, axesList);
            AddAxesFromBinding(point.Z轴绑定轴, axesList);
            AddAxesFromBinding(point.U轴绑定轴, axesList);
            AddAxesFromBinding(point.R轴绑定轴, axesList);
        }

        /// <summary>
        /// 从绑定字符串中解析轴并添加到列表
        /// </summary>
        private void AddAxesFromBinding(string binding, List<string> axesList)
        {
            if (string.IsNullOrEmpty(binding)) return;

            var axes = binding.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var axis in axes)
            {
                if (!string.IsNullOrEmpty(axis) && axis != "未绑定")
                {
                    axesList.Add(axis);
                }
            }
        }

        /// <summary>
        /// 设置点位数据网格视图的列标题
        /// </summary>
        private void SetupPointDataGridView()
        {
            Column1.HeaderText = "索引";
            Column2.HeaderText = "点位名称";
            Column3.HeaderText = "X轴目标位置";
            Column4.HeaderText = "X轴运行速度";
            Column5.HeaderText = "Y轴目标位置";
            Column6.HeaderText = "Y轴运行速度";
            Column7.HeaderText = "Z轴目标位置";
            Column8.HeaderText = "Z轴运行速度";
            Column9.HeaderText = "U轴目标位置";
            Column10.HeaderText = "U轴运行速度";
            Column11.HeaderText = "R轴目标位置";
            Column12.HeaderText = "R轴运行速度";

            // 禁用列排序（避免用户误操作）
            foreach (DataGridViewColumn column in uiDataGridView_点位资讯.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        /// <summary>
        /// 向网格视图添加单个点位数据
        /// </summary>
        /// <param name="index">行索引</param>
        /// <param name="point">点位数据对象</param>
        private void AddPointToGridView(int index, MotionPointDto point)
        {
            uiDataGridView_点位资讯.Rows.Add(); // 添加空行
            var row = uiDataGridView_点位资讯.Rows[index];

            // 填充单元格数据
            row.Cells[0].Value = index;
            row.Cells[1].Value = point.点位名称;
            row.Cells[2].Value = point.X轴目标位置;
            row.Cells[3].Value = point.X轴运行速度;
            row.Cells[4].Value = point.Y轴目标位置;
            row.Cells[5].Value = point.Y轴运行速度;
            row.Cells[6].Value = point.Z轴目标位置;
            row.Cells[7].Value = point.Z轴运行速度;
            row.Cells[8].Value = point.U轴目标位置;
            row.Cells[9].Value = point.U轴运行速度;
            row.Cells[10].Value = point.R轴目标位置;
            row.Cells[11].Value = point.R轴运行速度;
        }

        /// <summary>
        /// 点位表格点击事件：点击表格行时同步选中对应的点位
        /// </summary>
        private void uiDataGridView_点位资讯_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            HandleOperation(sender, () =>
            {
                // 忽略表头点击（行索引=-1为表头）
                if (e.RowIndex > -1 && e.RowIndex < uiComboBox_选中点位名称.Items.Count)
                {
                    uiComboBox_选中点位名称.SelectedIndex = e.RowIndex;
                }
            });
        }
        #endregion

        #region 按钮事件处理
        /// <summary>
        /// "获取"按钮点击事件：获取当前各轴位置并更新到输入框
        /// </summary>
        private void uiButton_获取_Click(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                if (!IsManualMode()) return; // 非手动模式禁止操作

                // 显示确认对话框，防止误操作
                if (!ShowConfirmDialog("请确认是否获取当前位置的坐标！！！\r\n(不带编码反馈的轴勿操作)"))
                    return;

                // 获取各轴当前位置并更新UI
                UpdateAxisPosition("X轴", uiDoubleUpDown_X_Value, uiDoubleUpDown_X_Speed,
                    p => p.X轴目标位置, p => p.X轴运行速度);

                UpdateAxisPosition("Y轴", uiDoubleUpDown_Y_Value, uiDoubleUpDown_Y_Speed,
                    p => p.Y轴目标位置, p => p.Y轴运行速度);

                UpdateAxisPosition("Z轴", uiDoubleUpDown_Z_Value, uiDoubleUpDown_Z_Speed,
                    p => p.Z轴目标位置, p => p.Z轴运行速度);

                UpdateAxisPosition("U轴", uiDoubleUpDown_U_Value, uiDoubleUpDown_U_Speed,
                    p => p.U轴目标位置, p => p.U轴运行速度);

                UpdateAxisPosition("R轴", uiDoubleUpDown_R_Value, uiDoubleUpDown_R_Speed,
                    p => p.R轴目标位置, p => p.R轴运行速度);
            });
        }

        /// <summary>
        /// "设置"按钮点击事件：将当前输入的点位参数保存到系统
        /// </summary>
        private void uiButton_设置_Click(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                if (!IsManualMode()) return; // 非手动模式禁止操作

                if (uiComboBox_选中点位名称.SelectedItem == null)
                {
                    ShowInformationMessage("提示", "请先选择一个点位");
                    return;
                }

                // 显示确认对话框
                if (!ShowConfirmDialog($"请确认是否校正点: {uiComboBox_选中点位名称.SelectedItem}"))
                    return;

                // 更新点位数据并刷新UI和系统
                if (_currentMotionPoint != null)
                {
                    UpdateMotionPointValues(); // 更新点位值
                    UpdatePointGridView(); // 刷新表格数据
                    MotionSystem.Instance.RefreshDataAsync(); // 刷新系统数据
                }
            });
        }

        /// <summary>
        /// "移动"按钮点击事件：执行选中点位的多轴联动移动，限制最大速度为25%，完成后恢复到150%
        /// </summary>
        private void uiButton_移动_Click(object sender, EventArgs e)
        {
            HandleOperation(sender, async () =>  // 支持async/await的异步lambda
            {
                // 1. 模式与安全校验
                if (!IsManualMode())
                    return;

                if (!IsSystemInitialized())
                    return;

                if (_listAxisPoint.Count == 0)
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, "无点位数据，无法执行移动操作！");
                    ShowInformationMessage("提示", "无点位数据，无法执行移动操作！");
                    return;
                }

                if (!SafetyInterlock.CheckMotionSafety(_currentMotionPoint))
                {
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, "安全联锁检查未通过，禁止移动！");
                    ShowWarningMessage("安全提示", "安全联锁检查未通过，禁止移动！");
                    return;
                }

                PerformEmergencyStop();//执行急停操作的核心逻辑

                // 2. 安全确认
                if (!ShowConfirmDialog("请确认伺服动作前安全状态 ！！！"))
                    return;


                // 显示速度选择对话框
                using (var speedBox = new Frm_SpeedBox())
                {
                    Project.Instance.StationInfo.RunningSpeedRatio = 25;
                    speedBox.uiTrackBar_速度比例.Value = Project.Instance.StationInfo.RunningSpeedRatio;
                    speedBox.uiTrackBar_速度比例.Maximum = MaxSpeedLimit;
                    speedBox.lab_速度比例显示.Text = $"{speedBox.uiTrackBar_速度比例.Value}%";
                    speedBox.lbl_info.Text = $"在执行该流程前，请选择轴运行的速度！！默认：{speedBox.uiTrackBar_速度比例.Value}%！！！\r\n注意：最大速度限制为{MaxSpeedLimit}%！";
                    speedBox.TopMost = true; // 置顶显示
                    speedBox.ShowDialog();
                }


                try
                {
                    // 4. 执行移动操作（等待异步操作完成）
                    await ExecuteAxisMovementAsync();
                }
                finally
                {
                    // 5. 移动完成后，将速度限制恢复到100%
                    using (var speedBox = new Frm_SpeedBox())
                    {
                        Project.Instance.StationInfo.RunningSpeedRatio = 100;
                        speedBox.uiTrackBar_速度比例.Value = Project.Instance.StationInfo.RunningSpeedRatio;
                        speedBox.uiTrackBar_速度比例.Maximum = MaxSpeedLimit;
                        speedBox.lab_速度比例显示.Text = $"{speedBox.uiTrackBar_速度比例.Value}%";
                        speedBox.lbl_info.Text = $"在执行该流程前，请选择轴运行的速度！！默认：{speedBox.uiTrackBar_速度比例.Value}%！！！\r\n注意：最大速度限制为{MaxSpeedLimit}%！";
                        speedBox.TopMost = true; // 置顶显示
                    }

                    // 记录速度限制已恢复
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                                $"移动完成，速度限制已恢复到100%");
                }
            });
        }

        /// <summary>
        /// 异步执行轴移动操作，支持取消
        /// </summary>
        private async Task ExecuteAxisMovementAsync()
        {
            // 取消之前的操作
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                // 禁用移动按钮防止重复操作
                uiButton_移动.Enabled = false;
                await Task.Run(() =>
                {
                    ExecuteAxisMovement();
                    // 等待所有轴移动完成
                    WaitForAllAxesToComplete(token);
                }, token);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowErrorMessage("移动失败", $"执行移动操作时发生错误：{ex.Message}");
            }
            finally
            {
                uiButton_移动.Enabled = true;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 等待所有轴移动完成
        /// </summary>
        private void WaitForAllAxesToComplete(CancellationToken cancellationToken)
        {
            if (_currentMotionPoint == null) return;

            var axesToWait = new List<MotionAxisDto>();

            // 收集需要等待的轴
            if (!string.IsNullOrEmpty(_currentMotionPoint.X轴绑定轴) && _currentMotionPoint.X轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["X轴"]);

            if (!string.IsNullOrEmpty(_currentMotionPoint.Y轴绑定轴) && _currentMotionPoint.Y轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["Y轴"]);

            if (!string.IsNullOrEmpty(_currentMotionPoint.Z轴绑定轴) && _currentMotionPoint.Z轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["Z轴"]);

            if (!string.IsNullOrEmpty(_currentMotionPoint.U轴绑定轴) && _currentMotionPoint.U轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["U轴"]);

            if (!string.IsNullOrEmpty(_currentMotionPoint.R轴绑定轴) && _currentMotionPoint.R轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["R轴"]);

            // 等待所有轴完成移动或取消
            while (axesToWait.Any(axis => !MotionSystem.Instance.MotionController.CheckMoveDone(axis)))
            {
                PerformEmergencyStop();
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 检查系统是否已完成初始化
        /// </summary>
        /// <returns>系统是否就绪</returns>
        private bool IsSystemInitialized()
        {
            if (Project.Instance?.StationInfo?.ResetProcessStatus != ProcessStatus.Completed)
            {
                string message = "手动执行多轴联动移动操作前，先进行初始化操作！！！";
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, message);
                ShowWarningMessage("系统未初始化", message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// "急停"按钮点击事件：停止当前选中轴的运动
        /// </summary>
        private void uiButton_急停_Click(object sender, EventArgs e)
        {
            // 调用封装后的急停处理方法
            HandleOperation(sender, PerformEmergencyStop);
            ShowInformationMessage("提示", "已执行急停操作");
        }

        /// <summary>
        /// 执行急停操作的核心逻辑
        /// </summary>
        private void PerformEmergencyStop()
        {
            // 取消任何正在执行的操作
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            // 停止所有轴运动
            foreach (var axis in MotionSystem.Instance.MotionAxes)
            {
                // 程序即将退出时终止操作，避免资源冲突
                if (Project.Instance?.GlobalVariables?.IsProgramExiting == true)
                    break;

                // 安全停止轴运动（检查控制器实例是否有效）
                MotionSystem.Instance.MotionController?.SmtStop(axis, 0);
            }
        }

        /// <summary>
        /// 报警复位按钮点击事件处理
        /// </summary>
        private void uiButton_报警复位_Click(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                // 提前检查选中的轴参数是否为空，避免后续空引用
                if (_selectedAxisParam == null)
                {
                    ShowInformationMessage("提示", "请先选择一个运动轴");
                    return;
                }

                try
                {
                    var motionController = MotionSystem.Instance.MotionController;
                    if (motionController == null)
                    {
                        ShowErrorMessage("错误", "运动控制器未初始化");
                        return;
                    }

                    // 执行报警复位操作（1表示复位，0表示取消复位）
                    motionController.ERCSet(_selectedAxisParam, 1);

                    // 短暂等待，确保复位指令生效
                    Thread.Sleep(100);

                    // 完成复位后恢复正常状态
                    motionController.ERCSet(_selectedAxisParam, 0);

                    // 检查伺服报警是否已解除
                    if (motionController.CheckServoAlarm(_selectedAxisParam))
                    {
                        // 报警未解除的处理逻辑
                        ShowWarningMessage("复位失败", "该轴仍存在报警状态，请检查设备");
                    }
                    else
                    {
                        // 报警已解除的处理逻辑
                        ShowInformationMessage("成功", "报警已成功复位");
                    }
                }
                catch (Exception ex)
                {
                    // 捕获并记录操作过程中的异常
                    RLog.SaveError(ex);
                    ShowErrorMessage("操作失败", $"报警复位时发生错误：{ex.Message}");
                }
            });
        }

        /// <summary>
        /// "使能"按钮点击事件：切换当前选中轴的使能状态
        /// </summary>
        private void uiButton_使能_Click(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                if (_selectedAxisParam == null)
                {
                    ShowInformationMessage("提示", "请先选择一个运动轴");
                    return;
                }

                try
                {
                    // 判断当前使能状态（根据按钮文本）
                    bool enable = uiButton_使能.Text == "使能：OFF";
                    // 调用运动控制器设置使能（1=使能，0=禁用）
                    MotionSystem.Instance.MotionController.ServoSet(_selectedAxisParam, enable ? (ushort)1 : (ushort)0);

                    // 短暂延迟后更新状态
                    Thread.Sleep(100);
                    UpdateEnableButtonState();

                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                        $"{_selectedAxisParam.轴名称} {(enable ? "已使能" : "已禁用")}");
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex);
                    ShowErrorMessage("操作失败", $"切换使能状态时发生错误：{ex.Message}");
                }
            });
        }

        /// <summary>
        /// "回原点"按钮点击事件：启动/停止当前选中轴的回原点操作
        /// </summary>
        private void uiButton_回原点_Click(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                if (_selectedAxisParam == null)
                {
                    ShowInformationMessage("提示", "请先选择一个运动轴");
                    return;
                }

                // 确认回原点操作
                if (!ShowConfirmDialog($"请确认是否手动执行<{_selectedAxisParam.轴名称}>回原点！！！"))
                    return;

                PerformEmergencyStop();//执行急停操作的核心逻辑

                // 切换回原点定时器状态（控制回原点过程）
                if (timer_Home.Enabled)
                {
                    // 停止回原点
                    timer_Home.Enabled = false;
                    MotionSystem.Instance.MotionController.SmtStop(_selectedAxisParam, 0);
                    ShowInformationMessage("提示", "手动回原已停止");
                }
                else
                {
                    // 启动回原点
                    timer_Home.Enabled = true;
                    _isHomed = false; // 重置回原点状态标记
                    ShowInformationMessage("提示", "开始执行回原点操作...");
                }
            });
        }
        #endregion

        #region 轴移动与控制
        /// <summary>
        /// 处理轴移动操作（正向/负向）
        /// </summary>
        /// <param name="sender">触发事件的控件</param>
        /// <param name="axisType">轴类型（如"X轴"）</param>
        /// <param name="isPositive">是否正向移动</param>
        private void HandleAxisMovement(object sender, string axisType, bool isPositive)
        {
            try
            {
                if (sender is UIImageButton button)
                {
                    button.Enabled = false; // 禁用按钮防止重复点击

                    if (!IsManualMode()) // 非手动模式禁止操作
                    {
                        RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, "工位未处于手动模式，不允许手动执行操作！！！");
                        ShowWarningMessage("模式错误", "工位未处于手动模式，不允许手动执行操作！");
                        return;
                    }

                    // 获取轴参数
                    if (!_axisParameters.TryGetValue(axisType, out var axisParam) || axisParam == null)
                    {
                        ShowInformationMessage("提示", $"未找到{axisType}的参数配置");
                        return;
                    }

                    // 检查轴是否运动中
                    if (MotionSystem.Instance.MotionController.CheckMoveDone(axisParam) == false)
                    {
                        ShowWarningMessage("操作提示", $"{axisType}正在运行中，请稍等");
                        return;
                    }


                    // 检查轴是否已使能
                    if (MotionSystem.Instance.MotionController.ServoRead(axisParam) != 1)
                    {
                        ShowWarningMessage("操作提示", $"{axisType}未使能，请先使能轴");
                        return;
                    }

                    // 设置轴运行速度（脉冲比转换实际速度）
                    axisParam.单轴运行速度 = SelectedAxisSpeed * axisParam.脉冲比;

                    // 计算移动距离（正向为步径值，负向为负值）
                    double distance = isPositive ? SelectedAxisStep : -SelectedAxisStep;

                    // 执行相对移动
                    MotionSystem.Instance.MotionController.RelMove(axisParam, distance);
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                        $"{axisType} 移动 {distance} 单位，速度 {SelectedAxisSpeed}");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录异常
                ShowErrorMessage("移动失败", $"执行{axisType}移动时发生错误：{ex.Message}");
            }
            finally
            {
                if (sender is UIImageButton button && !button.IsDisposed)
                {
                    button.Enabled = true; // 恢复按钮状态
                }
            }
        }

        /// <summary>
        /// 更新轴的位置或速度值（当输入框变化时）
        /// </summary>
        /// <param name="sender">输入控件</param>
        /// <param name="axisType">轴类型</param>
        /// <param name="isPosition">是否为位置值（否则为速度值）</param>
        private void UpdateAxisValue(object sender, string axisType, bool isPosition)
        {
            HandleOperation(sender, () =>
            {
                // 未选中点位则返回
                if (uiComboBox_选中点位名称.SelectedItem == null || _currentMotionPoint == null)
                    return;

                if (!(sender is UIDoubleUpDown control)) return; // 非数值输入控件则返回
                var axisParam = _axisParameters[axisType];

                // 检查轴是否绑定
                string bindingProperty = GetAxisBindingProperty(axisType, isPosition);
                if (string.IsNullOrEmpty(bindingProperty) ||
                    GetAxisBindingValue(axisType) == "未绑定" || axisParam == null)
                    return;

                // 记录旧值用于日志
                double oldValue = isPosition ? GetAxisPosition(axisType) : GetAxisSpeed(axisType);

                // 更新值（位置或速度）
                if (isPosition)
                {
                    SetAxisPosition(axisType, control.Value);
                }
                else
                {
                    SetAxisSpeed(axisType, control.Value);
                }

                // 记录操作日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                    $"手动操作--》{control.Name}--数据从{oldValue}更改为{control.Value}");
            });
        }

        /// <summary>
        /// 执行轴移动操作（多轴联动）
        /// 特殊处理Z轴（先降后升，避免碰撞）
        /// </summary>
        private void ExecuteAxisMovement()
        {
            if (_currentMotionPoint == null) return;

            // 特殊处理Z轴（通常为垂直轴，优先保证安全）
            if (!string.IsNullOrEmpty(_currentMotionPoint.Z轴绑定轴) && _currentMotionPoint.Z轴绑定轴 != "未绑定" && _axisParameters["Z轴"] != null)
            {
                var zAxis = _axisParameters["Z轴"];
                double currentZPos = MotionSystem.Instance.MotionController.GetEncPos(zAxis); // 获取当前Z轴位置
                Project.Instance.StationInfo.ZAxisPosition = currentZPos; // 保存当前位置

                // 如果目标位置低于当前位置（向下移动），先执行Z轴移动
                if (_currentMotionPoint.Z轴目标位置 <= currentZPos)
                {
                    MotionSystem.Instance.MotionController.AbsMove(zAxis, _currentMotionPoint.Z轴目标位置,
                        _currentMotionPoint.Z轴运行速度);

                    // 等待Z轴移动完成
                    while (!MotionSystem.Instance.MotionController.CheckMoveDone(zAxis))
                    {
                        Thread.Sleep(50);
                    }
                }
            }

            // 移动其他轴（X/Y/U/R）
            MoveAxis("X轴", _currentMotionPoint.X轴绑定轴, _currentMotionPoint.X轴目标位置, _currentMotionPoint.X轴运行速度);
            MoveAxis("Y轴", _currentMotionPoint.Y轴绑定轴, _currentMotionPoint.Y轴目标位置, _currentMotionPoint.Y轴运行速度);
            MoveAxis("U轴", _currentMotionPoint.U轴绑定轴, _currentMotionPoint.U轴目标位置, _currentMotionPoint.U轴运行速度);
            MoveAxis("R轴", _currentMotionPoint.R轴绑定轴, _currentMotionPoint.R轴目标位置, _currentMotionPoint.R轴运行速度);

            // 等待水平轴移动完成
            WaitForHorizontalAxesCompletion();

            // 再次处理Z轴（如果需要上升）
            if (!string.IsNullOrEmpty(_currentMotionPoint.Z轴绑定轴) && _currentMotionPoint.Z轴绑定轴 != "未绑定" && _axisParameters["Z轴"] != null)
            {
                var zAxis = _axisParameters["Z轴"];
                // 如果目标位置高于初始位置（向上移动），最后执行Z轴移动
                if (_currentMotionPoint.Z轴目标位置 >= Project.Instance.StationInfo.ZAxisPosition)
                {
                    MotionSystem.Instance.MotionController.AbsMove(zAxis, _currentMotionPoint.Z轴目标位置,
                        _currentMotionPoint.Z轴运行速度);

                    // 等待Z轴移动完成
                    while (!MotionSystem.Instance.MotionController.CheckMoveDone(zAxis))
                    {
                        Thread.Sleep(50);
                    }
                }
            }
        }

        /// <summary>
        /// 等待水平轴移动完成
        /// </summary>
        private void WaitForHorizontalAxesCompletion()
        {
            var axesToWait = new List<MotionAxisDto>();

            if (!string.IsNullOrEmpty(_currentMotionPoint.X轴绑定轴) && _currentMotionPoint.X轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["X轴"]);

            if (!string.IsNullOrEmpty(_currentMotionPoint.Y轴绑定轴) && _currentMotionPoint.Y轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["Y轴"]);

            if (!string.IsNullOrEmpty(_currentMotionPoint.U轴绑定轴) && _currentMotionPoint.U轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["U轴"]);

            if (!string.IsNullOrEmpty(_currentMotionPoint.R轴绑定轴) && _currentMotionPoint.R轴绑定轴 != "未绑定")
                axesToWait.Add(_axisParameters["R轴"]);

            // 等待所有水平轴完成移动
            while (axesToWait.Any(axis => !MotionSystem.Instance.MotionController.CheckMoveDone(axis)))
            {
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// 移动单个轴到目标位置
        /// </summary>
        /// <param name="axisType">轴类型</param>
        /// <param name="boundAxis">绑定的实际轴名称</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="speed">运行速度</param>
        private void MoveAxis(string axisType, string boundAxis, double targetPosition, double speed)
        {
            // 轴未绑定则不执行
            if (string.IsNullOrEmpty(boundAxis) || boundAxis == "未绑定")
                return;

            // 获取轴参数并执行绝对移动
            if (_axisParameters.TryGetValue(axisType, out var axisParam) && axisParam != null)
            {
                // 检查轴是否已使能
                if (MotionSystem.Instance.MotionController.ServoRead(axisParam) != 1)
                {
                    throw new InvalidOperationException($"{axisType}未使能，无法执行移动操作");
                }

                // 执行绝对移动
                MotionSystem.Instance.MotionController.AbsMove(axisParam, targetPosition, speed);
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                    $"{axisType} 移动到 {targetPosition} 位置，速度 {speed}");
            }
        }
        #endregion

        #region 速度与步径控制
        /// <summary>
        /// 步径输入框变化事件：更新选中的步径值
        /// </summary>
        private void uiDoubleUpDown_步径_ValueChanged(object sender, double value)
        {
            HandleOperation(sender, () =>
            {
                if (value <= 0)
                {
                    ShowWarningMessage("输入错误", "步径值必须大于0");
                    return;
                }

                SelectedAxisStep = value;
                // 更新快速步径列表的选中状态
                int index = Array.IndexOf(_quickStepOptions, value);
                if (index >= 0)
                {
                    uiListBox_快速步径.SelectedIndex = index;
                }
                else
                {
                    uiListBox_快速步径.SelectedItem = null;
                }
            });
        }

        /// <summary>
        /// 速度输入框变化事件：更新选中的速度值
        /// </summary>
        private void uiDoubleUpDown_速度_ValueChanged(object sender, double value)
        {
            HandleOperation(sender, () =>
            {
                if (value <= 0)
                {
                    ShowWarningMessage("输入错误", "速度值必须大于0");
                    return;
                }

                SelectedAxisSpeed = value;
            });
        }

        /// <summary>
        /// 快速步径选择变化事件：同步更新步径输入框
        /// </summary>
        private void uiListBox_快速步径_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                // 解析选中的步径值并同步到输入框
                if (uiListBox_快速步径.SelectedItem != null &&
                    double.TryParse(uiListBox_快速步径.SelectedItem.ToString(), out double step) && step > 0)
                {
                    SelectedAxisStep = step;
                    uiDoubleUpDown_步径.Value = step;
                }
            });
        }

        /// <summary>
        /// 速度预设单选按钮变化事件：更新速度比例
        /// </summary>
        private void uiRadioButton_速度预设_CheckedChanged(object sender, EventArgs e)
        {
            // 仅当按钮被选中时处理
            if (sender is UIRadioButton radio && radio.Checked)
            {
                HandleOperation(sender, () =>
                {
                    // 根据选中的按钮设置速度比例
                    int speedRatio = radio == uiRadioButton_低速 ? LowSpeedRatio :
                                     radio == uiRadioButton_中速 ? MediumSpeedRatio :
                                     HighSpeedRatio;

                    UpdateSpeedRatio(speedRatio);
                });
            }
        }

        /// <summary>
        /// 自定义速度单选按钮变化事件：取消预设速度的选中状态
        /// </summary>
        private void uiRadioButton_速度自定义_CheckedChanged(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                if (uiRadioButton_速度自定义.Checked)
                {
                    uiRadioButton_低速.Checked = false;
                    uiRadioButton_中速.Checked = false;
                    uiRadioButton_高速.Checked = false;
                }
            });
        }

        /// <summary>
        /// 速度比例滑块变化事件：更新速度比例并同步到对话框
        /// </summary>
        private void uiTrackBar_速度比例_ValueChanged(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                int ratio = uiTrackBar_速度比例.Value;
                // 限制最大速度
                if (ratio > MaxSpeedLimit)
                {
                    ratio = MaxSpeedLimit;
                    uiTrackBar_速度比例.Value = ratio;
                }

                UpdateSpeedRatio(ratio); // 更新速度比例
                SyncSpeedToSpeedBox(ratio); // 同步到速度对话框
                uiRadioButton_速度自定义.Checked = true; // 选中自定义速度
            });
        }

        /// <summary>
        /// 更新速度比例（0~MaxSpeedLimit范围）
        /// </summary>
        /// <param name="ratio">速度比例值</param>
        private void UpdateSpeedRatio(int ratio)
        {
            // 兼容低版本.NET Framework的范围限制（替代Math.Clamp）
            int clampedRatio;
            if (ratio < 0)
                clampedRatio = 0;
            else if (ratio > MaxSpeedLimit)
                clampedRatio = MaxSpeedLimit;
            else
                clampedRatio = ratio;

            // 仅在值发生变化时才执行后续操作（避免无效更新）
            if (clampedRatio == Project.Instance.StationInfo.RunningSpeedRatio)
            {
                return;
            }

            // 更新系统参数
            Project.Instance.StationInfo.RunningSpeedRatio = clampedRatio;

            // 更新UI显示（提取速度计算逻辑，提高可读性）
            double speedValue = 25d * clampedRatio / 100; // 使用double避免整数除法精度丢失
            uiLabel_速度比例显示.Text = string.Format(SpeedRatioFormat, clampedRatio);
            uiTrackBar_速度比例.Value = clampedRatio;
            uiLabel_速度.Text = string.Format("{0:F1}mm/s", speedValue); // 低版本兼容的格式化方式
        }

        /// <summary>
        /// 同步速度比例到速度选择对话框
        /// </summary>
        /// <param name="ratio">速度比例</param>
        private void SyncSpeedToSpeedBox(int ratio)
        {
            // 如果速度对话框已存在，更新其值
            var speedBox = Application.OpenForms.OfType<Frm_SpeedBox>().FirstOrDefault();
            if (speedBox != null)
            {
                UpdateSpeedBoxControls(speedBox, ratio);
            }
        }

        /// <summary>
        /// 更新速度对话框的控件状态
        /// </summary>
        /// <param name="speedBox">速度对话框实例</param>
        /// <param name="ratio">速度比例</param>
        private void UpdateSpeedBoxControls(Frm_SpeedBox speedBox, int ratio)
        {
            // 确保控件已创建且不在销毁过程中
            if (speedBox.IsDisposed) return;

            // 跨线程安全更新
            if (speedBox.InvokeRequired)
            {
                speedBox.Invoke(new Action(() => UpdateSpeedBoxControlsInternal(speedBox, ratio)));
            }
            else
            {
                UpdateSpeedBoxControlsInternal(speedBox, ratio);
            }
        }

        /// <summary>
        /// 内部方法：更新速度对话框控件
        /// </summary>
        private void UpdateSpeedBoxControlsInternal(Frm_SpeedBox speedBox, int ratio)
        {
            if (speedBox.uiTrackBar_速度比例.Created)
            {
                speedBox.uiTrackBar_速度比例.Value = ratio; // 同步滑块
                speedBox.lab_速度比例显示.Text = $"{ratio}%"; // 同步显示
            }
        }
        #endregion

        #region 轴选择与状态更新
        /// <summary>
        /// 选中运动轴变化事件：更新选中轴参数并显示对应的移动按钮
        /// </summary>
        private void uiListBox_选中运动轴_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                HideAllAxisButtons(); // 隐藏所有轴的移动按钮

                if (uiListBox_选中运动轴.SelectedItem == null)
                {
                    _selectedAxisParam = null; // 清除选中轴
                    // 当没有选中轴时，清除绑定列表，恢复默认排序
                    Frm_MotionStatus.Instance.SetSelectedPointBindingAxes(null);
                    return;
                }

                string selectedAxisName = uiListBox_选中运动轴.SelectedItem.ToString();

                // 查找选中轴的参数
                _selectedAxisParam = MotionSystem.Instance?.MotionAxes?
                    .FirstOrDefault(a => a.轴名称 == selectedAxisName);

                if (_selectedAxisParam == null)
                {
                    // 若选中轴无效，清除绑定列表
                    Frm_MotionStatus.Instance.SetSelectedPointBindingAxes(null);
                    ShowWarningMessage("错误", $"未找到轴 {selectedAxisName} 的参数配置");
                    return;
                }

                // 创建绑定轴列表，将当前选中的轴作为优先显示的轴
                List<string> bindingAxes = new List<string> { selectedAxisName };
                // 调用方法设置绑定轴列表，让运动状态面板优先显示该轴
                Frm_MotionStatus.Instance.SetSelectedPointBindingAxes(bindingAxes);

                // 根据轴类型显示对应的移动按钮，并更新轴参数字典
                switch (_selectedAxisParam.轴类型)
                {
                    case "X轴":
                        ShowAxisButtons("X轴");
                        _axisParameters["X轴"] = _selectedAxisParam;
                        break;
                    case "Y轴":
                        ShowAxisButtons("Y轴");
                        _axisParameters["Y轴"] = _selectedAxisParam;
                        break;
                    case "Z轴":
                        ShowAxisButtons("Z轴");
                        _axisParameters["Z轴"] = _selectedAxisParam;
                        break;
                    case "U轴":
                        ShowAxisButtons("U轴");
                        _axisParameters["U轴"] = _selectedAxisParam;
                        break;
                    case "R轴":
                        ShowAxisButtons("R轴");
                        _axisParameters["R轴"] = _selectedAxisParam;
                        break;
                    default:
                        ShowInformationMessage("提示", $"不支持的轴类型: {_selectedAxisParam.轴类型}");
                        break;
                }

                UpdateEnableButtonState(); // 更新使能按钮状态
            });
        }

        /// <summary>
        /// 更新使能按钮状态（根据当前轴的实际使能状态）
        /// </summary>
        private void UpdateEnableButtonState()
        {
            if (_selectedAxisParam == null) return;

            try
            {
                // 读取当前轴的使能状态
                int servoState = MotionSystem.Instance.MotionController.ServoRead(_selectedAxisParam);
                if (servoState == 1) // 已使能
                {
                    uiButton_使能.Text = "使能：ON";
                    uiButton_使能.FillColor = Color.FromArgb(80, 160, 255); // 蓝色
                }
                else // 未使能
                {
                    uiButton_使能.Text = "使能：OFF";
                    uiButton_使能.FillColor = Color.Red; // 红色
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                uiButton_使能.Text = "使能：未知";
                uiButton_使能.FillColor = Color.Gray;
            }
        }

        /// <summary>
        /// 定期更新运动状态信息
        /// </summary>
        private void UpdateMotionStatus(object state)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;

                // 在UI线程更新状态
                Invoke(new Action(() =>
                {
                    // 更新选中轴的使能状态
                    if (_selectedAxisParam != null)
                    {
                        UpdateEnableButtonState();
                    }

                    // 检查是否有轴处于报警状态
                    CheckAxisAlarms();
                }));
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 检查轴是否处于报警状态
        /// </summary>
        private void CheckAxisAlarms()
        {
            if (_selectedAxisParam == null) return;

            try
            {

                if (MotionSystem.Instance.MotionController.CheckServoAlarm(_selectedAxisParam))
                {
                    uiButton_报警复位.Visible = true;
                    // 使用ToolTip组件设置提示信息，而不是直接访问ToolTip属性
                    _toolTip.SetToolTip(uiButton_报警复位, $"{_selectedAxisParam.轴名称} 存在报警，请复位");
                }
                else
                {
                    uiButton_报警复位.Visible = false;
                    // 清除提示信息
                    _toolTip.SetToolTip(uiButton_报警复位, string.Empty);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 定时器事件
        /// <summary>
        /// 回原点定时器事件：监控回原点过程并判断是否完成
        /// 定时器间隔内持续检查状态
        /// </summary>
        private void timer_Home_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_selectedAxisParam == null || IsDisposed)
                {
                    timer_Home.Enabled = false;
                    return;
                }

                // 获取轴的运动状态（数组包含多种状态标识）
                bool[] motionStatus = MotionSystem.Instance.MotionController.GetMotionSts(_selectedAxisParam);

                // 未开始回原点则启动
                if (!_isHomed)
                {
                    _isHomed = true;
                    MotionSystem.Instance.MotionController.GoHome(_selectedAxisParam); // 执行回原点命令
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                        $"{_selectedAxisParam.轴名称} 开始回原点操作");
                }
                // 检查回原点是否完成（移动已完成 + 原点信号触发 + 无错误）
                else if (MotionSystem.Instance.MotionController.CheckMoveDone(_selectedAxisParam) &&
                         motionStatus[3] && !motionStatus[5])
                {
                    System.Threading.Thread.Sleep(500); // 等待稳定
                    timer_Home.Enabled = false; // 停止定时器
                    _isHomed = false; // 重置状态
                    MotionSystem.Instance.MotionController.SetPos(0, _selectedAxisParam); // 设置原点位置为0
                    ShowInformationMessage("完成", $"{_selectedAxisParam.轴名称} 手动回原完成"); // 显示完成信息
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Black,
                        $"{_selectedAxisParam.轴名称} 回原点操作完成");
                }
            }
            catch (Exception ex)
            {
                timer_Home.Enabled = false;
                _isHomed = false;
                RLog.SaveError(ex);
                ShowErrorMessage("错误", $"回原点操作失败：{ex.Message}");
            }
        }
        #endregion

        #region 选项卡事件
        /// <summary>
        /// 功能选择选项卡变化事件：切换显示不同的状态面板
        /// </summary>
        private void uiTabControl_功能选择_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // 根据选中的标签页更新显示的状态面板
                switch (uiTabControl_功能选择.SelectedTab.Text)
                {
                    case "轴运动控制":
                        UpdateMotionStatusPanel(uiPanel_MotionStatus_轴运动控制, DockStyle.Top);
                        break;
                    case "轴状态":
                        UpdateMotionStatusHPanel(uiPanel_MotionStatus, DockStyle.Fill);
                        break;
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowErrorMessage("切换失败", $"切换选项卡时发生错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新运动状态面板（嵌入Frm_MotionStatus并设置停靠方式）
        /// </summary>
        /// <param name="panel">目标面板</param>
        /// <param name="dockStyle">停靠方式</param>
        private void UpdateMotionStatusPanel(UIPanel panel, DockStyle dockStyle)
        {
            panel.Visible = false;
            panel.Controls.Clear();
            panel.Dock = DockStyle.Fill;

            try
            {
                var motionStatus = Frm_MotionStatus.Instance;
                motionStatus.TopLevel = false;
                motionStatus.Dock = dockStyle; // 设置停靠方式
                motionStatus.AutoScaleMode = AutoScaleMode.None;
                motionStatus.Show();

                panel.Controls.Add(motionStatus);
                panel.Visible = true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                panel.Controls.Add(new Label
                {
                    Text = "运动状态面板加载失败",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                panel.Visible = true;
            }
        }

        private void UpdateMotionStatusHPanel(UIPanel panel, DockStyle dockStyle)
        {
            panel.Visible = false;
            panel.Controls.Clear();
            panel.Dock = DockStyle.Fill;

            try
            {
                var motionStatus = Frm_MotionStatusH.Instance;
                motionStatus.TopLevel = false;
                motionStatus.Dock = dockStyle; // 设置停靠方式
                motionStatus.AutoScaleMode = AutoScaleMode.None;
                motionStatus.Show();

                panel.Controls.Add(motionStatus);
                panel.Visible = true;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                panel.Controls.Add(new Label
                {
                    Text = "水平运动状态面板加载失败",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                panel.Visible = true;
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 处理控件操作的通用方法
        /// 包含：跨线程检查、控件禁用/启用、异常处理
        /// </summary>
        /// <param name="control">触发操作的控件</param>
        /// <param name="action">要执行的操作</param>
        private void HandleOperation(object control, Action action)
        {
            try
            {
                // 检查控件是否已释放
                if (control is System.Windows.Forms.Control c && c.IsDisposed)
                    return;

                // 跨线程检查：如果需要，在UI线程执行操作
                if (control is System.Windows.Forms.Control ctrl && ctrl.InvokeRequired)
                {
                    ctrl.Invoke(action);
                }
                else
                {
                    // 执行前禁用控件，防止重复操作
                    if (control is System.Windows.Forms.Control ctrl2 && !(ctrl2 is UIPanel)) // 面板不需要禁用
                    {
                        ctrl2.Enabled = false;
                    }

                    lock (_lockObj)
                    {
                        action(); // 执行核心操作
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // 控件已释放，忽略
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录异常
                ShowErrorMessage("操作失败", ex.Message);
            }
            finally
            {
                // 操作完成后恢复控件启用状态
                if (control is System.Windows.Forms.Control ctrl && !ctrl.IsDisposed && !(ctrl is UIPanel))
                {
                    ctrl.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 检查当前是否为手动模式
        /// 非手动模式下禁止手动操作
        /// </summary>
        /// <returns>是否为手动模式</returns>
        private bool IsManualMode()
        {
            if (MenuProxy.Instance.MachineRunModel != MachineMode.Manual)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, "工位未处于手动模式状态，不允许手动执行操作！！！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <returns>用户是否确认（Yes/No）</returns>
        private bool ShowConfirmDialog(string message)
        {
            Frm_ConfirmBox.Instance.lbl_Message.Text = message;
            Frm_ConfirmBox.Instance.Visible = false;
            Frm_ConfirmBox.Instance.TopMost = true; // 置顶显示
            Frm_ConfirmBox.Instance.ShowDialog();
            var result = Frm_ConfirmBox.Instance.Result;

            return result == ConfirmBoxResult.Yes;
        }

        /// <summary>
        /// 显示信息提示框
        /// </summary>
        private void ShowInformationMessage(string title, string message)
        {
            if (IsDisposed) return;

            Frm_MessageBox.Instance.MessageBoxShow(message, title, Log.Frm.MessageBoxIcon.Information);
        }

        /// <summary>
        /// 显示警告提示框
        /// </summary>
        private void ShowWarningMessage(string title, string message)
        {
            if (IsDisposed) return;

            Frm_MessageBox.Instance.MessageBoxShow(message, title, Log.Frm.MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 显示错误提示框
        /// </summary>
        private void ShowErrorMessage(string title, string message)
        {
            if (IsDisposed) return;

            Frm_MessageBox.Instance.MessageBoxShow(message, title, Log.Frm.MessageBoxIcon.Error);
        }

        /// <summary>
        /// 获取当前选中的运动点位数据
        /// </summary>
        /// <returns>选中的MotionPointDto对象，未选中则为null</returns>
        private MotionPointDto GetSelectedMotionPoint()
        {
            // 检查是否有选中的点位、类型和定义
            if (uiComboBox_选中点位名称.SelectedItem == null ||
                uiListBox_点位类型.SelectedItem == null ||
                uiListBox_点位定义.SelectedItem == null)
                return null;

            string pointName = uiComboBox_选中点位名称.SelectedItem.ToString();
            string pointType = uiListBox_点位类型.SelectedItem.ToString();
            string pointDefinition = uiListBox_点位定义.SelectedItem.ToString();

            // 从运动系统中查找匹配的点位
            return MotionSystem.Instance?.MotionPoints?
                .FirstOrDefault(p => p.点位名称 == pointName &&
                                    p.点位类型 == pointType &&
                                    p.点位定义 == pointDefinition);
        }

        /// <summary>
        /// 隐藏所有轴的控制面板
        /// </summary>
        private void HideAllAxisPanels()
        {
            groupBox_X轴.Visible = false;
            groupBox_Y轴.Visible = false;
            groupBox_Z轴.Visible = false;
            groupBox_U轴.Visible = false;
            groupBox_R轴.Visible = false;
        }

        /// <summary>
        /// 隐藏所有轴的移动按钮
        /// </summary>
        private void HideAllAxisButtons()
        {
            ShowAxisButtons("X轴", false);
            ShowAxisButtons("Y轴", false);
            ShowAxisButtons("Z轴", false);
            ShowAxisButtons("U轴", false);
            ShowAxisButtons("R轴", false);
        }

        /// <summary>
        /// 显示或隐藏指定轴的移动按钮
        /// </summary>
        /// <param name="axisType">轴类型</param>
        /// <param name="show">是否显示</param>
        private void ShowAxisButtons(string axisType, bool show = true)
        {
            switch (axisType)
            {
                case "X轴":
                    uiImageButton_X轴_正向移动.Visible = show;
                    uiImageButton_X轴_负向移动.Visible = show;
                    break;
                case "Y轴":
                    uiImageButton_Y轴_正向移动.Visible = show;
                    uiImageButton_Y轴_负向移动.Visible = show;
                    break;
                case "Z轴":
                    uiImageButton_Z轴_正向移动.Visible = show;
                    uiImageButton_Z轴_负向移动.Visible = show;
                    break;
                case "U轴":
                    uiImageButton_U轴_正向移动.Visible = show;
                    uiImageButton_U轴_负向移动.Visible = show;
                    break;
                case "R轴":
                    uiImageButton_R轴_正向移动.Visible = show;
                    uiImageButton_R轴_负向移动.Visible = show;
                    break;
            }
        }

        /// <summary>
        /// 更新轴的位置（从控制器获取当前位置并更新UI）
        /// </summary>
        private void UpdateAxisPosition(string axisType, UIDoubleUpDown valueControl, UIDoubleUpDown speedControl,
            Func<MotionPointDto, double> positionSelector, Func<MotionPointDto, double> speedSelector)
        {
            if (_currentMotionPoint == null) return;

            var axisParam = _axisParameters[axisType];
            if (axisParam != null)
            {
                try
                {
                    // 从控制器获取当前位置
                    valueControl.Value = MotionSystem.Instance.MotionController.GetEncPos(axisParam);
                    // 保持速度值不变（从点位数据获取）
                    speedControl.Value = speedSelector(_currentMotionPoint);
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex);
                    ShowErrorMessage("获取位置失败", $"获取{axisType}当前位置时发生错误");
                }
            }
            else
            {
                // 轴参数为空时重置为0
                valueControl.Value = 0;
                speedControl.Value = 0;
            }
        }

        /// <summary>
        /// 更新运动点位的参数值（从UI控件同步到数据对象）
        /// </summary>
        private void UpdateMotionPointValues()
        {
            // 同步X轴参数（如果已绑定）
            if (!string.IsNullOrEmpty(_currentMotionPoint.X轴绑定轴) && _currentMotionPoint.X轴绑定轴 != "未绑定" && _axisParameters["X轴"] != null)
            {
                _currentMotionPoint.X轴目标位置 = uiDoubleUpDown_X_Value.Value;
                _currentMotionPoint.X轴运行速度 = uiDoubleUpDown_X_Speed.Value;
            }

            // 同步Y轴参数
            if (!string.IsNullOrEmpty(_currentMotionPoint.Y轴绑定轴) && _currentMotionPoint.Y轴绑定轴 != "未绑定" && _axisParameters["Y轴"] != null)
            {
                _currentMotionPoint.Y轴目标位置 = uiDoubleUpDown_Y_Value.Value;
                _currentMotionPoint.Y轴运行速度 = uiDoubleUpDown_Y_Speed.Value;
            }

            // 同步Z轴参数
            if (!string.IsNullOrEmpty(_currentMotionPoint.Z轴绑定轴) && _currentMotionPoint.Z轴绑定轴 != "未绑定" && _axisParameters["Z轴"] != null)
            {
                _currentMotionPoint.Z轴目标位置 = uiDoubleUpDown_Z_Value.Value;
                _currentMotionPoint.Z轴运行速度 = uiDoubleUpDown_Z_Speed.Value;
            }

            // 同步U轴参数
            if (!string.IsNullOrEmpty(_currentMotionPoint.U轴绑定轴) && _currentMotionPoint.U轴绑定轴 != "未绑定" && _axisParameters["U轴"] != null)
            {
                _currentMotionPoint.U轴目标位置 = uiDoubleUpDown_U_Value.Value;
                _currentMotionPoint.U轴运行速度 = uiDoubleUpDown_U_Speed.Value;
            }

            // 同步R轴参数
            if (!string.IsNullOrEmpty(_currentMotionPoint.R轴绑定轴) && _currentMotionPoint.R轴绑定轴 != "未绑定" && _axisParameters["R轴"] != null)
            {
                _currentMotionPoint.R轴目标位置 = uiDoubleUpDown_R_Value.Value;
                _currentMotionPoint.R轴运行速度 = uiDoubleUpDown_R_Speed.Value;
            }

            // 更新运动系统中的点位数据
            MotionSystem.Instance.UpdateMotionPointAsync(_currentMotionPoint);
        }

        /// <summary>
        /// 更新点位数据网格中的数据（同步当前点位的最新值）
        /// </summary>
        private void UpdatePointGridView()
        {
            if (uiComboBox_选中点位名称.SelectedIndex < 0 ||
                uiComboBox_选中点位名称.SelectedIndex >= uiDataGridView_点位资讯.Rows.Count)
                return;

            int selectedIndex = uiComboBox_选中点位名称.SelectedIndex;
            var row = uiDataGridView_点位资讯.Rows[selectedIndex];

            // 更新表格中的各轴参数
            row.Cells[2].Value = _currentMotionPoint.X轴目标位置;
            row.Cells[3].Value = _currentMotionPoint.X轴运行速度;
            row.Cells[4].Value = _currentMotionPoint.Y轴目标位置;
            row.Cells[5].Value = _currentMotionPoint.Y轴运行速度;
            row.Cells[6].Value = _currentMotionPoint.Z轴目标位置;
            row.Cells[7].Value = _currentMotionPoint.Z轴运行速度;
            row.Cells[8].Value = _currentMotionPoint.U轴目标位置;
            row.Cells[9].Value = _currentMotionPoint.U轴运行速度;
            row.Cells[10].Value = _currentMotionPoint.R轴目标位置;
            row.Cells[11].Value = _currentMotionPoint.R轴运行速度;
        }

        /// <summary>
        /// 获取轴绑定的属性名称（用于反射或日志）
        /// </summary>
        private string GetAxisBindingProperty(string axisType, bool isPosition)
        {
            switch (axisType)
            {
                case "X轴": return isPosition ? nameof(_currentMotionPoint.X轴目标位置) : nameof(_currentMotionPoint.X轴运行速度);
                case "Y轴": return isPosition ? nameof(_currentMotionPoint.Y轴目标位置) : nameof(_currentMotionPoint.Y轴运行速度);
                case "Z轴": return isPosition ? nameof(_currentMotionPoint.Z轴目标位置) : nameof(_currentMotionPoint.Z轴运行速度);
                case "U轴": return isPosition ? nameof(_currentMotionPoint.U轴目标位置) : nameof(_currentMotionPoint.U轴运行速度);
                case "R轴": return isPosition ? nameof(_currentMotionPoint.R轴目标位置) : nameof(_currentMotionPoint.R轴运行速度);
                default: return null;
            }
        }

        /// <summary>
        /// 获取轴的绑定值（实际绑定的轴名称）
        /// </summary>
        private string GetAxisBindingValue(string axisType)
        {
            if (_currentMotionPoint == null) return null;

            switch (axisType)
            {
                case "X轴": return _currentMotionPoint.X轴绑定轴;
                case "Y轴": return _currentMotionPoint.Y轴绑定轴;
                case "Z轴": return _currentMotionPoint.Z轴绑定轴;
                case "U轴": return _currentMotionPoint.U轴绑定轴;
                case "R轴": return _currentMotionPoint.R轴绑定轴;
                default: return null;
            }
        }

        /// <summary>
        /// 获取轴的当前目标位置
        /// </summary>
        private double GetAxisPosition(string axisType)
        {
            if (_currentMotionPoint == null) return 0;

            switch (axisType)
            {
                case "X轴": return _currentMotionPoint.X轴目标位置;
                case "Y轴": return _currentMotionPoint.Y轴目标位置;
                case "Z轴": return _currentMotionPoint.Z轴目标位置;
                case "U轴": return _currentMotionPoint.U轴目标位置;
                case "R轴": return _currentMotionPoint.R轴目标位置;
                default: return 0;
            }
        }

        /// <summary>
        /// 设置轴的目标位置
        /// </summary>
        private void SetAxisPosition(string axisType, double value)
        {
            if (_currentMotionPoint == null) return;

            switch (axisType)
            {
                case "X轴": _currentMotionPoint.X轴目标位置 = value; break;
                case "Y轴": _currentMotionPoint.Y轴目标位置 = value; break;
                case "Z轴": _currentMotionPoint.Z轴目标位置 = value; break;
                case "U轴": _currentMotionPoint.U轴目标位置 = value; break;
                case "R轴": _currentMotionPoint.R轴目标位置 = value; break;
            }
        }

        /// <summary>
        /// 获取轴的运行速度
        /// </summary>
        private double GetAxisSpeed(string axisType)
        {
            if (_currentMotionPoint == null) return 0;

            switch (axisType)
            {
                case "X轴": return _currentMotionPoint.X轴运行速度;
                case "Y轴": return _currentMotionPoint.Y轴运行速度;
                case "Z轴": return _currentMotionPoint.Z轴运行速度;
                case "U轴": return _currentMotionPoint.U轴运行速度;
                case "R轴": return _currentMotionPoint.R轴运行速度;
                default: return 0;
            }
        }

        /// <summary>
        /// 设置轴的运行速度
        /// </summary>
        private void SetAxisSpeed(string axisType, double value)
        {
            if (_currentMotionPoint == null) return;

            switch (axisType)
            {
                case "X轴": _currentMotionPoint.X轴运行速度 = value; break;
                case "Y轴": _currentMotionPoint.Y轴运行速度 = value; break;
                case "Z轴": _currentMotionPoint.Z轴运行速度 = value; break;
                case "U轴": _currentMotionPoint.U轴运行速度 = value; break;
                case "R轴": _currentMotionPoint.R轴运行速度 = value; break;
            }
        }

        /// <summary>
        /// 位置模式单选按钮事件：启用位置输入，禁用速度输入
        /// </summary>
        private void uiRadioButton_Value_CheckedChanged(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                if (uiRadioButton_Value.Checked && _currentMotionPoint != null)
                {
                    SetAxisValueControlsEnabled(true); // 启用位置输入
                    uiRadioButton_Speed.Checked = false; // 取消速度模式
                }
            });
        }

        /// <summary>
        /// 速度模式单选按钮事件：启用速度输入，禁用位置输入
        /// </summary>
        private void uiRadioButton_Speed_CheckedChanged(object sender, EventArgs e)
        {
            HandleOperation(sender, () =>
            {
                if (uiRadioButton_Speed.Checked && _currentMotionPoint != null)
                {
                    SetAxisValueControlsEnabled(false); // 启用速度输入
                    uiRadioButton_Value.Checked = false; // 取消位置模式
                }
            });
        }

        /// <summary>
        /// 设置所有轴的数值控件启用状态（位置/速度模式切换）
        /// </summary>
        private void SetAxisValueControlsEnabled(bool isPositionEnabled)
        {
            SetAxisControlEnabled("X轴", isPositionEnabled);
            SetAxisControlEnabled("Y轴", isPositionEnabled);
            SetAxisControlEnabled("Z轴", isPositionEnabled);
            SetAxisControlEnabled("U轴", isPositionEnabled);
            SetAxisControlEnabled("R轴", isPositionEnabled);
        }

        /// <summary>
        /// 设置单个轴的控件启用状态
        /// </summary>
        private void SetAxisControlEnabled(string axisType, bool isPositionEnabled)
        {
            if (!_axisParameters.TryGetValue(axisType, out var axisParam) || axisParam == null)
                return;

            // 根据模式启用/禁用位置和速度输入框
            switch (axisType)
            {
                case "X轴":
                    uiDoubleUpDown_X_Value.Enabled = isPositionEnabled;
                    uiDoubleUpDown_X_Speed.Enabled = !isPositionEnabled;
                    break;
                case "Y轴":
                    uiDoubleUpDown_Y_Value.Enabled = isPositionEnabled;
                    uiDoubleUpDown_Y_Speed.Enabled = !isPositionEnabled;
                    break;
                case "Z轴":
                    uiDoubleUpDown_Z_Value.Enabled = isPositionEnabled;
                    uiDoubleUpDown_Z_Speed.Enabled = !isPositionEnabled;
                    break;
                case "U轴":
                    uiDoubleUpDown_U_Value.Enabled = isPositionEnabled;
                    uiDoubleUpDown_U_Speed.Enabled = !isPositionEnabled;
                    break;
                case "R轴":
                    uiDoubleUpDown_R_Value.Enabled = isPositionEnabled;
                    uiDoubleUpDown_R_Speed.Enabled = !isPositionEnabled;
                    break;
            }
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 页面关闭事件：清理资源
        /// </summary>
        private void Frm_Motion_FormClosed(object sender, FormClosedEventArgs e)
        {
            Dispose();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public new void Dispose()
        {
            CleanupEvents(); // 清理事件订阅，防止内存泄漏
            _motionStatusTimer?.Dispose();
            timer_Home?.Dispose();
            _cancellationTokenSource?.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// 清理事件订阅
        /// 避免因事件引用导致的内存泄漏
        /// </summary>
        private void CleanupEvents()
        {
            Load -= Frm_MotionControl_Load;
            FormClosed -= Frm_Motion_FormClosed;

            // 清理定时器事件
            timer_Home.Tick -= timer_Home_Tick;

            // 清理其他事件
            uiComboBox_选中点位名称.SelectedIndexChanged -= uiComboBox_选中点位名称_SelectedIndexChanged;
            uiListBox_点位类型.SelectedIndexChanged -= uiListBox_点位类型_SelectedIndexChanged;
            uiListBox_点位定义.SelectedIndexChanged -= uiListBox_点位定义_SelectedIndexChanged;
            uiDataGridView_点位资讯.CellMouseClick -= uiDataGridView_点位资讯_CellMouseClick;

            uiButton_获取.Click -= uiButton_获取_Click;
            uiButton_设置.Click -= uiButton_设置_Click;
            uiButton_移动.Click -= uiButton_移动_Click;
            uiButton_急停.Click -= uiButton_急停_Click;
            uiButton_使能.Click -= uiButton_使能_Click;
            uiButton_回原点.Click -= uiButton_回原点_Click;
            uiButton_报警复位.Click -= uiButton_报警复位_Click;
        }
        #endregion
    }
}
