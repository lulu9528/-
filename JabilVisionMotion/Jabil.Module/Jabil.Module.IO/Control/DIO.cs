using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Sunny.UI;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Jabil.Module.IO.Control
{
    /// <summary>
    /// 数字I/O点位控制显示控件
    /// 用于单个IO点位（输入/输出）的状态展示和交互操作
    /// 核心功能：
    /// 1. 显示点位名称、索引和当前状态（通过指示灯颜色区分）
    /// 2. 支持点击交互（输出点位可触发控制操作）
    /// 3. 确保UI更新线程安全（避免跨线程操作异常）
    /// 继承自SunnyUI的UIUserControl，保持统一的UI风格
    /// </summary>
    public partial class DIO : UIUserControl
    {
        #region 事件定义（交互接口）
        /// <summary>
        /// 点位点击事件委托
        /// 定义事件的参数和返回值规范
        /// </summary>
        /// <param name="sender">触发事件的DIO控件实例（便于定位哪个点位被点击）</param>
        /// <param name="e">事件参数（标准EventArgs）</param>
        public delegate void PointClickEventHandler(object sender, EventArgs e);

        /// <summary>
        /// 点位点击事件
        /// 当用户点击控件时触发（主要用于输出点位的手动控制）
        /// </summary>
        public event PointClickEventHandler PointClick;
        #endregion

        #region 私有字段（状态存储）
        /// <summary>
        /// 点位名称（如"急停输入"、"电机启动输出"）
        /// 私有字段，通过公共属性暴露，确保赋值时触发UI更新
        /// </summary>
        private string _pointName = "DI-";

        /// <summary>
        /// 点位索引（硬件编号，如第3个点位索引为3）
        /// 用于关联硬件点位，确保操作的准确性
        /// </summary>
        private int _pointIndex = 999;

        /// <summary>
        /// 点位状态（0:关闭/断开，1:开启/导通，其他值:未知）
        /// 存储当前点位的实时状态，状态变化时更新指示灯
        /// </summary>
        private int _pointState = 999;
        #endregion

        #region 公共属性（外部访问接口）
        /// <summary>
        /// 点位名称（业务标识）
        /// 用于在UI上显示点位的业务含义（如"传送带到位检测"）
        /// </summary>
        [Description("点位的业务名称，如'急停输入'"), Category("控件定义")]
        public string PointName
        {
            get => _pointName;
            set
            {
                // 仅当值变化时才更新，避免无效UI刷新
                if (_pointName != value)
                {
                    _pointName = value;
                    // 在UI线程更新标签文本（确保线程安全）
                    UpdateControlUI(() => label_点位名称.Text = _pointName);
                }
            }
        }

        /// <summary>
        /// 点位索引（硬件标识）
        /// 对应硬件上的点位编号，用于定位具体点位
        /// </summary>
        [Description("点位的硬件索引编号，与硬件寻址对应"), Category("控件定义")]
        public int PointIndex
        {
            get => _pointIndex;
            set
            {
                if (_pointIndex != value)
                {
                    _pointIndex = value;
                    // 更新索引标签（线程安全）
                    UpdateControlUI(() => label_点位索引.Text = _pointIndex.ToString());
                }
            }
        }

        /// <summary>
        /// 点位状态（0:关闭, 1:开启, 其他:未知）
        /// 状态变化时自动更新指示灯显示
        /// </summary>
        [Description("点位的当前状态：0-关闭，1-开启，其他值-未知"), Category("控件定义")]
        public int PointState
        {
            get => _pointState;
            set
            {
                if (_pointState != value)
                {
                    _pointState = value;
                    // 更新指示灯状态（封装为独立方法）
                    UpdateControlState();
                }
            }
        }
        #endregion

        #region 构造函数与初始化
        /// <summary>
        /// 构造函数
        /// 初始化控件UI并设置绘图优化
        /// </summary>
        public DIO()
        {
            // 初始化Windows窗体控件（设计器生成的代码）
            InitializeComponent();

            // 启用双缓冲和重绘优化，减少UI闪烁（工业场景下提升视觉体验）
            // OptimizedDoubleBuffer：使用双缓冲绘制控件
            // AllPaintingInWmPaint：忽略WM_ERASEBKGND消息，减少背景重绘
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
        #endregion

        #region UI状态更新（核心逻辑）
        /// <summary>
        /// 更新控件状态显示（指示灯）
        /// 根据当前点位状态设置指示灯的状态（Off/On/Blink）
        /// </summary>
        private void UpdateControlState()
        {
            // 在UI线程执行更新（避免跨线程操作异常）
            UpdateControlUI(() =>
            {
                // 根据状态值设置指示灯状态
                switch (_pointState)
                {
                    case 0:
                        // 状态0：关闭/断开 → 指示灯熄灭
                        uiLight_点位状态.State = UILightState.Off;
                        break;
                    case 1:
                        // 状态1：开启/导通 → 指示灯点亮（默认绿色，可在设计器修改）
                        uiLight_点位状态.State = UILightState.On;
                        break;
                    default:
                        // 其他值：未知状态 → 指示灯闪烁（提示异常）
                        uiLight_点位状态.State = UILightState.Blink;
                        break;
                }
            });
        }

        /// <summary>
        /// 安全更新UI（确保在主线程执行）
        /// 封装跨线程UI更新逻辑，避免因多线程操作导致的异常
        /// </summary>
        /// <param name="updateAction">需要在UI线程执行的操作（如更新标签、指示灯）</param>
        private void UpdateControlUI(Action updateAction)
        {
            // 校验参数：若操作为空，直接返回
            if (updateAction == null) return;

            // 使用工具类确保在主线程执行UI更新
            // WFUtil.UpdateMainThreadUIAsync内部会判断是否需要Invoke
            WFUtil.UpdateMainThreadUIAsync(this, updateAction);
        }
        #endregion

        #region 事件触发与处理（交互逻辑）
        /// <summary>
        /// 触发点位点击事件
        /// 封装事件触发逻辑，确保线程安全并处理异常
        /// </summary>
        private void RaisePointClickEvent()
        {
            try
            {
                // 在UI线程触发事件（避免事件处理函数中操作UI时跨线程）
                UpdateControlUI(() =>
                {
                    // 若有订阅者，则触发事件
                    PointClick?.Invoke(this, EventArgs.Empty);
                });
            }
            catch (Exception ex)
            {
                // 记录事件触发异常，便于排查问题
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 点位名称标签点击事件
        /// 转发到自定义的PointClick事件
        /// </summary>
        private void label_点位名称_Click(object sender, EventArgs e) => RaisePointClickEvent();

        /// <summary>
        /// 指示灯点击事件
        /// 转发到自定义的PointClick事件
        /// </summary>
        private void uiLight_点位状态_Click(object sender, EventArgs e) => RaisePointClickEvent();
        #endregion

        #region 控件加载（初始化状态）
        /// <summary>
        /// 控件加载事件
        /// 设置初始状态（指示灯闪烁，等待实际状态更新）
        /// </summary>
        private void DIO_Load(object sender, EventArgs e)
        {
            // 在UI线程执行初始化
            UpdateControlUI(() =>
            {
                // 初始状态：闪烁（表示未获取到实际状态）
                uiLight_点位状态.State = UILightState.Blink;
                // 禁用标签激活状态（保持默认样式）
                label_点位名称.Active = false;
            });
        }
        #endregion
    }
}