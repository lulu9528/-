using AutoMapper;
using Jabil.Module.TCPIP.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.TCPIP.Service
{    
    /// <summary>
     /// AutoMapper帮助类，提供单例的映射器实例
     /// </summary>
    public class AutoMapperHelper
    {
        private static readonly Lazy<AutoMapperHelper> _instance = new Lazy<AutoMapperHelper>(() => new AutoMapperHelper(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static AutoMapperHelper Instance => _instance.Value;

        public IMapper Mapper { get; }

        private AutoMapperHelper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            });
            Mapper = config.CreateMapper();
        }
    }

}
