using AutoMapper;
using Jabil.Module.TCPIP.Common.Dtos;
using Jabil.Module.TCPIP.Entity;

namespace Jabil.Module.TCPIP.Common
{
    /// <summary>
    /// AutoMapper配置文件，定义实体与DTO之间的映射关系
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 初始化映射配置
        /// </summary>
        public AutoMapperProfile()
        {
            // TCPIP通讯配置实体与DTO的双向映射
            CreateMap<TcpIpCommunicationEntity, TcpIpCommunicationDto>()
                .ReverseMap();

            // TCPIP通讯DIO配置实体与DTO的双向映射
            CreateMap<TcpIpCommunicationDioEntity, TcpIpCommunicationDioDto>()
                .ReverseMap();

            // 可以在这里添加更多映射关系
            // CreateMap<SourceType, DestinationType>().ReverseMap();
        }
    }
}
