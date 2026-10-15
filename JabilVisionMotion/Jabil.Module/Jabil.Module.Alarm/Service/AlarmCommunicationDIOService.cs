using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Alarm.Common.Dtos;
using Jabil.Module.Alarm.Entity;
using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jabil.Module.Alarm.Service
{
    /// <summary>
    /// 报警通信DIO服务类
    /// 继承自AlarmServiceBase，提供报警通信数字输入输出（DIO）设备的CRUD操作
    /// 负责处理与报警通信DIO相关的所有数据访问和业务逻辑
    /// </summary>
    public class AlarmCommunicationDIOService : AlarmServiceBase
    {
        #region 查询操作

        /// <summary>
        /// 异步获取所有报警通信DIO数据
        /// 从数据库查询所有实体并映射为DTO返回
        /// </summary>
        /// <returns>包含DTO列表的ApiResponse响应对象</returns>
        public async Task<ApiResponse<List<AlarmCommunicationDIODto>>> GetAllAsync()
        {
            try
            {
                // 从数据库异步查询所有报警通信DIO实体
                // ConfigureAwait(false)：避免上下文切换，提高非UI线程操作性能
                var entities = await SugarSQLHelper.Instance.DB
                    .Queryable<AlarmCommunicationDIOEntity>()
                    .ToListAsync()
                    .ConfigureAwait(false);

                // 将实体列表映射为DTO列表（使用基类中的_mapper实例）
                var dtos = _mapper.Map<List<AlarmCommunicationDIODto>>(entities);

                // 返回成功响应，包含DTO数据
                return new ApiResponse<List<AlarmCommunicationDIODto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                // 记录异常并返回失败响应
                RLog.SaveError(ex);
                return new ApiResponse<List<AlarmCommunicationDIODto>>("查询失败：" + ex.Message);
            }
        }

        #endregion

        #region 添加与更新操作

        /// <summary>
        /// 异步添加报警通信DIO记录
        /// 包含重复数据检查，确保设备名称和点位索引组合唯一
        /// </summary>
        /// <param name="dto">待添加的数据传输对象</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task<ApiResponse> AddAsync(AlarmCommunicationDIODto dto)
        {
            // 参数验证：使用卫语句检查输入有效性
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "添加的数据不能为空");

            try
            {
                // 优化查询：直接检查是否存在相同的设备名称和点位索引组合
                // 避免查询所有数据再筛选，减少数据传输和内存占用
                var exists = await SugarSQLHelper.Instance.DB
                    .Queryable<AlarmCommunicationDIOEntity>()
                    .AnyAsync(x => x.设备名称 == dto.设备名称 && x.点位索引 == dto.点位索引)
                    .ConfigureAwait(false);

                // 存在重复数据时返回失败响应
                if (exists)
                {
                    return new ApiResponse("已存在相同的设备名称和点位索引，不允许重复添加");
                }

                // 将DTO映射为实体并执行插入操作
                var entity = _mapper.Map<AlarmCommunicationDIOEntity>(dto);
                var affectRows = await SugarSQLHelper.Instance.DB
                    .Insertable(entity)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                // 根据受影响的行数返回相应结果
                return affectRows > 0
                    ? new ApiResponse("添加成功", true)
                    : new ApiResponse("添加失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 异步添加或更新报警通信DIO记录
        /// 根据点位索引判断记录是否存在，自动执行添加或更新操作
        /// </summary>
        /// <param name="dto">数据传输对象</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task<ApiResponse> AddOrUpdateAsync(AlarmCommunicationDIODto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "数据不能为空");

            try
            {
                // 将DTO映射为实体
                var entity = _mapper.Map<AlarmCommunicationDIOEntity>(dto);

                // 根据点位索引查询是否存在该记录
                var existingEntity = await SugarSQLHelper.Instance.DB
                    .Queryable<AlarmCommunicationDIOEntity>()
                    .SingleAsync(u => u.点位索引 == dto.点位索引)
                    .ConfigureAwait(false);

                int affectRows;
                // 记录不存在则执行添加操作
                if (existingEntity == null)
                {
                    affectRows = await SugarSQLHelper.Instance.DB
                        .Insertable(entity)
                        .ExecuteCommandAsync()
                        .ConfigureAwait(false);
                    return affectRows > 0
                        ? new ApiResponse("添加IO卡完成", true)
                        : new ApiResponse("添加IO卡失败");
                }
                // 记录存在则执行更新操作
                else
                {
                    affectRows = await SugarSQLHelper.Instance.DB
                        .Updateable(entity)
                        .ExecuteCommandAsync()
                        .ConfigureAwait(false);
                    return affectRows > 0
                        ? new ApiResponse("更新IO卡完成", true)
                        : new ApiResponse("更新IO卡失败");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 异步批量更新报警通信DIO记录
        /// </summary>
        /// <param name="dtos">待更新的数据传输对象集合</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        public async Task<ApiResponse> UpdateAsync(IEnumerable<AlarmCommunicationDIODto> dtos)
        {
            // 验证更新数据是否为空
            if (dtos == null || !dtos.Any())
                return new ApiResponse("无更新对象");

            try
            {
                // 将DTO集合映射为实体集合
                var entities = _mapper.Map<List<AlarmCommunicationDIOEntity>>(dtos);

                // 执行批量更新操作
                var affectRows = await SugarSQLHelper.Instance.DB
                    .Updateable(entities)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                return affectRows > 0
                    ? new ApiResponse($"更新IO卡完成，共{affectRows}条记录", true)
                    : new ApiResponse("更新IO卡失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 异步更新单个报警通信DIO记录
        /// </summary>
        /// <param name="dto">待更新的数据传输对象</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        public async Task<ApiResponse> UpdateAsync(AlarmCommunicationDIODto dto)
        {
            // 验证更新数据是否为空
            if (dto == null)
                return new ApiResponse("无更新对象");

            try
            {
                // 将DTO映射为实体
                var entity = _mapper.Map<AlarmCommunicationDIOEntity>(dto);

                // 执行更新操作
                var affectRows = await SugarSQLHelper.Instance.DB
                    .Updateable(entity)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                return affectRows > 0
                    ? new ApiResponse($"更新IO卡完成，共{affectRows}条记录", true)
                    : new ApiResponse("更新IO卡失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        #endregion

        #region 删除操作

        /// <summary>
        /// 异步删除单个报警通信DIO记录
        /// </summary>
        /// <param name="dto">待删除的数据传输对象</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task<ApiResponse> DeleteAsync(AlarmCommunicationDIODto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "删除的数据不能为空");

            try
            {
                // 将DTO映射为实体
                var entity = _mapper.Map<AlarmCommunicationDIOEntity>(dto);

                // 根据点位索引执行删除操作
                var affectRows = await SugarSQLHelper.Instance.DB
                    .Deleteable<AlarmCommunicationDIOEntity>(u => u.点位索引 == entity.点位索引)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                return affectRows > 0
                    ? new ApiResponse($"删除完成，IO卡{entity.点位索引}已删除", true)
                    : new ApiResponse($"删除失败，IO卡{entity.点位索引}不存在");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 异步批量删除报警通信DIO记录
        /// </summary>
        /// <param name="dtos">待删除的数据传输对象集合</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        public async Task<ApiResponse> DeleteAsync(IEnumerable<AlarmCommunicationDIODto> dtos)
        {
            // 验证删除数据是否为空
            if (dtos == null || !dtos.Any())
                return new ApiResponse("请选择要删除的数据");

            try
            {
                // 将DTO集合映射为实体集合
                var entities = _mapper.Map<List<AlarmCommunicationDIOEntity>>(dtos);

                // 执行批量删除操作
                var affectRows = await SugarSQLHelper.Instance.DB
                    .Deleteable(entities)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                return affectRows > 0
                    ? new ApiResponse("删除IO卡完成", true)
                    : new ApiResponse("删除失败，IO卡不存在或已删除");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        #endregion
    }
}
