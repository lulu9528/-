﻿using AutoMapper;
using Jabil.Module.Modbus.Entity;
using Jabil.Module.Modbus.Common.Dtos;

namespace Jabil.Module.Modbus.Common
{
    /// <summary>
    /// Modbus模块对象映射配置
    /// 负责实体与DTO之间的映射关系定义，确保数据传输的一致性和完整性
    /// 采用显式映射配置，提高代码可读性和可维护性
    /// </summary>
    public class ModbusAutoMapperProfile : Profile
    {
        /// <summary>
        /// 初始化映射配置
        /// 集中定义所有实体与DTO的映射规则，支持双向转换
        /// </summary>
        public ModbusAutoMapperProfile()
        {
            // 配置Modbus通信实体与DTO的双向映射
            CreateMap<ModbusCommunicationEntity, ModbusCommunicationDto>()
                .ForMember(dest => dest.设备名称, opt => opt.MapFrom(src => src.设备名称))
                .ForMember(dest => dest.设备类型, opt => opt.MapFrom(src => src.设备类型))
                .ForMember(dest => dest.站号, opt => opt.MapFrom(src => src.站号))
                .ForMember(dest => dest.设备IP, opt => opt.MapFrom(src => src.设备IP))
                .ForMember(dest => dest.设备Port, opt => opt.MapFrom(src => src.设备Port))
                .ForMember(dest => dest.超时, opt => opt.MapFrom(src => src.超时))
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
                .ReverseMap()
                // 反向映射时处理可能的类型转换和默认值
                .ForMember(src => src.设备Port, opt => opt.MapFrom(dest => dest.设备Port == 0 ? 502 : dest.设备Port))
                .ForMember(src => src.超时, opt => opt.MapFrom(dest => dest.超时 <= 0 ? 3000 : dest.超时));

            // 配置Modbus DIO实体与DTO的双向映射
            CreateMap<ModbusCommunicationDioEntity, ModbusCommunicationDioDto>()
                .ForMember(dest => dest.点位名称, opt => opt.MapFrom(src => src.点位名称))
                .ForMember(dest => dest.点位类型, opt => opt.MapFrom(src => src.点位类型))
                .ForMember(dest => dest.点位索引, opt => opt.MapFrom(src => src.点位索引))
                .ForMember(dest => dest.点位值, opt => opt.MapFrom(src => src.点位值))
                .ForMember(dest => dest.点位定义, opt => opt.MapFrom(src => src.点位定义))
                .ForMember(dest => dest.数据类型, opt => opt.MapFrom(src => src.数据类型))
                .ForMember(dest => dest.绑定代码, opt => opt.MapFrom(src => src.绑定代码))
                .ForMember(dest => dest.设备名称, opt => opt.MapFrom(src => src.设备名称))
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
                .ReverseMap()
                // 处理DTO中可能的空值映射
                .ForMember(src => src.点位定义, opt => opt.MapFrom(dest => string.IsNullOrWhiteSpace(dest.点位定义) ? string.Empty : dest.点位定义))
                .ForMember(src => src.绑定代码, opt => opt.MapFrom(dest => string.IsNullOrWhiteSpace(dest.绑定代码) ? string.Empty : dest.绑定代码));

            // 映射配置说明：
            // 1. 采用显式字段映射，避免因属性名变更导致的隐式映射失败
            // 2. 针对特殊字段（如端口号、超时时间）设置默认值，确保数据有效性
            // 3. 处理空字符串场景，避免数据库存储NULL值
            // 4. 保留双向映射(ReverseMap)支持，满足读写双向数据转换需求
        }
    }
}