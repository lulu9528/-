using AutoMapper;
using Jabil.Module.Permission.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Service
{
    /// <summary>
    /// AutoMapper辅助类
    /// 提供单例模式的AutoMapper实例，用于对象之间的映射转换
    /// 封装了AutoMapper的配置和初始化逻辑，简化对象映射操作
    /// </summary>
    public class AutoMapperHelper
    {
        /// <summary>
        /// 单例实例
        /// 保证应用程序中只存在一个AutoMapperHelper实例
        /// </summary>
        private static AutoMapperHelper _instance;

        /// <summary>
        /// 单例访问属性
        /// 懒加载创建实例，确保线程安全（简单实现）
        /// </summary>
        public static AutoMapperHelper Instance => _instance ?? new AutoMapperHelper();

        /// <summary>
        /// AutoMapper映射器实例
        /// 用于执行对象之间的映射操作
        /// </summary>
        private IMapper _mapper;

        /// <summary>
        /// 公开的映射器属性
        /// 供外部调用者使用IMapper的映射功能
        /// </summary>
        public IMapper Mapper { get => _mapper; }

        /// <summary>
        /// 私有构造函数
        /// 防止外部直接实例化，确保单例模式
        /// 初始化AutoMapper配置并创建映射器
        /// </summary>
        private AutoMapperHelper()
        {
            // 创建AutoMapper配置对象
            var automapperConfig = new MapperConfiguration(config =>
            {
                // 添加自定义的映射配置文件
                // AutoMapperProFile中定义了具体的对象映射规则
                config.AddProfile(new AutoMapperProFile());
            });

            // 根据配置创建映射器实例
            _mapper = automapperConfig.CreateMapper();
        }
    }
}