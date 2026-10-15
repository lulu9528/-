using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Log.Lib;
using Jabil.Module.Vision.Common.Dtos;
using Jabil.Module.Vision.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Vision.Service
{
    /// <summary>
    /// 视觉工具服务类
    /// 继承自通用视觉服务泛型基类，专注于处理视觉工具的数据库CRUD操作
    /// 负责视觉工具数据的持久化和检索，实现工具的添加、更新和删除功能
    /// </summary>
    public class VisionToolService : GenericVisionService<VisionToolEntity, VisionToolDto>
    {
        /// <summary>
        /// 添加或更新视觉工具
        /// 根据工具类型和索引判断是否存在，存在则更新，不存在则添加
        /// </summary>
        /// <param name="dto">视觉工具数据传输对象，包含工具的属性信息</param>
        /// <returns>ApiResponse对象，包含操作结果状态和消息</returns>
        public override async Task<ApiResponse> AddOrUpdateAsync(VisionToolDto dto)
        {
            try
            {
                // 使用AutoMapper将DTO转换为实体对象
                var entity = _mapper.Map<VisionToolEntity>(dto);

                // 检查数据库中是否已存在相同类型和索引的工具
                // 工具唯一性由"检测工具类型+检测工具索引"组合确定
                var existingEntity = await SugarSQLHelper.Instance.DB
                    .Queryable<VisionToolEntity>()
                    .SingleAsync(u => u.检测工具类型 == dto.检测工具类型 &&
                                     u.检测工具索引 == dto.检测工具索引);

                // 根据是否存在现有实体执行插入或更新操作
                int result = existingEntity == null
                    ? await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync()  // 插入新工具
                    : await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync(); // 更新现有工具

                // 根据操作结果返回相应的响应信息
                return result > 0
                    ? new ApiResponse(existingEntity == null ? "添加视觉工具成功" : "更新视觉工具成功", true)
                    : new ApiResponse(existingEntity == null ? "添加视觉工具失败" : "更新视觉工具失败");
            }
            catch (Exception ex)
            {
                // 记录异常信息到日志系统
                RLog.SaveError(ex);
                // 返回包含错误信息的响应
                return new ApiResponse($"操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除指定的视觉工具
        /// 根据工具名称执行删除操作
        /// </summary>
        /// <param name="dto">包含待删除工具信息的数据传输对象</param>
        /// <returns>ApiResponse对象，包含删除操作结果状态和消息</returns>
        public override async Task<ApiResponse> DeleteAsync(VisionToolDto dto)
        {
            try
            {
                // 将DTO转换为实体对象，用于提取删除条件
                var entity = _mapper.Map<VisionToolEntity>(dto);

                // 执行删除操作，使用工具名称作为删除条件
                var result = await SugarSQLHelper.Instance.DB
                    .Deleteable<VisionToolEntity>()
                    .Where(u => u.检测工具名称 == entity.检测工具名称)
                    .ExecuteCommandAsync();

                // 根据删除结果返回相应的响应信息
                return result > 0
                    ? new ApiResponse($"删除成功: {dto.检测工具名称}", true)
                    : new ApiResponse($"删除失败: 工具 {dto.检测工具名称} 不存在");
            }
            catch (Exception ex)
            {
                // 记录异常信息到日志系统
                RLog.SaveError(ex);
                // 返回包含错误信息的响应
                return new ApiResponse($"删除失败: {ex.Message}");
            }
        }
    }
}