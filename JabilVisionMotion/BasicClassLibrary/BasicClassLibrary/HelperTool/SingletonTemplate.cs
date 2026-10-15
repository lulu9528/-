using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 单例模板类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingletonTemplate<T> where T : class, new()
    {
        private static T m_instance;
        private static readonly object m_lock;

        static SingletonTemplate()
        {
            SingletonTemplate<T>.m_lock = new object();
        }

        /// <summary>
        /// 获取单例类实例
        /// </summary>
        /// <returns></returns>
        public static T GetInstance()
        {
            if (SingletonTemplate<T>.m_instance == null)
            {
                object @lock = SingletonTemplate<T>.m_lock;
                lock (@lock)
                {
                    if (SingletonTemplate<T>.m_instance == null)
                    {
                        SingletonTemplate<T>.m_instance = Activator.CreateInstance<T>();
                    }
                }
            }
            return SingletonTemplate<T>.m_instance;
        }
    }
}
