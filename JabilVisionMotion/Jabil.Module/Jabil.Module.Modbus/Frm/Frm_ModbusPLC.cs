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
    /// PLC设备监控页面
    /// 动态生成所有PLC设备的标签页及对应的控制界面
    /// 采用单例模式确保全局唯一实例，避免重复创建
    /// </summary>
    public partial class Frm_ModbusPLC : UIPage
    {
        #region 单例模式（线程安全）
        private static Frm_ModbusPLC _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static Frm_ModbusPLC Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    lock (_lock)
                    {
                        if (_instance == null || _instance.IsDisposed)
                        {
                            _instance = new Frm_ModbusPLC();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化PLC监控页面
        /// 启用双缓冲减少界面闪烁
        /// </summary>
        public Frm_ModbusPLC()
        {
            InitializeComponent();
            // 启用双缓冲和WM_PAINT消息绘制，提升界面流畅度
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
        #endregion

        #region 页面加载与初始化
        /// <summary>
        /// 页面加载事件
        /// 加载PLC设备并创建对应的标签页和控制界面
        /// </summary>
        private void Frm_ModbusPLC_Load(object sender, EventArgs e)
        {
            try
            {
                // 初始化标签页
                InitializePlcTabs();
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
        /// 初始化PLC设备标签页
        /// 从Modbus系统获取所有PLC设备，为每个设备创建独立标签页
        /// </summary>
        private void InitializePlcTabs()
        {
            // 筛选出所有PLC类型的设备
            var plcDevices = GetPlcDevices();
            if (!plcDevices.Any()) return;

            // 配置标签控件样式
            ConfigureTabControl();

            // 为每个PLC设备创建标签页
            foreach (var device in plcDevices)
            {
                var tabPage = CreatePlcTabPage(device);
                var control = CreateModbusControl(device);

                tabPage.Controls.Add(control);
                uiTabControl1.Controls.Add(tabPage);
            }
        }

        /// <summary>
        /// 获取所有PLC类型的设备
        /// </summary>
        /// <returns>PLC设备列表</returns>
        private List<ModbusCommunicationDto> GetPlcDevices()
        {
            return ModbusSystem.Instance.ModbusCommunications
                .Where(d => d.设备类型 == "PLC")
                .OrderBy(d => d.站号) // 按站号排序，保证标签页顺序一致
                .ToList();
        }

        /// <summary>
        /// 配置标签控件样式
        /// </summary>
        private void ConfigureTabControl()
        {
            uiTabControl1.Alignment = TabAlignment.Bottom; // 标签显示在底部
            uiTabControl1.Dock = DockStyle.Fill; // 填充整个容器
        }
        #endregion

        #region 控件创建
        /// <summary>
        /// 为PLC设备创建标签页
        /// </summary>
        /// <param name="device">PLC设备信息</param>
        /// <returns>创建的标签页</returns>
        private TabPage CreatePlcTabPage(ModbusCommunicationDto device)
        {
            return new TabPage
            {
                Name = $"tab_{device.设备名称}_{device.站号}", // 唯一名称（设备名+站号）
                Text = device.设备名称, // 标签显示文本
                Size = new Size(1582, 965),
                Location = new Point(0, 40),
                TabIndex = device.站号,
                UseVisualStyleBackColor = true
            };
        }

        /// <summary>
        /// 创建Modbus控制界面
        /// </summary>
        /// <param name="device">PLC设备信息</param>
        /// <returns>创建的控制界面</returns>
        private ModbusControlUI CreateModbusControl(ModbusCommunicationDto device)
        {
            return new ModbusControlUI(device)
            {
                Dock = DockStyle.Fill, // 填充标签页
                Name = $"ctrl_{device.设备名称}",
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
        /// 更新页面元素权限
        /// 根据当前用户权限设置控件的可见性或可用性
        /// </summary>
        private void UpdatePermission()
        {
            // 示例：根据权限控制工具按钮可见性
            // toolStripButton_IOControl.Visible = PermissionSystem.CheckPermission("手动/IO/监控");
        }
        #endregion
    }
}
