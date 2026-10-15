using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 角色数据传输对象
    /// 继承自BaseDto，支持属性变更通知
    /// 用于封装角色信息，包括角色基本属性和关联的权限信息
    /// </summary>
    public class RoleDto : BaseDto
    {
        /// <summary>
        /// 角色唯一标识
        /// 用于区分不同的角色
        /// </summary>
        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(); // 通知UI属性值变更
            }
        }

        /// <summary>
        /// 角色是否为只读状态
        /// 标识该角色是否可编辑（如系统内置角色可能设为只读）
        /// </summary>
        private bool _isReadOnly = false;
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set
            {
                _isReadOnly = value;
                OnPropertyChanged(); // 通知UI属性值变更
            }
        }

        /// <summary>
        /// 角色是否被勾选
        /// 用于角色选择场景（如用户分配角色时标记选中状态）
        /// </summary>
        private bool _isChecked = false;
        public bool IsCheck
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged(); // 通知UI属性值变更
            }
        }

        /// <summary>
        /// 角色名称
        /// 显示在UI上的角色标识（如"管理员"、"普通用户"）
        /// </summary>
        private string _roleName;
        public string RoleName
        {
            get => _roleName;
            set
            {
                _roleName = value;
                OnPropertyChanged(); // 通知UI属性值变更
            }
        }

        /// <summary>
        /// 权限关键字字符串
        /// 存储该角色拥有的权限ID集合（通常以逗号分隔，如"1,3,5"）
        /// 用于快速查询和保存角色关联的权限
        /// </summary>
        public string PermissionKey { get; set; }

        /// <summary>
        /// 角色创建时间
        /// 记录角色的创建时间，默认为当前时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 角色更新时间
        /// 记录角色信息最后一次修改的时间
        /// </summary>
        private DateTime _updateTime;
        public DateTime UpdateTime
        {
            get => _updateTime;
            set
            {
                _updateTime = value;
                OnPropertyChanged(); // 通知UI属性值变更
            }
        }
    }
}