using AutoMapper;
using Jabil.Module.Motion.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Motion.Service
{
     public class AutoMapperHelper
    {
        private static AutoMapperHelper _instance;
        public static AutoMapperHelper Instance => _instance ?? new AutoMapperHelper();
        private IMapper _mapper;
        public IMapper Mapper { get=>_mapper; }

        private AutoMapperHelper()
        {
            var automapperConfog = new MapperConfiguration(config =>
            {
                config.AddProfile(new AutoMapperProfile());
            });
            _mapper = automapperConfog.CreateMapper();

        }
    }
}
