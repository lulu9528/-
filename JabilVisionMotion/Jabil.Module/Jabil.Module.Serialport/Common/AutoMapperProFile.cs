using AutoMapper;
using Jabil.Module.Serialport.Entities;
using Jabil.Module.Serialport.Common.Dtos;

namespace Jabil.Module.Serialport.Common
{
    /// <summary>
    /// AutoMapper对象映射配置文件
    /// 负责配置实体与DTO之间的映射关系
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 初始化映射配置
        /// </summary>
        public AutoMapperProfile()
        {
            // 配置Serialport实体与DTO的双向映射
            CreateMap<Entities.Serialport, SerialportDto>()
                .ReverseMap();

            // 配置SerialportDIO实体与DTO的双向映射
            CreateMap<SerialportDIO, SerialportDIODto>()
                .ReverseMap();

            // 可以在这里添加更多映射配置
        }
    }
}
