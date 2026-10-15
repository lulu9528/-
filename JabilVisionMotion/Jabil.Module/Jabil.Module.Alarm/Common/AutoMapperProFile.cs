using AutoMapper;
using Jabil.Module.Alarm.Common.Dtos;
using Jabil.Module.Alarm.Entity;

namespace Jabil.Module.Alarm.Common
{
    /// <summary>
    /// AutoMapper映射配置文件
    /// 集中管理所有实体与DTO之间的映射关系
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 初始化映射配置
        /// 在构造函数中定义所有实体与DTO的映射规则
        /// </summary>
        public AutoMapperProfile()
        {
            // 配置报警通信DIO实体与DTO的双向映射
            CreateMap<AlarmCommunicationDIOEntity, AlarmCommunicationDIODto>()
                .ReverseMap();

            // 配置过程错误实体与DTO的双向映射
            CreateMap<ProcessErrorEntity, ProcessErrorDto>()
                .ReverseMap();

            // 可在此处添加更多实体与DTO的映射配置
            // CreateMap<SourceType, TargetType>().ReverseMap();
        }
    }
}
