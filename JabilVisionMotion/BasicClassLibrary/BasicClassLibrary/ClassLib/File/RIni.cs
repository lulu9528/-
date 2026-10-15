using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace BasicClassLibrary.RexHelps
{
    /// <summary>
    /// ini文件操作类
    /// </summary>
    public class RIni
    {
        /// <summary>
        /// ini文件存储路径
        /// </summary>
        public string Filepath;
        /// <summary>
        /// 类的构造函数，传递INI文件名 
        /// </summary>
        /// <param name="input">输入文件名</param>
        public RIni(string input)
        {
            if (input != "")
            {
                Filepath = input;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Helper.RIni.RIni：" + input + "输入目录为空！");
            }
        }

        #region "操作"

        #region "读"
        /// 读取INI文件
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">键</param>
        /// <returns>返回的键值</returns>
        public string ReadValue(string section, string key,string value)
        {
            try
            {
                StringBuilder temp = new StringBuilder(1024);
                int i = GetPrivateProfileString(section, key, "", temp, 1024, this.Filepath);
                if (temp.Length < 1)
                {
                    return value;
                }
                return temp.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
                return null;
            }
        }
        /// 读取INI文件
        /// </summary>
        /// <param name="Section">段，格式[]</param>
        /// <param name="Key">键</param>
        /// <returns>返回byte类型的section组或键值组</returns>
        public byte[] IniReadValues(string section, string key)
        {
            byte[] temp = new byte[255];
            int i = GetPrivateProfileString(section, key, "", temp, 255, this.Filepath);
            return temp;
        }
        #endregion

        #region "写"
        /// 写INI文件
        /// </summary>
        /// <param name="section">段落</param>
        /// <param name="key">键</param>
        /// <param name="iValue">值</param>
        public void WriteValue(string section, string key, string value)
        {
            try
            {
                WritePrivateProfileString(section, key, value, this.Filepath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region "删除"
        /// <summary>
        /// 删除指定字段
        /// </summary>
        /// <param name="sectionName">段落</param>
        /// <param name="keyName">键</param>
        public void iniDelete(string sectionName, string keyName)
        {
            try
            {
                WritePrivateProfileString(sectionName, keyName, null, this.Filepath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 删除字段重载
        /// </summary>
        /// <param name="sectionName">段落</param>
        public void iniDelete(string sectionName)
        {
            try
            {
                WritePrivateProfileString(sectionName, null, null, this.Filepath);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 删除ini文件下所有段落
        /// </summary>
        public void ClearAllSection()
        {
            try
            {
                WriteValue(null, null, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 删除ini文件下personal段落下的所有键
        /// </summary>
        /// <param name="Section"></param>
        public void ClearSection(string Section)
        {
            try
            {
                WriteValue(Section, null, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
        #endregion

        #region "API"
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);
        #endregion
    }
}
