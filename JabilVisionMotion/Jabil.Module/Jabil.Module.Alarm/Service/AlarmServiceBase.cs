using AutoMapper;
using System;

namespace Jabil.Module.Alarm.Service
{
    /// <summary>
    /// 报警服务基类
    /// 为所有报警相关的服务类提供基础功能和共享依赖
    /// 采用继承模式，统一管理服务类的公共资源
    /// </summary>
    public class AlarmServiceBase
    {
        /// <summary>
        /// AutoMapper映射器实例
        /// 用于在DTO（数据传输对象）和实体（Entity）之间进行对象转换
        /// 标记为protected，允许派生类直接访问，避免重复定义
        /// 标记为readonly，确保初始化后不可修改，保证线程安全
        /// </summary>
        protected readonly IMapper _mapper;

        /// <summary>
        /// 基类构造函数
        /// 初始化AutoMapper映射器实例，供所有派生类共享使用
        /// 采用单例模式获取AutoMapperHelper实例，确保全局配置一致性
        /// </summary>
        public AlarmServiceBase()
        {
            // 通过AutoMapperHelper单例获取全局唯一的Mapper实例
            // 保证所有服务类使用相同的映射配置，避免配置不一致问题
            _mapper = AutoMapperHelper.Instance.Mapper;
        }
    }
}
