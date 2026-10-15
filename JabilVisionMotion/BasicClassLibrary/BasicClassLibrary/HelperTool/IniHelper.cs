using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 进制
    /// </summary>
    public enum Base
    {
        /// <summary>
        /// 二进制
        /// </summary>
        BASE_BINARY = 2,
        /// <summary>
        /// 10进制
        /// </summary>
        BASE_DECIMAL = 10,
        /// <summary>
        /// 16进制
        /// </summary>
        BASE_HEX = 0x10,
        /// <summary>
        /// 8进制
        /// </summary>
        BASE_OCTAL = 8
    }

    /// <summary>
    /// INI文件读写类
    /// </summary>
    public class IniHelper
    {
        private const int DEF_PROFILE_THRESHOLD = 0x200;
        private string m_strIniFile;

        /// <summary>
        /// 获取INI文件中节点对应的所有关键字和值
        /// </summary>
        /// <param name="lpAppName"></param>
        /// <param name="lpReturnedString"></param>
        /// <param name="nSize"></param>
        /// <param name="lpFileName"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileSection(string lpAppName, byte[] lpReturnedString, int nSize, string lpFileName);
        /// <summary>
        /// 获取INI文件中所有节点的名称
        /// </summary>
        /// <param name="lpszReturnBuffer">结果缓冲区</param>
        /// <param name="nSize">缓冲区大小</param>
        /// <param name="lpFileName">INI文件路径</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileSectionNames(byte[] lpszReturnBuffer, int nSize, string lpFileName);
        /// <summary>
        /// 获取INI文件中的键值
        /// </summary>
        /// <param name="lpAppName">ini节名</param>
        /// <param name="lpKeyName">ini键名</param>
        /// <param name="lpDefault">默认值：当无对应键值，则返回该值。</param>
        /// <param name="lpReturnedString">结果缓冲区</param>
        /// <param name="nSize">结果缓冲区大小</param>
        /// <param name="lpFileName">ini文件位置</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
        /// <summary>
        /// 获取INI文件中的键值
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="def"></param>
        /// <param name="retVal"></param>
        /// <param name="size"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);
        /// <summary>
        /// 构造函数
        /// </summary>
        public IniHelper()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="strIniFile"></param>
        public IniHelper(string strIniFile)
        {
            this.m_strIniFile = strIniFile;
        }

        /// <summary>
        /// 往指定节点和关键字追加字符串
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="strValue"></param>
        /// <returns></returns>
        public bool AppendString(string strSection, string strKey, string strValue)
        {
            string str = this.GetString(strSection, strKey, "") + strValue;
            return this.WriteString(strSection, strKey, str);
        }

        /// <summary>
        /// 删除键
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <returns></returns>
        public bool DeleteKey(string strSection, string strKey)
        {
            return (WritePrivateProfileString(strSection, strKey, null, this.m_strIniFile) == 1);
        }

        /// <summary>
        /// 删除节点下的所有键
        /// </summary>
        /// <param name="strSection"></param>
        /// <returns></returns>
        public bool DeleteKeys(string strSection)
        {
            foreach (string str in this.GetKeyNames(strSection))
            {
                if (!this.DeleteKey(strSection, str))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <returns></returns>
        public bool DeleteSection(string strSection)
        {
            return (WritePrivateProfileString(strSection, null, "", this.m_strIniFile) == 1);
        }

        /// <summary>
        /// 获取指定节点和关键字的数值数组
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="separator">分隔符</param>
        /// <param name="bTrim">是否Trim</param>
        /// <returns></returns>
        public string[] GetArray(string strSection, string strKey, char separator = ',', bool bTrim = true)
        {
            string str = this.GetString(strSection, strKey, "");
            if (string.IsNullOrEmpty(str))
            {
                return new string[0];
            }
            char[] chArray1 = new char[] { separator };
            string[] strArray = str.Split(chArray1, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < strArray.Length; i++)
            {
                if (bTrim)
                {
                    strArray[i] = strArray[i].Trim();
                }
            }
            return strArray;
        }

        /// <summary>
        /// 获取指定节点和关键字的bool型数值
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="bDefault"></param>
        /// <returns></returns>
        public bool GetBool(string strSection, string strKey, bool bDefault)
        {
            string str = this.GetString(strSection, strKey, "");
            try
            {
                return Convert.ToBoolean(str);
            }
            catch
            {
                return bDefault;
            }
        }

        /// <summary>
        /// 获取指定节点和关键字的浮点型数值
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="dbDefault"></param>
        /// <returns></returns>
        public double GetDouble(string strSection, string strKey, double dbDefault)
        {
            string str = this.GetString(strSection, strKey, "");
            try
            {
                return Convert.ToDouble(str);
            }
            catch
            {
                return dbDefault;
            }
        }

        /// <summary>
        /// 获取指定节点和关键字的数值数组
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="separator">分隔符</param>
        /// <returns></returns>
        public double[] GetDoubleArray(string strSection, string strKey, char separator = ',')
        {
            string str = this.GetString(strSection, strKey, "");
            if (string.IsNullOrEmpty(str))
            {
                return new double[0];
            }
            try
            {
                char[] chArray1 = new char[] { separator };
                string[] strArray = str.Split(chArray1, StringSplitOptions.RemoveEmptyEntries);
                double[] numArray2 = new double[strArray.Length];
                for (int i = 0; i < strArray.Length; i++)
                {
                    numArray2[i] = Convert.ToDouble(strArray[i].Trim());
                }
                return numArray2;
            }
            catch (Exception)
            {
                return new double[0];
            }
        }

        /// <summary>
        /// 获取指定节点和关键字的数值
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <param name="strKey">关键字</param>
        /// <param name="nDefault">默认值</param>
        /// <param name="fromBase">进制</param>
        /// <returns></returns>
        public int GetInt(string strSection, string strKey, int nDefault, Base fromBase = Base.BASE_DECIMAL)
        {
            string str = this.GetString(strSection, strKey, "");
            try
            {
                return Convert.ToInt32(str, (int)fromBase);
            }
            catch
            {
                return nDefault;
            }
        }

        /// <summary>
        /// 获取指定节点和关键字的数值数组
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="separator">分隔符</param>
        /// <returns></returns>
        public int[] GetIntArray(string strSection, string strKey, char separator = ',')
        {
            string str = this.GetString(strSection, strKey, "");
            if (string.IsNullOrEmpty(str))
            {
                return new int[0];
            }
            try
            {
                char[] chArray1 = new char[] { separator };
                string[] strArray = str.Split(chArray1, StringSplitOptions.RemoveEmptyEntries);
                int[] numArray2 = new int[strArray.Length];
                for (int i = 0; i < strArray.Length; i++)
                {
                    numArray2[i] = Convert.ToInt32(strArray[i].Trim());
                }
                return numArray2;
            }
            catch (Exception)
            {
                return new int[0];
            }
        }

        /// <summary>
        /// 获取指定节点的所有键
        /// </summary>
        /// <param name="strSection"></param>
        /// <returns></returns>
        public string[] GetKeyNames(string strSection)
        {
            byte[] buffer;
            int nSize = 0;
            int count = 0;
            do
            {
                nSize += 0x200;
                buffer = new byte[nSize + 1];
                count = GetPrivateProfileSection(strSection, buffer, nSize, this.m_strIniFile);
            }
            while ((count + 2) >= nSize);
            string[] strArray = Encoding.Default.GetString(buffer, 0, count).ToString().Split(new char[1], StringSplitOptions.RemoveEmptyEntries);
            List<string> list = new List<string>();
            foreach (string str2 in strArray)
            {
                char[] separator = new char[] { '=' };
                string[] strArray3 = str2.Split(separator, StringSplitOptions.None);
                if ((strArray3.Length == 2) && (strArray3[0].Trim().Length > 0))
                {
                    list.Add(strArray3[0].Trim());
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 获取百分比
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="dbDefault"></param>
        /// <returns></returns>
        public double GetPercent(string strSection, string strKey, double dbDefault)
        {
            string str = this.GetString(strSection, strKey, "").Replace("%", "");
            try
            {
                return (Convert.ToDouble(str) / 100.0);
            }
            catch
            {
                return dbDefault;
            }
        }

        /// <summary>
        /// 获取所有的节点
        /// </summary>
        /// <returns></returns>
        public string[] GetSectionNames()
        {
            byte[] buffer;
            int nSize = 0;
            int count = 0;
            do
            {
                nSize += 0x200;
                buffer = new byte[nSize + 1];
                count = GetPrivateProfileSectionNames(buffer, nSize, this.m_strIniFile);
            }
            while ((count + 2) >= nSize);
            return Encoding.Default.GetString(buffer, 0, count).Split(new char[1], StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 获取指定节点的键值
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <param name="strKey">关键字</param>
        /// <param name="strDefault">默认值</param>
        /// <returns>键值</returns>
        public string GetString(string strSection, string strKey, string strDefault = "")
        {
            if (string.IsNullOrEmpty(strSection) || string.IsNullOrEmpty(strKey))
            {
                if (string.IsNullOrEmpty(strDefault))
                {
                    strDefault = "";
                }
                return strDefault;
            }
            byte[] buffer;
            int nSize = 0;
            int count = 0;
            do
            {
                nSize += 0x200;

                buffer = new byte[nSize + 1];

                count = GetPrivateProfileString(strSection, strKey, strDefault, buffer, nSize, this.m_strIniFile);
            }
            while ((count + 1) >= nSize);

            //更改成UTF8，防止越南语读不了乱码，Ini必须保存为UTF8格式
            //暂时先用defalult 

            return Encoding.Default.GetString(buffer, 0, count);

        }

        /// <summary>
        /// 指定键是否存在
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <returns></returns>
        public bool IsKeyExist(string strSection, string strKey)
        {
            return (Array.IndexOf<string>(this.GetKeyNames(strSection), strKey) >= 0);
        }

        /// <summary>
        /// 判断节点是否存在
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <returns></returns>
        public bool IsSectionExist(string strSection)
        {
            return (Array.IndexOf<string>(this.GetSectionNames(), strSection) >= 0);
        }

        /// <summary>
        /// 写指定节点和关键字的数组
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <param name="strKey">关键字</param>
        /// <param name="array">数组</param>
        /// <param name="nWriteCount">写入数组的长度</param>
        /// <param name="separator">分隔符</param>
        /// <returns></returns>
        public bool WriteArray(string strSection, string strKey, double[] array, int nWriteCount = -1, char separator = ',')
        {
            if (nWriteCount < 0)
            {
                nWriteCount = array.Length;
            }
            else
            {
                nWriteCount = Math.Min(nWriteCount, array.Length);
            }
            StringBuilder builder = new StringBuilder("");
            for (int i = 0; i < nWriteCount; i++)
            {
                builder.Append(array[i].ToString("F3"));
                if (i != (nWriteCount - 1))
                {
                    builder.Append(separator);
                }
            }
            return this.WriteString(strSection, strKey, builder.ToString());
        }

        /// <summary>
        /// 写指定节点和关键字的数组
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <param name="strKey">关键字</param>
        /// <param name="array">数组</param>
        /// <param name="nWriteCount">写入数组的长度</param>
        /// <param name="separator">分隔符</param>
        /// <returns></returns>
        public bool WriteArray(string strSection, string strKey, int[] array, int nWriteCount = -1, char separator = ',')
        {
            if (nWriteCount < 0)
            {
                nWriteCount = array.Length;
            }
            else
            {
                nWriteCount = Math.Min(nWriteCount, array.Length);
            }
            StringBuilder builder = new StringBuilder("");
            for (int i = 0; i < nWriteCount; i++)
            {
                builder.Append(array[i]);
                if (i != (nWriteCount - 1))
                {
                    builder.Append(separator);
                }
            }
            return this.WriteString(strSection, strKey, builder.ToString());
        }

        /// <summary>
        /// 写指定节点和关键字的数组
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <param name="strKey">关键字</param>
        /// <param name="strArray">字符串数组</param>
        /// <param name="nWriteCount">写入数组的长度</param>
        /// <param name="separator">分隔符</param>
        /// <returns></returns>
        public bool WriteArray(string strSection, string strKey, string[] strArray, int nWriteCount = -1, char separator = ',')
        {
            if (nWriteCount < 0)
            {
                nWriteCount = strArray.Length;
            }
            else
            {
                nWriteCount = Math.Min(nWriteCount, strArray.Length);
            }
            StringBuilder builder = new StringBuilder("");
            for (int i = 0; i < nWriteCount; i++)
            {
                builder.Append(strArray[i]);
                if (i != (nWriteCount - 1))
                {
                    builder.Append(separator);
                }
            }
            return this.WriteString(strSection, strKey, builder.ToString());
        }

        /// <summary>
        /// 写入指定节点关键字的bool型数值
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="bValue"></param>
        /// <returns></returns>
        public bool WriteBool(string strSection, string strKey, bool bValue)
        {
            string strValue = bValue.ToString();
            return this.WriteString(strSection, strKey, strValue);
        }

        /// <summary>
        /// 写入指定节点关键字的浮点数数值
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <param name="strKey">关键字</param>
        /// <param name="dbValue">数值</param>
        /// <param name="nPrecision">精度</param>
        /// <returns></returns>
        public bool WriteDouble(string strSection, string strKey, double dbValue, int nPrecision = -1)
        {
            string strValue = dbValue.ToString();
            if (nPrecision > 0)
            {
                string format = string.Format("f{0}", nPrecision);
                strValue = dbValue.ToString(format);
            }
            return this.WriteString(strSection, strKey, strValue);
        }

        /// <summary>
        /// 写入指定节点关键字的数值
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="nValue"></param>
        /// <param name="toBase"></param>
        /// <returns></returns>
        public bool WriteInt(string strSection, string strKey, int nValue, Base toBase = Base.BASE_DECIMAL)
        {
            string strValue = Convert.ToString(nValue, (int)toBase);
            return this.WriteString(strSection, strKey, strValue);
        }

        /// <summary>
        /// 写百分比
        /// </summary>
        /// <param name="strSection"></param>
        /// <param name="strKey"></param>
        /// <param name="dbValue"></param>
        /// <param name="nPrecision"></param>
        /// <returns></returns>
        public bool WritePercent(string strSection, string strKey, double dbValue, int nPrecision = -1)
        {
            dbValue *= 100.0;
            string strValue = dbValue.ToString();
            if (nPrecision > 0)
            {
                string format = string.Format("f{0}", nPrecision);
                strValue = dbValue.ToString(format) + "%";
            }
            return this.WriteString(strSection, strKey, strValue);
        }

        /// <summary>
        /// 替换INI文件中的节点名称
        /// </summary>
        /// <param name="lpAppName">旧节点名称</param>
        /// <param name="lpString">新的节点名称</param>
        /// <param name="lpFileName">INI文件</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int WritePrivateProfileSection(string lpAppName, string lpString, string lpFileName);
        /// <summary>
        /// 写入INI文件键值
        /// </summary>
        /// <param name="lpAppName">ini节名</param>
        /// <param name="lpKeyName">ini键名</param>
        /// <param name="lpString">写入值</param>
        /// <param name="lpFileName">ini文件位置</param>
        /// <returns>0：写入失败 1：写入成功</returns>
        [DllImport("kernel32.dll")]
        private static extern int WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
        /// <summary>
        /// 写入指定节点键值
        /// </summary>
        /// <param name="strSection">节点</param>
        /// <param name="strKey">关键字</param>
        /// <param name="strValue">值</param>
        /// <returns></returns>
        public bool WriteString(string strSection, string strKey, string strValue)
        {
            if (string.IsNullOrEmpty(strSection) || string.IsNullOrEmpty(strKey))
            {
                return false;
            }
            int x = WritePrivateProfileString(strSection, strKey, strValue, this.m_strIniFile);
            return (WritePrivateProfileString(strSection, strKey, strValue, this.m_strIniFile) == 1);
        }

        /// <summary>
        /// INI文件
        /// </summary>
        public string IniFile
        {
            get
            {
                return this.m_strIniFile;
            }
            set
            {
                this.m_strIniFile = value;
            }
        }
    }
}
