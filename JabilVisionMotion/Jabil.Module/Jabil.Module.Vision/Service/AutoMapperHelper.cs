using AutoMapper;
using Jabil.Module.Vision.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Vision.Service
{
    /// <summary>
    /// AutoMapper辅助类
    /// 封装AutoMapper的单例实例，提供类型映射功能
    /// 确保应用程序中仅存在一个IMapper实例，优化资源使用并保持映射配置一致性
    /// </summary>
    public class AutoMapperHelper
    {
        /// <summary>
        /// 单例实例
        /// 私有静态变量，存储类的唯一实例
        /// </summary>
        private static AutoMapperHelper _instance;

        /// <summary>
        /// 线程同步锁
        /// 用于确保单例实例创建过程的线程安全性
        /// </summary>
        private static readonly object _syncLock = new object();

        /// <summary>
        /// AutoMapper映射器实例
        /// 负责执行实际的对象到对象映射操作
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// 单例实例访问属性
        /// 提供全局访问点，采用双重检查锁定模式确保线程安全
        /// </summary>
        public static AutoMapperHelper Instance
        {
            get
            {
                // 第一次检查，避免不必要的锁获取，提高性能
                if (_instance == null)
                {
                    // 锁定操作，确保同一时刻只有一个线程进入临界区
                    lock (_syncLock)
                    {
                        // 第二次检查，防止多个线程同时通过第一次检查后重复创建实例
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
        /// AutoMapper映射器公开访问属性
        /// 提供外部访问IMapper接口的途径，用于执行映射操作
        /// </summary>
        public IMapper Mapper => _mapper;

        /// <summary>
        /// 私有构造函数
        /// 防止外部直接实例化，确保单例模式有效性
        /// 初始化AutoMapper配置并创建映射器实例
        /// </summary>
        private AutoMapperHelper()
        {
            // 创建映射配置对象
            var config = new MapperConfiguration(cfg =>
            {
                // 添加映射配置文件，集中管理所有映射关系
                // AutoMapperProFile中定义了具体的类型映射规则
                cfg.AddProfile<AutoMapperProfile>();
            });

            // 验证映射配置的有效性
            // 在应用程序启动时检测配置错误，避免运行时异常
            config.AssertConfigurationIsValid();

            // 创建映射器实例，基于上述配置
            _mapper = config.CreateMapper();
        }
    }
}
