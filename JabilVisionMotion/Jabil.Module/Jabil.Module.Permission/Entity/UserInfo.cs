using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Entity
{
    /// <summary>
    /// 用户信息实体类
    /// 用于存储用户的基础认证信息和权限标识
    /// 映射到数据库中的"User"表
    /// </summary>
    [SugarTable("User")] // 指定数据库表名为"User"
    public class UserInfo
    {
        /// <summary>
        /// 用户名
        /// 用于用户登录和身份标识，通常具有唯一性
        /// </summary>
        [SugarColumn(ColumnName = nameof(UserName))] // 数据库列名与属性名保持一致
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// 用户登录时的验证密码，建议存储加密后的字符串
        /// 注意：属性名"PassWorld"可能为笔误（应为"Password"）
        /// </summary>
        [SugarColumn(ColumnName = nameof(PassWorld))] // 数据库列名与属性名保持一致
        public string PassWorld { get; set; }

        /// <summary>
        /// 权限字符串
        /// 存储用户拥有的权限标识，通常为逗号分隔的权限ID或权限编码
        /// 用于快速判断用户是否拥有特定权限
        /// </summary>
        [SugarColumn(ColumnName = nameof(Permission))] // 数据库列名与属性名保持一致
        public string Permission { get; set; }
    }
}