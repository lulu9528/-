using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 寄存器帮助类 ，用于共享内存间的访问
    /// </summary>
    public class RegistorHelper : SingletonTemplate<RegistorHelper>
    {
        private ConcurrentDictionary<int, bool> m_dictRegBit = new ConcurrentDictionary<int, bool>();
        private ConcurrentDictionary<int, double> m_dictRegDouble = new ConcurrentDictionary<int, double>();
        private ConcurrentDictionary<int, int> m_dictRegInt = new ConcurrentDictionary<int, int>();
        private ConcurrentDictionary<int, string> m_dictRegString = new ConcurrentDictionary<int, string>();

        /// <summary>
        /// BOOL型寄存器改变事件
        /// </summary>

        public event SystemRegChangedHandler<bool> RegBitChangedEvent;

        /// <summary>
        /// DOUBLE型寄存器改变事件
        /// </summary>

        public event SystemRegChangedHandler<double> RegDoubleChangedEvent;

        /// <summary>
        /// INT型寄存器改变事件
        /// </summary>

        public event SystemRegChangedHandler<int> RegIntChangedEvent;

        /// <summary>
        /// 字符串型寄存器改变事件
        /// </summary>

        public event SystemRegChangedHandler<string> RegStringChangedEvent;

        /// <summary>
        /// 获取系统寄存器的值
        /// </summary>
        /// <param name="index">系统寄存器</param>
        /// <returns></returns>
        public bool GetRegBit(int index)
        {
            return this.m_dictRegBit.GetOrAdd(index, false);
        }

        /// <summary>
        /// 获取系统寄存器的值
        /// </summary>
        /// <param name="index">系统寄存器</param>
        /// <returns></returns>
        public double GetRegDouble(int index)
        {
            return this.m_dictRegDouble.GetOrAdd(index, 0.0);
        }

        /// <summary>
        /// 获取系统寄存器的值
        /// </summary>
        /// <param name="index">系统寄存器</param>
        /// <returns></returns>
        public int GetRegInt(int index)
        {
            return this.m_dictRegInt.GetOrAdd(index, 0);
        }

        /// <summary>
        /// 获取系统寄存器的值
        /// </summary>
        /// <param name="index">系统寄存器</param>
        /// <returns></returns>
        public string GetRegString(int index)
        {
            return this.m_dictRegString.GetOrAdd(index, "");
        }

        /// <summary>
        /// 设置系统寄存器的值
        /// </summary>
        /// <param name="index">系统寄存器</param>
        /// <param name="value">值</param>
        /// <param name="bNotify">是否发事件通知</param>
        public void SetRegBit(int index, bool value, bool bNotify = false)
        {
            bool regBit = value;
            if (bNotify && (this.RegBitChangedEvent != null))
            {
                regBit = this.GetRegBit(index);
            }
            this.m_dictRegBit.AddOrUpdate(index, value, (oldKey, oldValue) => value);
            if ((bNotify && (this.RegBitChangedEvent != null)) && (regBit != value))
            {
                this.RegBitChangedEvent(index, regBit, value);
            }
        }

        /// <summary>
        /// 设置系统寄存器的值
        /// </summary>
        /// <param name="index">系统寄存器</param>
        /// <param name="value">值</param>
        /// <param name="bNotify">是否发事件通知</param>
        public void SetRegDouble(int index, double value, bool bNotify = false)
        {
            double regDouble = value;
            if (bNotify && (this.RegDoubleChangedEvent != null))
            {
                regDouble = this.GetRegDouble(index);
            }
            this.m_dictRegDouble.AddOrUpdate(index, value, (oldKey, oldValue) => value);
            if ((bNotify && (this.RegDoubleChangedEvent != null)) && !(regDouble == value))
            {
                this.RegDoubleChangedEvent(index, regDouble, value);
            }
        }

        /// <summary>
        /// 设置系统寄存器的值
        /// </summary>
        /// <param name="index">系统寄存器</param>
        /// <param name="value">值</param>
        /// <param name="bNotify">是否发事件通知</param>
        public void SetRegInt(int index, int value, bool bNotify = false)
        {
            int regInt = value;
            if (bNotify && (this.RegIntChangedEvent != null))
            {
                regInt = this.GetRegInt(index);
            }
            this.m_dictRegInt.AddOrUpdate(index, value, (oldKey, oldValue) => value);
            if ((bNotify && (this.RegIntChangedEvent != null)) && (regInt != value))
            {
                this.RegIntChangedEvent(index, regInt, value);
            }
        }

        /// <summary>
        /// 设置系统寄存器的值
        /// </summary>
        /// <param name="index">系统寄存器</param>
        /// <param name="value">值</param>
        /// <param name="bNotify">是否发事件通知</param>
        public void SetRegString(int index, string value, bool bNotify = false)
        {
            string regString = value;
            if (bNotify && (this.RegStringChangedEvent != null))
            {
                regString = this.GetRegString(index);
            }
            this.m_dictRegString.AddOrUpdate(index, value, (oldKey, oldValue) => value);
            if ((bNotify && (this.RegStringChangedEvent != null)) && (regString != value))
            {
                this.RegStringChangedEvent(index, regString, value);
            }
        }

        /// <summary>
        /// 系统寄存器委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public delegate void SystemRegChangedHandler<T>(int index, T oldValue, T newValue);
    }
}
