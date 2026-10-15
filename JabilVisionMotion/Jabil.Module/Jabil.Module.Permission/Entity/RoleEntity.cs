using SqlSugar;

namespace Jabil.Module.Permission.Entity
{
    /// <summary>
    /// 角色实体类
    /// 继承自BaseEntity，包含基础字段（Id、CreateTime、UpdateTime）
    /// 用于存储系统角色信息及关联的权限，映射到数据库中的RoleEntity表
    /// </summary>
    [SugarTable("RoleEntity")] // 指定数据库表名为"RoleEntity"
    public class RoleEntity : BaseEntity
    {
        /// <summary>
        /// 角色名称
        /// 角色的唯一标识名称，如"超级管理员"、"技术员"、"操作员"
        /// 用于角色的显示和区分
        /// </summary>
        [SugarColumn(ColumnName = nameof(RoleName))] // 数据库列名与属性名一致
        public string RoleName { get; set; }

        /// <summary>
        /// 权限键字符串
        /// 存储角色拥有的权限ID集合，格式为逗号分隔的字符串（如"1,2,3"）
        /// 实际对应权限ID的列表，通过序列化方式存储
        /// </summary>
        [SugarColumn(ColumnName = nameof(PermissionKey))] // 数据库列名与属性名一致
        public string PermissionKey { get; set; }

        ///// <summary>
        ///// 权限ID（已注释，可能为历史遗留或备用字段）
        ///// 原本用于关联单个权限，现改为通过PermissionKey关联多个权限
        ///// </summary>
        ////[SugarColumn(ColumnName = nameof(PermissionId))]
        ////public int PermissionId { get; set; }
    }
}