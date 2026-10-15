using AutoMapper;
using Jabil.Module.Vision.Common.Dtos;
using Jabil.Module.Vision.Entity;
using System;

namespace Jabil.Module.Vision.Common
{
    /// <summary>
    /// AutoMapper映射配置文件
    /// 定义所有实体与DTO之间的映射关系
    /// 集中管理对象映射规则，确保转换逻辑的一致性
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 初始化映射配置
        /// 在构造函数中定义所有需要的对象映射关系
        /// </summary>
        public AutoMapperProfile()
        {
            // 视觉检测点位实体与DTO的双向映射
            // 包含所有属性的自动映射，名称相同的属性将自动匹配
            CreateMap<VisionInspectionEntity, VisionInspectionDto>()
                .ReverseMap();

            // 视觉工具实体与DTO的双向映射
            CreateMap<VisionToolEntity, VisionToolDto>()
                .ReverseMap();

            // 可在此处添加更多映射关系
            // CreateMap<SourceType, DestinationType>().ReverseMap();
        }
    }
}
