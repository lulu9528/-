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
    /// 轴卡服务
    /// </summary>
    public class MotionCardService : MotionServiceBase<MotionCardEntity, MotionCardDto>
    {
        protected override async Task<bool> ExistsAsync(MotionCardDto dto)
        {
            return await SugarSQLHelper.Instance.DB
                .Queryable<MotionCardEntity>()
                .AnyAsync(x => x.轴卡名称 == dto.轴卡名称);
        }

        protected override async Task<MotionCardEntity> GetExistingEntityAsync(MotionCardDto dto)
        {
            return await SugarSQLHelper.Instance.DB
                .Queryable<MotionCardEntity>()
                .SingleAsync(x => x.轴卡名称 == dto.轴卡名称);
        }

        protected override string GetEntityName() => "轴卡";
    }
}
