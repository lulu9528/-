using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Log.Lib;
using Jabil.Module.Modbus.Common.Dtos;
using Jabil.Module.Modbus.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jabil.Module.Modbus.Service
{
    /// <summary>
    /// Modbus DIO通信服务类
    /// 继承自ModbusServiceBase，提供Modbus数字量输入输出(DI/DO)点位的数据库操作
    /// 负责DIO点位的查询、新增、更新和删除等CRUD操作
    /// </summary>
    public class ModbusCommunicationDIOService : ModbusServiceBase
    {
        #region 查询操作
        /// <summary>
        /// 异步获取所有Modbus DIO点位信息
        /// 从数据库查询所有DIO点位实体，转换为DTO后返回
        /// </summary>
        /// <returns>包含所有DIO点位DTO列表的ApiResponse响应对象</returns>
        internal async Task<ApiResponse<List<ModbusCommunicationDioDto>>> GetAllAsync()
        {
            try
            {
                // 使用SugarSQLHelper查询所有DIO点位实体
                var entities = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .ToListAsync();

                // 将实体列表映射为DTO列表（依赖于基类中的AutoMapper实例）
                var dtos = _mapper.Map<List<ModbusCommunicationDioDto>>(entities);

                // 返回成功响应，包含DTO数据
                return new ApiResponse<List<ModbusCommunicationDioDto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                // 记录异常并返回失败响应
                RLog.SaveError(ex);
                return new ApiResponse<List<ModbusCommunicationDioDto>>("获取DIO点位列表失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 异步根据设备名称获取Modbus DIO点位信息
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>包含指定设备DIO点位DTO列表的ApiResponse响应对象</returns>
        internal async Task<ApiResponse<List<ModbusCommunicationDioDto>>> GetByDeviceNameAsync(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                return new ApiResponse<List<ModbusCommunicationDioDto>>("设备名称不能为空");
            }

            try
            {
                var entities = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .Where(e => e.设备名称 == deviceName)
                    .ToListAsync();

                var dtos = _mapper.Map<List<ModbusCommunicationDioDto>>(entities);
                return new ApiResponse<List<ModbusCommunicationDioDto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse<List<ModbusCommunicationDioDto>>($"获取设备[{deviceName}]的DIO点位失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步根据点位索引获取单个Modbus DIO点位信息
        /// </summary>
        /// <param name="pointIndex">点位索引</param>
        /// <returns>包含DIO点位DTO的ApiResponse响应对象</returns>
        internal async Task<ApiResponse<ModbusCommunicationDioDto>> GetByPointIndexAsync(string pointIndex)
        {
            if (string.IsNullOrWhiteSpace(pointIndex))
            {
                return new ApiResponse<ModbusCommunicationDioDto>("点位索引不能为空");
            }

            try
            {
                var entity = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .FirstAsync(e => e.点位索引 == pointIndex);

                if (entity == null)
                {
                    return new ApiResponse<ModbusCommunicationDioDto>($"未找到索引为[{pointIndex}]的DIO点位");
                }

                var dto = _mapper.Map<ModbusCommunicationDioDto>(entity);
                return new ApiResponse<ModbusCommunicationDioDto>(null, true, dto);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse<ModbusCommunicationDioDto>($"获取点位[{pointIndex}]信息失败：{ex.Message}");
            }
        }
        #endregion

        #region 新增与更新操作
        /// <summary>
        /// 异步新增Modbus DIO点位
        /// 包含唯一性校验（同一设备下点位索引不能重复）
        /// </summary>
        /// <param name="dto">待新增的DIO点位DTO</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> AddAsync(ModbusCommunicationDioDto dto)
        {
            if (dto == null)
            {
                return new ApiResponse("新增的DIO点位信息不能为空");
            }

            // 基础数据校验
            if (string.IsNullOrWhiteSpace(dto.设备名称))
            {
                return new ApiResponse("设备名称不能为空");
            }
            if (string.IsNullOrWhiteSpace(dto.点位索引))
            {
                return new ApiResponse("点位索引不能为空");
            }
            if (string.IsNullOrWhiteSpace(dto.点位类型))
            {
                return new ApiResponse("点位类型不能为空");
            }

            try
            {
                // 优化查询：仅查询需要的字段用于唯一性校验
                bool isDuplicate = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .AnyAsync(e => e.设备名称 == dto.设备名称 && e.点位索引 == dto.点位索引);

                if (isDuplicate)
                {
                    return new ApiResponse($"设备[{dto.设备名称}]已存在索引为[{dto.点位索引}]的点位，不允许重复添加");
                }

                // 补充创建时间信息
                var newEntity = _mapper.Map<ModbusCommunicationDioEntity>(dto);
                newEntity.创建时间 = DateTime.Now;
                newEntity.更新时间 = DateTime.Now;

                // 执行数据库插入操作
                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Insertable(newEntity)
                    .ExecuteCommandAsync();

                // 根据受影响行数返回相应结果
                if (affectedRows > 0)
                    return new ApiResponse($"设备[{dto.设备名称}]的点位[{dto.点位索引}]添加成功", true);

                return new ApiResponse($"设备[{dto.设备名称}]的点位[{dto.点位索引}]添加失败");
            }
            catch (Exception ex)
            {
                // 记录异常并返回失败响应
                RLog.SaveError(ex);
                return new ApiResponse($"添加DIO点位失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步新增或更新Modbus DIO点位
        /// 根据点位索引判断：不存在则新增，存在则更新
        /// </summary>
        /// <param name="dto">包含点位信息的DTO</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> AddOrUpdateAsync(ModbusCommunicationDioDto dto)
        {
            if (dto == null)
            {
                return new ApiResponse("DIO点位信息不能为空");
            }

            if (string.IsNullOrWhiteSpace(dto.点位索引))
            {
                return new ApiResponse("点位索引不能为空");
            }

            try
            {
                // 将DTO映射为实体
                var entity = _mapper.Map<ModbusCommunicationDioEntity>(dto);
                entity.更新时间 = DateTime.Now;

                // 根据设备名称和点位索引查询数据库中是否存在该记录
                ModbusCommunicationDioEntity existingEntity = null;
                try
                {
                    existingEntity = await SugarSQLHelper.Instance.DB
                        .Queryable<ModbusCommunicationDioEntity>()
                        .Where(u => u.设备名称 == dto.设备名称 && u.点位索引 == dto.点位索引)
                        .FirstAsync();
                }
                catch (Exception)
                {
                    // 如果没有找到记录，FirstAsync会抛出异常，这里我们将其视为没有找到记录
                    existingEntity = null;
                }

                int affectedRows = 0;

                if (existingEntity == null)
                {
                    // 新增时补充创建时间
                    entity.创建时间 = DateTime.Now;
                    // 记录不存在，执行新增操作
                    affectedRows = await SugarSQLHelper.Instance.DB
                        .Insertable(entity)
                        .ExecuteCommandAsync();

                    if (affectedRows >= 0)
                    {
                        return new ApiResponse($"点位[{dto.点位索引}]添加完成", true);
                    }
                    return new ApiResponse($"点位[{dto.点位索引}]添加失败");
                }
                else
                {
                    // 保留原有创建时间
                    entity.创建时间 = existingEntity.创建时间;
                    // 记录存在，执行更新操作
                    affectedRows = await SugarSQLHelper.Instance.DB
                        .Updateable(entity)
                        .ExecuteCommandAsync();

                    if (affectedRows > 0)
                    {
                        return new ApiResponse($"点位[{dto.点位索引}]更新完成", true);
                    }
                    return new ApiResponse($"点位[{dto.点位索引}]更新失败，未找到匹配记录或数据未变更");
                }
            }
            catch (Exception ex)
            {
                // 记录异常并返回失败响应
                RLog.SaveError(ex);
                return new ApiResponse($"点位[{dto.点位索引}]操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步批量更新Modbus DIO点位
        /// </summary>
        /// <param name="dtos">待更新的DIO点位DTO集合</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> UpdateAsync(IEnumerable<ModbusCommunicationDioDto> dtos)
        {
            try
            {
                // 参数校验：确保更新集合不为空
                if (dtos == null || !dtos.Any())
                {
                    return new ApiResponse("无更新对象");
                }

                // 验证所有点位索引不为空
                var invalidDtos = dtos.Where(d => string.IsNullOrWhiteSpace(d.点位索引)).ToList();
                if (invalidDtos.Any())
                {
                    return new ApiResponse($"存在{invalidDtos.Count}个点位索引为空的记录，不允许更新");
                }

                // 获取所有要更新的点位索引
                var pointIndexes = dtos.Select(d => d.点位索引).ToList();
                // 检查这些点位是否都存在
                var existingCount = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .Where(e => pointIndexes.Contains(e.点位索引))
                    .CountAsync();

                if (existingCount != pointIndexes.Count)
                {
                    return new ApiResponse("部分点位不存在，无法完成批量更新");
                }

                // 将DTO集合映射为实体集合
                var entities = _mapper.Map<List<ModbusCommunicationDioEntity>>(dtos);
                entities.ForEach(e => e.更新时间 = DateTime.Now);

                // 执行批量更新操作
                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Updateable(entities)
                    .ExecuteCommandAsync();

                // 根据受影响行数返回相应结果
                if (affectedRows > 0)
                {
                    return new ApiResponse($"批量更新完成，共更新{affectedRows}条记录", true);
                }
                else
                {
                    return new ApiResponse("批量更新失败，未找到匹配的记录或数据未变更");
                }
            }
            catch (Exception ex)
            {
                // 记录异常并返回失败响应
                RLog.SaveError(ex);
                return new ApiResponse("批量更新DIO点位失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 异步更新单个Modbus DIO点位
        /// </summary>
        /// <param name="dto">待更新的DIO点位DTO</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> UpdateAsync(ModbusCommunicationDioDto dto)
        {
            try
            {
                // 参数校验：确保更新对象不为空
                if (dto == null)
                {
                    return new ApiResponse("无更新对象");
                }

                if (string.IsNullOrWhiteSpace(dto.点位索引))
                {
                    return new ApiResponse("点位索引不能为空");
                }

                // 检查点位是否存在
                var exists = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .AnyAsync(e => e.点位索引 == dto.点位索引);

                if (!exists)
                {
                    return new ApiResponse($"点位[{dto.点位索引}]不存在，无法更新");
                }

                // 将DTO映射为实体并更新时间
                var entity = _mapper.Map<ModbusCommunicationDioEntity>(dto);
                entity.更新时间 = DateTime.Now;

                // 执行更新操作
                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Updateable(entity)
                    .ExecuteCommandAsync();

                // 根据受影响行数返回相应结果
                if (affectedRows > 0)
                {
                    return new ApiResponse($"点位[{dto.点位索引}]更新完成", true);
                }
                else
                {
                    return new ApiResponse($"点位[{dto.点位索引}]更新失败，数据未变更");
                }
            }
            catch (Exception ex)
            {
                // 记录异常并返回失败响应
                RLog.SaveError(ex);
                return new ApiResponse($"更新点位[{dto?.点位索引}]失败：{ex.Message}");
            }
        }
        #endregion

        #region 删除操作
        /// <summary>
        /// 异步删除单个Modbus DIO点位
        /// 根据点位索引执行删除操作
        /// </summary>
        /// <param name="dto">待删除的DIO点位DTO</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> DeleteAsync(ModbusCommunicationDioDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.点位索引))
            {
                return new ApiResponse("点位索引不能为空");
            }

            try
            {
                // 先检查点位是否存在
                var exists = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .AnyAsync(e => e.点位索引 == dto.点位索引);

                if (!exists)
                {
                    return new ApiResponse($"点位[{dto.点位索引}]不存在，无需删除");
                }

                // 执行删除操作
                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Deleteable<ModbusCommunicationDioEntity>(u => u.点位索引 == dto.点位索引)
                    .ExecuteCommandAsync();

                // 根据受影响行数返回相应结果
                if (affectedRows > 0)
                {
                    return new ApiResponse($"点位[{dto.点位索引}]删除完成", true);
                }

                return new ApiResponse($"点位[{dto.点位索引}]删除失败");
            }
            catch (Exception ex)
            {
                // 记录异常并返回失败响应
                RLog.SaveError(ex);
                return new ApiResponse($"删除点位[{dto.点位索引}]失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步批量删除Modbus DIO点位
        /// </summary>
        /// <param name="dtos">待删除的DIO点位DTO集合</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> DeleteAsync(IEnumerable<ModbusCommunicationDioDto> dtos)
        {
            try
            {
                // 参数校验：确保删除集合不为空
                if (dtos == null || !dtos.Any())
                {
                    return new ApiResponse("无删除对象");
                }

                // 提取所有有效的点位索引
                var pointIndexes = dtos
                    .Where(d => !string.IsNullOrWhiteSpace(d.点位索引))
                    .Select(d => d.点位索引)
                    .Distinct()
                    .ToList();

                if (!pointIndexes.Any())
                {
                    return new ApiResponse("所有删除对象的点位索引均为空，无法执行删除");
                }

                // 检查存在的点位数量
                var existsCount = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .Where(e => pointIndexes.Contains(e.点位索引))
                    .CountAsync();

                if (existsCount == 0)
                {
                    return new ApiResponse("所有点位均不存在，无需删除");
                }

                // 执行批量删除操作
                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Deleteable<ModbusCommunicationDioEntity>(e => pointIndexes.Contains(e.点位索引))
                    .ExecuteCommandAsync();

                // 根据受影响行数返回相应结果
                if (affectedRows > 0)
                {
                    return new ApiResponse($"批量删除完成，共删除{affectedRows}个点位", true);
                }

                return new ApiResponse("批量删除失败，未找到可删除的点位");
            }
            catch (Exception ex)
            {
                // 记录异常并返回失败响应
                RLog.SaveError(ex);
                return new ApiResponse("批量删除DIO点位失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 异步删除指定设备的所有DIO点位
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> DeleteByDeviceNameAsync(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
            {
                return new ApiResponse("设备名称不能为空");
            }

            try
            {
                // 检查该设备是否有点位
                var existsCount = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationDioEntity>()
                    .Where(e => e.设备名称 == deviceName)
                    .CountAsync();

                if (existsCount == 0)
                {
                    return new ApiResponse($"设备[{deviceName}]没有关联的DIO点位，无需删除");
                }

                // 执行删除操作
                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Deleteable<ModbusCommunicationDioEntity>(e => e.设备名称 == deviceName)
                    .ExecuteCommandAsync();

                if (affectedRows > 0)
                {
                    return new ApiResponse($"设备[{deviceName}]的{affectedRows}个DIO点位已全部删除", true);
                }

                return new ApiResponse($"删除设备[{deviceName}]的DIO点位失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"删除设备[{deviceName}]的DIO点位失败：{ex.Message}");
            }
        }
        #endregion
    }
}