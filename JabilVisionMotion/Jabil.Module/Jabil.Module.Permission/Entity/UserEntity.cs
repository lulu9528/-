using SqlSugar;

namespace Jabil.Module.Permission.Entity
{
    /// <summary>
    /// 用户实体类
    /// 继承自BaseEntity，包含基础字段（Id、CreateTime、UpdateTime）
    /// 用于存储系统用户信息及关联的角色，映射到数据库中的UserEntity表
    /// </summary>
    [SugarTable("UserEntity")] // 指定数据库表名为"UserEntity"
    public class UserEntity : BaseEntity
    {
        /// <summary>
        /// 登录账户
        /// 用户登录系统时使用的账号，具有唯一性
        /// 用于身份验证和用户标识
        /// </summary>
        [SugarColumn(ColumnName = nameof(Account))] // 数据库列名与属性名一致
        public string Account { get; set; }

        /// <summary>
        /// 用户名
        /// 用户的显示名称，用于UI展示和友好提示
        /// 可与Account不同，通常为真实姓名或昵称
        /// </summary>
        [SugarColumn(ColumnName = nameof(UserName))] // 数据库列名与属性名一致
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// 用户登录时使用的密码，通常存储加密后的字符串
        /// 用于身份验证
        /// </summary>
        [SugarColumn(ColumnName = nameof(PassWord))] // 数据库列名与属性名一致
        public string PassWord { get; set; }

        /// <summary>
        /// 角色ID字符串
        /// 存储用户关联的角色ID集合，格式为逗号分隔的字符串（如"1,2"）
        /// 实际对应角色ID的列表，通过序列化方式存储，支持用户关联多个角色
        /// </summary>
        [SugarColumn(ColumnName = nameof(RoleId))] // 数据库列名与属性名一致
        public string RoleId { get; set; }

        /// <summary>
        /// 员工编号
        /// 关联企业内部的员工编号，常用于RFID卡登录模式
        /// 可作为用户的唯一标识补充
        /// </summary>
        [SugarColumn(ColumnName = nameof(EmpNo))] // 数据库列名与属性名一致
        public string EmpNo { get; set; }

        /// <summary>
        /// 登录模式
        /// 枚举类型，指定用户的登录方式（如账号密码登录、RFID卡登录等）
        /// 控制登录验证的逻辑分支
        /// </summary>
        [SugarColumn(ColumnName = nameof(LoginMode))] // 数据库列名与属性名一致
        public LoginMode LoginMode { get; set; }
    }
}