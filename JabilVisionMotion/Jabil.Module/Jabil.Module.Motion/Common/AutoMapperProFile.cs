using AutoMapper;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Entity;

namespace Jabil.Module.Motion.Common
{
    /// <summary>
    /// Motion模块对象映射配置类
    /// 负责配置实体(Entity)与数据传输对象(DTO)之间的映射关系
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 初始化映射配置
        /// </summary>
        public AutoMapperProfile()
        {
            // 配置MotionAxis相关映射（双向映射）
            CreateMap<MotionAxisEntity, MotionAxisDto>()
                .ReverseMap();

            // 配置MotionCard相关映射（双向映射）
            CreateMap<MotionCardEntity, MotionCardDto>()
                .ReverseMap();

            // 配置MotionPoint相关映射（双向映射）
            CreateMap<MotionPointEntity, MotionPointDto>()
                .ReverseMap();

            // 可在此处添加更多模块的映射配置
        }
    }
}
