using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.View;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Jabil.Module.Permission.ViewModel
{
    /// <summary>
    /// 用户管理视图模型
    /// 负责用户管理界面的逻辑处理，包括用户的新增、编辑、删除等操作
    /// 继承自BindableBase，支持MVVM模式的属性变化通知
    /// </summary>
    public class UsersViewModel : BindableBase
    {
        /// <summary>
        /// 异步测试命令（用于演示异步操作）
        /// </summary>
        private IAsyncCommand _testCommand;

        /// <summary>
        /// 构造函数：初始化用户管理视图模型及命令
        /// </summary>
        public UsersViewModel()
        {
            _testCommand = new AsyncCommand(ExeAsyncMethod);
            // 【修复点5】初始化备份列表，防止空引用
            BackUsers = new List<UserDto>();
        }

        /// <summary>
        /// 打开对话框命令
        /// 用于触发新增用户、编辑用户等对话框操作
        /// 接收字符串参数区分不同操作类型
        /// </summary>
        public ICommand RunDialogCommand => new DelegateCommand<string>(ExecuteRunDialog);

        /// <summary>
        /// 执行命令
        /// 用于处理删除用户、保存更改等直接操作
        /// 接收字符串参数指定具体操作
        /// </summary>
        public ICommand ExecuteCommand => new DelegateCommand<string>(ExecuteMethod);

        /// <summary>
        /// 异步测试命令属性（绑定到UI）
        /// </summary>
        public IAsyncCommand TestCommand
        {
            get => _testCommand;
            set
            {
                _testCommand = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 异步方法示例
        /// 模拟耗时操作（延迟3秒）
        /// </summary>
        private async Task ExeAsyncMethod(object arg)
        {
            await Task.Delay(3000); // 模拟3秒耗时操作
        }

        /// <summary>
        /// 执行具体操作的方法
        /// 根据传入的操作标识执行对应的系统命令
        /// </summary>
        /// <param name="obj">操作类型标识（Delete/SaveChange等）</param>
        private void ExecuteMethod(string obj)
        {
            switch (obj)
            {
                case "Delete":
                    // 【修复点2】检查是否有选中的用户
                    if (!PermissionSystem.Instance.Users.Any(u => u.IsCheck))
                    {
                        PermissionSystem.ShowMsg("请先选择要删除的用户。");
                        return;
                    }
                    // 【修复点1】修正命令名称拼写错误 (Excute -> Execute)
                    PermissionSystem.ExcuteCommand.Execute("DeleteUser");
                    break;
                case "SaveChange":
                    // 【修复点1】修正命令名称拼写错误 (Excute -> Execute)
                    PermissionSystem.ExcuteCommand.Execute("SaveChange");
                    break;
            }
        }

        #region 数据网格全选功能
        /// <summary>
        /// 全选状态标志
        /// 控制数据网格中所有用户项的选中状态
        /// </summary>
        private bool _isAllSelect;
        public bool IsAllSelect
        {
            get { return _isAllSelect; }
            set
            {
                _isAllSelect = value;
                RaisePropertyChanged();
                // 根据全选状态更新所有用户的选中状态
                if (PermissionSystem.Instance.Users != null) // 增加空值判断
                {
                    PermissionSystem.Instance.Users.ToList().ForEach(x => x.IsCheck = _isAllSelect);
                }
            }
        }
        #endregion

        /// <summary>
        /// 执行对话框操作
        /// 根据操作类型打开对应的对话框（新增/编辑用户）
        /// </summary>
        /// <param name="obj">操作类型（Add/Edit）</param>
        private async void ExecuteRunDialog(string obj)
        {
            // 新增用户
            if (obj == "Add")
            {
                var view = new AddUserView() { DataContext = new UserDto() };
                await DialogHost.Show(view, "root", OpenedEventHandler, ClosingAddViewEventHandler);
            }
            // 编辑用户
            else if (obj == "Edit")
            {
                var view = new EditUserView()
                {
                    DataContext = PermissionSystem.Instance.Users.FirstOrDefault(u => u.IsCheck && (!u.IsReadOnly))
                };
                if (view.DataContext is null)
                {
                    PermissionSystem.ShowMsg("请先选择一个非只读的用户进行编辑。");
                    return;
                }
                var result = await DialogHost.Show(view, "root", OpenedEventHandler, ClosingEditViewEventHandler);
            }
        }

        /// <summary>
        /// 对话框打开事件处理
        /// </summary>
        private void OpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
        {
            // 对话框打开时的处理逻辑（预留）
        }

        /// <summary>
        /// 新增用户对话框关闭事件处理
        /// </summary>
        private void ClosingAddViewEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter is bool parameter && parameter == false) return;

            if (eventArgs.Parameter is UserDto user)
            {
                // 【修复点3 & 6】优化验证逻辑，使其更严谨，并给出明确提示
                if (string.IsNullOrEmpty(user.Account) || string.IsNullOrEmpty(user.PassWord))
                {
                    PermissionSystem.ShowMsg("添加失败：账号和密码不能为空！");
                    eventArgs.Cancel();
                    return;
                }
                // 可以根据业务需求添加更多必填项验证，例如 UserName
                if (string.IsNullOrEmpty(user.UserName))
                {
                    PermissionSystem.ShowMsg("添加失败：用户名不能为空！");
                    eventArgs.Cancel();
                    return;
                }

                PermissionSystem.Instance.AddOrUpdateUser(user);
                PermissionSystem.ShowMsg($"用户 '{user.UserName}' 添加成功！");
            }
            else if (eventArgs.Parameter is UserDto)
            {
                PermissionSystem.ShowMsg("添加失败，输入信息不合规范，请重新输入。");
                eventArgs.Cancel();
            }
            // else: 用户取消，不提示
        }

        /// <summary>
        /// 编辑用户对话框关闭事件处理
        /// </summary>
        private void ClosingEditViewEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter is bool parameter && parameter == false) return;

            if (eventArgs.Parameter is UserDto user)
            {
                // 【修复点4 & 6】优化验证逻辑，使其更严谨
                if (string.IsNullOrEmpty(user.Account) || string.IsNullOrEmpty(user.PassWord))
                {
                    PermissionSystem.ShowMsg("修改失败：账号和密码不能为空！");
                    eventArgs.Cancel();
                    return;
                }
                if (string.IsNullOrEmpty(user.UserName))
                {
                    PermissionSystem.ShowMsg("修改失败：用户名不能为空！");
                    eventArgs.Cancel();
                    return;
                }

                PermissionSystem.Instance.AddOrUpdateUser(user);
                PermissionSystem.ShowMsg($"用户 '{user.UserName}' 修改成功！");
            }
            else if (eventArgs.Parameter is UserDto)
            {
                PermissionSystem.ShowMsg("修改失败，输入信息不合规范，请重新输入。");
                eventArgs.Cancel();
            }
            // else: 用户取消，不提示
        }

        #region 用户数据备份与恢复
        /// <summary>
        /// 用户数据备份集合
        /// </summary>
        public List<UserDto> BackUsers { get; set; }

        /// <summary>
        /// 深度备份所有用户数据
        /// </summary>
        private void BackUpAllUsers()
        {
            if (PermissionSystem.Instance.Users == null) return;

            var json = JsonConvert.SerializeObject(PermissionSystem.Instance.Users);
            BackUsers = JsonConvert.DeserializeObject<List<UserDto>>(json) ?? new List<UserDto>();
        }

        /// <summary>
        /// 从备份恢复用户数据
        /// </summary>
        private void ReturnBackUp()
        {
            if (BackUsers?.Count() == 0)
                return;
            PermissionSystem.Instance.Users = BackUsers.ToList();
            BackUsers.Clear();
        }
        #endregion

        #region 编辑状态控制
        /// <summary>
        /// 控制用户项是否可选择
        /// </summary>
        private bool _isEnable = true;
        public bool IsEnable
        {
            get { return _isEnable; }
            set { SetProperty(ref _isEnable, value); }
        }

        /// <summary>
        /// 切换选中用户的可编辑状态
        /// </summary>
        private void ChangeEditable()
        {
            if (PermissionSystem.Instance.Users == null) return;

            PermissionSystem.Instance.Users.Where(x => x.IsCheck)
                                           .ToList()
                                           .ForEach(x => x.IsReadOnly = !x.IsReadOnly);
        }
        #endregion

        // 以下为预留的冗余或未使用的代码块，建议根据实际情况清理
        // ... (Execute, Delete, AddUser, EditOrCancel, ExcuteEdit, ExcuteCancel, SaveChange)
    }
}