using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.View;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Jabil.Module.Permission.ViewModel
{
    /// <summary>
    /// 主视图模型类，继承自BindableBase以支持属性变化通知
    /// 负责管理主界面的视图切换、命令处理和业务逻辑交互
    /// </summary>
    public class MainViewModel : BindableBase, IDisposable // 实现IDisposable接口
    {
        /// <summary>
        /// 当前激活的视图项，用于控制UI显示哪个视图
        /// </summary>
        private DemoItem _current;

        /// <summary>
        /// 构造函数：初始化命令和事件订阅
        /// </summary>
        /// <param name="snackbarMessageQueue">消息队列，用于显示通知信息</param>
        public MainViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            // 初始化命令
            OpenCommand = new DelegateCommand<string>(OpenView);
            ApplyTempPasswordCommad = new DelegateCommand<string>(ApplyPassword);
            VerifiedTempPasswordCommand = new DelegateCommand<string>(VerifiedPassword);

            // 默认打开登录视图
            OpenView("Login");

            // 订阅退出登录事件
            PermissionSystem.Instance.LoginOutEventHandler += Instance_LoginOutEventHandler;
        }

        /// <summary>
        /// 释放资源，取消事件订阅
        /// </summary>
        public void Dispose()
        {
            // 取消订阅，防止内存泄漏
            if (PermissionSystem.Instance != null)
            {
                PermissionSystem.Instance.LoginOutEventHandler -= Instance_LoginOutEventHandler;
            }
        }

        /// <summary>
        /// 验证临时密码（模拟实现）
        /// </summary>
        /// <param name="obj">临时密码（参数未实际使用，仅为命令格式兼容）</param>
        private void VerifiedPassword(string obj)
        {
            // 随机数模拟密码验证
            Random random = new Random();
            var tem = random.Next(10);

            // 偶数模拟验证成功，奇数模拟失败
            if (tem % 2 == 0)
            {
                // 提升用户权限
                PermissionSystem.Instance.ImproveUserPermission();
                PermissionSystem.ShowMsg("临时管理员登录成功");
            }
            else
            {
                PermissionSystem.ShowMsg("密码验证失败");
            }
        }

        /// <summary>
        /// 申请临时密码
        /// </summary>
        /// <param name="obj">参数未实际使用，仅为命令格式兼容</param>
        private void ApplyPassword(string obj)
        {
            PermissionSystem.ShowMsg("临时密码申请成功，请验证登录");
        }

        /// <summary>
        /// 退出登录事件处理：退出登录时切换到登录视图
        /// </summary>
        private void Instance_LoginOutEventHandler()
        {
            OpenView("Login");
        }

        /// <summary>
        /// 登录成功处理：导航到默认主视图
        /// </summary>
        /// <param name="relt">登录是否成功</param>
        private void LoginIn(bool relt)
        {
            if (relt && this.Current != null)
            {
                this.Current = null;
            }
        }

        /// <summary>
        /// 打开指定视图
        /// </summary>
        /// <param name="obj">视图标识（如"Login"、"Users"等）</param>
        private void OpenView(string obj) // 移除了 async 和 Task.Run
        {
            switch (obj)
            {
                case "Login":
                    // 如果已有登录用户，先执行退出登录
                    if (PermissionSystem.Instance.CurrentUser != null)
                    {
                        // 【修复点5】修正命令名称拼写错误 (Excute -> Execute)
                        PermissionSystem.ExcuteCommand.Execute("LoginOut");
                        Current = null; // 直接在UI线程操作
                    }

                    // 如果当前视图不是登录视图，则切换到登录视图
                    if (Current is null || Current.Name != obj)
                    {
                        // 创建登录用户DTO，传入登录成功后的回调函数
                        var user = new LoginUserDto(LoginIn);
                        // 创建登录视图项
                        Current = new DemoItem("Login", typeof(LoginView), user);
                    }
                    break;

                case "Home": // 假设的默认主视图
                    //Current = new DemoItem("Home", typeof(HomeView)); // 假设存在 HomeView
                    break;

                case "Users":
                    // 检查用户是否有"权限/用户管理"权限
                    if (!PermissionSystem.CheckPermission("权限/用户管理", out string msg))
                    {
                        PermissionSystem.ShowMsg(msg);
                        return;
                    }
                    // 切换到用户管理视图
                    Current = new DemoItem("Users", typeof(UsersView));
                    break;

                case "Roles":
                    // 检查用户是否有"权限/角色管理"权限
                    if (!PermissionSystem.CheckPermission("权限/角色管理", out string msg1))
                    {
                        PermissionSystem.ShowMsg(msg1);
                        return;
                    }
                    // 切换到角色管理视图
                    Current = new DemoItem("Roles", typeof(RolesView));
                    break;

                case "Permission":
                    // 【修复点4】为权限管理视图添加权限检查
                    if (!PermissionSystem.CheckPermission("权限/权限管理", out string msg2))
                    {
                        PermissionSystem.ShowMsg(msg2);
                        return;
                    }
                    // 切换到权限管理视图
                    Current = new DemoItem("Permission", typeof(PermissionView));
                    break;

                default:
                    // 如果请求的视图不存在，可以导航到一个错误视图或默认视图
                    Current = null;
                    break;
            }
        }

        /// <summary>
        /// 打开视图命令，绑定到UI元素，用于触发视图切换
        /// </summary>
        public DelegateCommand<string> OpenCommand { get; }

        /// <summary>
        /// 申请临时密码命令
        /// </summary>
        public DelegateCommand<string> ApplyTempPasswordCommad { get; }

        /// <summary>
        /// 验证临时密码命令
        /// </summary>
        public DelegateCommand<string> VerifiedTempPasswordCommand { get; }

        /// <summary>
        /// 当前激活的视图项属性（用于UI绑定）
        /// </summary>
        public DemoItem Current
        {
            get => _current;
            set => SetProperty(ref _current, value);
        }

        /// <summary>
        /// 视图关闭事件处理（用于登录对话框关闭时的逻辑）
        /// </summary>
        private async void ClosingViewEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            // 检查关闭参数是否为登录用户DTO
            if (eventArgs.Parameter is LoginUserDto user &&
                !string.IsNullOrEmpty(user.Account) &&
                !string.IsNullOrEmpty(user.PassWord))
            {
                // 执行登录验证
                var relt = await PermissionSystem.Instance.Login(user);
                if (relt)
                {
                    // 登录成功，允许关闭对话框
                    return;
                }
                else
                {
                    // 登录失败，给出提示
                    PermissionSystem.ShowMsg("登录失败，请检查账号和密码！");
                }
            }
            // 如果参数无效或登录失败，则取消关闭对话框
            eventArgs.Cancel();
        }
    }
}