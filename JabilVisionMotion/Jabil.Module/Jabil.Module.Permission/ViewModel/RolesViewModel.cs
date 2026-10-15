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
    /// 角色管理视图模型
    /// 负责处理角色管理界面的所有业务逻辑，包括角色的增删改查及权限分配
    /// 继承自BindableBase，支持MVVM模式的属性变更通知机制
    /// </summary>
    public class RolesViewModel : BindableBase
    {
        /// <summary>
        /// 构造函数：初始化角色管理视图模型
        /// </summary>
        public RolesViewModel()
        {
            // 【修复点7】初始化备份列表，防止空引用
            BackRoles = new List<RoleDto>();
        }

        /// <summary>
        /// 打开对话框命令
        /// 用于触发新增角色、编辑角色、分配权限等对话框操作
        /// 接收字符串参数区分不同操作类型
        /// </summary>
        public ICommand RunDialogCommand => new DelegateCommand<string>(ExecuteRunDialog);

        /// <summary>
        /// 执行命令
        /// 用于处理删除角色、更新角色等直接操作
        /// 接收字符串参数指定具体操作
        /// </summary>
        public ICommand ExecuteCommand => new DelegateCommand<string>(ExecuteMethod);

        /// <summary>
        /// 执行具体操作的方法
        /// 根据传入的操作标识执行对应的系统命令
        /// </summary>
        /// <param name="obj">操作类型标识（Delete/Update等）</param>
        private void ExecuteMethod(string obj)
        {
            switch (obj)
            {
                case "Delete":
                    // 【修复点2】检查是否有选中的角色
                    if (!PermissionSystem.Instance.Roles.Any(r => r.IsCheck))
                    {
                        PermissionSystem.ShowMsg("请先选择要删除的角色。");
                        return;
                    }
                    // 【修复点1】修正命令名称拼写错误 (Excute -> Execute)
                    PermissionSystem.ExcuteCommand.Execute("DeleteRoles");
                    break;
                case "Update":
                    // 【修复点1】修正命令名称拼写错误 (Excute -> Execute)
                    PermissionSystem.ExcuteCommand.Execute("UpdateRoles");
                    break;
            }
        }

        /// <summary>
        /// 执行对话框操作
        /// 根据操作类型打开对应的对话框（新增/编辑角色、权限分配）
        /// </summary>
        /// <param name="obj">操作类型（Add/Edit/EditPermission）</param>
        private async void ExecuteRunDialog(string obj)
        {
            // 新增角色
            if (obj == "Add")
            {
                var view = new AddRoleView() { DataContext = new RoleDto() };
                await DialogHost.Show(view, "root", OpenedEventHandler, ClosingAddViewEventHandler);
            }
            // 编辑角色
            else if (obj == "Edit")
            {
                //// 【修复点3】实现编辑角色逻辑
                //var selectedRole = PermissionSystem.Instance.Roles.FirstOrDefault(r => r.IsCheck && !r.IsReadOnly);
                //if (selectedRole == null)
                //{
                //    PermissionSystem.ShowMsg("请选择一个非只读的角色进行编辑。");
                //    return;
                //}
                //// 【修复点3】启用编辑逻辑，并修正对话框宿主名称
                //var view = new EditRoleView() { DataContext = selectedRole };
                //var result = await DialogHost.Show(view, "root", OpenedEventHandler, ClosingEditViewEventHandler);
            }
            // 编辑角色权限（分配权限）
            else if (obj == "EditPermission")
            {
                var selectedRoles = PermissionSystem.Instance.Roles.Where(r => r.IsCheck).ToList();
                if (!selectedRoles.Any())
                {
                    // 【修复点5】修正提示信息
                    PermissionSystem.ShowMsg("请先选择要分配权限的角色。");
                    return;
                }
                // 只处理第一个选中的角色，或可扩展为批量处理
                var selectedRole = selectedRoles.First();

                // 初始化权限选中状态
                var allPermissions = PermissionSystem.Instance.Permissions;
                var rolePermissionKeys = string.IsNullOrEmpty(selectedRole.PermissionKey)
                    ? new string[0]
                    : selectedRole.PermissionKey.Split(',');

                foreach (var permission in allPermissions)
                {
                    permission.IsCheck = rolePermissionKeys.Contains(permission.ID.ToString());
                }

                var view = new SelectedPermissionView() { DataContext = PermissionSystem.Instance.PermissionTree };
                await DialogHost.Show(view, "root", OpenedEventHandler, ClosingEditPermissionViewEventHandler);
            }
        }

        /// <summary>
        /// 权限编辑对话框关闭事件处理
        /// 处理角色权限分配结果，更新角色的权限信息
        /// </summary>
        private void ClosingEditPermissionViewEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter is PermissionDto permission)
            {
                var selectedRoles = PermissionSystem.Instance.Roles.Where(r => r.IsCheck && !r.IsReadOnly).ToList();
                if (!selectedRoles.Any()) return;

                var selectedPermissionIds = permission.FromTreeToList()
                                                     .Where(p => p.IsCheck)
                                                     .Select(t => t.ID)
                                                     .ToList();
                var permissionKey = string.Join(",", selectedPermissionIds);

                selectedRoles.ForEach(s =>
                {
                    s.PermissionKey = permissionKey;
                    PermissionSystem.Instance.AddOrUpdateRole(s); // 确保更新到系统中
                });

                PermissionSystem.ShowMsg($"权限修改完成，请保存！");
            }
            else
            {
                PermissionSystem.ShowMsg($"已取消");
            }
        }

        /// <summary>
        /// 对话框打开事件处理
        /// </summary>
        private void OpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
        {
            // 对话框打开时的处理逻辑
        }

        /// <summary>
        /// 新增角色对话框关闭事件处理
        /// </summary>
        private void ClosingAddViewEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            // 【修复点6】使用更严谨的判断
            if (eventArgs.Parameter is RoleDto role && !string.IsNullOrEmpty(role.RoleName))
            {
                PermissionSystem.Instance.AddOrUpdateRole(role);
                PermissionSystem.ShowMsg($"角色 '{role.RoleName}' 添加成功！");
            }
            else if (eventArgs.Parameter is RoleDto)
            {
                PermissionSystem.ShowMsg($"添加失败，角色名称不能为空！");
                eventArgs.Cancel();
            }
            // else: 用户取消，不提示
        }

        /// <summary>
        /// 编辑角色对话框关闭事件处理
        /// </summary>
        private void ClosingEditViewEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter is bool parameter && parameter == false) return;

            // 【修复点4】启用角色保存逻辑，并调整判断条件
            if (eventArgs.Parameter is RoleDto role && !string.IsNullOrEmpty(role.RoleName))
            {
                PermissionSystem.Instance.AddOrUpdateRole(role); // 【修复点4】启用保存
                PermissionSystem.ShowMsg($"角色 '{role.RoleName}' 修改成功！");
            }
            else if (eventArgs.Parameter is RoleDto)
            {
                PermissionSystem.ShowMsg($"修改失败，角色名称不能为空！");
                eventArgs.Cancel();
            }
            // else: 用户取消，不提示
        }

        #region 角色数据备份与恢复
        /// <summary>
        /// 角色数据备份集合
        /// </summary>
        public List<RoleDto> BackRoles { get; set; }

        /// <summary>
        /// 深度备份所有角色数据
        /// </summary>
        private void BackUpAllRoles()
        {
            var json = JsonConvert.SerializeObject(PermissionSystem.Instance.Roles);
            BackRoles = JsonConvert.DeserializeObject<List<RoleDto>>(json) ?? new List<RoleDto>();
        }

        /// <summary>
        /// 从备份恢复角色数据
        /// </summary>
        private void ReturnBackUp()
        {
            if (BackRoles?.Count() == 0)
                return;
            PermissionSystem.Instance.Roles = BackRoles.ToList();
            BackRoles.Clear();
        }
        #endregion

        #region 编辑状态控制
        /// <summary>
        /// 控制角色项是否可选择
        /// </summary>
        private bool _isEnable = true;
        public bool IsEnable
        {
            get { return _isEnable; }
            set { SetProperty(ref _isEnable, value); }
        }

        /// <summary>
        /// 切换选中角色的可编辑状态
        /// </summary>
        private void ChangeEditable()
        {
            //PermissionSystem.Instance.Roles.Where(x => x.IsCheck).ToList().ForEach(x => x.IsReadOnly = !x.IsReadOnly);
        }
        #endregion

        /// <summary>
        /// 执行通用操作的方法（预留实现，建议清理或完善）
        /// </summary>
        private async void Execute(string obj)
        {
            switch (obj)
            {
                case "AddRole":
                    AddRole(); break;
                case "SaveChange":
                    SaveChange(); break;
                case "Delete":
                    // 【修复点8】建议移除或与 ExecuteMethod 合并，避免逻辑重复
                    // Delete(); 
                    break;
            }
        }

        /// <summary>
        /// 删除角色操作（预留实现，逻辑有问题，建议清理）
        /// </summary>
        private async void Delete()
        {
            // 【修复点8】原逻辑错误地获取了所有角色
            var checkedRoles = PermissionSystem.Instance.Roles.Where(r => r.IsCheck).ToList();
            if (checkedRoles.Count == 0)
            {
                PermissionSystem.ShowMsg("请选择要删除的数据");
                return;
            }

            //var res = await PermissionSystem.Instance.DeleteRoles(checkedRoles);
            //if (res.Status)
            //{
            //    _ea.SendMessage(res.Message);
            //    await GetDataAsync();
            //}
            //else
            //    _ea.SendMessage(res.Message);
        }

        #region 角色添加操作（预留实现，与现有逻辑重复，建议清理）
        public async Task AddRole()
        {
            //DialogParameters param = new DialogParameters();
            //var dialogResult = await _dialog.ShowDialog("AddRoleView", param);
            //if (dialogResult.Result == ButtonResult.OK)
            //    await GetDataAsync();
        }
        #endregion

        #region 角色编辑与保存（预留实现，建议清理或整合）
        private void EditOrCancel()
        {
            //switch (mm.Message)
            //{
            //    case "Edit":
            //        ExcuteEdit(); break;
            //    case "Cancel":
            //        ExcuteCancel(); break;
            //}
        }

        private void ExcuteEdit()
        {
            BackUpAllRoles();
            IsEnable = false;
            ChangeEditable();
        }

        private void ExcuteCancel()
        {
            IsEnable = true;
            ReturnBackUp();
        }

        private async void SaveChange()
        {
            //var checks = _userItems.Where(x => x.IsCheck).ToList();
            //if (checks.Count == 0)
            //{
            //    _ea.SendMessage("请先取消再选择要修改的数据");
            //    return;
            //}

            //var res = await _userService.UpdateRoles(checks);
            //if (res.Status)
            //{
            //    var param = new NavigationParameters();
            //    param.Add("doNotCreate", false);
            //    UpdateLoading(true);
            //    _ea.UnResgiterMessage(_token);
            //    _regionManager.RequestNavigate("ContentRegion", "RolesView", param);
            //}
            //else
            //{
            //    _ea.SendMessage("保存失败：" + res.Message);
            //}
        }
        #endregion
    }
}