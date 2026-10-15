using AutoMapper;
using Jabil.Module.Log.Lib;
using System;

namespace Jabil.Module.Modbus.Service
{
    /// <summary>
    /// Modbus服务基类
    /// 所有Modbus相关业务服务类的抽象基类，实现横切关注点的复用
    /// 核心职责：统一管理AutoMapper实例，为派生类提供对象映射能力
    /// 设计模式：模板方法模式的基础实现，封装公共依赖组件
    /// </summary>
    public abstract class ModbusServiceBase : IDisposable
    {
        /// <summary>
        /// AutoMapper映射器实例
        /// 用于实体(Entity)与数据传输对象(DTO)之间的双向转换
        /// 访问修饰符说明：
        /// - protected：允许派生类（如ModbusCommunicationService）访问
        /// - readonly：确保初始化后不可修改，保证线程安全
        /// </summary>
        protected readonly IMapper _mapper;

        /// <summary>
        /// 服务是否已释放
        /// </summary>
        protected bool _disposed = false;

        /// <summary>
        /// 基类构造函数
        /// 负责初始化共享的AutoMapper实例，为所有派生类提供统一的对象映射配置
        /// </summary>
        /// <exception cref="InvalidOperationException">当AutoMapper初始化失败时抛出</exception>
        protected ModbusServiceBase()
        {
            try
            {
                // 从AutoMapperHelper单例获取预配置的IMapper实例
                // 优势：
                // 1. 确保全局使用同一套映射规则，避免配置不一致
                // 2. 减少IMapper实例的重复创建，提升性能
                // 3. 集中管理映射配置，便于维护
                _mapper = AutoMapperHelper.Instance.Mapper
                    ?? throw new InvalidOperationException("获取AutoMapper实例失败，AutoMapperHelper未正确初始化");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex, "ModbusServiceBase初始化AutoMapper失败");
                throw new InvalidOperationException("Modbus服务基类初始化失败", ex);
            }
        }

        /// <summary>
        /// 映射对象转换（简化派生类调用）
        /// </summary>
        /// <typeparam name="TSource">源类型</typeparam>
        /// <typeparam name="TDestination">目标类型</typeparam>
        /// <param name="source">源对象</param>
        /// <returns>转换后的目标对象</returns>
        /// <exception cref="ArgumentNullException">源对象为null时抛出</exception>
        protected TDestination Map<TSource, TDestination>(TSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "源对象不能为null");

            return _mapper.Map<TSource, TDestination>(source);
        }

        /// <summary>
        /// 映射对象转换（简化派生类调用）
        /// </summary>
        /// <typeparam name="TSource">源类型</typeparam>
        /// <typeparam name="TDestination">目标类型</typeparam>
        /// <param name="source">源对象</param>
        /// <param name="destination">目标对象</param>
        /// <returns>更新后的目标对象</returns>
        /// <exception cref="ArgumentNullException">源对象或目标对象为null时抛出</exception>
        protected TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "源对象不能为null");

            if (destination == null)
                throw new ArgumentNullException(nameof(destination), "目标对象不能为null");

            return _mapper.Map(source, destination);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实际实现
        /// </summary>
        /// <param name="disposing">是否主动释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 释放托管资源
            }

            // 释放非托管资源

            _disposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~ModbusServiceBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// 检查服务是否已释放
        /// </summary>
        /// <exception cref="ObjectDisposedException">服务已释放时抛出</exception>
        protected void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name, "当前服务实例已释放，无法执行操作");
        }
    }
}