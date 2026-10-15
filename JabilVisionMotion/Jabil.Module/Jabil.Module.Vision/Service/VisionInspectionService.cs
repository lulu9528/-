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
    /// 视觉检测点位服务类
    /// 继承自泛型视觉服务类，专注于处理检测点位的数据库CRUD操作
    /// 负责检测点位数据的持久化和检索，协调DTO与实体对象的转换
    /// </summary>
    public class VisionInspectionService : GenericVisionService<VisionInspectionEntity, VisionInspectionDto>
    {
        /// <summary>
        /// 添加或更新检测点位
        /// 当检测点位不存在时执行插入操作，存在时执行更新操作
        /// </summary>
        /// <param name="dto">检测点位数据传输对象，包含待添加或更新的点位信息</param>
        /// <returns>ApiResponse对象，包含操作结果状态和消息</returns>
        public override async Task<ApiResponse> AddOrUpdateAsync(VisionInspectionDto dto)
        {
            try
            {
                // 使用AutoMapper将DTO转换为实体对象
                var entity = _mapper.Map<VisionInspectionEntity>(dto);

                // 检查数据库中是否已存在相同的检测点位
                // 判断条件：(类型、名称、定义相同) 或 索引相同，满足任一条件即视为同一点位
                var existingEntity = await SugarSQLHelper.Instance.DB
                    .Queryable<VisionInspectionEntity>()
                    .SingleAsync(u => (u.检测点位类型 == dto.检测点位类型 &&
                                      u.检测点位名称 == dto.检测点位名称 &&
                                      u.检测点位定义 == dto.检测点位定义) ||
                                     u.检测点位索引 == dto.检测点位索引);

                // 根据是否存在现有实体执行插入或更新操作
                int result = existingEntity == null
                    ? await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync()  // 插入新点位
                    : await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync(); // 更新现有点位

                // 根据操作结果返回相应的响应信息
                return result > 0
                    ? new ApiResponse(existingEntity == null ? "添加检测点位成功" : "更新检测点位成功", true)
                    : new ApiResponse(existingEntity == null ? "添加检测点位失败" : "更新检测点位失败");
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
        /// 删除指定的检测点位
        /// 根据点位的类型、名称和定义组合条件进行删除
        /// </summary>
        /// <param name="dto">包含待删除点位信息的数据传输对象</param>
        /// <returns>ApiResponse对象，包含删除操作结果状态和消息</returns>
        public override async Task<ApiResponse> DeleteAsync(VisionInspectionDto dto)
        {
            try
            {
                // 将DTO转换为实体对象，用于提取删除条件
                var entity = _mapper.Map<VisionInspectionEntity>(dto);

                // 执行删除操作，使用类型、名称和定义作为联合删除条件
                var result = await SugarSQLHelper.Instance.DB
                    .Deleteable<VisionInspectionEntity>()
                    .Where(u => u.检测点位类型 == entity.检测点位类型 &&
                               u.检测点位名称 == entity.检测点位名称 &&
                               u.检测点位定义 == entity.检测点位定义)
                    .ExecuteCommandAsync();

                // 根据删除结果返回相应的响应信息
                return result > 0
                    ? new ApiResponse($"删除成功: {dto.检测点位名称}", true)
                    : new ApiResponse($"删除失败: 点位 {dto.检测点位名称} 不存在");
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
