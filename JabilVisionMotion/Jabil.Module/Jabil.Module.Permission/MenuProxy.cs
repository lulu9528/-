using Jabil.Lib;
using Jabil.Module.Permission.Common;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// 打开视图事件委托，用于通知其他模块处理视图切换逻辑
    /// </summary>
    /// <param name="sender">事件发送者</param>
    /// <param name="e">事件参数，包含要打开的视图信息</param>
    public delegate void OpenViewEventHandler(object sender, OpenViewEventArgs e);

    /// <summary>
    /// 菜单代理类（部分类）
    /// 负责菜单栏的属性控制、视图切换管理和按钮状态控制
    /// 包含Start/Pause/Stop/Init等按钮的使能控制
    /// 支持标题栏图标高度自适应（通过TitleHeight属性设置）
    /// </summary>
    public partial class MenuProxy
    {
        /// <summary>
        /// 当前激活的视图标识，用于避免重复打开同一视图
        /// </summary>
        private static string currentKey;

        /// <summary>
        /// 打开视图事件，当请求打开新视图时触发
        /// </summary>
        public static event OpenViewEventHandler OnOpenView;

        /// <summary>
        /// 打开视图命令，绑定到UI元素（如按钮、菜单项）
        /// 接收字符串参数作为视图标识（key）
        /// </summary>
        public static ICommand OpenCommand => new DelegateCommand<string>(RaiseOpenView);

        /// <summary>
        /// 触发打开视图事件的内部方法
        /// 处理视图切换前的状态检查和更新
        /// </summary>
        /// <param name="key">要打开的视图标识</param>
        internal static void RaiseOpenView(string key)
        {
            // 检查视图切换的合法性
            if (Check(key))
            {
                // 处理特殊视图的状态更新
                if (key == "初始化")
                {
                    // 初始化时重置暂停状态
                    MenuProxy.Instance.PauseStatus = 0;
                }
                if (key == "暂停")
                {
                    // 切换暂停/继续状态
                    if (Instance.PauseStatus == 1)
                    {
                        key = "继续"; // 状态为1时，实际执行"继续"操作
                    }
                    // 切换暂停状态（0:暂停, 1:继续）
                    Instance.PauseStatus = Instance.PauseStatus == 0 ? 1 : 0;
                }
                // 【修复点3】移除了重复的 "初始化" 状态设置代码
            }

            // 调试输出：记录视图打开请求
            System.Diagnostics.Debug.WriteLine($"请求打开{key}页面");

            // 触发打开视图事件
            if (OnOpenView != null)
            {
                currentKey = key; // 更新当前视图标识
                OnOpenView.Invoke(null, new OpenViewEventArgs(key));

                // 权限视图特殊处理（此处预留逻辑）
                if (key != "权限")
                {
                    // 可添加非权限视图的额外处理逻辑
                }
            }
        }

        /// <summary>
        /// 消息队列，用于显示 Snackbar 通知
        /// 集成 Material Design 组件的消息提示功能
        /// </summary>
        internal ISnackbarMessageQueue MessageQueue { get; set; }

        /// <summary>
        /// 检查视图切换的合法性
        /// </summary>
        /// <param name="key">视图标识</param>
        /// <returns>是否允许切换到该视图</returns>
        private static bool Check(string key)
        {
            // 以下系统操作始终允许执行
            switch (key)
            {
                case "运行":
                case "暂停":
                case "继续":
                case "停止":
                case "初始化":
                case "复位":
                case "退出":
                    return true;
                default:
                    break;
            }

            // 其他视图需检查是否与当前视图相同（避免重复打开）
            return key != currentKey;
        }
    }

    /// <summary>
    /// 菜单代理类（部分类）
    /// 继承自BindableBase，支持属性变化通知，管理UI绑定的属性
    /// </summary>
    public partial class MenuProxy : BindableBase
    {
        /// <summary>
        /// 单例模式实现：确保全局只有一个MenuProxy实例
        /// Lazy<T>保证线程安全且延迟初始化
        /// </summary>
        private static readonly Lazy<MenuProxy> lazy = new Lazy<MenuProxy>(() => new MenuProxy());
        public static MenuProxy Instance => lazy.Value;

        /// <summary>
        /// 设备运行状态变更事件，当设备状态改变时触发
        /// </summary>
        public event EventHandler<MachineRunStatus> MachineRunStatusChanged;

        // 【修复点2】修正了属性名拼写错误（Enabele -> Enable）
        // 按钮使能状态字段
        private bool _enableStart;          // 运行按钮使能
        private bool _enablePause = true;   // 暂停按钮使能（默认允许）
        private bool _enableStop;           // 停止按钮使能
        private bool _enableInit = true;    // 初始化按钮使能（默认允许）
        private int _pauseStatus;          // 暂停状态（0:显示暂停, 1:显示继续）
        private bool _enableReset = true;  // 复位按钮显示/隐藏（默认显示）
        private double _titleHeight = 100; // 标题栏高度（默认100）

        /// <summary>
        /// 显示消息提示
        /// 通过Material Design的Snackbar组件展示
        /// </summary>
        /// <param name="message">要显示的消息内容</param>
        public void ShowMessage(string message)
        {
            // 消息为空或消息队列未初始化时不显示
            if (message is null || string.IsNullOrEmpty(message) || MessageQueue is null)
            {
                return;
            }
            MessageQueue.Enqueue(message);
        }

        // 设备运行模式使能状态
        private bool _enableMachineRunType;
        public bool EnableMachineRunType
        {
            get => _enableMachineRunType;
            set
            {
                _enableMachineRunType = value;
                RaisePropertyChanged(); // 通知UI更新
            }
        }

        /// <summary>
        /// 运行按钮使能状态
        /// </summary>
        public bool EnableStart
        {
            get => _enableStart;
            set
            {
                _enableStart = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 暂停按钮使能状态
        /// </summary>
        public bool EnablePause
        {
            get => _enablePause;
            set
            {
                _enablePause = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 停止按钮使能状态
        /// </summary>
        public bool EnableStop
        {
            get => _enableStop;
            set
            {
                _enableStop = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 初始化按钮使能状态
        /// </summary>
        public bool EnableInit
        {
            get => _enableInit;
            set
            {
                _enableInit = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 复位按钮的显示/隐藏状态
        /// </summary>
        public bool EnableReset
        {
            get => _enableReset;
            set
            {
                _enableReset = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 暂停按钮状态（0:显示"暂停"文本, 1:显示"继续"文本）
        /// </summary>
        public int PauseStatus
        {
            get => _pauseStatus;
            set
            {
                _pauseStatus = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 菜单栏高度（最小值80，确保UI显示正常）
        /// </summary>
        public double TitleHeight
        {
            get => _titleHeight;
            set
            {
                if (value >= 80) // 限制最小高度为80
                {
                    _titleHeight = value;
                    RaisePropertyChanged();
                }
            }
        }

        // 线程安全锁对象（用于多线程访问时的同步）
        private static readonly object Lock_machineRunStatus = new object();

        /// <summary>
        /// 设备运行状态（线程安全访问）
        /// 可能的状态：None（无状态）、HomeSuccess（回零成功）等
        /// </summary>
        private MachineRunStatus _machineRunStatus = MachineRunStatus.None;
        public MachineRunStatus MachineRunStatus
        {
            get
            {
                lock (Lock_machineRunStatus) // 加锁确保线程安全
                    return _machineRunStatus;
            }
            set
            {
                // 【修复点1】修正了锁对象，确保get和set使用同一个锁
                // 【修复点4】将RaisePropertyChanged()和事件触发移入锁内，确保状态变更的原子性
                lock (Lock_machineRunStatus)
                {
                    _machineRunStatus = value;
                    OnMachineRunStatusChanged(_machineRunStatus); // 触发状态变更事件
                    RaisePropertyChanged(); // 通知UI状态已更新
                }
            }
        }

        /// <summary>
        /// 触发设备运行状态变更事件
        /// 通知所有订阅者设备状态已改变
        /// </summary>
        /// <param name="newStatus">新的设备运行状态</param>
        protected virtual void OnMachineRunStatusChanged(MachineRunStatus newStatus)
        {
            MachineRunStatusChanged?.Invoke(this, newStatus);
        }

        // 线程安全锁对象
        private static readonly object Lock_machineRunModel = new object();

        /// <summary>
        /// 设备运行模式（线程安全访问）
        /// 例如：Manual（手动模式）、Auto（自动模式）等
        /// </summary>
        private MachineMode _machineRunModel = MachineMode.Auto;
        public MachineMode MachineRunModel
        {
            get
            {
                lock (Lock_machineRunModel)
                    return _machineRunModel;
            }
            set
            {
                lock (Lock_machineRunModel)
                    _machineRunModel = value;
                RaisePropertyChanged();
            }
        }

        // 线程安全锁对象
        private static readonly object Lock_operateType = new object();

        /// <summary>
        /// 操作模式（线程安全访问）
        /// 例如：Online（联机模式）、Offline（单机模式）等
        /// </summary>
        private OperateMode _operateType = OperateMode.Online;
        public OperateMode OperateType
        {
            get
            {
                lock (Lock_operateType)
                    return _operateType;
            }
            set
            {
                lock (Lock_operateType)
                {
                    // 仅当设备处于回零成功状态时允许切换操作模式
                    if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.HomeSuccess)
                    {
                        _operateType = value;
                    }
                    else
                    {
                        // 状态不满足时显示提示信息
                        ShowMessage("单|联机模式仅允许在 HomeSucess 后才有效！！！");
                    }
                }
                RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// 打开视图事件参数类
    /// 包含要打开的视图名称信息
    /// </summary>
    public class OpenViewEventArgs : EventArgs
    {
        private string _viewName;

        /// <summary>
        /// 构造函数：初始化视图名称
        /// </summary>
        /// <param name="viewKey">视图标识</param>
        public OpenViewEventArgs(string viewKey)
        {
            this._viewName = viewKey;
        }

        /// <summary>
        /// 获取要打开的视图名称
        /// </summary>
        public virtual string ViewName { get => _viewName; }
    }
}