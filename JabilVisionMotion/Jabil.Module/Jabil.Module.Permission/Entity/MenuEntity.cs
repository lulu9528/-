namespace Jabil.Module.Permission.Entity
{
    /// <summary>
    /// 菜单实体类
    /// 继承自BaseEntity，包含基础字段（Id、CreateTime、UpdateTime）
    /// 用于存储系统菜单的相关信息，支持多级菜单结构
    /// </summary>
    public class MenuEntity : BaseEntity
    {
        /// <summary>
        /// 父级ID
        /// 用于构建多级菜单结构，0表示顶级菜单
        /// </summary>
        public int PId { get; set; }

        /// <summary>
        /// 菜单标题
        /// 显示在UI上的菜单名称，如"用户管理"、"角色管理"
        /// </summary>
        public string TitleName { get; set; }

        /// <summary>
        /// 菜单级别
        /// 表示菜单在层级结构中的深度，如1级、2级菜单
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 排序序号
        /// 用于控制菜单在同一层级中的显示顺序，数值越小越靠前
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 是否可见
        /// 控制菜单是否在UI上显示，true表示可见，false表示隐藏
        /// </summary>
        public bool IsView { get; set; }

        /// <summary>
        /// 目标路由
        /// 用于配置前端路由地址，点击菜单时导航到对应的页面
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// 图标标识
        /// 存储图标的资源标识或编码，用于在菜单前显示对应的图标
        /// </summary>
        public int Icon { get; set; }
    }
}