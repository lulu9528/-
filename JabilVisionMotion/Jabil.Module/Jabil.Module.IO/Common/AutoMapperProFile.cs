using AutoMapper;
using Jabil.Module.IO.Common.Dtos;
using Jabil.Module.IO.Entity;

namespace Jabil.Module.IO.Common
{
    /// <summary>
    /// IO模块AutoMapper映射配置类
    /// 继承自AutoMapper的Profile类，集中管理IO模块中实体(Entity)与数据传输对象(DTO)的映射关系
    /// 作用：
    /// 1. 简化实体与DTO之间的转换逻辑，避免重复的手动赋值代码
    /// 2. 统一维护映射规则，提高代码可维护性
    /// 3. 支持双向映射，满足数据读取(Entity→DTO)和数据持久化(DTO→Entity)需求
    /// </summary>
    public class IoAutoMapperProfile : Profile
    {
        /// <summary>
        /// 初始化映射配置
        /// 在构造函数中定义所有需要的映射关系，AutoMapper会在初始化时加载这些配置
        /// </summary>
        public IoAutoMapperProfile()
        {
            // 配置IO卡实体与DTO的双向映射
            // IOCardEntity（数据库实体） ←→ IOCardDto（传输对象）
            // ReverseMap()：自动生成反向映射，无需单独定义IOCardDto→IOCardEntity
            CreateMap<IOCardEntity, IOCardDto>()
                .ReverseMap();

            // 配置IO卡点位实体与DTO的双向映射
            // IOCardDIOEntity（点位数据库实体） ←→ IOCardDIODto（点位传输对象）
            CreateMap<IOCardDIOEntity, IOCardDIODto>()
                .ReverseMap();

            // 扩展说明：如需添加其他实体与DTO的映射，按以下格式配置
            // 示例：CreateMap<SourceEntity, TargetDto>().ReverseMap();
            // 若需要特殊映射规则（如字段名不同），可使用ForMember配置
            // 示例：
            // CreateMap<Entity, Dto>()
            //     .ForMember(dest => dest.DtoField, opt => opt.MapFrom(src => src.EntityField))
            //     .ReverseMap();
        }
    }
}