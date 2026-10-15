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
    /// Modbus通信服务类
    /// 继承自ModbusServiceBase，提供Modbus通信设备（如PLC、机器人）的数据库操作
    /// 核心职责：处理设备信息的查询、新增、更新和删除等数据访问操作
    /// 依赖SugarSQLHelper进行数据库交互，通过AutoMapper实现实体与DTO的转换
    /// </summary>
    public class ModbusCommunicationService : ModbusServiceBase
    {
        #region 常量定义
        private const string DefaultSuccessMessage = "操作成功";
        private const string DefaultFailureMessage = "操作失败";
        private const string DuplicateErrorMessage = "已存在相同配置的设备，不允许重复添加";
        private const string NullReferenceErrorMessage = "传入参数不能为空";
        private const string RecordNotFoundMessage = "未找到匹配的记录";
        #endregion

        #region 查询操作
        /// <summary>
        /// 异步获取所有Modbus通信设备信息
        /// </summary>
        /// <returns>ApiResponse响应对象，包含设备DTO列表或错误信息</returns>
        internal async Task<ApiResponse<List<ModbusCommunicationDto>>> GetAllAsync()
        {
            try
            {
                var entities = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationEntity>()
                    .ToListAsync();

                var dtos = _mapper.Map<List<ModbusCommunicationDto>>(entities);
                return new ApiResponse<List<ModbusCommunicationDto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse<List<ModbusCommunicationDto>>($"查询失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步根据设备名称查询Modbus通信设备
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>ApiResponse响应对象，包含设备DTO或错误信息</returns>
        internal async Task<ApiResponse<ModbusCommunicationDto>> GetByDeviceNameAsync(string deviceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    return new ApiResponse<ModbusCommunicationDto>("设备名称不能为空");
                }

                var entity = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationEntity>()
                    .FirstAsync(u => u.设备名称 == deviceName);

                if (entity == null)
                {
                    return new ApiResponse<ModbusCommunicationDto>($"{RecordNotFoundMessage}：{deviceName}");
                }

                var dto = _mapper.Map<ModbusCommunicationDto>(entity);
                return new ApiResponse<ModbusCommunicationDto>(null, true, dto);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse<ModbusCommunicationDto>($"查询失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步根据IP地址查询Modbus通信设备
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <returns>ApiResponse响应对象，包含设备DTO列表或错误信息</returns>
        internal async Task<ApiResponse<List<ModbusCommunicationDto>>> GetByIpAddressAsync(string ipAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    return new ApiResponse<List<ModbusCommunicationDto>>("IP地址不能为空");
                }

                var entities = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationEntity>()
                    .Where(u => u.设备IP == ipAddress)
                    .ToListAsync();

                var dtos = _mapper.Map<List<ModbusCommunicationDto>>(entities);
                return new ApiResponse<List<ModbusCommunicationDto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse<List<ModbusCommunicationDto>>($"查询失败：{ex.Message}");
            }
        }
        #endregion

        #region 新增与更新操作
        /// <summary>
        /// 异步新增Modbus通信设备（已过时）
        /// </summary>
        /// <param name="dto">待新增的设备数据传输对象</param>
        /// <returns>ApiResponse响应对象，包含操作结果信息</returns>
        [Obsolete("此方法已过时，建议使用AddOrUpdateAsync方法替代，以支持新增和更新统一处理")]
        internal async Task<ApiResponse> AddAsync(ModbusCommunicationDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return new ApiResponse(NullReferenceErrorMessage);
                }

                // 优化查询：只查询需要的字段进行唯一性校验
                var isDuplicate = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationEntity>()
                    .AnyAsync(x => x.设备名称 == dto.设备名称 && x.站号 == dto.站号);

                if (isDuplicate)
                {
                    return new ApiResponse(DuplicateErrorMessage);
                }

                var newEntity = _mapper.Map<ModbusCommunicationEntity>(dto);
                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Insertable(newEntity)
                    .ExecuteCommandAsync();

                return affectedRows > 0
                    ? new ApiResponse("添加成功", true)
                    : new ApiResponse("添加失败");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步新增或更新Modbus通信设备
        /// </summary>
        /// <param name="dto">包含设备信息的数据传输对象</param>
        /// <returns>ApiResponse响应对象，包含操作结果信息</returns>
        internal async Task<ApiResponse> AddOrUpdateAsync(ModbusCommunicationDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return new ApiResponse(NullReferenceErrorMessage);
                }

                // 验证设备必填信息
                if (string.IsNullOrWhiteSpace(dto.设备名称) || string.IsNullOrWhiteSpace(dto.设备IP))
                {
                    return new ApiResponse("设备名称和IP地址为必填项");
                }

                var entity = _mapper.Map<ModbusCommunicationEntity>(dto);
                var existingEntity = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationEntity>()
                    .FirstAsync(u => u.设备名称 == dto.设备名称);

                int affectedRows = 0;
                string successMessage;

                if (existingEntity == null)
                {
                    // 新增前检查唯一性
                    var isDuplicate = await SugarSQLHelper.Instance.DB
                        .Queryable<ModbusCommunicationEntity>()
                        .AnyAsync(x => x.设备IP == dto.设备IP && x.设备Port == dto.设备Port && x.站号 == dto.站号);

                    if (isDuplicate)
                    {
                        return new ApiResponse("已存在相同IP、端口和站号的设备");
                    }

                    affectedRows = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();
                    successMessage = "添加IO卡完成";
                }
                else
                {
                    // 保留创建时间等不可修改字段
                    entity.创建时间 = existingEntity.创建时间;
                    affectedRows = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();
                    successMessage = "更新IO卡完成";
                }

                return affectedRows > 0
                    ? new ApiResponse(successMessage, true)
                    : new ApiResponse($"{DefaultFailureMessage}：未影响任何记录");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步批量更新Modbus通信设备
        /// </summary>
        /// <param name="dtos">待更新的设备DTO集合</param>
        /// <returns>ApiResponse响应对象，包含操作结果信息</returns>
        internal async Task<ApiResponse> UpdateAsync(IEnumerable<ModbusCommunicationDto> dtos)
        {
            try
            {
                if (dtos == null || !dtos.Any())
                {
                    return new ApiResponse(NullReferenceErrorMessage);
                }

                var entities = _mapper.Map<List<ModbusCommunicationEntity>>(dtos);
                var deviceNames = entities.Select(e => e.设备名称).ToList();

                // 验证所有设备是否存在
                var existingCount = await SugarSQLHelper.Instance.DB
                    .Queryable<ModbusCommunicationEntity>()
                    .Where(e => deviceNames.Contains(e.设备名称))
                    .CountAsync();

                if (existingCount != deviceNames.Count)
                {
                    return new ApiResponse("部分设备不存在，无法更新");
                }

                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Updateable(entities)
                    .ExecuteCommandAsync();

                return affectedRows > 0
                    ? new ApiResponse($"更新IO卡完成，共更新{affectedRows}条记录", true)
                    : new ApiResponse($"{RecordNotFoundMessage}或数据未变更");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步更新单个Modbus通信设备
        /// </summary>
        /// <param name="dto">待更新的设备DTO</param>
        /// <returns>ApiResponse响应对象，包含操作结果信息</returns>
        internal async Task<ApiResponse> UpdateAsync(ModbusCommunicationDto dto)
        {
            return await UpdateAsync(new List<ModbusCommunicationDto> { dto });
        }
        #endregion

        #region 删除操作
        /// <summary>
        /// 异步删除单个Modbus通信设备
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>ApiResponse响应对象，包含操作结果信息</returns>
        internal async Task<ApiResponse> DeleteAsync(string deviceName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceName))
                {
                    return new ApiResponse("设备名称不能为空");
                }

                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Deleteable<ModbusCommunicationEntity>(u => u.设备名称 == deviceName)
                    .ExecuteCommandAsync();

                return affectedRows > 0
                    ? new ApiResponse($"删除完成，IO卡[{deviceName}]已删除", true)
                    : new ApiResponse($"删除失败，IO卡[{deviceName}]不存在");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 异步删除单个Modbus通信设备
        /// </summary>
        /// <param name="dto">待删除的设备DTO</param>
        /// <returns>ApiResponse响应对象，包含操作结果信息</returns>
        internal async Task<ApiResponse> DeleteAsync(ModbusCommunicationDto dto)
        {
            if (dto == null)
            {
                return new ApiResponse(NullReferenceErrorMessage);
            }

            return await DeleteAsync(dto.设备名称);
        }

        /// <summary>
        /// 异步批量删除Modbus通信设备
        /// </summary>
        /// <param name="dtos">待删除的设备DTO集合</param>
        /// <returns>ApiResponse响应对象，包含操作结果信息</returns>
        internal async Task<ApiResponse> DeleteAsync(IEnumerable<ModbusCommunicationDto> dtos)
        {
            try
            {
                if (dtos == null || !dtos.Any())
                {
                    return new ApiResponse(NullReferenceErrorMessage);
                }

                var deviceNames = dtos.Select(d => d.设备名称).Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
                if (!deviceNames.Any())
                {
                    return new ApiResponse("设备名称不能为空");
                }

                var affectedRows = await SugarSQLHelper.Instance.DB
                    .Deleteable<ModbusCommunicationEntity>(u => deviceNames.Contains(u.设备名称))
                    .ExecuteCommandAsync();

                return affectedRows > 0
                    ? new ApiResponse($"删除IO卡完成，共删除{affectedRows}条记录", true)
                    : new ApiResponse($"{RecordNotFoundMessage}或已删除");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }
        #endregion

        #region 验证方法
        /// <summary>
        /// 验证设备IP地址格式
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <returns>验证结果</returns>
        private bool IsValidIpAddress(string ipAddress)
        {
            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }

        /// <summary>
        /// 验证Modbus站号范围（1-247）
        /// </summary>
        /// <param name="stationNumber">站号</param>
        /// <returns>验证结果</returns>
        private bool IsValidStationNumber(byte stationNumber)
        {
            return stationNumber >= 1 && stationNumber <= 247;
        }
        #endregion
    }
}