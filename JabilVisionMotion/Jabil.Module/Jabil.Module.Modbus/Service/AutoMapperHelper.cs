using AutoMapper;
using Jabil.Module.Modbus.Common;
using Jabil.Module.Log.Lib;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Drawing;

namespace Jabil.Module.Modbus.Service
{
    /// <summary>
    /// AutoMapper辅助类
    /// 提供全局唯一的AutoMapper实例，负责对象之间的映射转换
    /// 采用线程安全的单例模式确保映射配置的一致性和资源高效利用
    /// 设计特点：
    /// 1. 延迟初始化：首次使用时才创建实例，减少启动时间
    /// 2. 异常隔离：映射过程中的异常统一包装，便于问题定位
    /// 3. 配置验证：启动时验证所有映射规则，提前暴露配置问题
    /// </summary>
    public sealed class AutoMapperHelper : ISerializable
    {
        /// <summary>
        /// 用于线程同步的锁定对象
        /// 确保单例实例创建过程的线程安全性
        /// </summary>
        private static readonly object _lockObj = new object();

        /// <summary>
        /// 私有静态实例，存储AutoMapperHelper的唯一实例
        /// volatile关键字确保多线程环境下的可见性
        /// </summary>
        private static volatile AutoMapperHelper _instance;

        /// <summary>
        /// AutoMapper核心映射器实例
        /// 用于执行对象之间的映射操作
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// 公开的映射器访问属性
        /// 外部通过此属性获取IMapper实例进行对象映射
        /// </summary>
        public IMapper Mapper => _mapper ?? throw new InvalidOperationException("AutoMapper尚未初始化完成");

        /// <summary>
        /// 公共静态属性，提供单例实例的访问点
        /// 采用双重检查锁定模式确保线程安全和高效访问
        /// </summary>
        public static AutoMapperHelper Instance
        {
            get
            {
                // 第一重检查，避免每次访问都加锁
                if (_instance == null)
                {
                    // 加锁确保多线程环境下的安全性
                    lock (_lockObj)
                    {
                        // 第二重检查，防止多个线程同时通过第一重检查后重复创建实例
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
        /// 私有构造函数，确保只能通过Instance属性创建实例（单例模式要求）
        /// 初始化AutoMapper配置并创建映射器实例
        /// </summary>
        private AutoMapperHelper()
        {
            try
            {
                // 创建AutoMapper配置对象
                var automapperConfig = new MapperConfiguration(config =>
                {
                    // 添加自定义映射配置文件
                    config.AddProfile<ModbusAutoMapperProfile>();

                    // 全局配置
                    config.AllowNullCollections = true;
                    config.AllowNullDestinationValues = true;

                    // 兼容低版本AutoMapper：移除CreateMissingTypeMaps配置,移除ValidateInlineMaps配置
                    // 低版本中此配置默认值为false，与当前代码意图一致
                });

                // 验证配置并创建映射器实例
                // 若配置存在错误（如映射规则不完整），会抛出异常
                automapperConfig.AssertConfigurationIsValid();
                _mapper = automapperConfig.CreateMapper();

                // 记录初始化成功日志
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, "AutoMapper配置初始化成功");
            }
            catch (Exception ex)
            {
                // 记录映射配置错误
                RLog.SaveError(ex);
                throw new InvalidOperationException("AutoMapper配置初始化失败", ex);
            }
        }

        /// <summary>
        /// 将源对象映射到新的目标对象
        /// 提供简化的映射方法，避免外部直接操作IMapper
        /// </summary>
        /// <typeparam name="TSource">源对象类型</typeparam>
        /// <typeparam name="TDestination">目标对象类型</typeparam>
        /// <param name="source">源对象实例</param>
        /// <returns>映射后的目标对象实例</returns>
        /// <exception cref="ArgumentNullException">当源对象为null时抛出</exception>
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "源对象不能为null");

            try
            {
                return _mapper.Map<TSource, TDestination>(source);
            }
            catch (AutoMapperMappingException ex)
            {
                var errorMsg = $"对象映射失败，源类型: {typeof(TSource).Name}, 目标类型: {typeof(TDestination).Name}";
                RLog.SaveError(ex, errorMsg);
                throw new InvalidOperationException(errorMsg, ex);
            }
        }

        /// <summary>
        /// 将源对象映射到已存在的目标对象
        /// 提供简化的映射方法，避免外部直接操作IMapper
        /// </summary>
        /// <typeparam name="TSource">源对象类型</typeparam>
        /// <typeparam name="TDestination">目标对象类型</typeparam>
        /// <param name="source">源对象实例</param>
        /// <param name="destination">目标对象实例</param>
        /// <returns>映射后的目标对象实例</returns>
        /// <exception cref="ArgumentNullException">当源对象或目标对象为null时抛出</exception>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "源对象不能为null");

            if (destination == null)
                throw new ArgumentNullException(nameof(destination), "目标对象不能为null");

            try
            {
                return _mapper.Map(source, destination);
            }
            catch (AutoMapperMappingException ex)
            {
                var errorMsg = $"对象映射失败，源类型: {typeof(TSource).Name}, 目标类型: {typeof(TDestination).Name}";
                RLog.SaveError(ex, errorMsg);
                throw new InvalidOperationException(errorMsg, ex);
            }
        }

        /// <summary>
        /// 尝试将源对象映射到目标对象，不抛出异常
        /// 适用于非关键路径的映射操作
        /// </summary>
        /// <typeparam name="TSource">源对象类型</typeparam>
        /// <typeparam name="TDestination">目标对象类型</typeparam>
        /// <param name="source">源对象实例</param>
        /// <param name="destination">映射结果</param>
        /// <returns>映射是否成功</returns>
        public bool TryMap<TSource, TDestination>(TSource source, out TDestination destination)
        {
            destination = default;
            try
            {
                if (source == null)
                    return false;

                destination = _mapper.Map<TSource, TDestination>(source);
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"对象映射尝试失败，源类型: {typeof(TSource).Name}, 目标类型: {typeof(TDestination).Name}";
                RLog.SaveError(ex, errorMsg);
                return false;
            }
        }

        /// <summary>
        /// 防止通过反射创建实例
        /// </summary>
        private AutoMapperHelper(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException("不支持序列化创建实例");
        }

        /// <summary>
        /// 实现ISerializable接口，防止序列化
        /// </summary>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException("不支持序列化操作");
        }

        /// <summary>
        /// 防止克隆实例
        /// </summary>
        /// <returns>不返回任何值，直接抛出异常</returns>
        protected object MemberwiseClone()
        {
            throw new NotSupportedException("不支持克隆操作");
        }
    }
}