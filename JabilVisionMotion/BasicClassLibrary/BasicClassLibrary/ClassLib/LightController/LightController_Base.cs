using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary.LightController
{
    [Serializable]
    public class LightController_Base
    {

        /// <summary>
        /// 控制器句柄
        /// </summary>
        public long ControllerHandle = 0;
        /// <summary>
        /// 光源控制器名称
        /// </summary>
        public string Name = string.Empty;
        /// <summary>
        /// 光源控制器IP
        /// </summary>
        public string IP = "192.168.0.1";
        /// <summary>
        /// 八个端口的亮度值
        /// </summary>
        public int[] Brightness = new int[8];
        /// <summary>
        /// 是否初始化成功
        /// </summary>
        public bool InitSucceed = false;
        /// <summary>
        /// 程序开启后打开所有光源通道
        /// </summary>
        public bool OpenAllChAfterStart = true;
        /// <summary>
        /// 程序关闭前关闭所有光源通道
        /// </summary>
        public bool CloseAllChBeforeClose = true;


        /// <summary>
        /// 初始化控制器
        /// </summary>
        public virtual void OpenController() { }
        /// <summary>
        /// 关闭控制器
        /// </summary>
        public virtual void CloseController() { }
        /// <summary>
        /// 打开通道
        /// </summary>
        public virtual void OpenChannel(int ch) { }
        /// <summary>
        /// 关闭通道
        /// </summary>
        /// <param name="ch">通道索引</param>
        public virtual void CloseChannel(int ch) { }
        /// <summary>
        /// 设置通道亮度值
        /// </summary>
        /// <param name="ch">通道索引</param>
        /// <param name="value">亮度值</param>
        public virtual void SetValue(int ch, int value) { }
        /// <summary>
        /// 获取通道亮度值
        /// </summary>
        /// <param name="ch">通道索引</param>
        /// <returns>亮度值</returns>
        public virtual int GetValue(int ch) { return 0; }
        /// <summary>
        /// 打开所有通道
        /// </summary>
        public virtual void OpenAllChannel()
        {
            for (int i = 0; i < 8; i++)
            {
                OpenChannel(i + 1);
            }
        }
        /// <summary>
        /// 关闭所有通道
        /// </summary>
        public virtual void CloseAllChannel()
        {
            for (int i = 0; i < 8; i++)
            {
                CloseChannel(i + 1);
            }
        }

    }
}
