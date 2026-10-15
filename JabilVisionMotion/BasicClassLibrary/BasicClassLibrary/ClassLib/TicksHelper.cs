using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class TicksHelper
    {
        /// <summary>
        /// 查询系统计数器从开机到当前的计数值
        /// </summary>
        /// <param name="lpPerformanceCount"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpCount);

        /// <summary>
        /// 查询系统计数器的计数频率
        /// </summary>
        /// <param name="lpPerformanceCount"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFreq);

        /// <summary>
        /// 获取基于系统计数器的Ticks值
        /// </summary>
        public static long SystemTicks
        {
            get
            {
                long count = 0, lpFreq;
                QueryPerformanceCounter(out count);
                QueryPerformanceFrequency(out lpFreq);
                return (long)(count * 1000000.0 / lpFreq);
            }
        }
    }
}
