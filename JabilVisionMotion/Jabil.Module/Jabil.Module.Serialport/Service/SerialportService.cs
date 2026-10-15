using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Serialport.Common.Dtos;
using Jabil.Module.Serialport.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Serialport.Service
{    /// <summary>
     /// 串口配置服务类，负责串口配置的数据库操作
     /// </summary>
    public class SerialportService : SerialportServiceBase
    {
        /// <summary>
        /// 获取所有串口配置
        /// </summary>
        public async Task<ApiResponse<List<SerialportDto>>> GetAllAsync()
        {
            try
            {
                var entities = await SugarSQLHelper.Instance.DB.Queryable<Entities.Serialport>().ToListAsync();
                var dtos = _mapper.Map<List<SerialportDto>>(entities);
                return new ApiResponse<List<SerialportDto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<SerialportDto>>($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 添加或更新串口配置
        /// </summary>
        public async Task<ApiResponse> AddOrUpdateAsync(SerialportDto dto)
        {
            try
            {
                var entity = _mapper.Map<Entities.Serialport>(dto);
                var existingEntity = await SugarSQLHelper.Instance.DB
                    .Queryable<Entities.Serialport>()
                    .SingleAsync(u => u.COM口 == dto.COM口);

                int result;
                if (existingEntity == null)
                {
                    result = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();
                    return result > 0
                        ? new ApiResponse("添加IO卡完成", true)
                        : new ApiResponse("添加IO卡失败");
                }
                else
                {
                    result = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();
                    return result > 0
                        ? new ApiResponse("更新IO卡完成", true)
                        : new ApiResponse("更新IO卡失败");
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新串口配置
        /// </summary>
        public async Task<ApiResponse> UpdateAsync(IEnumerable<SerialportDto> dtos)
        {
            try
            {
                if (dtos?.Any() != true)
                {
                    return new ApiResponse("无更新对象");
                }

                var entities = _mapper.Map<List<Entities.Serialport>>(dtos);
                var result = await SugarSQLHelper.Instance.DB.Updateable(entities).ExecuteCommandAsync();

                return result > 0
                    ? new ApiResponse($"更新IO卡完成{result}", true)
                    : new ApiResponse("更新IO卡失败");
            }
            catch (Exception ex)
            {
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新单个串口配置
        /// </summary>
        public async Task<ApiResponse> UpdateAsync(SerialportDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return new ApiResponse("无更新对象");
                }

                var entity = _mapper.Map<Entities.Serialport>(dto);
                var result = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();

                return result > 0
                    ? new ApiResponse($"更新IO卡完成{result}", true)
                    : new ApiResponse("更新IO卡失败");
            }
            catch (Exception ex)
            {
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量删除串口配置
        /// </summary>
        public async Task<ApiResponse> DeleteAsync(IEnumerable<SerialportDto> dtos)
        {
            try
            {
                if (dtos?.Any() != true)
                {
                    return new ApiResponse("没有可删除的配置数据");
                }

                var entities = _mapper.Map<List<Entities.Serialport>>(dtos);
                var result = await SugarSQLHelper.Instance.DB.Deleteable(entities).ExecuteCommandAsync();

                return result > 0
                    ? new ApiResponse("删除IO卡完成", true)
                    : new ApiResponse("删除失败,IO卡不存在或已删除");
            }
            catch (Exception ex)
            {
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 删除单个串口配置
        /// </summary>
        public async Task<ApiResponse> DeleteAsync(SerialportDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return new ApiResponse("删除对象不能为空");
                }

                var entity = _mapper.Map<Entities.Serialport>(dto);
                var result = await SugarSQLHelper.Instance.DB
                    .Deleteable<Entities.Serialport>(u => u.COM口 == entity.COM口)
                    .ExecuteCommandAsync();

                if (result > 0)
                {
                    return new ApiResponse($"删除完成,IO卡{dto.COM口}已删除", true);
                }

                return new ApiResponse($"删除失败,IO卡{dto.COM口}不存在");
            }
            catch (Exception ex)
            {
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }
    }

}
