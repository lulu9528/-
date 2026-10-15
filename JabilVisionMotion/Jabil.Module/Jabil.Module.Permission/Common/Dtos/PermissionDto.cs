using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 权限数据传输对象
    /// 继承自BaseDto，支持属性变更通知
    /// 用于封装权限信息，支持树形结构展示和权限勾选的级联操作
    /// </summary>
    public class PermissionDto : BaseDto
    {
        // 注释：原始代码中存在一个被注释的构造函数，可能用于初始化基础属性，此处保留注释以说明历史逻辑
        // public PermissionDto(int id, int pId, int level, bool isPermission, int sort) : base(id, pId, level, isPermission, sort)
        // {
        // }

        /// <summary>
        /// 权限唯一标识
        /// 用于区分不同的权限节点
        /// </summary>
        private int _id;
        public int ID
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(); // 通知UI属性值变更
            }
        }

        /// <summary>
        /// 权限目标标识
        /// 可能用于关联具体的操作对象（如按钮、菜单等）
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// 权限路径
        /// 用于表示权限在树形结构中的层级路径（类似文件路径，如"/系统管理/用户管理"）
        /// 辅助判断权限间的父子关系
        /// </summary>
        private string _path;
        public string Path
        {
            get { return _path; }
            set { _path = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 父权限名称
        /// 用于显示当前权限的父级权限名称，便于UI展示
        /// </summary>
        private string _parentName;
        public string ParentName
        {
            get { return _parentName; }
            set { _parentName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 权限标题名称
        /// 显示在UI上的权限名称（如"用户管理"、"角色分配"）
        /// </summary>
        private string _titleName;
        public string TitleName
        {
            get => _titleName;
            set
            {
                _titleName = value;
                OnPropertyChanged(); // 通知UI属性值变更
            }
        }

        /// <summary>
        /// 权限勾选状态
        /// 标识当前权限是否被勾选（授予），支持级联操作
        /// </summary>
        private bool _isCheck;
        public bool IsCheck
        {
            get { return _isCheck; }
            set
            {
                _isCheck = value;

                // 勾选状态为true时，级联勾选所有父权限
                if (_isCheck)
                {
                    // 从权限树中查找当前权限的父节点
                    var parent = this.FindParent(PermissionSystem.Instance.PermissionTree);
                    if (parent != null)
                    {
                        parent.IsCheck = true; // 勾选父权限
                    }
                }
                // 勾选状态为false时，级联取消所有子权限
                else
                {
                    if (this.Children != null)
                    {
                        foreach (var child in this.Children)
                        {
                            child.IsCheck = false; // 取消所有子权限勾选
                        }
                    }
                }

                OnPropertyChanged(); // 通知UI属性值变更
            }
        }

        /// <summary>
        /// 子权限集合
        /// 存储当前权限的下级权限，构成权限树形结构
        /// 使用ObservableCollection支持UI自动更新
        /// </summary>
        private ObservableCollection<PermissionDto> _children = new ObservableCollection<PermissionDto>();
        public ObservableCollection<PermissionDto> Children
        {
            get => _children;
            set { _children = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 父权限引用
        /// 指向当前权限的父级权限，便于快速访问父节点
        /// </summary>
        public PermissionDto Parent { get; set; }
    }

    /// <summary>
    /// 权限DTO扩展方法类
    /// 提供权限树形结构处理的辅助方法，如父子关系判断、对象复制、树形转列表等
    /// </summary>
    public static class PermissionDtoExtension
    {
        /// <summary>
        /// 判断当前权限是否是目标权限的子权限
        /// </summary>
        /// <param name="temp">当前权限</param>
        /// <param name="parent">目标父权限</param>
        /// <returns>如果是子权限返回true，否则返回false</returns>
        public static bool IsChild(this PermissionDto temp, PermissionDto parent)
        {
            if (temp.Path is null) return false;

            // 计算当前权限路径的深度（斜杠数量）
            var depth1 = temp.Path.Count(s => s == '/');

            // 特殊处理根节点（标题为"root"且无路径）：根节点的直接子节点深度为1
            if (parent.Path is null && parent.TitleName == "root" && depth1 == 1)
                return true;
            else if (parent.Path is null && parent.TitleName == "root" && depth1 != 1)
                return false;

            // 计算父权限路径的深度
            var depth2 = parent.Path.Count(s => s == '/');

            // 子权限深度应为父权限深度+1，且路径前缀与父权限路径一致
            if (depth1 == depth2 + 1 &&
                temp.Path.Remove(temp.Path.LastIndexOf('/')) == parent.Path)
                return true;

            return false;
        }

        /// <summary>
        /// 判断当前权限是否是目标权限的父权限
        /// </summary>
        /// <param name="parent">当前权限</param>
        /// <param name="temp">目标子权限</param>
        /// <returns>如果是父权限返回true，否则返回false</returns>
        public static bool IsParent(this PermissionDto parent, PermissionDto temp)
        {
            if (temp.Path is null) return false;

            // 计算子权限路径的深度
            var depth1 = temp.Path.Count(s => s == '/');

            // 特殊处理根节点（标题为"root"且无路径）：根节点的直接子节点深度为1
            if (parent.Path is null && parent.TitleName == "root" && depth1 == 1)
                return true;
            else if (parent.Path is null && parent.TitleName == "root" && depth1 != 1)
                return false;

            // 计算当前权限路径的深度
            var depth2 = parent.Path.Count(s => s == '/');

            // 父权限深度应为子权限深度-1，且子权限路径以父权限路径为前缀
            if (depth1 == depth2 + 1 && temp.Path.StartsWith(parent.Path))
                return true;

            return false;
        }

        /// <summary>
        /// 复制权限对象（深拷贝）
        /// 使用JSON序列化与反序列化实现对象的完全复制
        /// </summary>
        /// <param name="source">源权限对象</param>
        /// <param name="destination">目标权限对象（通过引用传递）</param>
        public static void Copy(this PermissionDto source, ref PermissionDto destination)
        {
            // 序列化源对象为JSON字符串
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(source);
            // 反序列化JSON字符串为新对象，实现深拷贝
            destination = Newtonsoft.Json.JsonConvert.DeserializeObject<PermissionDto>(jsonStr);
        }

        /// <summary>
        /// 在权限树中查找当前权限的父节点
        /// </summary>
        /// <param name="child">当前子权限</param>
        /// <param name="source">权限树根节点</param>
        /// <returns>找到的父权限，未找到则返回null</returns>
        public static PermissionDto FindParent(this PermissionDto child, PermissionDto source)
        {
            // 检查当前节点是否为父节点
            if (child.IsChild(source))
                return source;

            PermissionDto result = null;
            // 递归查找子节点中的父节点
            foreach (var item in source.Children)
            {
                result = FindParent(child, item);
                if (result != null)
                {
                    return result; // 找到后立即返回
                }
            }
            return result;
        }

        /// <summary>
        /// 获取权限勾选的关键字（未实现完整逻辑）
        /// 计划用于收集勾选的权限ID，可能用于权限保存
        /// </summary>
        /// <param name="permissionDto">权限对象</param>
        /// <param name="str">存储结果的字符串</param>
        public static void GetPermissionKey(this PermissionDto permissionDto, string str)
        {
            string key = string.Empty;
            // 注释：原始代码中存在未实现的逻辑框架，可能用于拼接勾选的权限ID
            // if (permissionDto.IsCheck&&permissionDto.ID>0)
            // {
            //     key += permissionDto.ID+",";
            // }
            // foreach (var item in permissionDto.Children)
            // {
            //     if (permissionDto.IsCheck && permissionDto.ID > 0)
            //     {
            //         key += permissionDto.ID + ",";
            //     }
            // }
        }

        /// <summary>
        /// 将权限树形结构转换为扁平列表
        /// 用于需要线性处理所有权限的场景（如权限保存、批量操作）
        /// </summary>
        /// <param name="node">权限树根节点</param>
        /// <returns>包含所有权限的扁平列表（排除root节点且ID>0的权限）</returns>
        public static IList<PermissionDto> FromTreeToList(this PermissionDto node)
        {
            List<PermissionDto> resultList = new List<PermissionDto>();
            // 排除root节点且仅添加有效权限（ID>0）
            if (node.TitleName != "root" && node.ID > 0)
            {
                resultList.Add(node);
            }
            // 递归处理所有子节点
            foreach (var item in node.Children)
            {
                Revers(item, resultList);
            }
            return resultList;
        }

        /// <summary>
        /// 获取权限列表中的最大ID
        /// 用于生成新权限的ID（确保唯一性）
        /// </summary>
        /// <param name="list">权限列表</param>
        /// <returns>列表中的最大ID，空列表返回0</returns>
        public static int GetMaxId(this IList<PermissionDto> list)
        {
            return list.Count > 0 ? list.Max(s => s.ID) : 0;
        }

        /// <summary>
        /// 递归遍历权限树，将所有节点添加到扁平列表
        /// 辅助FromTreeToList方法实现树形转列表
        /// </summary>
        /// <param name="currentNode">当前处理的权限节点</param>
        /// <param name="resultList">存储结果的扁平列表</param>
        private static void Revers(PermissionDto currentNode, List<PermissionDto> resultList)
        {
            resultList.Add(currentNode); // 添加当前节点
            // 递归处理子节点
            if (currentNode.Children != null && currentNode.Children.Count > 0)
            {
                foreach (var item in currentNode.Children)
                {
                    Revers(item, resultList);
                }
            }
        }
    }
}