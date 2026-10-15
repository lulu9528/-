using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.IO.Common.Dtos;
using Jabil.Module.IO.Entity;
using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.IO.Service
{
    /// <summary>
    /// IO卡点位服务类
    /// 继承自IOServiceBase基础服务类，提供IO卡点位的完整CRUD（创建、读取、更新、删除）操作
    /// 所有方法均支持异步操作和任务取消功能，确保在长时间操作中可以安全终止
    /// </summary>
    public class IOCardDIOService : IOServiceBase
    {
        #region 查询操作
        /// <summary>
        /// 获取所有IO卡点位信息
        /// 支持通过CancellationToken取消正在执行的异步操作
        /// </summary>
        /// <param name="cancellationToken">取消令牌，用于请求取消异步操作</param>
        /// <returns>包含所有IO卡点位DTO列表的ApiResponse响应对象</returns>
        internal async Task<ApiResponse<List<IOCardDIODto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 使用SugarSQLHelper查询所有IOCardDIOEntity实体
                // ToListAsync方法支持传入cancellationToken实现异步取消
                // ConfigureAwait(false)用于避免上下文切换，提高异步操作性能
                var entities = await SugarSQLHelper.Instance.DB
                    .Queryable<IOCardDIOEntity>()
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                // 将实体列表映射为DTO列表（通过AutoMapper实现）
                var dtos = _mapper.Map<List<IOCardDIODto>>(entities);

                // 返回成功响应，包含查询结果
                return new ApiResponse<List<IOCardDIODto>>(null, true, dtos);
            }
            // 捕获操作取消异常（当调用方请求取消时触发）
            catch (OperationCanceledException)
            {
                // 记录取消操作日志，使用蓝色标识
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位查询已取消");
                // 返回取消操作响应
                return new ApiResponse<List<IOCardDIODto>>("查询已取消");
            }
            // 捕获所有其他异常
            catch (Exception ex)
            {
                // 记录错误日志
                RLog.SaveError(ex);
                // 返回包含错误信息的响应
                return new ApiResponse<List<IOCardDIODto>>($"操作失败：{ex.Message}");
            }
        }
        #endregion

        #region 新增与更新操作
        /// <summary>
        /// 新增IO卡点位（已过时）
        /// 此方法已不推荐使用，建议使用AddOrUpdateAsync方法替代，以支持新增和更新统一处理
        /// </summary>
        /// <param name="dto">包含点位信息的数据传输对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        [Obsolete("请使用AddOrUpdateAsync方法")]
        internal async Task<ApiResponse> AddAsync(IOCardDIODto dto, CancellationToken cancellationToken = default)
        {
            // 验证DTO对象是否为空
            if (dto == null)
                return new ApiResponse("点位信息不能为空");

            try
            {
                // 检查是否存在相同卡名称和点位索引的记录（唯一约束验证）
                bool isExist = await SugarSQLHelper.Instance.DB
                    .Queryable<IOCardDIOEntity>()
                    .AnyAsync(e => e.卡名称 == dto.卡名称 && e.点位索引 == dto.点位索引,
                              cancellationToken)
                    .ConfigureAwait(false);

                // 如果已存在相同记录，返回错误信息
                if (isExist)
                    return new ApiResponse("该卡下已存在相同点位索引");

                // 将DTO映射为实体对象
                var entity = _mapper.Map<IOCardDIOEntity>(dto);

                // 执行插入操作并获取受影响的行数
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Insertable(entity)
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                // 根据受影响行数返回相应的操作结果
                return rowsAffected > 0
                    ? new ApiResponse("新增点位成功", true)
                    : new ApiResponse("新增点位失败");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位新增已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 新增或更新IO卡点位
        /// 根据DTO中的ID自动判断执行新增（ID≤0）或更新（ID>0）操作
        /// 支持操作取消功能
        /// </summary>
        /// <param name="dto">包含点位信息的数据传输对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> AddOrUpdateAsync(IOCardDIODto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
                return new ApiResponse("点位信息不能为空");

            try
            {
                // 将DTO映射为实体
                var entity = _mapper.Map<IOCardDIOEntity>(dto);

                // 根据ID判断是新增还是更新操作
                if (entity.ID <= 0) // 新增操作：ID未设置或小于等于0
                {
                    // 验证卡名称和点位索引的唯一性
                    bool isExist = await SugarSQLHelper.Instance.DB
                        .Queryable<IOCardDIOEntity>()
                        .AnyAsync(e => e.卡名称 == entity.卡名称 && e.点位索引 == entity.点位索引,
                                  cancellationToken)
                        .ConfigureAwait(false);

                    if (isExist)
                        return new ApiResponse("该卡下已存在相同点位索引");

                    // 执行插入
                    int rowsAffected = await SugarSQLHelper.Instance.DB
                        .Insertable(entity)
                        .ExecuteCommandAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return rowsAffected > 0
                        ? new ApiResponse("新增点位成功", true)
                        : new ApiResponse("新增点位失败");
                }
                else // 更新操作：ID已设置且大于0
                {
                    // 执行更新，仅更新ID匹配的记录
                    int rowsAffected = await SugarSQLHelper.Instance.DB
                        .Updateable(entity)
                        .Where(e => e.ID == entity.ID) // 确保只更新指定ID的记录
                        .ExecuteCommandAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return rowsAffected > 0
                        ? new ApiResponse("更新点位成功", true)
                        : new ApiResponse("未找到需要更新的点位或无变更");
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位新增/更新已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新IO卡点位
        /// 一次性处理多个点位的更新操作，提高批量处理效率
        /// </summary>
        /// <param name="dtos">包含多个点位信息的DTO集合</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> UpdateAsync(IEnumerable<IOCardDIODto> dtos, CancellationToken cancellationToken = default)
        {
            // 验证输入集合是否有效
            if (dtos == null || !dtos.Any())
                return new ApiResponse("无有效更新对象");

            try
            {
                // 将DTO集合映射为实体集合，并过滤掉ID无效的记录（ID≤0）
                var entities = _mapper.Map<List<IOCardDIOEntity>>(dtos)
                                     .Where(e => e.ID > 0)
                                     .ToList();

                // 检查是否有有效实体需要更新
                if (!entities.Any())
                    return new ApiResponse("所有更新对象均无有效ID");

                // 执行批量更新操作
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Updateable(entities)
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                return rowsAffected > 0
                    ? new ApiResponse($"批量更新成功，影响{rowsAffected}条记录", true)
                    : new ApiResponse("批量更新失败或无变更");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位批量更新已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 单条更新IO卡点位
        /// 针对单个点位进行更新操作，适用于单独修改某个点位信息的场景
        /// </summary>
        /// <param name="dto">包含点位信息的数据传输对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> UpdateAsync(IOCardDIODto dto, CancellationToken cancellationToken = default)
        {
            // 验证DTO和ID的有效性
            if (dto == null || dto.ID <= 0)
                return new ApiResponse("更新对象必须包含有效ID");

            try
            {
                // 映射DTO为实体
                var entity = _mapper.Map<IOCardDIOEntity>(dto);

                // 执行单条更新，仅更新ID匹配的记录
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Updateable(entity)
                    .Where(e => e.ID == entity.ID)
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                return rowsAffected > 0
                    ? new ApiResponse("更新点位成功", true)
                    : new ApiResponse("未找到需要更新的点位或无变更");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位更新已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }
        #endregion

        #region 删除操作
        /// <summary>
        /// 单条删除IO卡点位
        /// 根据DTO中的ID删除对应的点位记录
        /// </summary>
        /// <param name="dto">包含待删除点位ID的数据传输对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> DeleteAsync(IOCardDIODto dto, CancellationToken cancellationToken = default)
        {
            // 验证DTO和ID的有效性
            if (dto == null || dto.ID <= 0)
                return new ApiResponse("删除对象必须包含有效ID");

            try
            {
                // 根据ID执行删除操作
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Deleteable<IOCardDIOEntity>()
                    .Where(e => e.ID == dto.ID)
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                return rowsAffected > 0
                    ? new ApiResponse($"删除成功，点位ID：{dto.ID}", true)
                    : new ApiResponse("未找到需要删除的点位");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位删除已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量删除IO卡点位
        /// 一次性删除多个点位记录，适用于需要批量清理点位的场景
        /// </summary>
        /// <param name="dtos">包含多个待删除点位ID的DTO集合</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含操作结果的ApiResponse响应对象</returns>
        internal async Task<ApiResponse> DeleteAsync(IEnumerable<IOCardDIODto> dtos, CancellationToken cancellationToken = default)
        {
            // 验证输入集合是否有效
            if (dtos == null || !dtos.Any())
                return new ApiResponse("请选择需要删除的点位");

            try
            {
                // 提取有效ID（ID>0）并去重
                var ids = dtos.Where(d => d.ID > 0).Select(d => d.ID).ToList();
                if (!ids.Any())
                    return new ApiResponse("所有删除对象均无有效ID");

                // 执行批量删除操作
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Deleteable<IOCardDIOEntity>()
                    .In(ids) // 使用IN条件删除多个ID对应的记录
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                return rowsAffected > 0
                    ? new ApiResponse($"批量删除成功，共删除{rowsAffected}条记录", true)
                    : new ApiResponse("未找到需要删除的点位");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位批量删除已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }
        #endregion
    }
}