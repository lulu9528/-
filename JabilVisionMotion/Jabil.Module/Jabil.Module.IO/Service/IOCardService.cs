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
    /// IO卡服务类
    /// 核心职责：封装IO卡的所有数据访问操作（CRUD），作为业务逻辑与数据库之间的中间层
    /// 设计特点：
    /// - 继承IOServiceBase，自动获取AutoMapper实例，简化实体与DTO的转换
    /// - 所有方法支持异步操作，避免阻塞UI线程
    /// - 支持取消令牌（CancellationToken），允许操作中途终止
    /// - 统一的异常处理和日志记录，确保上层调用的稳定性
    /// </summary>
    public class IOCardService : IOServiceBase
    {
        #region 查询操作
        /// <summary>
        /// 获取系统中所有IO卡的信息
        /// 应用场景：初始化加载、刷新IO卡列表、验证卡是否存在等
        /// </summary>
        /// <param name="cancellationToken">取消令牌，可在操作执行中终止任务（如用户取消刷新）</param>
        /// <returns>ApiResponse对象，包含操作状态和IO卡DTO列表</returns>
        internal async Task<ApiResponse<List<IOCardDto>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. 从数据库异步查询所有IO卡实体
                // 使用ToListAsync(cancellationToken)：
                // - 支持异步操作，不阻塞当前线程
                // - 接受取消令牌，可在查询过程中终止
                // ConfigureAwait(false)：避免上下文切换，提升非UI线程操作性能
                var entities = await SugarSQLHelper.Instance.DB
                    .Queryable<IOCardEntity>() // 查询IO卡实体表
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                // 2. 将实体列表映射为DTO列表
                // 映射目的：
                // - 隔离数据库实体与前端数据模型，防止数据库结构变更影响上层
                // - DTO仅包含必要字段，减少数据传输量
                var dtos = _mapper.Map<List<IOCardDto>>(entities);

                // 3. 返回成功响应，包含映射后的DTO列表
                return new ApiResponse<List<IOCardDto>>(null, true, dtos);
            }
            catch (OperationCanceledException)
            {
                // 处理用户主动取消的情况（非错误）
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡查询已取消");
                return new ApiResponse<List<IOCardDto>>("查询已取消");
            }
            catch (Exception ex)
            {
                // 记录异常详情（包含堆栈信息），便于问题排查
                RLog.SaveError(ex);
                // 返回失败响应，包含用户可理解的错误信息
                return new ApiResponse<List<IOCardDto>>($"操作失败：{ex.Message}");
            }
        }
        #endregion

        #region 新增与更新操作
        /// <summary>
        /// 新增IO卡
        /// 业务约束：同一卡名称+卡号索引的组合必须唯一（避免硬件冲突）
        /// </summary>
        /// <param name="dto">包含新增IO卡信息的DTO（如卡名称、类型、索引等）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>ApiResponse对象，包含操作结果和提示信息</returns>
        internal async Task<ApiResponse> AddAsync(IOCardDto dto, CancellationToken cancellationToken = default)
        {
            // 参数校验：防止空对象传入，避免后续映射和数据库操作异常
            if (dto == null)
                return new ApiResponse("IO卡信息不能为空");

            try
            {
                // 检查唯一性约束：卡名称+卡号索引组合不能重复
                // 使用AnyAsync：直接在数据库层执行判断，无需加载全部数据到内存，性能更优
                bool isExist = await SugarSQLHelper.Instance.DB
                    .Queryable<IOCardEntity>()
                    .AnyAsync(
                        // 条件：卡名称和卡号索引均匹配
                        e => e.卡名称 == dto.卡名称 && e.卡号索引 == dto.卡号索引,
                        cancellationToken // 传递取消令牌
                    )
                    .ConfigureAwait(false);

                // 若已存在相同记录，返回重复提示
                if (isExist)
                    return new ApiResponse("已存在相同卡名称和卡号索引的记录");

                // 将DTO映射为数据库实体（准备写入数据库）
                var entity = _mapper.Map<IOCardEntity>(dto);

                // 执行新增操作，返回受影响的行数（1表示成功，0表示失败）
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Insertable(entity) // 插入实体
                    .ExecuteCommandAsync(cancellationToken) // 异步执行并支持取消
                    .ConfigureAwait(false);

                // 根据受影响行数返回成功/失败响应
                return rowsAffected > 0
                    ? new ApiResponse("IO卡新增成功", true)
                    : new ApiResponse("IO卡新增失败");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡新增已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 新增或更新IO卡（推荐使用）
        /// 自动判断操作类型：
        /// - 当DTO的ID≤0时：执行新增操作
        /// - 当DTO的ID>0时：执行更新操作
        /// 优势：统一新增和更新的调用入口，简化上层代码
        /// </summary>
        /// <param name="dto">IO卡信息DTO</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>ApiResponse对象，包含操作结果</returns>
        internal async Task<ApiResponse> AddOrUpdateAsync(IOCardDto dto, CancellationToken cancellationToken = default)
        {
            // 参数验证：防止空对象传入
            if (dto == null)
                return new ApiResponse("IO卡信息不能为空");

            try
            {
                // 将DTO映射为实体（统一操作实体对象，便于后续数据库操作）
                var entity = _mapper.Map<IOCardEntity>(dto);

                // 新增逻辑：ID≤0表示新记录（未在数据库中存在）
                if (entity.ID <= 0)
                {
                    // 检查唯一性约束（卡名称+卡号索引）
                    bool isExist = await SugarSQLHelper.Instance.DB
                        .Queryable<IOCardEntity>()
                        .AnyAsync(
                            e => e.卡名称 == entity.卡名称 && e.卡号索引 == entity.卡号索引,
                            cancellationToken
                        )
                        .ConfigureAwait(false);

                    if (isExist)
                        return new ApiResponse("已存在相同卡名称和卡号索引的记录");

                    // 执行新增操作
                    int rowsAffected = await SugarSQLHelper.Instance.DB
                        .Insertable(entity)
                        .ExecuteCommandAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return rowsAffected > 0
                        ? new ApiResponse("IO卡新增成功", true)
                        : new ApiResponse("IO卡新增失败");
                }
                // 更新逻辑：ID>0表示已有记录（需修改现有数据）
                else
                {
                    // 带条件更新：仅更新ID匹配的记录
                    // 安全机制：防止因DTO数据错误导致的批量更新风险
                    int rowsAffected = await SugarSQLHelper.Instance.DB
                        .Updateable(entity) // 待更新的实体
                        .Where(e => e.ID == entity.ID) // 过滤条件：仅更新当前ID的记录
                        .ExecuteCommandAsync(cancellationToken)
                        .ConfigureAwait(false);

                    return rowsAffected > 0
                        ? new ApiResponse("IO卡更新成功", true)
                        : new ApiResponse("未找到需要更新的IO卡或无变更");
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡新增/更新已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新IO卡
        /// 应用场景：一次性修改多条IO卡信息（如批量修改卡描述、所属设备等）
        /// </summary>
        /// <param name="dtos">IO卡DTO集合（需包含有效ID，否则会被过滤）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>ApiResponse对象，包含操作结果</returns>
        internal async Task<ApiResponse> UpdateAsync(IEnumerable<IOCardDto> dtos, CancellationToken cancellationToken = default)
        {
            // 参数验证：确保有有效数据
            if (dtos == null || !dtos.Any())
                return new ApiResponse("无有效更新对象");

            try
            {
                // 1. 将DTO集合映射为实体集合
                // 2. 过滤无效记录：仅保留ID>0的实体（确保更新的是数据库中已存在的记录）
                var entities = _mapper.Map<List<IOCardEntity>>(dtos)
                                     .Where(e => e.ID > 0) // 过滤无有效ID的记录
                                     .ToList();

                // 若过滤后无有效实体，返回错误提示
                if (!entities.Any())
                    return new ApiResponse("所有更新对象均无有效ID");

                // 执行批量更新操作
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Updateable(entities) // 批量更新的实体列表
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                // 根据受影响行数返回结果
                return rowsAffected > 0
                    ? new ApiResponse($"批量更新成功，影响{rowsAffected}条记录", true)
                    : new ApiResponse("批量更新失败或无变更");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡批量更新已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 单条更新IO卡
        /// 与AddOrUpdateAsync的区别：此方法强制要求DTO包含有效ID（仅用于更新）
        /// </summary>
        /// <param name="dto">IO卡DTO（必须包含有效ID，否则更新失败）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>ApiResponse对象，包含操作结果</returns>
        internal async Task<ApiResponse> UpdateAsync(IOCardDto dto, CancellationToken cancellationToken = default)
        {
            // 参数验证：确保对象非空且ID有效（ID是定位数据库记录的唯一标识）
            if (dto == null || dto.ID <= 0)
                return new ApiResponse("更新对象必须包含有效ID");

            try
            {
                // 将DTO映射为实体
                var entity = _mapper.Map<IOCardEntity>(dto);

                // 带条件更新：仅更新ID匹配的记录（确保操作安全性）
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Updateable(entity)
                    .Where(e => e.ID == entity.ID) // 精准定位要更新的记录
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                return rowsAffected > 0
                    ? new ApiResponse("IO卡更新成功", true)
                    : new ApiResponse("未找到需要更新的IO卡或无变更");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡更新已取消");
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
        /// 单条删除IO卡
        /// 应用场景：移除单个不再使用的IO卡配置
        /// </summary>
        /// <param name="dto">IO卡DTO（必须包含有效ID，用于定位要删除的记录）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>ApiResponse对象，包含操作结果</returns>
        internal async Task<ApiResponse> DeleteAsync(IOCardDto dto, CancellationToken cancellationToken = default)
        {
            // 参数验证：确保删除对象有效
            if (dto == null || dto.ID <= 0)
                return new ApiResponse("删除对象必须包含有效ID");

            try
            {
                // 根据ID删除记录：主键删除效率最高，且能精准定位
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Deleteable<IOCardEntity>() // 要删除的实体类型
                    .Where(e => e.ID == dto.ID) // 条件：仅删除指定ID的记录
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                return rowsAffected > 0
                    ? new ApiResponse($"IO卡（ID：{dto.ID}）删除成功", true)
                    : new ApiResponse("未找到需要删除的IO卡");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡删除已取消");
                return new ApiResponse("操作已取消");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量删除IO卡
        /// 应用场景：设备升级、系统重构等需要一次性移除多个IO卡的场景
        /// </summary>
        /// <param name="dtos">IO卡DTO集合（需包含有效ID）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>ApiResponse对象，包含操作结果</returns>
        internal async Task<ApiResponse> DeleteAsync(IEnumerable<IOCardDto> dtos, CancellationToken cancellationToken = default)
        {
            // 参数验证：确保有需要删除的对象
            if (dtos == null || !dtos.Any())
                return new ApiResponse("请选择需要删除的IO卡");

            try
            {
                // 提取有效ID：过滤无ID或ID无效的记录，避免删除错误
                var ids = dtos.Where(d => d.ID > 0) // 仅保留ID有效（>0）的记录
                              .Select(d => d.ID)   // 提取ID列表
                              .ToList();

                // 若没有有效ID，返回错误提示
                if (!ids.Any())
                    return new ApiResponse("所有删除对象均无有效ID");

                // 批量删除：通过In方法高效处理多个ID的删除
                int rowsAffected = await SugarSQLHelper.Instance.DB
                    .Deleteable<IOCardEntity>()
                    .In(ids) // 根据ID集合删除
                    .ExecuteCommandAsync(cancellationToken)
                    .ConfigureAwait(false);

                return rowsAffected > 0
                    ? new ApiResponse($"批量删除成功，共删除{rowsAffected}条记录", true)
                    : new ApiResponse("未找到需要删除的IO卡");
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡批量删除已取消");
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
