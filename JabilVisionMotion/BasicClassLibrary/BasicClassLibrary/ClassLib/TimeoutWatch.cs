using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class DateTimeUserToken
    {
        DateTime startTime;
        bool isWatching = false;

        public void DataTimeUserToken()
        {
            startTime = DateTime.MinValue;
        }

        /// <summary>
        /// 开始计时
        /// </summary>
        public void StartWatch()
        {
            if (isWatching == false)
            {
                startTime = DateTime.Now;
                isWatching = true;
            }
        }

        /// <summary>
        /// 停止计时
        /// </summary>
        public void StopWatch()
        {
            if (isWatching)
            {
                isWatching = false;
                startTime = DateTime.MinValue;
            }
        }

        /// <summary>
        /// 返回对应名称到当前时间的时间间隔，单位毫秒
        /// </summary>
        /// <returns>从开始计时到当期时间的毫秒数</returns>
        public double GetTimeSpan()
        {
            if (isWatching)
            {
                TimeSpan ts = DateTime.Now - startTime;
                return ts.TotalMilliseconds;
            }
            else
            {
                return -1;
            }
        }

        public void Retry()
        {
            StopWatch();
        }
    }

    public class TimeoutWatch : IEnumerable
    {
        Dictionary<string, DateTimeUserToken> timeDictionary = new Dictionary<string, DateTimeUserToken>(20);

        /// <summary>
        /// 判断是否存在指定名称的计时器
        /// </summary>
        /// <param name="name">计时器名称</param>
        /// <returns></returns>
        public bool IsExit(string name)
        {
            if (timeDictionary.ContainsKey(name))
                return true;
            else
                return false;
        }

        /// <summary>
        /// 启动指定的计时器
        /// </summary>
        /// <param name="name">计时器名称</param>
        public void StartWatch(string name)
        {
            if (timeDictionary.ContainsKey(name))
            {
                timeDictionary[name].StartWatch();
            }
            else
            {
                DateTimeUserToken t = new DateTimeUserToken();
                t.StartWatch();
                timeDictionary.Add(name, t);
            }
        }

        /// <summary>
        /// 停止指定的计时器
        /// </summary>
        /// <param name="name">计时器名称</param>
        public void StopWatch(string name)
        {
            if (timeDictionary.ContainsKey(name))
            {
                timeDictionary[name].StopWatch();
            }
        }

        /// <summary>
        /// 返回对应名称到当前时间的时间间隔，单位毫秒
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public double GetTimeSpan(string name)
        {
            if (!timeDictionary.ContainsKey(name))
            {
                return -1;
            }
            return timeDictionary[name].GetTimeSpan();
        }

        /// <summary>
        /// 启动计时器并检测是否超时
        /// </summary>
        /// <param name="watcherName">计时器名称</param>
        /// <param name="milliseconds">超时时间</param>
        /// <param name="bAlarm">是否报警</param>
        /// <returns></returns>
        public bool StartCheckIsTimeout(string watcherName, int milliseconds)
        {
            StartWatch(watcherName);
            if (GetTimeSpan(watcherName) > milliseconds)
            {
                StopWatch(watcherName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 停止所有的计时器
        /// </summary>
        public void StopAllWatch()
        {
            foreach (DateTimeUserToken timer in timeDictionary.Values)
            {
                timer.StopWatch();
            }
        }

        public IEnumerator GetEnumerator()
        {
            return timeDictionary.GetEnumerator();
        }
    }
}
