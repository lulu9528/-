using AutoMapper;
using Jabil.Module.IO.Common;
using System;

namespace Jabil.Module.IO.Service
{
    /// <summary>
    /// AutoMapper辅助类（单例模式）
    /// 核心功能：
    /// 1. 管理全局唯一的AutoMapper映射器实例
    /// 2. 封装AutoMapper的初始化过程与映射配置加载
    /// 3. 提供简化的对象映射方法，降低外部使用复杂度
    /// 确保整个应用程序中AutoMapper配置的一致性和单例复用
    /// </summary>
    public class AutoMapperHelper
    {
        #region 单例模式实现（线程安全）
        /// <summary>
        /// 静态懒实例的懒加载器
        /// 使用Lazy<T>确保：
        /// - 线程安全：多线程环境下仅创建一个实例
        /// - 延迟初始化：首次访问时才初始化，减少应用启动时间
        /// </summary>
        private static readonly Lazy<AutoMapperHelper> _lazyInstance = new Lazy<AutoMapperHelper>(() => new AutoMapperHelper());

        /// <summary>
        /// 全局唯一访问点，外部通过此属性获取AutoMapperHelper实例
        /// </summary>
        public static AutoMapperHelper Instance => _lazyInstance.Value;

        /// <summary>
        /// 私有构造函数，防止外部直接实例化
        /// 确保单例模式的唯一性，所有操作必须通过Instance访问
        /// </summary>
        private AutoMapperHelper()
        {
            // 初始化AutoMapper配置（在实例创建时自动执行）
            InitializeMapper();
        }
        #endregion

        #region 字段与属性
        /// <summary>
        /// AutoMapper映射器实例（私有字段）
        /// 存储初始化后的IMapper对象，供内部使用
        /// </summary>
        private IMapper _mapper;

        /// <summary>
        /// 公开的映射器接口，供外部调用执行映射操作
        /// 提供null检查，确保映射器已正确初始化
        /// </summary>
        public IMapper Mapper
        {
            get
            {
                // 若映射器未初始化（如初始化失败），抛出明确异常
                if (_mapper == null)
                    throw new InvalidOperationException("AutoMapper尚未初始化，请检查配置是否正确");
                return _mapper;
            }
        }
        #endregion

        #region 初始化方法
        /// <summary>
        /// 初始化AutoMapper配置
        /// 加载所有映射配置文件并创建映射器实例
        /// 此方法在类实例化时自动调用，确保映射器可用
        /// </summary>
        private void InitializeMapper()
        {
            try
            {
                // 创建映射配置对象，用于定义映射规则
                var config = new MapperConfiguration(cfg =>
                {
                    // 加载IO模块的映射配置文件（IoAutoMapperProfile中定义了实体与DTO的映射关系）
                    cfg.AddProfile<IoAutoMapperProfile>();

                    // 扩展点：如需添加其他模块的映射配置，在此处继续添加
                    // 示例：cfg.AddProfile<OtherModuleProfile>();
                });

                // 验证映射配置的有效性（开发环境建议保留，生产环境可移除以提升性能）
                // 检查所有定义的映射是否存在未映射的属性或类型不匹配问题
                config.AssertConfigurationIsValid();

                // 根据配置创建映射器实例，完成初始化
                _mapper = config.CreateMapper();
            }
            catch (Exception ex)
            {
                // 包装初始化异常，添加明确的错误信息，便于问题排查
                throw new InvalidOperationException("AutoMapper初始化失败，请检查映射配置是否正确", ex);
            }
        }
        #endregion

        #region 映射方法封装（简化外部调用）
        /// <summary>
        /// 将源对象映射为目标类型的新实例
        /// 简化AutoMapper的Map方法调用，无需直接操作IMapper
        /// </summary>
        /// <typeparam name="TSource">源对象类型（如实体类）</typeparam>
        /// <typeparam name="TDestination">目标对象类型（如DTO）</typeparam>
        /// <param name="source">要映射的源对象</param>
        /// <returns>映射后的目标类型实例，源对象为null时返回默认值（通常为null）</returns>
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            // 处理源对象为null的情况，避免映射器抛出异常
            if (source == null)
                return default;

            // 调用AutoMapper的映射方法，执行对象转换
            return Mapper.Map<TSource, TDestination>(source);
        }

        /// <summary>
        /// 将源对象映射到已存在的目标对象（更新目标对象属性）
        /// 适用于需要复用目标对象实例的场景（如更新现有实体）
        /// </summary>
        /// <typeparam name="TSource">源对象类型</typeparam>
        /// <typeparam name="TDestination">目标对象类型</typeparam>
        /// <param name="source">要映射的源对象</param>
        /// <param name="destination">已存在的目标对象（将被更新）</param>
        /// <returns>更新后的目标对象，源或目标为null时返回原目标对象</returns>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            // 处理源或目标为null的情况，直接返回目标对象
            if (source == null || destination == null)
                return destination;

            // 调用AutoMapper的映射方法，将源对象属性更新到目标对象
            return Mapper.Map(source, destination);
        }
        #endregion
    }
}