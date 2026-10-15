using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 系统帮助类，常用的系统方法
    /// </summary>
    public static class SystemHelper
    {
        /// <summary>
        /// 是否注册
        /// </summary>
        private static bool IsValid = (Security.GetRemainDays() > 0);
        /// <summary>
        /// Hides the window and activates another window
        /// </summary>
        public const int SW_HIDE = 0;
        /// <summary>
        /// Activates and displays the window. 
        /// If the window is minimized or maximized, 
        /// the system restores it to its original size and position. 
        /// An application should specify this flag when restoring a minimized window
        /// </summary>
        public const int SW_RESTORE = 9;
        /// <summary>
        /// Activates the window and displays it in its current size and position
        /// </summary>
        public const int SW_SHOW = 5;
        /// <summary>
        /// Activates and displays a window. 
        /// If the window is minimized or maximized, 
        /// the system restores it to its original size and position.
        /// An application should specify this flag when displaying the window for the first time.
        /// </summary>
        public const int SW_SHOWNORMAL = 1;

        /// <summary>
        /// 数组转换为字符分割的字符串
        /// </summary>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string ArrayToString(ArrayList array, char separator = ',')
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < array.Count; i++)
            {
                builder.Append(array[i]);
                if (i != (array.Count - 1))
                {
                    builder.Append(separator);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// ASCII字符串转换为字节数组
        /// </summary>
        /// <param name="strASCII"></param>
        /// <returns></returns>
        public static byte[] ASCIIStringToByte(string strASCII)
        {
            byte[] buffer = new byte[strASCII.Length / 2];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Convert.ToByte(strASCII.Substring(i * 2, 2), 0x10);
            }
            return buffer;
        }

        /// <summary>
        /// ASCII字符串转换为字节数组
        /// </summary>
        /// <param name="strASCII"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public static byte[] ASCIIStringToByte(string strASCII, string split)
        {
            return ASCIIStringToByte(strASCII.Replace(split, ""));
        }

        /// <summary>
        /// 将字节单位转换成合适单位
        /// </summary>
        /// <param name="v"></param>
        /// <param name="level"></param>
        public static void ByteConvert(ref double v, ref string level)
        {
            string str = level;
            if (!(str == "B"))
            {
                if (str == "KB")
                {
                    if ((v / 1024.0) > 1.0)
                    {
                        level = "MB";
                        v /= 1024.0;
                        ByteConvert(ref v, ref level);
                    }
                }
                else if ((str == "MB") && ((v / 1024.0) > 1.0))
                {
                    level = "GB";
                    v /= 1024.0;
                    ByteConvert(ref v, ref level);
                }
            }
            else if ((v / 1024.0) > 1.0)
            {
                level = "KB";
                v /= 1024.0;
                ByteConvert(ref v, ref level);
            }
        }

        /// <summary>
        /// 字节数组转换为ASCII字符串
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ByteToASCIIString(byte[] array)
        {
            StringBuilder builder = new StringBuilder();
            foreach (byte num2 in array)
            {
                builder.Append(num2.ToString("X2"));
            }
            return builder.ToString();
        }

        /// <summary>
        /// 字节数组转换为ASCII字符串
        /// </summary>
        /// <param name="array"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public static string ByteToASCIIString(byte[] array, int startIndex, int length, char split = ' ')
        {
            StringBuilder builder = new StringBuilder();
            for (int i = startIndex; i < length; i++)
            {
                builder.Append(array[i].ToString("X2"));
                if (i < (length - 1))
                {
                    builder.Append(split);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// 反射实现两个类的对象之间相同属性的值的复制
        /// 适用于初始化新实体
        /// </summary>
        /// <typeparam name="D">返回的实体</typeparam>
        /// <typeparam name="S">数据源实体</typeparam>
        /// <param name="s">数据源实体</param>
        /// <returns>返回的新实体</returns>
        public static D Copy<D, S>(S s)
        {
            D local = Activator.CreateInstance<D>();
            try
            {
                System.Type type = s.GetType();
                System.Type type2 = typeof(D);
                foreach (PropertyInfo info in type.GetProperties())
                {
                    foreach (PropertyInfo info2 in type2.GetProperties())
                    {
                        if (((info2.Name == info.Name) && (info2.PropertyType == info.PropertyType)) && info2.CanWrite)
                        {
                            info2.SetValue(local, info.GetValue(s, null), null);
                            break;
                        }
                    }
                }
                foreach (FieldInfo info3 in type.GetFields())
                {
                    foreach (FieldInfo info4 in type2.GetFields())
                    {
                        if (((info4.Name == info3.Name) && (info4.FieldType == info3.FieldType)) && !info4.IsLiteral)
                        {
                            info4.SetValue(local, info3.GetValue(s));
                            break;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return local;
        }

        /// <summary>
        /// 反射实现两个类的对象之间相同属性的值的复制
        /// 适用于没有新建实体之间
        /// </summary>
        /// <typeparam name="D">返回的实体</typeparam>
        /// <typeparam name="S">数据源实体</typeparam>
        /// <param name="d">返回的实体</param>
        /// <param name="s">数据源实体</param>
        /// <returns></returns>
        public static D Copy<D, S>(D d, S s)
        {
            try
            {
                System.Type type = s.GetType();
                System.Type type2 = typeof(D);
                foreach (PropertyInfo info in type.GetProperties())
                {
                    foreach (PropertyInfo info2 in type2.GetProperties())
                    {
                        if (((info2.Name == info.Name) && (info2.PropertyType == info.PropertyType)) && info2.CanWrite)
                        {
                            info2.SetValue(d, info.GetValue(s, null), null);
                            break;
                        }
                    }
                }
                foreach (FieldInfo info3 in type.GetFields())
                {
                    foreach (FieldInfo info4 in type2.GetFields())
                    {
                        if (((info4.Name == info3.Name) && (info4.FieldType == info3.FieldType)) && !info4.IsLiteral)
                        {
                            info4.SetValue(d, info3.GetValue(s));
                            break;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return d;
        }

        /// <summary>
        /// 拷贝数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="length"></param>
        public static void CopyData<T>(IntPtr source, T[] destination, int length)
        {
            using (PinnedObject obj2 = new PinnedObject(destination))
            {
                int num = Marshal.SizeOf(typeof(T)) * length;
                CopyMemory(obj2.Pointer, source, (uint)num);
            }
        }

        /// <summary>
        /// 拷贝数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="length"></param>
        public static void CopyData<T>(T[] source, IntPtr destination, int length)
        {
            using (PinnedObject obj2 = new PinnedObject(source))
            {
                int num = Marshal.SizeOf(typeof(T)) * length;
                CopyMemory(destination, obj2.Pointer, (uint)num);
            }
        }

        /// <summary>
        /// 复制内存
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="source"></param>
        /// <param name="length"></param>
        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr destination, IntPtr source, uint length);
        /// <summary>
        /// 创建对象实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fullName">命名空间.类型名</param>
        /// <param name="assemblyName">程序集</param>
        /// <returns></returns>
        public static T CreateInstance<T>(string fullName, string assemblyName)
        {
            return (T)Activator.CreateInstance(System.Type.GetType(fullName + "," + assemblyName), true);
        }

        /// <summary>
        /// 创建对象实例
        /// </summary>
        /// <typeparam name="T">要创建对象的类型</typeparam>
        /// <param name="assemblyName">类型所在程序集名称</param>
        /// <param name="nameSpace">类型所在命名空间</param>
        /// <param name="className">类型名</param>
        /// <returns></returns>
        public static T CreateInstance<T>(string assemblyName, string nameSpace, string className)
        {
            try
            {
                string typeName = nameSpace + "." + className;
                return (T)Assembly.Load(assemblyName).CreateInstance(typeName);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 在DataGridView的单元格中加入控件
        /// </summary>
        /// <param name="grid">数据列表</param>
        /// <param name="row">行</param>
        /// <param name="col">列</param>
        /// <param name="control">控件</param>
        public static void DataGridCellControl(DataGridView grid, int row, int col, Control control)
        {
            if (!grid.Controls.Contains(control))
            {
                grid.Controls.Add(control);
            }
            Rectangle rectangle = grid.GetCellDisplayRectangle(col, row, true);
            control.Top = rectangle.Top;
            control.Left = rectangle.Left;
            control.Width = rectangle.Width;
            control.Height = rectangle.Height;
        }

        /// <summary>
        /// 格式化容量大小
        /// </summary>
        /// <param name="size">容量（B）</param>
        /// <returns>已格式化的容量</returns>
        public static string FormatSize(double size)
        {
            double num = size;
            int index = 0;
            while ((num > 1024.0) && (index < 5))
            {
                num /= 1024.0;
                index++;
            }
            string[] strArray = new string[] { "B", "KB", "MB", "GB", "TB" };
            return string.Format("{0} {1}", Math.Round(num, 2), strArray[index]);
        }

        /// <summary>
        /// 获得当前可用物理内存大小
        /// </summary>
        /// <returns>当前可用物理内存（B）</returns>
        public static ulong GetAvailPhys()
        {
            return GetMemoryStatus().ullAvailPhys;
        }

        /// <summary>
        /// 获取当前进程的实例
        /// </summary>
        /// <param name="bIgnorePath">是否忽略路径比较，如果忽略路径比较，只能运行一个实例；
        /// 反之同一个路径下的只能运行一个</param>
        /// <returns></returns>
        public static Process GetCurrentInstance(bool bIgnorePath = true)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processesByName = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (Process process2 in processesByName)
            {
                if (process2.Id != currentProcess.Id)
                {
                    if (bIgnorePath)
                    {
                        return process2;
                    }
                    if (Assembly.GetExecutingAssembly().Location.Replace("/", @"\") == currentProcess.MainModule.FileName)
                    {
                        return process2;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取枚举类型的分类属性
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string GetEnumCategory(System.Enum enumValue)
        {
            string name = enumValue.ToString();
            object[] customAttributes = enumValue.GetType().GetField(name).GetCustomAttributes(typeof(CategoryAttribute), false);
            if ((customAttributes == null) || (customAttributes.Length == 0))
            {
                return "";
            }
            CategoryAttribute attribute = (CategoryAttribute)customAttributes[0];
            return attribute.Category;
        }

        /// <summary>
        /// 获取枚举类型的描述属性
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string GetEnumDescription(System.Enum enumValue)
        {
            string name = enumValue.ToString();
            object[] customAttributes = enumValue.GetType().GetField(name).GetCustomAttributes(typeof(DescriptionAttribute), false);
            if ((customAttributes == null) || (customAttributes.Length == 0))
            {
                return name;
            }
            DescriptionAttribute attribute = (DescriptionAttribute)customAttributes[0];
            return attribute.Description;
        }

        /// 获取指定驱动器的剩余空间总大小(单位为B) 
        /// 
        /// 只需输入代表驱动器的字母即可
        public static long GetHardDiskFreeSpace(string strHardDiskName)
        {
            long totalFreeSpace = 0L;
            if (!strHardDiskName.EndsWith(@":\"))
            {
                strHardDiskName = strHardDiskName + @":\";
            }
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo info in drives)
            {
                if (info.Name == strHardDiskName)
                {
                    totalFreeSpace = info.TotalFreeSpace;
                }
            }
            return totalFreeSpace;
        }

        /// 获取指定驱动器的空间总大小(单位为B) 
        /// 
        /// 只需输入代表驱动器的字母即可 （大写）
        public static long GetHardDiskSpace(string strHardDiskName)
        {
            long totalSize = 0L;
            if (!strHardDiskName.EndsWith(@":\"))
            {
                strHardDiskName = strHardDiskName + @":\";
            }
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo info in drives)
            {
                if (info.Name == strHardDiskName)
                {
                    totalSize = info.TotalSize;
                }
            }
            return totalSize;
        }
        /// <summary>
        /// Contains the time of the last input.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            /// <summary>
            /// The size of the structure, in bytes. This member must be set to sizeof(LASTINPUTINFO).
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            /// <summary>
            /// The tick count when the last input event was received.
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }
        /// <summary>
        /// 获取鼠标键盘空闲时间
        /// </summary>
        /// <returns></returns>
        public static int GetIdleTick()
        {
            LASTINPUTINFO lastinputinfo = new LASTINPUTINFO();
            lastinputinfo = new LASTINPUTINFO
            {
                cbSize = Marshal.SizeOf<LASTINPUTINFO>(lastinputinfo)
            };
            if (!GetLastInputInfo(ref lastinputinfo))
            {
                return 0;
            }
            return (Environment.TickCount - ((int)lastinputinfo.dwTime));
        }

        /// <summary>
        /// IP地址转换为字节
        /// </summary>
        /// <param name="strIP"></param>
        /// <returns></returns>
        public static byte[] GetIPAddress(string strIP)
        {
            char[] separator = new char[] { '.' };
            string[] strArray = strIP.Split(separator);
            byte[] buffer = new byte[4];
            for (int i = 0; i < Math.Min(4, strArray.Length); i++)
            {
                buffer[i] = Convert.ToByte(strArray[i]);
            }
            return buffer;
        }

        /// <summary>
        /// 调用windows API获取鼠标键盘空闲时间
        /// </summary>
        /// <param name="plii"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        /// <summary>
        /// 获取内存使用率
        /// </summary>
        /// <returns></returns>
        public static double GetMemortRate()
        {
            return (((double)GetMemoryStatus().dwMemoryLoad) / 100.0);
        }

        /// <summary>
        /// 获得当前内存使用情况
        /// </summary>
        /// <returns></returns>
        private static MEMORY_INFO GetMemoryStatus()
        {
            MEMORY_INFO memory_info = new MEMORY_INFO();
            memory_info = new MEMORY_INFO
            {
                dwLength = (uint)Marshal.SizeOf<MEMORY_INFO>(memory_info)
            };
            GlobalMemoryStatusEx(ref memory_info);
            return memory_info;
        }

        /// <summary>
        /// 获得当前总计物理内存大小
        /// </summary>
        /// <returns>总计物理内存大小（B）</returns>
        public static ulong GetTotalPhys()
        {
            return GetMemoryStatus().ullTotalPhys;
        }

        /// <summary>
        /// 根据类型全名获取类型
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        public static System.Type GetTypeByFullName(string fullName)
        {
            System.Type type = null;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            int length = assemblies.Length;
            for (int i = 0; i < length; i++)
            {
                type = assemblies[i].GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }
            for (int j = 0; j < length; j++)
            {
                System.Type[] types = assemblies[j].GetTypes();
                int num4 = types.Length;
                for (int k = 0; k < num4; k++)
                {
                    if (types[k].FullName.Equals(fullName))
                    {
                        return types[k];
                    }
                }
            }
            return type;
        }

        /// <summary>
        /// 获得当前已使用的内存大小
        /// </summary>
        /// <returns>已使用的内存大小（B）</returns>
        public static ulong GetUsedPhys()
        {
            MEMORY_INFO memoryStatus = GetMemoryStatus();
            return (memoryStatus.ullTotalPhys - memoryStatus.ullAvailPhys);
        }

        /// <summary>
        /// 获取属性或字段的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="strName"></param>
        /// <returns></returns>
        public static object GetValue<T>(T obj, string strName)
        {
            object obj2;
            try
            {
                System.Type type = obj.GetType();
                foreach (PropertyInfo info in type.GetProperties())
                {
                    if (info.Name == strName)
                    {
                        return info.GetValue(obj);
                    }
                }
                foreach (FieldInfo info2 in type.GetFields())
                {
                    if (info2.Name == strName)
                    {
                        return info2.GetValue(obj);
                    }
                }
                obj2 = null;
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return obj2;
        }

        /// <summary>
        /// 获取属性或字段的值和类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="strName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetValue<T>(T obj, string strName, out System.Type type)
        {
            object obj2;
            try
            {
                type = null;
                System.Type type2 = obj.GetType();
                foreach (PropertyInfo info in type2.GetProperties())
                {
                    if (info.Name == strName)
                    {
                        type = info.PropertyType;
                        return info.GetValue(obj);
                    }
                }
                foreach (FieldInfo info2 in type2.GetFields())
                {
                    if (info2.Name == strName)
                    {
                        type = info2.FieldType;
                        return info2.GetValue(obj);
                    }
                }
                obj2 = null;
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return obj2;
        }

        /// <summary>
        /// 根据变量获取变量名称
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static string GetVariableName<T>(Expression<Func<T, T>> exp)
        {
            MemberExpression body = (MemberExpression)exp.Body;
            return body.Member.Name;
        }

        /// <summary>
        /// 扩展 获取变量名称(字符串)
        /// </summary>
        /// <param name="var_name"></param>
        /// <param name="exp"></param>
        /// <returns>return string</returns>
        public static string GetVarName<T>(this T var_name, Expression<Func<T, T>> exp)
        {
            MemberExpression body = (MemberExpression)exp.Body;
            return body.Member.Name;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);
        /// <summary>
        /// 从HTTP获取数据
        /// </summary>
        /// <param name="url">HTTP网址</param>
        /// <returns></returns>
        public static string HttpGet(string url)
        {
            string requestUriString = url;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUriString);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            Stream responseStream = ((HttpWebResponse)request.GetResponse()).GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string str2 = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();
            return str2;
        }

        /// <summary>
        /// 发送数据给HTTP
        /// </summary>
        /// <param name="url">HTTP网址</param>
        /// <param name="str">发送的内容</param>
        /// <returns></returns>
        public static string HttpPost(string url, string str)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            request.ContentLength = bytes.Length;
            using (Stream stream2 = request.GetRequestStream())
            {
                stream2.Write(bytes, 0, bytes.Length);
                stream2.Close();
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// .指定的窗口是否最小化
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);
        /// <summary>
        /// 判断某个属性是否只读
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="strName"></param>
        /// <returns></returns>
        public static bool IsReadOnly<T>(T obj, string strName)
        {
            bool flag2;
            try
            {
                System.Type type = obj.GetType();
                foreach (PropertyInfo info in type.GetProperties())
                {
                    if ((info.Name == strName) && info.CanWrite)
                    {
                        return false;
                    }
                }
                flag2 = true;
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return flag2;
        }

        /// <summary>
        /// 是否有效IP
        /// </summary>
        /// <param name="strIP"></param>
        /// <returns></returns>
        public static bool IsValidIP(string strIP)
        {
            IPAddress address;
            return IPAddress.TryParse(strIP, out address);
        }

        /// <summary>
        /// 下移
        /// </summary>
        /// <param name="index"></param>
        /// <param name="list"></param>
        public static void MoveDown(int index, IList list)
        {
            if (index != (list.Count - 1))
            {
                object obj2 = list[index];
                list.RemoveAt(index);
                list.Insert(index + 1, obj2);
            }
        }

        /// <summary>
        /// 上移
        /// </summary>
        /// <param name="index"></param>
        /// <param name="list"></param>
        public static void MoveUp(int index, IList list)
        {
            if (index != 0)
            {
                object obj2 = list[index];
                list.RemoveAt(index);
                list.Insert(index - 1, obj2);
            }
        }

        /// <summary>
        /// 打开已经存在的窗口，如果不存在重新创建
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Form OpenExistForm(System.Type type)
        {
            Form form = Application.OpenForms[type.Name];
            if ((form == null) || form.IsDisposed)
            {
                return (Form)Activator.CreateInstance(type);
            }
            form.Activate();
            if (form.WindowState == FormWindowState.Minimized)
            {
                form.WindowState = FormWindowState.Normal;
            }
            return form;
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        public static void RestartApplication()
        {
            Process.Start(Assembly.GetExecutingAssembly().Location);
        }

        /// <summary>
        /// 该函数将创建指定窗口的线程设置到前台，并且激活该窗口。键盘输入转向该窗口，并为用户改各种可视的记号。
        /// 系统给创建前台窗口的线程分配的权限稍高于其他线程。 
        /// </summary>
        /// <param name="hWnd">将被激活并被调入前台的窗口句柄</param>
        /// <returns>如果窗口设入了前台，返回值为非零；如果窗口未被设入前台，返回值为零</returns>
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        /// <summary>
        /// 根据对象属性或字段设置值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="strName"></param>
        /// <param name="value"></param>
        public static void SetValue<T>(T obj, string strName, object value)
        {
            try
            {
                System.Type type = obj.GetType();
                foreach (PropertyInfo info in type.GetProperties())
                {
                    if ((info.Name == strName) && info.CanWrite)
                    {
                        info.SetValue(obj, value);
                        return;
                    }
                }
                foreach (FieldInfo info2 in type.GetFields())
                {
                    if ((info2.Name == strName) && !info2.IsLiteral)
                    {
                        info2.SetValue(obj, value);
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        /// <summary>
        /// 该函数设置由不同线程产生的窗口的显示状态
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="cmdShow">指定窗口如何显示。查看允许值列表，请查阅ShowWindow函数的说明部分</param>
        /// <returns>如果函数原来可见，返回值为非零；如果函数原来被隐藏，返回值为零</returns>
        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        /// <summary>
        /// 分割字符串转换为Double型数组
        /// </summary>
        /// <param name="strText"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static double[] StringToDoubleArray(string strText, char separator = ',')
        {
            try
            {
                char[] chArray1 = new char[] { separator };
                string[] strArray = strText.Split(chArray1, StringSplitOptions.RemoveEmptyEntries);
                double[] numArray = new double[strArray.Length];
                for (int i = 0; i < strArray.Length; i++)
                {
                    numArray[i] = Convert.ToDouble(strArray[i].Trim());
                }
                return numArray;
            }
            catch (Exception)
            {
                return new double[0];
            }
        }

        /// <summary>
        /// 分割字符串转换为Int型数组
        /// </summary>
        /// <param name="strText"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static int[] StringToIntArray(string strText, char separator = ',')
        {
            try
            {
                char[] chArray1 = new char[] { separator };
                string[] strArray = strText.Split(chArray1, StringSplitOptions.RemoveEmptyEntries);
                int[] numArray = new int[strArray.Length];
                for (int i = 0; i < strArray.Length; i++)
                {
                    numArray[i] = Convert.ToInt32(strArray[i].Trim());
                }
                return numArray;
            }
            catch (Exception)
            {
                return new int[0];
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_INFO
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }
    }
}
