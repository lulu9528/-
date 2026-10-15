using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Alarm.Lib
{
    /// <summary>
    /// INI配置文件操作类
    /// 提供对INI格式配置文件的读写、删除等操作
    /// 通过调用Windows API实现底层功能，封装为易用的C#方法
    /// </summary>
    public class IniConfig
    {
        #region Windows API 导入

        /// <summary>
        /// 向INI文件写入键值对
        /// </summary>
        /// <param name="section">节点名称（段落名）</param>
        /// <param name="key">键名</param>
        /// <param name="val">要写入的值</param>
        /// <param name="filePath">INI文件路径</param>
        /// <returns>操作成功返回非0值，失败返回0</returns>
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString", CharSet = CharSet.Ansi)]
        public static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        /// <summary>
        /// 从INI文件读取指定键的值
        /// </summary>
        /// <param name="section">节点名称（段落名）</param>
        /// <param name="key">键名</param>
        /// <param name="def">默认值（当键不存在时返回此值）</param>
        /// <param name="retVal">用于接收结果的StringBuilder</param>
        /// <param name="size">接收缓冲区的大小</param>
        /// <param name="filePath">INI文件路径</param>
        /// <returns>返回读取到的字符数，不包括终止符</returns>
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString", CharSet = CharSet.Ansi)]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// 获取INI文件中所有节点名称
        /// </summary>
        /// <param name="lpszReturnBuffer">接收结果的缓冲区指针</param>
        /// <param name="nSize">缓冲区大小</param>
        /// <param name="filePath">INI文件路径</param>
        /// <returns>返回写入缓冲区的字符数，不包括终止符</returns>
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileSectionNames", CharSet = CharSet.Ansi)]
        public static extern int GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, int nSize, string filePath);

        /// <summary>
        /// 获取INI文件中指定节点下的所有键值对
        /// </summary>
        /// <param name="lpAppName">节点名称</param>
        /// <param name="lpReturnedString">接收结果的字节数组</param>
        /// <param name="nSize">缓冲区大小</param>
        /// <param name="filePath">INI文件路径</param>
        /// <returns>返回写入缓冲区的字符数，不包括终止符</returns>
        [DllImport("KERNEL32.DLL ", EntryPoint = "GetPrivateProfileSection", CharSet = CharSet.Ansi)]
        public static extern int GetPrivateProfileSection(string lpAppName, byte[] lpReturnedString, int nSize, string filePath);

        #endregion

        /// <summary>
        /// INI文件默认路径
        /// 默认为应用程序根目录下的"Config\AlarmConfig.ini"
        /// </summary>
        private static string path = System.AppDomain.CurrentDomain.BaseDirectory + "Config\\AlarmConfig.ini";

        /// <summary>
        /// INI文件路径属性
        /// 提供读取和修改INI文件路径的功能
        /// </summary>
        public static string Path
        {
            get { return path; }
            set { path = value; }
        }

        /// <summary>
        /// 向INI文件写入数据
        /// </summary>
        /// <param name="Section">节点名（段落名）</param>
        /// <param name="Key">键名</param>
        /// <param name="Value">要写入的值</param>
        public static void Write(string Section, string Key, string Value)
        {
            // 调用Windows API写入键值对
            WritePrivateProfileString(Section, Key, Value, path);
        }

        /// <summary>
        /// 从INI文件读取指定键的值
        /// </summary>
        /// <param name="Section">节点名（段落名）</param>
        /// <param name="Key">键名</param>
        /// <returns>读取到的值，若不存在则返回空字符串</returns>
        public static string Read(string Section, string Key)
        {
            // 创建缓冲区，大小为1024字节
            StringBuilder temp = new StringBuilder(1024);

            // 调用Windows API读取值
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, path);

            // 返回读取到的字符串
            return temp.ToString();
        }

        /// <summary>
        /// 读取INI文件中所有的节点名称
        /// </summary>
        /// <param name="sections">输出参数，接收所有节点名称的数组</param>
        /// <returns>成功返回0，失败返回-1</returns>
        public static int GetAllSectionNames(out string[] sections)
        {
            // 定义最大缓冲区大小
            int MAX_BUFFER = 32767;

            // 分配非托管内存作为缓冲区
            IntPtr pReturnedString = Marshal.AllocCoTaskMem(MAX_BUFFER);

            try
            {
                // 调用API获取所有节点名称
                int bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, path);

                // 检查是否获取成功
                if (bytesReturned == 0)
                {
                    sections = null;
                    return -1;
                }

                // 将非托管内存中的数据转换为字符串
                string local = Marshal.PtrToStringAnsi(pReturnedString, (int)bytesReturned).ToString();

                // 分割字符串获取各个节点名称（节点间以null字符分隔）
                // 移除最后一个null字符后再分割
                sections = local.Substring(0, local.Length - 1).Split('\0');
                return 0;
            }
            finally
            {
                // 释放非托管内存，防止内存泄漏
                Marshal.FreeCoTaskMem(pReturnedString);
            }
        }

        /// <summary>
        /// 获取指定节点下所有的键值对
        /// </summary>
        /// <param name="section">节点名称</param>
        /// <returns>包含所有键值对的字典集合</returns>
        public static Dictionary<string, string> GetAllKeyValues(string section)
        {
            // 创建字节数组作为缓冲区（65535字节足够大多数场景）
            byte[] b = new byte[65535];

            // 调用API获取指定节点下的所有键值对
            GetPrivateProfileSection(section, b, b.Length, path);

            // 将字节数组转换为字符串（使用UTF8编码）
            string s = System.Text.Encoding.UTF8.GetString(b);

            // 分割字符串（键值对之间以null字符分隔）
            string[] tmp = s.Split((char)0);

            // 过滤空字符串
            ArrayList result = new ArrayList();
            foreach (string r in tmp)
            {
                if (r != string.Empty)
                    result.Add(r);
            }

            // 解析每个键值对（键和值之间以等号分隔）
            string[] keys = new string[result.Count];
            string[] values = new string[result.Count];

            for (int i = 0; i < result.Count; i++)
            {
                string[] item = result[i].ToString().Split(new char[] { '=' });

                // 处理不同的分割结果
                if (item.Length == 2)
                {
                    keys[i] = item[0].Trim();
                    values[i] = item[1].Trim();
                }
                else if (item.Length == 1)
                {
                    keys[i] = item[0].Trim();
                    values[i] = "";
                }
                else
                {
                    keys[i] = "";
                    values[i] = "";
                }
            }

            // 将键值对存入字典并返回
            Dictionary<string, string> dic = new Dictionary<string, string>();
            for (int i = 0; i < keys.Length; i++)
            {
                dic.Add(keys[i], values[i]);
            }
            return dic;
        }

        /// <summary>
        /// 从INI文件中删除指定的键
        /// </summary>
        /// <param name="section">节点名</param>
        /// <param name="key">要删除的键名</param>
        public static void Delete(string section, string key)
        {
            // 调用API删除键（将值设为null即可删除该键）
            WritePrivateProfileString(section, key, null, path);
        }
    }
}
