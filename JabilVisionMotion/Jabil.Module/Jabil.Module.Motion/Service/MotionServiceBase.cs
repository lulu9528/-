using AutoMapper;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Motion.Service
{
    /// <summary>
    /// 通用运动服务基类
    /// 封装CRUD操作的通用逻辑，减少代码重复
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TDto">DTO类型</typeparam>
    public abstract class MotionServiceBase<TEntity, TDto>
        where TEntity : BaseEntity, new()
        where TDto : BaseDto, new()
    {
        protected readonly IMapper _mapper;

        protected MotionServiceBase()
        {
            _mapper = AutoMapperHelper.Instance.Mapper;
        }

        /// <summary>
        /// 获取所有数据
        /// </summary>
        public async Task<ApiResponse<List<TDto>>> GetAllAsync()
        {
            try
            {
                var entities = await SugarSQLHelper.Instance.DB.Queryable<TEntity>().ToListAsync();
                var dtos = _mapper.Map<List<TDto>>(entities);
                return new ApiResponse<List<TDto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse<List<TDto>>($"获取{GetEntityName()}数据失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 添加数据
        /// </summary>
        public async Task<ApiResponse> AddAsync(TDto dto)
        {
            try
            {
                if (await ExistsAsync(dto))
                {
                    return new ApiResponse($"已存在相同的{GetEntityName()}，不允许重复添加");
                }

                var entity = _mapper.Map<TEntity>(dto);
                var result = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();
                return result > 0
                    ? new ApiResponse($"添加{GetEntityName()}成功", true)
                    : new ApiResponse($"添加{GetEntityName()}失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"添加{GetEntityName()}失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 添加或更新数据
        /// </summary>
        public async Task<ApiResponse> AddOrUpdateAsync(TDto dto)
        {
            try
            {
                var entity = _mapper.Map<TEntity>(dto);
                var existingEntity = await GetExistingEntityAsync(dto);

                if (existingEntity == null)
                {
                    var insertResult = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();
                    return insertResult >= 0
                        ? new ApiResponse($"添加{GetEntityName()}完成", true)
                        : new ApiResponse($"添加{GetEntityName()}失败");
                }
                else
                {
                    // 确保更新时使用现有实体的ID
                    entity.ID = existingEntity.ID;
                    var updateResult = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();
                    return updateResult > 0
                        ? new ApiResponse($"更新{GetEntityName()}完成", true)
                        : new ApiResponse($"更新{GetEntityName()}失败");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"{GetEntityName()}操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新单个数据
        /// </summary>
        public async Task<ApiResponse> UpdateAsync(TDto dto)
        {
            try
            {
                if (dto == null)
                    return new ApiResponse("更新对象不能为空");

                var entity = _mapper.Map<TEntity>(dto);
                var result = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();

                return result > 0
                    ? new ApiResponse($"更新{GetEntityName()}成功", true)
                    : new ApiResponse($"更新{GetEntityName()}失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"更新{GetEntityName()}失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新数据
        /// </summary>
        public async Task<ApiResponse> UpdateAsync(IEnumerable<TDto> dtos)
        {
            try
            {
                if (dtos?.Any() != true)
                    return new ApiResponse("没有可更新的对象");

                var entities = _mapper.Map<List<TEntity>>(dtos);
                var result = await SugarSQLHelper.Instance.DB.Updateable(entities).ExecuteCommandAsync();

                return result > 0
                    ? new ApiResponse($"成功更新{result}条{GetEntityName()}数据", true)
                    : new ApiResponse($"更新{GetEntityName()}数据失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"批量更新{GetEntityName()}失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 删除单个数据
        /// </summary>
        public async Task<ApiResponse> DeleteAsync(TDto dto)
        {
            try
            {
                var entity = _mapper.Map<TEntity>(dto);
                var result = await SugarSQLHelper.Instance.DB.Deleteable(entity).ExecuteCommandAsync();

                return result > 0
                    ? new ApiResponse($"{GetEntityName()}{dto.ID}删除成功", true)
                    : new ApiResponse($"{GetEntityName()}{dto.ID}不存在或已删除");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"删除{GetEntityName()}失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public async Task<ApiResponse> DeleteAsync(IEnumerable<TDto> dtos)
        {
            try
            {
                if (dtos?.Any() != true)
                    return new ApiResponse("没有可删除的对象");

                var entities = _mapper.Map<List<TEntity>>(dtos);
                var result = await SugarSQLHelper.Instance.DB.Deleteable(entities).ExecuteCommandAsync();

                return result > 0
                    ? new ApiResponse($"成功删除{result}条{GetEntityName()}数据", true)
                    : new ApiResponse($"删除{GetEntityName()}数据失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"批量删除{GetEntityName()}失败：{ex.Message}");
            }
        }

        #region 抽象方法（由子类实现具体逻辑）
        /// <summary>
        /// 检查数据是否已存在
        /// </summary>
        protected abstract Task<bool> ExistsAsync(TDto dto);

        /// <summary>
        /// 获取已存在的实体
        /// </summary>
        protected abstract Task<TEntity> GetExistingEntityAsync(TDto dto);

        /// <summary>
        /// 获取实体名称（用于提示信息）
        /// </summary>
        protected abstract string GetEntityName();
        #endregion
    }
}
