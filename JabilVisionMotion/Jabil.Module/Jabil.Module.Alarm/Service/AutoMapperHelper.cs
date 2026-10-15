using AutoMapper;
using Jabil.Module.Alarm.Common;
using System;

namespace Jabil.Module.Alarm.Service
{
    /// <summary>
    /// AutoMapper帮助类
    /// 负责管理AutoMapper的全局配置和实例，提供类型映射功能
    /// 采用单例模式设计，确保应用程序中只有一个映射配置实例
    /// 统一管理所有对象映射关系，避免重复配置和资源浪费
    /// </summary>
    public class AutoMapperHelper
    {
        /// <summary>
        /// 单例模式的线程安全锁
        /// 用于在多线程环境下保证实例创建的原子性操作
        /// 防止并发场景下创建多个实例导致的配置不一致问题
        /// </summary>
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// 单例实例变量
        /// 存储类的唯一实例，初始值为null
        /// </summary>
        private static AutoMapperHelper _instance;

        /// <summary>
        /// AutoMapper的映射器实例
        /// 负责实际的对象映射操作，通过配置初始化后使用
        /// 标记为readonly，确保初始化后不能被重新赋值
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// 单例实例访问属性
        /// 提供全局访问点，确保任何时候都只能获取到一个实例
        /// 采用双重检查锁定(Double-Check Locking)实现高效线程安全
        /// </summary>
        public static AutoMapperHelper Instance
        {
            get
            {
                // 第一次检查：未加锁，快速判断实例是否已创建
                // 避免每次访问都进入同步代码块，提高性能
                if (_instance == null)
                {
                    // 加锁：确保只有一个线程进入实例创建逻辑
                    lock (_instanceLock)
                    {
                        // 第二次检查：防止多个线程同时通过第一次检查后
                        // 在锁释放后重复创建实例
                        if (_instance == null)
                        {
                            _instance = new AutoMapperHelper();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 提供外部访问的Mapper实例属性
        /// 封装内部的_mapper字段，通过属性暴露给外部使用
        /// 采用表达式体成员简化代码
        /// </summary>
        public IMapper Mapper => _mapper;

        /// <summary>
        /// 私有构造函数
        /// 确保只能通过Instance属性获取实例，禁止外部直接创建
        /// 在构造函数中完成AutoMapper的配置和初始化
        /// </summary>
        private AutoMapperHelper()
        {
            // 创建AutoMapper配置对象
            // 用于定义源类型与目标类型之间的映射关系
            var config = new MapperConfiguration(cfg =>
            {
                // 添加映射配置文件
                // AutoMapperProFile中定义了具体的类型映射规则
                // 这种方式将映射规则集中管理，便于维护
                cfg.AddProfile<AutoMapperProfile>();
            });

            // 验证映射配置的有效性
            // 在应用程序启动时检查所有映射配置是否正确
            // 如存在未映射的属性或类型不匹配等问题，会抛出异常
            // 提前发现映射配置错误，避免运行时异常
            config.AssertConfigurationIsValid();

            // 根据配置创建Mapper实例
            // 该实例包含了所有已配置的映射规则，可直接用于对象转换
            _mapper = config.CreateMapper();
        }
    }
}
