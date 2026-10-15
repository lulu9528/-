using BasicClassLibrary.RexHelps;
using HslCommunication;
using HslCommunication.ModBus;
using Jabil.Module.Log.Lib;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Jabil.Module.Modbus.Lib
{
    public class Robot_HSLModbus
    {
        /// <summary>
        /// 对象实例
        /// </summary>
        private static Robot_HSLModbus _instance;
        public static Robot_HSLModbus Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Robot_HSLModbus();
                return _instance;
            }
        }
        public ModbusTcpNet busTcpClient;
        private readonly Object obj = new Object();
        //是否连接
        private bool _IsConnected = false;
        public bool IsConnected
        {
            get { return _IsConnected; }
        }
        private TcpClient RobotTCP;
        public string _设备名称;

        //建立连接
        public bool Connect(string strIp, int nPort)
        {
            try
            {
                busTcpClient = new ModbusTcpNet(strIp, 502);
                busTcpClient.ReceiveTimeOut = 1000;
                OperateResult rs = busTcpClient.ConnectServer();
                if (rs.IsSuccess)
                {
                    _IsConnected = true;
                }
                else
                {
                    _IsConnected = false;

                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                _IsConnected = false;
            }
            return _IsConnected;
        }
        //断开连接
        public void DisConnect()
        {
            try
            {
                busTcpClient.ConnectClose();
                _IsConnected = false;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        //向Robot写float数据 
        public bool ModbusWrite(float sendData, int Index)
        {
            bool bSuccess = false;
            try
            {
                if (_IsConnected)
                {
                    lock (obj)
                    {
                        ushort[] values = ConvertFloatToUshorts(sendData);
                        OperateResult result = busTcpClient.Write(((Index - 1) * 2).ToString(), new ushort[] { values[1], values[0] });
                        bSuccess = result.IsSuccess;
                    }
                }

            }
            catch (Exception ex)
            {
                _IsConnected = false;
                RLog.SaveError(ex);
            }
            return bSuccess;
        }
        //从Robot读float类型数据 
        public float ModbusRead(int Index)
        {
            try
            {
                if (_IsConnected)
                {
                    lock (obj)
                    {
                        int address = (Index - 1) * 2;
                        OperateResult<ushort[]> res = busTcpClient.ReadUInt16(address.ToString(), 2);
                        float result = GetFloat(res.Content[1], res.Content[0]);
                        return result;
                    }
                }
                else
                {
                    return -999;
                }

            }
            catch (Exception ex)
            {
                return -999; ;
            }
        }
        public static ushort[] ConvertFloatToUshorts(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            ushort high = BitConverter.ToUInt16(bytes, 0);
            ushort low = BitConverter.ToUInt16(bytes, 2);
            return new ushort[] { low, high };
        }
        public static float GetFloat(ushort P1, ushort P2)
        {
            int intSign, intSignRest, intExponent, intExponentRest;
            float faResult, faDigit;
            intSign = P1 / 32768;
            intSignRest = P1 % 32768;
            intExponent = intSignRest / 128;
            intExponentRest = intSignRest % 128;
            faDigit = (float)(intExponentRest * 65536 + P2) / 8388608;
            faResult = (float)Math.Pow(-1, intSign) * (float)Math.Pow(2, intExponent - 127) * (faDigit + 1);
            return (float)faResult.ToDouble(3);



        }
    }
}
