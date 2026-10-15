using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Motion.Service
{
    /// <summary>
    /// 运动点位服务
    /// </summary>
    public class MotionPointService : MotionServiceBase<MotionPointEntity, MotionPointDto>
    {
        protected override async Task<bool> ExistsAsync(MotionPointDto dto)
        {
            return await SugarSQLHelper.Instance.DB
                .Queryable<MotionPointEntity>()
                .AnyAsync(x =>
                    x.点位类型 == dto.点位类型 &&
                    x.点位定义 == dto.点位定义 &&
                    x.点位名称 == dto.点位名称);
        }

        protected override async Task<MotionPointEntity> GetExistingEntityAsync(MotionPointDto dto)
        {
            return await SugarSQLHelper.Instance.DB
                .Queryable<MotionPointEntity>()
                .SingleAsync(x =>
                    x.点位类型 == dto.点位类型 &&
                    x.点位定义 == dto.点位定义 &&
                    x.点位名称 == dto.点位名称);
        }

        protected override string GetEntityName() => "运动点位";
    }
}
