using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.Modbus.Control;
using Jabil.Module.Modbus.Lib;
using Jabil.Module.Permission;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Jabil.Module.Modbus.Frm
{
    /// <summary>
    /// 机器人Modbus监控页面
    /// 动态生成所有机器人设备的标签页及对应的控制界面
    /// 采用单例模式确保全局唯一实例，避免重复创建资源浪费
    /// </summary>
    public partial class Frm_ModbusRobot : UIPage
    {
        #region 单例模式（线程安全实现）
        private static Frm_ModbusRobot _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static Frm_ModbusRobot Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    lock (_lock)
                    {
                        if (_instance == null || _instance.IsDisposed)
                        {
                            _instance = new Frm_ModbusRobot();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化机器人监控页面
        /// 启用双缓冲减少界面闪烁，提升绘制性能
        /// </summary>
        public Frm_ModbusRobot()
        {
            InitializeComponent();
            // 启用双缓冲和WM_PAINT消息绘制，解决界面闪烁问题
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
        #endregion

        #region 页面初始化与加载
        /// <summary>
        /// 页面加载事件
        /// 加载机器人设备并创建对应的标签页和控制界面
        /// </summary>
        private void Frm_ModbusRobot_Load(object sender, EventArgs e)
        {
            try
            {
                // 初始化机器人设备标签页
                InitializeRobotTabs();
                // 更新权限控制
                UpdatePermission();
                // 订阅用户登录状态变化事件（按需启用）
                // PermissionSystem.Instance.OnLoginUserChanged += OnLoginUserChanged;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 初始化机器人设备标签页
        /// 从Modbus系统获取所有机器人设备，为每个设备创建独立标签页
        /// </summary>
        private void InitializeRobotTabs()
        {
            // 获取所有机器人类型的设备
            var robotDevices = GetRobotDevices();
            if (!robotDevices.Any()) return;

            // 配置标签控件样式
            ConfigureTabControl();

            // 为每个机器人设备创建标签页和控制界面
            foreach (var device in robotDevices)
            {
                var tabPage = CreateRobotTabPage(device);
                var control = CreateModbusControl(device);

                tabPage.Controls.Add(control);
                uiTabControl1.Controls.Add(tabPage);
            }
        }

        /// <summary>
        /// 获取所有机器人类型的设备
        /// </summary>
        /// <returns>机器人设备列表（按站号排序）</returns>
        private List<ModbusCommunicationDto> GetRobotDevices()
        {
            return ModbusSystem.Instance.ModbusCommunications
                .Where(d => d.设备类型 == "Robot")
                .OrderBy(d => d.站号) // 按站号排序，确保标签页顺序固定
                .ToList();
        }

        /// <summary>
        /// 配置标签控件的基础样式
        /// </summary>
        private void ConfigureTabControl()
        {
            uiTabControl1.Alignment = TabAlignment.Bottom; // 标签显示在底部
            uiTabControl1.Dock = DockStyle.Fill; // 填充整个容器
            uiTabControl1.Padding = new Point(10, 3); // 调整标签内边距，优化显示效果
        }
        #endregion

        #region 控件创建方法
        /// <summary>
        /// 为机器人设备创建标签页
        /// </summary>
        /// <param name="device">机器人设备信息</param>
        /// <returns>创建的标签页实例</returns>
        private TabPage CreateRobotTabPage(ModbusCommunicationDto device)
        {
            return new TabPage
            {
                // 标签页名称采用"前缀+设备名+站号"格式，确保唯一性
                Name = $"tab_Robot_{device.设备名称}_{device.站号}",
                Text = device.设备名称, // 标签显示文本
                Size = new Size(1582, 965),
                Location = new Point(0, 40),
                TabIndex = device.站号, // 按站号设置索引，与排序对应
                UseVisualStyleBackColor = true
            };
        }

        /// <summary>
        /// 创建机器人设备的Modbus控制界面
        /// </summary>
        /// <param name="device">机器人设备信息</param>
        /// <returns>创建的控制界面实例</returns>
        private ModbusControlUI CreateModbusControl(ModbusCommunicationDto device)
        {
            return new ModbusControlUI(device)
            {
                Dock = DockStyle.Fill, // 填充整个标签页
                Name = $"ctrl_Robot_{device.设备名称}", // 控件名称加前缀，便于调试
                Size = new Size(1000, 304),
                Location = new Point(3, 3),
                TabIndex = device.站号
            };
        }
        #endregion

        #region 权限控制
        /// <summary>
        /// 用户登录状态变化时更新权限
        /// </summary>
        private void OnLoginUserChanged(object sender, EventArgs e)
        {
            UpdatePermission();
        }

        /// <summary>
        /// 更新页面元素的权限状态
        /// 根据当前登录用户的权限设置控件的可见性或可用性
        /// </summary>
        private void UpdatePermission()
        {
            // 示例：根据权限控制工具按钮可见性
            // toolStripButton_ManualControl.Visible = PermissionSystem.CheckPermission("机器人/手动控制");
        }
        #endregion
    }
}
