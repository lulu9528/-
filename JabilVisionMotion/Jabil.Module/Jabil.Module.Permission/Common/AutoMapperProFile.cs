using AutoMapper.Configuration;
using Jabil.Module.Permission.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// AutoMapper配置文件
    /// 继承自MapperConfigurationExpression，用于定义实体与DTO之间的映射规则
    /// 集中管理对象映射关系，便于维护和扩展
    /// </summary>
    public class AutoMapperProFile : MapperConfigurationExpression
    {
        /// <summary>
        /// 构造函数
        /// 在初始化时定义所有实体与DTO的映射规则
        /// </summary>
        public AutoMapperProFile()
        {
            // 配置UserEntity与UserDto之间的双向映射
            // ReverseMap()表示同时创建UserDto到UserEntity的反向映射
            CreateMap<UserEntity, UserDto>().ReverseMap();

            // 以下为注释掉的示例：展示如何自定义映射规则
            // 示例中尝试将PermissionKey从字符串反序列化为List<int>
            // CreateMap<UserEntity, UserDto>()
            //     .ForMember(dest => dest.PermissionKey, 
            //         opt => opt.MapFrom(src => JsonConvert.DeserializeObject<List<int>>(src.PermissionKey) ?? new List<int>()));
            // CreateMap<UserEntity, UserDto>().ReverseMap();

            // 配置RoleEntity与RoleDto之间的双向映射
            CreateMap<RoleEntity, RoleDto>().ReverseMap();

            // 配置PermissionEntity与PermissionDto之间的双向映射
            CreateMap<PermissionEntity, PermissionDto>().ReverseMap();
        }
    }
}