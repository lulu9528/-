using AutoMapper;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Vision.Service
{
    /// <summary>
    /// 视觉服务基类
    /// 提供所有视觉服务类的基础功能，主要负责AutoMapper实例的初始化和共享
    /// 作为抽象类，不能直接实例化，只能被继承
    /// </summary>
    public abstract class VisionServiceBase
    {
        /// <summary>
        /// AutoMapper映射器实例
        /// 保护级别，允许派生类访问，用于DTO与实体对象之间的转换
        /// </summary>
        protected readonly IMapper _mapper;

        /// <summary>
        /// 构造函数
        /// 初始化AutoMapper实例，通过单例辅助类获取全局共享的映射器
        /// 确保所有服务类使用统一的映射配置
        /// </summary>
        protected VisionServiceBase()
        {
            // 从AutoMapperHelper单例获取映射器实例
            _mapper = AutoMapperHelper.Instance.Mapper;
        }
    }

    /// <summary>
    /// 通用视觉服务泛型基类
    /// 继承自VisionServiceBase，封装了CRUD操作的通用实现
    /// 采用泛型设计，为不同实体类型提供统一的数据访问接口
    /// </summary>
    /// <typeparam name="TEntity">数据库实体类型，必须是类且具有无参构造函数</typeparam>
    /// <typeparam name="TDto">数据传输对象类型，必须是类且具有无参构造函数</typeparam>
    public abstract class GenericVisionService<TEntity, TDto> : VisionServiceBase
        where TEntity : class, new()
        where TDto : class, new()
    {
        /// <summary>
        /// 获取所有数据
        /// 从数据库查询指定实体类型的所有记录，并转换为DTO集合返回
        /// </summary>
        /// <returns>包含DTO集合的ApiResponse对象，包含操作状态和结果数据</returns>
        public virtual async Task<ApiResponse<List<TDto>>> GetAllAsync()
        {
            try
            {
                // 从数据库查询所有实体
                var entities = await SugarSQLHelper.Instance.DB.Queryable<TEntity>().ToListAsync();

                // 将实体列表映射为DTO列表
                var dtos = _mapper.Map<List<TDto>>(entities);

                // 返回成功响应，包含DTO数据
                return new ApiResponse<List<TDto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                // 记录异常并返回错误响应
                RLog.SaveError(ex);
                return new ApiResponse<List<TDto>>($"获取数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加或更新数据
        /// 抽象方法，要求派生类实现具体的添加/更新逻辑
        /// 根据业务规则判断是执行插入还是更新操作
        /// </summary>
        /// <param name="dto">待添加或更新的数据传输对象</param>
        /// <returns>包含操作结果的ApiResponse对象</returns>
        public abstract Task<ApiResponse> AddOrUpdateAsync(TDto dto);

        /// <summary>
        /// 批量更新数据
        /// 对DTO集合进行批量更新，适用于一次更新多条记录的场景
        /// </summary>
        /// <param name="dtos">待更新的数据传输对象集合</param>
        /// <returns>包含操作结果的ApiResponse对象</returns>
        public virtual async Task<ApiResponse> UpdateAsync(IEnumerable<TDto> dtos)
        {
            try
            {
                // 空集合检查
                if (dtos == null || !dtos.Any())
                {
                    return new ApiResponse("无更新对象");
                }

                // 将DTO集合映射为实体集合
                var entities = _mapper.Map<List<TEntity>>(dtos);

                // 执行批量更新操作
                var result = await SugarSQLHelper.Instance.DB.Updateable(entities).ExecuteCommandAsync();

                // 根据更新结果返回相应响应
                return result > 0
                    ? new ApiResponse($"更新完成，共{result}条记录", true)
                    : new ApiResponse("更新失败");
            }
            catch (Exception ex)
            {
                // 记录异常并返回错误响应
                RLog.SaveError(ex);
                return new ApiResponse($"更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除数据
        /// 抽象方法，要求派生类实现具体的删除逻辑
        /// 根据业务规则执行删除操作
        /// </summary>
        /// <param name="dto">待删除的数据传输对象</param>
        /// <returns>包含操作结果的ApiResponse对象</returns>
        public abstract Task<ApiResponse> DeleteAsync(TDto dto);

        /// <summary>
        /// 批量删除数据
        /// 对DTO集合进行批量删除，适用于一次删除多条记录的场景
        /// </summary>
        /// <param name="dtos">待删除的数据传输对象集合</param>
        /// <returns>包含操作结果的ApiResponse对象</returns>
        public virtual async Task<ApiResponse> DeleteAsync(IEnumerable<TDto> dtos)
        {
            try
            {
                // 空集合检查
                if (dtos == null || !dtos.Any())
                {
                    return new ApiResponse("无删除对象");
                }

                // 将DTO集合映射为实体集合
                var entities = _mapper.Map<List<TEntity>>(dtos);

                // 执行批量删除操作
                var result = await SugarSQLHelper.Instance.DB.Deleteable(entities).ExecuteCommandAsync();

                // 根据删除结果返回相应响应
                return result > 0
                    ? new ApiResponse($"删除完成，共{result}条记录", true)
                    : new ApiResponse("删除失败，对象不存在或已删除");
            }
            catch (Exception ex)
            {
                // 记录异常并返回错误响应
                RLog.SaveError(ex);
                return new ApiResponse($"删除失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从数据库刷新数据到DataGridView
        /// 将查询结果绑定到数据网格控件，用于UI展示
        /// 确保在UI线程执行控件操作，避免跨线程异常
        /// </summary>
        /// <param name="dataGridView">目标数据网格控件，可为null（仅查询不绑定）</param>
        /// <returns>包含操作结果的ApiResponse对象</returns>
        public virtual async Task<ApiResponse> RefreshDataGridViewFromSqlAsync(DataGridView dataGridView = null)
        {
            try
            {
                // 从数据库查询所有实体
                var entities = await SugarSQLHelper.Instance.DB.Queryable<TEntity>().ToListAsync();

                // 转换为DTO集合
                var dtos = _mapper.Map<List<TDto>>(entities);

                // 如果指定了数据网格且有数据，则绑定到网格
                if (dataGridView != null && dtos.Count > 0)
                {
                    // 使用Invoke确保在UI线程更新控件，避免跨线程操作异常
                    dataGridView.Invoke(new Action(() =>
                    {
                        dataGridView.DataSource = dtos;
                    }));
                }

                // 根据数据量返回相应响应
                return dtos.Count > 0
                    ? new ApiResponse("数据加载成功", true)
                    : new ApiResponse("没有找到数据");
            }
            catch (Exception ex)
            {
                // 记录异常并返回错误响应
                RLog.SaveError(ex);
                return new ApiResponse($"数据加载失败: {ex.Message}");
            }
        }
    }
}
