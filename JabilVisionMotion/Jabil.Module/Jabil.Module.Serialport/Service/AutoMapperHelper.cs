using AutoMapper;
using Jabil.Module.Serialport.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.Serialport.Service
{    /// <summary>
     /// AutoMapper帮助类，负责对象映射配置
     /// </summary>
    public class AutoMapperHelper
    {
        private static readonly Lazy<AutoMapperHelper> _instance = new Lazy<AutoMapperHelper>(
            () => new AutoMapperHelper(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取AutoMapper帮助类的唯一实例
        /// </summary>
        public static AutoMapperHelper Instance => _instance.Value;

        /// <summary>
        /// 映射器实例
        /// </summary>
        public IMapper Mapper { get; }

        /// <summary>
        /// 初始化AutoMapper配置
        /// </summary>
        private AutoMapperHelper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            });

            // 验证映射配置
            config.AssertConfigurationIsValid();

            Mapper = config.CreateMapper();
        }
    }

}
