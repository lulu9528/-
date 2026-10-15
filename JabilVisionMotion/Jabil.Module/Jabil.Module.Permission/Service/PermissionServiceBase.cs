using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Service
{
    /// <summary>
    /// 权限服务基类
    /// 为所有权限相关服务提供基础功能和统一接口
    /// 采用抽象类设计，强制子类实现特定方法
    /// </summary>
    public abstract class PermissionServiceBase
    {
        /// <summary>
        /// AutoMapper实例
        /// 保护级别，供子类使用，用于实体与DTO之间的映射
        /// </summary>
        protected readonly IMapper _mapper;

        /// <summary>
        /// 构造函数
        /// 初始化AutoMapper实例，确保所有子类都能使用对象映射功能
        /// </summary>
        public PermissionServiceBase()
        {
            // 获取单例的AutoMapper实例，避免重复初始化
            _mapper = AutoMapperHelper.Instance.Mapper;
        }

        /// <summary>
        /// 确保权限表存在的抽象方法
        /// 由子类实现具体逻辑，通常用于初始化数据库表结构
        /// 系统启动时调用，保证权限相关表结构就绪
        /// </summary>
        public abstract void EnsureTable();
    }
}