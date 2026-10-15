using SqlSugar;

namespace Jabil.Module.Permission.Entity
{
    /// <summary>
    /// 权限实体类
    /// 继承自BaseEntity，包含基础字段（Id、CreateTime、UpdateTime）
    /// 用于存储系统权限的相关信息，映射到数据库中的PermissionEntity表
    /// </summary>
    [SugarTable("PermissionEntity")] // 指定数据库表名为"PermissionEntity"
    public class PermissionEntity : BaseEntity
    {
        /// <summary>
        /// 权限名称
        /// 显示在UI上的权限标识名称，如"用户管理权限"、"角色编辑权限"
        /// </summary>
        [SugarColumn(ColumnName = nameof(TitleName))] // 数据库列名与属性名一致
        public string TitleName { get; set; }

        /// <summary>
        /// 权限路径
        /// 权限的唯一标识路径，通常采用层级结构表示，如"/1/2"
        /// 用于权限的父子关系管理和权限校验
        /// </summary>
        [SugarColumn(ColumnName = nameof(Path))] // 数据库列名与属性名一致
        public string Path { get; set; }

        /// <summary>
        /// 权限级别
        /// 表示权限在层级结构中的深度，用于权限的层级展示和管理
        /// </summary>
        [SugarColumn(ColumnName = nameof(Level))] // 数据库列名与属性名一致
        public int Level { get; set; }

        /// <summary>
        /// 目标路径/权限索引
        /// 关联权限对应的功能或页面路径，用于权限与功能的映射
        /// 也可作为权限的索引标识，用于快速查找和校验
        /// </summary>
        [SugarColumn(ColumnName = nameof(Target))] // 数据库列名与属性名一致
        public string Target { get; set; }

        /// <summary>
        /// 排序序号
        /// 用于控制权限在UI展示时的顺序，数值越小越靠前
        /// </summary>
        [SugarColumn(ColumnName = nameof(Sort))] // 数据库列名与属性名一致
        public int Sort { get; set; }
    }
}