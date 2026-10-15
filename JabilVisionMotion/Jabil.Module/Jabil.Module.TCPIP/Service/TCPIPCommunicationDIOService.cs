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
    /// TCP/IP DIO配置服务
    /// </summary>
    public class TcpIpCommunicationDioService : TcpIpServiceBase<TcpIpCommunicationDioEntity, TcpIpCommunicationDioDto>
    {
        /// <summary>
        /// 添加或更新DIO配置
        /// </summary>
        public override async Task<ApiResponse> AddOrUpdateAsync(TcpIpCommunicationDioDto dto)
        {
            try
            {
                var entity = _mapper.Map<TcpIpCommunicationDioEntity>(dto);
                var existingEntity = await SugarSQLHelper.Instance.DB
                    .Queryable<TcpIpCommunicationDioEntity>()
                    .SingleAsync(e => e.点位名称 == dto.点位名称);

                if (existingEntity == null)
                {
                    var affectedRows = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();
                    return affectedRows > 0
                        ? new ApiResponse("添加DIO配置成功", true)
                        : new ApiResponse("添加DIO配置失败");
                }
                else
                {
                    var affectedRows = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();
                    return affectedRows > 0
                        ? new ApiResponse("更新DIO配置成功", true)
                        : new ApiResponse("更新DIO配置失败");
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse($"操作失败: {ex.Message}");
            }
        }
    }

}
