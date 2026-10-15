using AutoMapper;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.TCPIP.Service
{
    /// <summary>
    /// TCP/IP服务基类，提供通用的数据访问功能
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TDto">DTO类型</typeparam>
    public abstract class TcpIpServiceBase<TEntity, TDto>
        where TEntity : class, new()
        where TDto : class, new()
    {
        protected readonly IMapper _mapper;

        protected TcpIpServiceBase()
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
                return new ApiResponse<List<TDto>>($"查询失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 添加或更新数据
        /// </summary>
        public abstract Task<ApiResponse> AddOrUpdateAsync(TDto dto);

        /// <summary>
        /// 更新数据
        /// </summary>
        public async Task<ApiResponse> UpdateAsync(IEnumerable<TDto> dtos)
        {
            try
            {
                if (dtos?.Any() != true)
                {
                    return new ApiResponse("无有效更新对象");
                }

                var entities = _mapper.Map<List<TEntity>>(dtos);
                var affectedRows = await SugarSQLHelper.Instance.DB.Updateable(entities).ExecuteCommandAsync();

                return affectedRows > 0
                    ? new ApiResponse($"更新成功，影响{affectedRows}行", true)
                    : new ApiResponse("更新失败，未找到匹配数据");
            }
            catch (Exception ex)
            {
                return new ApiResponse($"更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        public async Task<ApiResponse> DeleteAsync(IEnumerable<TDto> dtos)
        {
            try
            {
                if (dtos?.Any() != true)
                {
                    return new ApiResponse("无有效删除对象");
                }

                var entities = _mapper.Map<List<TEntity>>(dtos);
                var affectedRows = await SugarSQLHelper.Instance.DB.Deleteable(entities).ExecuteCommandAsync();

                return affectedRows > 0
                    ? new ApiResponse($"删除成功，影响{affectedRows}行", true)
                    : new ApiResponse("删除失败，未找到匹配数据");
            }
            catch (Exception ex)
            {
                return new ApiResponse($"删除失败: {ex.Message}");
            }
        }
    }

}
