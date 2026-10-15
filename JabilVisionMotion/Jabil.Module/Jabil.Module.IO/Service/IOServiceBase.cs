using AutoMapper;
using Jabil.Module.Log.Lib;
using System;

namespace Jabil.Module.IO.Service
{
    /// <summary>
    /// IO服务基类
    /// 作用：为所有IO相关的服务类（如IOCardService、IOCardDIOService）提供基础支持
    /// 核心功能：
    /// 1. 统一管理AutoMapper映射器实例，避免重复初始化
    /// 2. 实现IDisposable接口，规范资源释放流程
    /// 设计为抽象类，强制子类继承并专注于业务逻辑实现
    /// </summary>
    public abstract class IOServiceBase : IDisposable
    {
        /// <summary>
        /// AutoMapper映射器实例（受保护成员）
        /// 供派生类使用，用于实体（Entity）与数据传输对象（DTO）之间的转换
        /// 访问修饰符为protected，确保仅子类可访问，符合封装原则
        /// </summary>
        protected readonly IMapper _mapper;

        /// <summary>
        /// 资源释放标记
        /// 用于跟踪对象是否已释放，避免重复释放资源导致的异常
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 默认构造函数（无参）
        /// 自动从AutoMapperHelper单例获取映射器实例
        /// 适用于正常业务场景
        /// </summary>
        protected IOServiceBase()
        {
            try
            {
                // 从全局AutoMapperHelper获取映射器实例，并验证非空
                // 若获取失败（如配置错误），抛出明确异常，避免后续使用时出现NullReferenceException
                _mapper = AutoMapperHelper.Instance.Mapper
                    ?? throw new InvalidOperationException("AutoMapper未正确初始化，请检查映射配置文件是否存在且有效");
            }
            catch (Exception ex)
            {
                // 记录初始化异常，包含完整堆栈信息，便于排查配置问题
                RLog.SaveError(ex);
                throw; // 向上传递异常，确保问题被上层捕获（如应用启动时快速失败）
            }
        }

        /// <summary>
        /// 带参数的构造函数（支持依赖注入）
        /// 允许外部传入IMapper实例，主要用于单元测试（如使用模拟对象Mock<IMapper>）
        /// </summary>
        /// <param name="mapper">外部提供的AutoMapper实例</param>
        /// <exception cref="ArgumentNullException">当mapper为null时抛出，确保映射器有效</exception>
        protected IOServiceBase(IMapper mapper)
        {
            // 强制检查参数非空，避免后续使用时出现空引用错误
            _mapper = mapper ?? throw new ArgumentNullException(
                nameof(mapper),
                "AutoMapper实例不能为空，请提供有效的IMapper对象"
            );
        }

        /// <summary>
        /// 公开的资源释放方法（实现IDisposable接口）
        /// 供外部调用，用于主动释放对象占用的资源
        /// </summary>
        public void Dispose()
        {
            // 调用实际释放逻辑，参数disposing=true表示主动释放（由用户触发）
            Dispose(true);
            // 通知垃圾回收器不需要再调用析构函数，提升性能
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 资源释放的实际实现（受保护的虚拟方法）
        /// 设计为virtual，允许子类重写以释放自身特有的资源
        /// </summary>
        /// <param name="disposing">
        /// true：表示主动释放（由Dispose()方法触发），需释放托管资源和非托管资源
        /// false：表示被动释放（由垃圾回收器触发），仅释放非托管资源
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            // 若已释放，则直接返回，避免重复操作
            if (_disposed)
                return;

            // 主动释放时，处理托管资源（如数据库连接、文件流等）
            if (disposing)
            {
                // 示例：若子类有托管资源（如SQL连接），可在此处释放
                // if (_dbConnection != null)
                // {
                //     _dbConnection.Dispose();
                //     _dbConnection = null;
                // }
            }

            // 释放非托管资源（如操作系统句柄、非托管内存等）
            // 示例：
            // if (_unmanagedHandle != IntPtr.Zero)
            // {
            //     NativeMethods.CloseHandle(_unmanagedHandle);
            //     _unmanagedHandle = IntPtr.Zero;
            // }

            // 标记为已释放
            _disposed = true;
        }

        /// <summary>
        /// 析构函数（Finalizer）
        /// 由垃圾回收器自动调用，用于被动释放未被主动释放的资源
        /// 仅处理非托管资源（托管资源此时可能已被回收）
        /// </summary>
        ~IOServiceBase()
        {
            // 被动释放时，调用Dispose(false)，仅处理非托管资源
            Dispose(false);
        }
    }
}