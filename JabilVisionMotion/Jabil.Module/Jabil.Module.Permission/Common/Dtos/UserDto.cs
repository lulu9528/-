using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 用户数据传输对象
    /// 继承自BaseDto，支持属性变更通知
    /// 封装用户的基本信息、认证信息和权限关联数据
    /// 用于用户管理、登录验证和权限控制场景
    /// </summary>
    public class UserDto : BaseDto
    {
        /// <summary>
        /// 用户唯一标识
        /// 数据库中的主键，用于区分不同用户
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 登录账号
        /// 用户登录系统时使用的账号名，通常具有唯一性
        /// </summary>
        private string _account;
        public string Account
        {
            get { return _account; }
            set { _account = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 用户姓名
        /// 显示在UI上的用户名称（通常为真实姓名）
        /// </summary>
        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 登录模式
        /// 记录用户的登录方式（如账号密码登录、RFID登录等）
        /// 对应LoginMode枚举类型
        /// </summary>
        public LoginMode LoginMode { get; set; }

        /// <summary>
        /// 登录密码
        /// 用户登录时使用的密码（通常存储加密后的字符串）
        /// </summary>
        private string _passWord;
        public string PassWord
        {
            get { return _passWord; }
            set { _passWord = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 角色ID
        /// 关联用户所属的角色，通过角色ID获取对应的权限集合
        /// 默认值为-1，表示未分配角色
        /// </summary>
        private int _roleId = -1;
        public int RoleId
        {
            get { return _roleId; }
            set { _roleId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 创建时间
        /// 记录用户账号的创建时间，默认为当前时间
        /// </summary>
        private DateTime _createTime = DateTime.Now;
        public DateTime CreateTime
        {
            get { return _createTime; }
            set { _createTime = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 更新时间
        /// 记录用户信息最后一次修改的时间，默认为当前时间
        /// </summary>
        private DateTime _updateTime = DateTime.Now;
        public DateTime UpdateTime
        {
            get { return _updateTime; }
            set { _updateTime = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 勾选状态
        /// 用于用户选择场景（如批量操作时标记选中的用户）
        /// </summary>
        private bool _isCheck = false;
        public bool IsCheck
        {
            get { return _isCheck; }
            set { _isCheck = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 只读状态
        /// 标识用户信息是否可编辑（如系统管理员可能设为只读）
        /// </summary>
        private bool _isReadOnly = false;
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 角色名称
        /// 只读属性，通过当前RoleId从权限系统中获取对应的角色名称
        /// 用于UI显示用户所属角色
        /// </summary>
        public string RoleKey
        {
            get
            {
                // 从权限系统的角色列表中查找当前角色ID对应的角色
                var role = PermissionSystem.Instance.Roles.Find(s => s.Id == _roleId);
                return role != null ? role.RoleName : "NULL";
            }
            set
            {
                // 通过角色名称反向查找角色ID并更新RoleId
                if (string.IsNullOrEmpty(value))
                {
                    _roleId = 1; // 默认为1（可能是"普通用户"角色）
                }
                else
                {
                    // 从权限系统中查找角色名称匹配的角色
                    var role = PermissionSystem.Instance.Roles.FirstOrDefault(s => s.RoleName == value);
                    if (role != null)
                    {
                        _roleId = role.Id;
                    }
                }
                OnPropertyChanged(); // 通知属性变更，触发UI更新
            }
        }

        /// <summary>
        /// 员工编号
        /// 存储用户的员工编号，可能用于RFID登录或其他身份验证场景
        /// </summary>
        private string _empNo;
        public string EmpNo
        {
            get => _empNo;
            set { _empNo = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 认证令牌
        /// 用于记录用户登录后的认证令牌，可能用于API调用或会话管理
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 权限ID集合
        /// 存储用户拥有的所有权限ID，用于快速判断用户是否拥有某项权限
        /// 默认为空列表
        /// </summary>
        public List<int> PermissionKey { get; set; } = new List<int>();
    }
}