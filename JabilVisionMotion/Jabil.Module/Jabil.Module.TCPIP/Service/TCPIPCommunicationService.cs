using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.TCPIP.Common.Dtos;
using Jabil.Module.TCPIP.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.TCPIP.Service
{
    /// <summary>
    /// TCP/IP通信配置服务
    /// </summary>
    public class TcpIpCommunicationService : TcpIpServiceBase<TcpIpCommunicationEntity, TcpIpCommunicationDto>
    {
        /// <summary>
        /// 添加或更新通信配置
        /// </summary>
        public override async Task<ApiResponse> AddOrUpdateAsync(TcpIpCommunicationDto dto)
        {
            try
            {
                var entity = _mapper.Map<TcpIpCommunicationEntity>(dto);
                var existingEntity = await SugarSQLHelper.Instance.DB
                    .Queryable<TcpIpCommunicationEntity>()
                    .SingleAsync(e => e.设备名称 == dto.设备名称);

                if (existingEntity == null)
                {
                    // 检查是否存在相同设备索引
                    var hasDuplicateIndex = await SugarSQLHelper.Instance.DB
                        .Queryable<TcpIpCommunicationEntity>()
                        .AnyAsync(e => e.设备名称 == dto.设备名称 && e.设备索引 == dto.设备索引);

                    if (hasDuplicateIndex)
                    {
                        return new ApiResponse("已存在相同的设备名称和索引，无法添加");
                    }

                    var affectedRows = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();
                    return affectedRows > 0
                        ? new ApiResponse("添加通信配置成功", true)
                        : new ApiResponse("添加通信配置失败");
                }
                else
                {
                    var affectedRows = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();
                    return affectedRows > 0
                        ? new ApiResponse("更新通信配置成功", true)
                        : new ApiResponse("更新通信配置失败");
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse($"操作失败: {ex.Message}");
            }
        }
    }

}
