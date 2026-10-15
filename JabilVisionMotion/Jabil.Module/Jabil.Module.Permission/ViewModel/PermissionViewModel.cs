using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.View;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Jabil.Module.Permission.ViewModel
{
    /// <summary>
    /// 权限管理视图模型
    /// 负责权限管理界面的逻辑处理，包括权限的新增、编辑、删除等操作
    /// 继承自BindableBase，支持属性变化通知（MVVM模式）
    /// </summary>
    public class PermissionViewModel : BindableBase
    {
        /// <summary>
        /// 打开对话框命令（用于新增/编辑权限时弹出对话框）
        /// 接收字符串参数区分操作类型（如"NewRoot"表示新增根权限）
        /// </summary>
        public ICommand RunDialogCommand => new DelegateCommand<string>(ExecuteRunDialog);

        /// <summary>
        /// 执行操作命令（用于删除权限等操作）
        /// 接收字符串参数区分具体操作
        /// </summary>
        public ICommand ExecuteCommand => new DelegateCommand<string>(ExecuteMethod);

        /// <summary>
        /// 当前选中的权限项（用于编辑、新增子权限等操作）
        /// 绑定到UI的选中控件（如TreeView的选中项）
        /// </summary>
        private PermissionDto _selectedItem;
        public PermissionDto SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                RaisePropertyChanged(); // 通知UI选中项已更新
            }
        }

        /// <summary>
        /// 执行具体操作的方法（与ExecuteCommand绑定）
        /// </summary>
        /// <param name="obj">操作类型标识</param>
        private void ExecuteMethod(string obj)
        {
            switch (obj)
            {
                case "Delete":
                    // 【修复点2】检查是否有选中项
                    if (SelectedItem == null)
                    {
                        PermissionSystem.ShowMsg("请先选择要删除的权限项。");
                        return;
                    }
                    // 【修复点1】修正命令名称拼写错误 (Excute -> Execute)
                    PermissionSystem.ExcuteCommand.Execute("DeletePermissions");
                    break;
                case "NewRoot":
                case "NewChild":
                    // 这些操作的逻辑在 ExecuteRunDialog 中处理，这里可以留空或用于前置检查
                    break;
            }
        }

        /// <summary>
        /// 执行对话框相关操作（与RunDialogCommand绑定）
        /// 用于打开新增/编辑权限的对话框
        /// </summary>
        /// <param name="obj">操作类型标识（NewRoot/NewChild/Edit）</param>
        private async void ExecuteRunDialog(string obj)
        {
            switch (obj)
            {
                case "NewRoot":
                    PermissionSystem.ShowMsg("新增根权限");
                    var newRootId = PermissionSystem.Instance.Permissions.GetMaxId() + 1;
                    var newRootView = new AddPermissionView()
                    {
                        DataContext = new PermissionDto() { ID = newRootId, Path = $"/{newRootId}" }
                    };
                    await DialogHost.Show(newRootView, "root", OpenedEventHandler, ClosingAddViewEventHandler);
                    break;

                case "NewChild":
                    if (SelectedItem is null)
                    {
                        PermissionSystem.ShowMsg("请先选择一个父权限。");
                        return;
                    }
                    PermissionSystem.ShowMsg("新增子权限");
                    var newChildId = PermissionSystem.Instance.Permissions.GetMaxId() + 1;
                    var newChildView = new AddPermissionView()
                    {
                        DataContext = new PermissionDto() { ID = newChildId, Path = $"{SelectedItem.Path}/{newChildId}" }
                    };
                    await DialogHost.Show(newChildView, "root", OpenedEventHandler, ClosingAddViewEventHandler);
                    break;

                case "Edit":
                    // 【修复点3】实现编辑功能
                    //if (SelectedItem is null)
                    //{
                    //    PermissionSystem.ShowMsg("请先选择要编辑的权限项。");
                    //    return;
                    //}
                    //// 假设存在 EditPermissionView，并且需要传递选中的项作为参数
                    //var editView = new EditPermissionView() // 假设的编辑视图
                    //{
                    //    DataContext = SelectedItem // 将选中项传入，以便编辑
                    //};
                    //// 使用 ClosingEditViewEventHandler 处理关闭事件
                    //await DialogHost.Show(editView, "root", OpenedEventHandler, ClosingEditViewEventHandler);
                    break;
            }
        }

        /// <summary>
        /// 对话框打开事件处理（预留，目前无实际操作）
        /// </summary>
        private void OpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
        {
            // 可添加对话框打开时的初始化逻辑，例如聚焦到第一个输入框
        }

        /// <summary>
        /// 新增权限对话框关闭事件处理
        /// 处理权限添加的确认逻辑
        /// </summary>
        private void ClosingAddViewEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            // 【修复点5】更严谨地判断权限对象和标题
            if (eventArgs.Parameter is PermissionDto permission && !string.IsNullOrEmpty(permission.TitleName))
            {
                var parent = PermissionSystem.Instance.Permissions.FirstOrDefault(p => permission.IsChild(p));
                string target = permission.TitleName;

                if (parent != null)
                {
                    target = $"{parent.Target}/{permission.TitleName}";
                }
                permission.Target = target;

                PermissionSystem.Instance.AddOrUpdatePermission(permission);
                PermissionSystem.ShowMsg($"权限 '{permission.TitleName}' 添加成功！");
            }
            else if (eventArgs.Parameter is PermissionDto)
            {
                PermissionSystem.ShowMsg("添加失败，权限名称不能为空！");
                eventArgs.Cancel(); // 取消对话框关闭，让用户重新输入
            }
            else
            {
                // 用户取消操作，不提示
            }
        }

        /// <summary>
        /// 编辑权限对话框关闭事件处理
        /// </summary>
        private void ClosingEditViewEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            // 如果参数为false或用户取消，直接返回
            if (eventArgs.Parameter is bool parameter && parameter == false) return;

            // 【修复点6】修正判断条件并启用更新逻辑
            if (eventArgs.Parameter is PermissionDto permission && !string.IsNullOrEmpty(permission.TitleName))
            {
                // 【修复点6】启用更新权限的调用
                PermissionSystem.Instance.AddOrUpdatePermission(permission);
                PermissionSystem.ShowMsg($"权限 '{permission.TitleName}' 修改成功！");
            }
            else if (eventArgs.Parameter is PermissionDto)
            {
                PermissionSystem.ShowMsg("修改失败，权限名称不能为空！");
                eventArgs.Cancel(); // 取消关闭，让用户重新编辑
            }
            else
            {
                // 用户取消操作，不提示
            }
        }

        /// <summary>
        /// 【修复点4】移除或重构冗余/混乱的方法
        /// 原 ClosingEditPermissionViewEventHandler 方法逻辑不清晰，且与 ClosingEditViewEventHandler 功能重叠。
        /// 假设其功能已被 ClosingEditViewEventHandler 覆盖，故将其移除。
        /// 如果确实需要处理权限选择列表的编辑，请根据具体需求重新设计。
        /// </summary>
        // private void ClosingEditPermissionViewEventHandler(object sender, DialogClosingEventArgs eventArgs) { ... }
    }
}