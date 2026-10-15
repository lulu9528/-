using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Lib.Parameter
{
    [Serializable]
    public class WeldParameters
    {
        public int 前焊接机压力 = 0;

        public int 后焊接机压力 = 0;

        public int 前焊接机振幅 = 0;

        public int 后焊接机振幅 = 0;
    }
}
