using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 菜单数据传输对象
    /// 继承自BaseDto（支持属性变更通知）和IComparable（支持排序）
    /// 用于封装菜单信息，支持树形结构展示和排序
    /// </summary>
    public class MenuDto : BaseDto, IComparable
    {
        /// <summary>
        /// 菜单唯一标识
        /// 用于区分不同的菜单节点
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 排序序号
        /// 用于控制菜单的显示顺序，值越小越靠前
        /// </summary>
        public int Sort { get; set; }

        /// <summary>
        /// 父菜单ID
        /// 用于构建菜单的树形结构，0表示根节点
        /// </summary>
        private int pId;
        public int PId
        {
            get { return pId; }
            set { pId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 菜单标题名称
        /// 显示在UI上的菜单文本
        /// </summary>
        private string titleName;
        public string TitleName
        {
            get { return titleName; }
            set { titleName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 菜单图标
        /// 用于指定菜单显示的图标（通常是图标库中的标识字符串）
        /// </summary>
        private string icon;
        public string Icon
        {
            get { return icon; }
            set { icon = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 子菜单集合
        /// 存储当前菜单的下级菜单，构成树形结构
        /// </summary>
        private List<MenuDto> children;
        public List<MenuDto> Children
        {
            get { return children; }
            set { children = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 实现IComparable接口的比较方法
        /// 用于菜单的排序逻辑
        /// </summary>
        /// <param name="obj">要比较的另一个MenuDto对象</param>
        /// <returns>比较结果：1（当前对象在后）、-1（当前对象在前）、0（相等）</returns>
        /// <exception cref="InvalidCastException">当obj不是MenuDto类型时抛出</exception>
        public int CompareTo(object obj)
        {
            // 将比较对象转换为MenuDto类型
            MenuDto other = obj as MenuDto;
            if (other == null)
                throw new InvalidCastException("无法将对象转换为MenuDto类型进行比较");

            // 首先按Sort属性排序：Sort值小的排在前面
            if (Sort != other.Sort)
                return Sort > other.Sort ? 1 : -1;

            // 当Sort值相同时，按TitleName的字符串顺序排序
            return string.Compare(TitleName, other.TitleName);
        }
    }
}