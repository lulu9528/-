using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.Service;
using Jabil.Module.Permission.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// 退出登录事件委托，用于通知其他模块处理退出登录逻辑
    /// </summary>
    public delegate void PermissionLoginOutEventHandler();

    /// <summary>
    /// 权限系统核心类，管理用户登录、权限验证、菜单控制等核心功能
    /// 继承自BindableBase，支持属性变化通知（MVVM模式数据绑定基础）
    /// </summary>
    public class PermissionSystem : BindableBase
    {
        // 服务实例：处理具体业务逻辑（用户、登录、角色、权限相关操作）
        private readonly UserService userService;
        private readonly LoginService loginService;
        private readonly RoleService roleService;
        private readonly PermissionService permissionService;

        /// <summary>
        /// 用户登录状态变化事件，当当前登录用户改变时触发
        /// </summary>
        public event EventHandler OnLoginUserChanged;

        /// <summary>
        /// 退出登录事件，当用户退出登录时触发
        /// </summary>
        public event PermissionLoginOutEventHandler LoginOutEventHandler;

        /// <summary>
        /// 是否正在操作（用于检测用户活动状态，防止自动登出）
        /// </summary>
        private bool _isOperating;
        public bool IsOperating
        {
            get => _isOperating;
            set
            {
                // 当用户开始操作时，重置自动登出计时器
                if (value == true)
                {
                    ResetLoginOut();
                }
                // 通过SetProperty更新字段并触发属性变化通知（UI绑定刷新）
                SetProperty(ref _isOperating, value);
            }
        }

        /// <summary>
        /// 登录超时时间（秒），超过此时间无操作则自动登出
        /// </summary>
        public int AlloSeconds = 100;

        /// <summary>
        /// 背景样式属性（用于UI绑定，动态更新背景）
        /// </summary>
        private string _backGround;
        public string BackGround
        {
            get => _backGround;
            set
            {
                _backGround = value;
                RaisePropertyChanged(); // 通知UI属性已更新
            }
        }

        // 单例模式：使用Lazy确保线程安全且延迟初始化
        private static readonly Lazy<PermissionSystem> _lazyInstance = new Lazy<PermissionSystem>(() => new PermissionSystem());
        public static PermissionSystem Instance => _lazyInstance.Value;

        // 初始化完成信号：确保数据加载完毕后再执行操作
        private readonly Task _initializationTask;

        /// <summary>
        /// 构造函数：初始化服务、数据和计时器
        /// </summary>
        private PermissionSystem()
        {
            // 设计模式下不执行初始化（避免设计器报错）
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                _initializationTask = Task.CompletedTask;
                return;
            }

            // 初始化服务实例
            userService = new UserService();
            loginService = new LoginService();
            roleService = new RoleService();
            permissionService = new PermissionService();

            // 确保数据库表存在（首次运行时创建表结构）
            permissionService.EnsureTable();
            roleService.EnsureTable();
            userService.EnsureTable();

            // 异步初始化数据（避免构造函数阻塞）
            _initializationTask = InitializeDataAsync();

            // 初始化基础菜单并默认选择第一个
            _menus = GetInitMenu();
            SelectMenu = _menus.FirstOrDefault();

            // 初始化用户空闲计时器（每10秒检测一次用户活动）
            _idleTime = new System.Timers.Timer(10 * 1000) { AutoReset = true };
            _idleTime.Elapsed += _idleTime_Elapsed;
            _idleTime.Start();
        }

        /// <summary>
        /// 析构函数：清理计时器资源（避免内存泄漏）
        /// </summary>
        ~PermissionSystem()
        {
            _idleTime?.Stop();
            _idleTime?.Dispose();

            timerLogin?.Stop();
            timerLogin?.Dispose();
        }

        /// <summary>
        /// 异步初始化数据：加载用户、角色、权限信息
        /// </summary>
        private async Task InitializeDataAsync()
        {
            await RefreshDataAsync();
        }

        /// <summary>
        /// 空闲计时器事件：当用户超过10秒无操作时，标记为非活跃状态
        /// </summary>
        private void _idleTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            IsOperating = false;
        }

        // 登录表单属性（绑定到登录界面的用户名/密码输入框）
        #region 登录表单属性
        private string userName;
        public string UserName
        {
            get => userName;
            set { userName = value; RaisePropertyChanged(); }
        }

        private string passWord;
        public string PassWord
        {
            get => passWord;
            set { passWord = value; RaisePropertyChanged(); }
        }
        #endregion

        /// <summary>
        /// 当前登录用户
        /// </summary>
        private UserDto _currentUser;
        public UserDto CurrentUser
        {
            get => _currentUser;
            private set
            {
                _currentUser = value;
                RaisePropertyChanged(); // 通知UI更新用户信息
                UpdateMenu(); // 根据用户权限更新菜单
                // 登录成功后启动自动登出计时器
                if (_currentUser != null)
                {
                    AutoLoginOut();
                }
                // 触发登录用户变化事件（通知其他模块）
                OnLoginUserChanged?.Invoke(null, null);
            }
        }

        /// <summary>
        /// 提升用户权限（测试/调试用，降低角色ID即提升权限）
        /// </summary>
        public void ImproveUserPermission()
        {
            if (CurrentUser != null && CurrentUser.RoleId > 1)
            {
                CurrentUser.RoleId -= 1;
                RaisePropertyChanged(nameof(CurrentUser)); // 通知UI权限变化
                UpdateMenu(); // 重新更新菜单
            }
        }

        /// <summary>
        /// 所有用户列表（过滤掉角色ID=1的超级管理员）
        /// </summary>
        private List<UserDto> _users = new List<UserDto>();
        public List<UserDto> Users
        {
            get => _users.Where(s => s.RoleId != 1).ToList();
            set { _users = value ?? new List<UserDto>(); RaisePropertyChanged(); }
        }

        /// <summary>
        /// 菜单列表（根据用户权限动态生成）
        /// </summary>
        private List<MenuDto> _menus = new List<MenuDto>();
        public List<MenuDto> Menus
        {
            get => _menus;
            private set { _menus = value ?? new List<MenuDto>(); RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的菜单
        /// </summary>
        private MenuDto _selectMenu;
        public MenuDto SelectMenu
        {
            get => _selectMenu;
            set
            {
                _selectMenu = value;
                RaisePropertyChanged(); // 通知UI选中状态更新
                // 触发菜单对应的视图打开
                if (_selectMenu != null)
                {
                    MenuProxy.RaiseOpenView(_selectMenu.TitleName);
                }
            }
        }

        /// <summary>
        /// 角色列表（过滤掉超级管理员角色）
        /// </summary>
        private List<RoleDto> _roles = new List<RoleDto>();
        public List<RoleDto> Roles
        {
            get => _roles.Where(s => s.RoleName != "超级管理员").ToList();
            set { _roles = value ?? new List<RoleDto>(); RaisePropertyChanged(); }
        }

        /// <summary>
        /// 权限列表（平级结构）
        /// </summary>
        private ObservableCollection<PermissionDto> _permissions = new ObservableCollection<PermissionDto>();
        public ObservableCollection<PermissionDto> Permissions
        {
            get => _permissions;
            private set { _permissions = value ?? new ObservableCollection<PermissionDto>(); RaisePropertyChanged(); }
        }

        /// <summary>
        /// 权限树（层级结构，用于UI展示权限层级关系）
        /// </summary>
        private PermissionDto _permissionTree;
        public PermissionDto PermissionTree
        {
            get => _permissionTree;
            private set { _permissionTree = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 用户拥有的权限列表
        /// </summary>
        private ObservableCollection<PermissionDto> _userPers = new ObservableCollection<PermissionDto>();
        public ObservableCollection<PermissionDto> UserPers
        {
            get => _userPers;
            private set { _userPers = value ?? new ObservableCollection<PermissionDto>(); RaisePropertyChanged(); }
        }

        /// <summary>
        /// 全局命令：统一处理各种操作（登录、退出、刷新等）
        /// DelegateCommand是自定义命令实现，接收字符串参数区分操作类型
        /// </summary>
        public static ICommand ExcuteCommand => new DelegateCommand<string>(ExcuteMethod);

        /// <summary>
        /// 命令执行方法：根据参数执行对应操作（确保数据初始化完成后执行）
        /// </summary>
        private static async void ExcuteMethod(string obj)
        {
            // 等待数据初始化完成，避免操作空数据
            await Instance._initializationTask;

            switch (obj)
            {
                case "Login": Instance.Login(); break;               // 登录
                case "LoginOut": Instance.LoginOut(); break;         // 退出登录
                case "RefreshData": await Instance.RefreshDataAsync(); break;   // 刷新数据
                case "DeleteUser": await Instance.DeleteUsersAsync(); break;    // 删除用户
                case "SaveChange": await Instance.UpdateUsersAsync(); break;    // 保存用户修改
                case "UpdateRoles": await Instance.UpdateRolesAsync(); break;   // 更新角色
                case "DeleteRoles": await Instance.DeleteRolesAsync(); break;   // 删除角色
                default: break;
            }
        }

        #region 数据增删改查操作
        /// <summary>
        /// 添加或更新用户（公共方法，兼容外部ViewModel调用）
        /// </summary>
        /// <param name="user">用户数据传输对象</param>
        public async void AddOrUpdateUser(UserDto user)
        {
            await AddOrUpdateUserAsync(user);
        }

        /// <summary>
        /// 添加或更新用户（内部异步实现）
        /// </summary>
        private async Task AddOrUpdateUserAsync(UserDto user)
        {
            if (user == null) return;
            var relt = await userService.AddOrUpdateUser(user);
            ShowMsg(relt.Message); // 显示操作结果消息
            if (relt.Status) // 操作成功则刷新数据
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 添加或更新角色（公共方法，兼容外部ViewModel调用）
        /// </summary>
        /// <param name="role">角色数据传输对象</param>
        public async void AddOrUpdateRole(RoleDto role)
        {
            await AddOrUpdateRoleAsync(role);
        }

        /// <summary>
        /// 添加或更新角色（内部异步实现）
        /// </summary>
        private async Task AddOrUpdateRoleAsync(RoleDto role)
        {
            if (role == null) return;
            var relt = await roleService.AddOrUpdateRole(role);
            ShowMsg(relt.Message);
            if (relt.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 添加或更新权限（公共方法，兼容外部ViewModel调用）
        /// </summary>
        /// <param name="permission">权限数据传输对象</param>
        public async void AddOrUpdatePermission(PermissionDto permission)
        {
            await AddOrUpdatePermissionAsync(permission);
        }

        /// <summary>
        /// 添加或更新权限（内部异步实现）
        /// </summary>
        private async Task AddOrUpdatePermissionAsync(PermissionDto permission)
        {
            if (permission == null) return;
            var relt = await permissionService.AddOrUpdatePermission(permission);
            ShowMsg(relt.Message);
            if (relt.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 删除用户（默认删除选中的非只读、非超级管理员用户）
        /// </summary>
        /// <param name="users">要删除的用户列表（可选，默认取选中项）</param>
        private async Task DeleteUsersAsync(IEnumerable<UserDto> users = null)
        {
            if (users == null)
            {
                // 筛选条件：已勾选、非只读、非超级管理员账号
                users = Users.Where(u => u.IsCheck && !u.IsReadOnly && u.Account != "superAdmin");
            }
            if (users == null || !users.Any())
            {
                ShowMsg("请选择要删除的数据");
                return;
            }
            var relt = await userService.DeleteUserAsync(users);
            ShowMsg(relt.Message);
            if (relt.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 删除角色（默认删除选中的非只读、非超级管理员角色）
        /// </summary>
        /// <param name="roles">要删除的角色列表（可选，默认取选中项）</param>
        private async Task DeleteRolesAsync(IEnumerable<RoleDto> roles = null)
        {
            if (roles == null)
            {
                // 筛选条件：已勾选、非只读、非超级管理员角色
                roles = Roles.Where(u => u.IsCheck && !u.IsReadOnly && u.RoleName != "超级管理员");
            }
            if (roles == null || !roles.Any())
            {
                ShowMsg("请选择要删除的数据");
                return;
            }
            var relt = await roleService.DeleteRoleAsync(roles);
            ShowMsg(relt.Message);
            if (relt.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 更新用户信息（默认更新选中的用户）
        /// </summary>
        /// <param name="users">要更新的用户列表（可选，默认取选中项）</param>
        private async Task UpdateUsersAsync(IEnumerable<UserDto> users = null)
        {
            if (users == null)
            {
                users = Users.Where(u => u.IsCheck);
            }
            if (users == null || !users.Any())
            {
                ShowMsg("请选择要保存的数据");
                return;
            }
            var relt = await userService.UpdateUsers(users);
            ShowMsg(relt.Message);
        }

        /// <summary>
        /// 更新角色信息（默认更新选中的角色）
        /// </summary>
        /// <param name="roles">要更新的角色列表（可选，默认取选中项）</param>
        private async Task UpdateRolesAsync(IEnumerable<RoleDto> roles = null)
        {
            if (roles == null)
            {
                roles = Roles.Where(u => u.IsCheck);
            }
            if (roles == null || !roles.Any())
            {
                ShowMsg("请选择要保存的数据");
                return;
            }
            var relt = await roleService.UpdateRoles(roles);
            ShowMsg(relt.Message);
            if (relt.Status)
            {
                await RefreshDataAsync();
            }
        }
        #endregion

        /// <summary>
        /// 是否默认禁用生产统计菜单
        /// </summary>
        public bool DefaultDisableStatistics
        {
            get => defaultDisableStatistics;
            set
            {
                defaultDisableStatistics = value;
                UpdateMenu(); // 菜单状态变化后更新菜单
            }
        }

        /// <summary>
        /// 是否默认禁用流程监控菜单
        /// </summary>
        public bool DefaultDisableMonitor
        {
            get => defaultDisableMonitor;
            set
            {
                defaultDisableMonitor = value;
                UpdateMenu(); // 菜单状态变化后更新菜单
            }
        }

        private bool defaultDisableStatistics;
        private bool defaultDisableMonitor;

        /// <summary>
        /// 初始化基础菜单（不依赖用户权限的默认菜单）
        /// </summary>
        /// <returns>基础菜单列表</returns>
        private List<MenuDto> GetInitMenu()
        {
            var menuDtos = new List<MenuDto>
            {
                new MenuDto { TitleName = "主页", Icon = "Home", Sort = 0 },
                new MenuDto { TitleName = "帮助", Icon = "About", Sort = 8 },
                new MenuDto { TitleName = "权限", Icon = "AccountGroup", Sort = 7 }
            };

            // 根据配置添加生产统计和流程监控菜单
            if (!DefaultDisableStatistics)
            {
                menuDtos.Add(new MenuDto { TitleName = "生产统计", Icon = "ChartBar", Sort = 1 });
            }
            if (!DefaultDisableMonitor)
            {
                menuDtos.Add(new MenuDto { TitleName = "流程监控", Icon = "StoreCog", Sort = 2 });
            }

            // 按排序号排序
            menuDtos.Sort((a, b) => a.Sort.CompareTo(b.Sort));
            return menuDtos;
        }

        /// <summary>
        /// 根据用户权限更新菜单（动态显示用户有权访问的菜单）
        /// </summary>
        private void UpdateMenu()
        {
            // 先获取基础菜单
            var menuDtos = GetInitMenu();

            // 根据权限添加其他菜单（视觉、设置、手动、报警）
            if (CheckPermission("视觉", out _))
            {
                menuDtos.Add(new MenuDto { TitleName = "视觉", Icon = "Camera", Sort = 3 });
            }
            if (CheckPermission("设置", out _))
            {
                menuDtos.Add(new MenuDto { TitleName = "设置", Icon = "Settings", Sort = 4 });
            }
            if (CheckPermission("手动", out _))
            {
                menuDtos.Add(new MenuDto { TitleName = "手动", Icon = "HandBackRight", Sort = 5 });
            }
            if (CheckPermission("报警", out _))
            {
                menuDtos.Add(new MenuDto { TitleName = "报警", Icon = "AlarmOff", Sort = 6 });
            }

            // 排序后更新菜单列表
            menuDtos.Sort((a, b) => a.Sort.CompareTo(b.Sort));
            Menus = menuDtos;
        }

        /// <summary>
        /// 刷新所有数据（用户、角色、权限）
        /// </summary>
        private async Task RefreshDataAsync()
        {
            // 加载所有角色
            var roleResult = await roleService?.GetAllRolesAsync();
            if (roleResult?.Status == true)
            {
                Roles = roleResult.Result;
            }

            // 加载所有权限并构建权限树
            var permissionResult = await permissionService?.GetAllPermissionsAsync();
            if (permissionResult?.Status == true)
            {
                Permissions = new ObservableCollection<PermissionDto>(permissionResult.Result);
                PermissionTree = new PermissionDto { TitleName = "root" }; // 根节点
                GetPermissionTree(PermissionTree, Permissions); // 构建层级结构
            }

            // 加载所有用户
            var userResult = await userService?.GetAllUsersAsync();
            if (userResult?.Status == true)
            {
                Users = userResult.Result;
            }
        }

        /// <summary>
        /// 递归构建权限树（层级结构）
        /// </summary>
        /// <param name="parent">父权限节点</param>
        /// <param name="rootsource">所有权限列表</param>
        private void GetPermissionTree(PermissionDto parent, IList<PermissionDto> rootsource)
        {
            if (parent == null || rootsource == null || !rootsource.Any()) return;

            // 查找当前父节点的子权限
            var children = rootsource.Where(p => p.IsChild(parent));
            foreach (var child in children)
            {
                parent.Children.Add(child); // 添加到父节点的子列表
                child.Parent = parent; // 设置父节点引用
                GetPermissionTree(child, rootsource); // 递归构建子树
            }
        }

        /// <summary>
        /// 登录方法（无参数，绑定到UI命令）- 公共可访问
        /// </summary>
        public async void Login()
        {
            await LoginInternalAsync();
        }

        /// <summary>
        /// 登录方法（带参数重载，供外部调用）- 公共可访问
        /// </summary>
        /// <param name="login">登录信息（账号密码）</param>
        /// <returns>登录是否成功</returns>
        public async Task<bool> Login(LoginUserDto login)
        {
            return await LoginAsync(login);
        }

        /// <summary>
        /// 内部登录逻辑（无参数）
        /// </summary>
        private async Task LoginInternalAsync()
        {
            // 测试账号快捷登录（Admin/123）
            if (UserName == "Admin" && PassWord == "123")
            {
                CurrentUser = new UserDto
                {
                    Account = "Admin",
                    PassWord = "123",
                    RoleId = 1,
                    UserName = "管理员"
                };
                ShowMsg($"登录成功，欢迎{CurrentUser.UserName}，请继续其他操作");
                PassWord = string.Empty; // 清空密码
                return;
            }

            // 验证账号密码不为空
            if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(PassWord))
            {
                ShowMsg("输入账号或密码为空，请重新输入！");
                return;
            }

            // 调用登录服务验证
            var loginResult = await loginService.Login(new LoginUserDto
            {
                Account = UserName,
                PassWord = PassWord
            });

            if (loginResult?.Status == true)
            {
                CurrentUser = loginResult.Result;
                ShowMsg($"登录成功，欢迎{CurrentUser.UserName}，请继续其他操作");
                PassWord = string.Empty; // 清空密码
            }
            else
            {
                var errorMsg = loginResult?.Message ?? "登录失败，请检查账号密码！";
                ShowMsg(errorMsg);
            }
        }

        /// <summary>
        /// 内部登录逻辑（带参数）
        /// </summary>
        private async Task<bool> LoginAsync(LoginUserDto login)
        {
            if (login == null || string.IsNullOrWhiteSpace(login.Account) || string.IsNullOrWhiteSpace(login.PassWord))
            {
                ShowMsg("账号或密码不能为空！");
                return false;
            }

            var loginResult = await loginService.Login(login);
            if (loginResult?.Status == true)
            {
                CurrentUser = loginResult.Result;
                ShowMsg($"登录成功，欢迎{CurrentUser.UserName}，请继续其他操作");
                PassWord = string.Empty; // 清空密码
                return true;
            }
            else
            {
                var errorMsg = loginResult?.Message ?? "登录失败，请检查账号密码！";
                ShowMsg(errorMsg);
                return false;
            }
        }

        /// <summary>
        /// 退出登录
        /// </summary>
        public void LoginOut()
        {
            CurrentUser = null; // 清空当前用户
            LoginOutEventHandler?.Invoke(); // 触发退出登录事件
            ShowMsg("退出登录完成！");
        }

        // 自动登出计时器
        private System.Timers.Timer timerLogin;

        /// <summary>
        /// 启动自动登出计时器（超时后自动退出登录）
        /// </summary>
        private void AutoLoginOut()
        {
            if (timerLogin == null)
            {
                timerLogin = new System.Timers.Timer(1000 * AlloSeconds) { AutoReset = false };
                timerLogin.Elapsed += Timer_Elapsed;
            }
            else
            {
                timerLogin.Stop();
            }
            timerLogin.Start(); // 启动/重启计时器
        }

        /// <summary>
        /// 重置自动登出计时器（用户操作时调用，延长超时时间）
        /// </summary>
        private void ResetLoginOut()
        {
            if (timerLogin != null)
            {
                timerLogin.Stop();
                timerLogin.Start();
            }
        }

        /// <summary>
        /// 自动登出计时器超时事件：在UI线程中执行退出登录
        /// </summary>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 使用Dispatcher确保在UI线程执行（避免跨线程异常）
            Application.Current.Dispatcher.Invoke(() => LoginOut());
        }

        /// <summary>
        /// 在UI上显示消息（通过Snackbar组件，确保线程安全）
        /// </summary>
        /// <param name="msg">要显示的消息</param>
        public static void ShowMsg(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;

            // 确保在UI线程执行消息显示
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MainView.Snackbar?.MessageQueue != null)
                {
                    MainView.Snackbar.MessageQueue.Enqueue(msg);
                }
                else
                {
                    // 降级处理：Snackbar不可用时使用MessageBox
                    MessageBox.Show(msg, "系统提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
        }

        /// <summary>
        /// 检查用户是否有指定视图的权限
        /// </summary>
        /// <param name="viewKey">视图标识（如"生产统计"）</param>
        /// <param name="msg">权限检查结果消息（无权限时返回原因）</param>
        /// <returns>是否有权限</returns>
        public static bool CheckPermission(string viewKey, out string msg)
        {
            msg = string.Empty;

            // 未登录状态：无权限
            if (Instance == null || Instance.CurrentUser == null)
            {
                msg = "用户未登录，请先登录！";
                return false;
            }

            // 数据未初始化完成：无权限
            if (!Instance._initializationTask.IsCompleted || Instance._roles == null || !Instance._roles.Any())
            {
                msg = "系统数据尚未加载完成，请稍候再试！";
                return false;
            }

            // 获取用户所属角色
            var userRole = Instance._roles.FirstOrDefault(r => r.Id == Instance.CurrentUser.RoleId);
            if (userRole == null)
            {
                msg = "用户未分配角色，请联系管理员！";
                return false;
            }

            // 角色无权限配置：无权限
            if (string.IsNullOrWhiteSpace(userRole.PermissionKey))
            {
                msg = "当前角色未配置任何权限，请联系管理员！";
                return false;
            }

            // 解析角色拥有的权限ID
            var rolePermissionIds = userRole.PermissionKey
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id =>
                {
                    int.TryParse(id, out int result);
                    return result;
                })
                .Where(id => id > 0)
                .ToList();

            // 检查是否有目标视图的权限
            var hasPermission = Instance.Permissions != null && Instance.Permissions
                .Where(p => rolePermissionIds.Contains(p.ID))
                .Any(p => p.Target == viewKey);

            if (!hasPermission)
            {
                msg = $"权限不足，无法访问「{viewKey}」，请联系管理员！";
            }

            return hasPermission;
        }

        // 字段定义
        private System.Timers.Timer _idleTime;
    }
}