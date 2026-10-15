using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Serialport.Service
{    /// <summary>
     /// 串口服务基类，提供通用服务功能
     /// </summary>
    public abstract class SerialportServiceBase
    {
        /// <summary>
        /// 对象映射器
        /// </summary>
        protected readonly IMapper _mapper;

        /// <summary>
        /// 初始化串口服务基类
        /// </summary>
        protected SerialportServiceBase()
        {
            _mapper = AutoMapperHelper.Instance.Mapper;
        }
    }

}
