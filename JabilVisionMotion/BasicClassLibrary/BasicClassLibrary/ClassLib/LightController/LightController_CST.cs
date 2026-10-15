using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BasicClassLibrary.RexHelps;

namespace BasicClassLibrary.LightController
{
    [Serializable]
    public class LightController_CST : LightController_Base
    {

        [DllImport("CSTControllerDll.dll")]
        public static extern int CST_EthernetConnectIP(string IP, ref Int64 Handle);
        [DllImport("CSTControllerDll.dll")]
        public static extern int CST_EthernetConnectStop(ref Int64 Handle);

        [DllImport("CSTControllerDll.dll")]
        public static extern UInt16 CST_EthernetGetDigitalValue(int CH, ref Int64 Handle);
        [DllImport("CSTControllerDll.dll")]
        public static extern UInt16 CST_EthernetSetLightState(int CH, ref Int64 Handle);

        [DllImport("CSTControllerDll.dll")]
        public static extern UInt16 CST_EthernetSetStrobeValue(int CH, int Light, ref Int64 Handle);
        [DllImport("CSTControllerDll.dll")]
        public static extern UInt16 CST_EthernetSetDigitalValue(int CH, int Light, ref Int64 Handle);

        [DllImport("CSTControllerDll.dll")]
        public static extern UInt16 CST_CreateSerialPort(int ComNum, ref Int64 Handle);

        [DllImport("CSTControllerDll.dll")]
        public static extern UInt16 CST_EthernetGetHost(string IP, ref string Handle);


        /// <summary>
        /// 初始化控制器
        /// </summary>
        public override void OpenController()
        {
            try
            {
                int RT = CST_EthernetConnectIP(IP, ref ControllerHandle);
                if (RT == 0)
                    InitSucceed = true;
                else
                    InitSucceed = false;
            }
            catch (Exception ex)
            {
                throw ex;
                InitSucceed = false;
            }
        }
        /// <summary>
        /// 关闭控制器
        /// </summary>
        public override void CloseController()
        {
            try
            {
                if (!InitSucceed)
                    return;

                CST_EthernetConnectStop(ref ControllerHandle);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 打开通道
        /// </summary>
        /// <param name="ch">通道索引</param>
        public override void OpenChannel(int ch)
        {
            if (!InitSucceed)
                return;

            CST_EthernetSetDigitalValue(ch, Brightness[ch - 1], ref ControllerHandle);
        }
        /// <summary>
        /// 关闭通道
        /// </summary>
        /// <param name="ch">通道索引</param>
        public override void CloseChannel(int ch)
        {
            if (!InitSucceed)
                return;

            CST_EthernetSetDigitalValue(ch, 0, ref ControllerHandle);
        }
        /// <summary>
        /// 设置通道亮度值
        /// </summary>
        /// <param name="ch">通道索引</param>
        /// <param name="value">亮度值</param>
        public override void SetValue(int ch, int value)
        {
            Brightness[ch - 1] = value;
            if (!InitSucceed)
                return;

            CST_EthernetSetDigitalValue(ch, value, ref ControllerHandle);
        }
        /// <summary>
        /// 获取通道亮度值
        /// </summary>
        /// <param name="ch">通道索引</param>
        /// <returns>亮度值</returns>
        public override int GetValue(int ch)
        {
            if (!InitSucceed)
                return Brightness[ch];

            ushort value = CST_EthernetGetDigitalValue(ch, ref ControllerHandle);
            return value;
        }
    }
}
