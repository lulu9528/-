using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Jabil.Module.Permission
{
    /// <summary>
    /// 鼠标钩子类
    /// 用于全局监听系统鼠标事件（低级鼠标钩子）
    /// 通过Windows API实现，可捕获鼠标点击、移动、滚轮等操作
    /// </summary>
    internal class MouseHook
    {
        /// <summary>
        /// 鼠标状态变化事件
        /// 当捕获到鼠标事件时触发，传递事件详细信息
        /// </summary>
        public static event EventHandler<MouseHookEventArgs> StatusChanged;

        /// <summary>
        /// 钩子ID
        /// 存储通过SetWindowsHookEx获取的钩子句柄，用于标识当前安装的钩子
        /// </summary>
        private static IntPtr HookId = IntPtr.Zero;

        /// <summary>
        /// 钩子回调函数委托实例
        /// 指向钩子事件处理的回调函数，用于Windows API调用
        /// </summary>
        private static readonly InteropValues.HookProc Proc = HookCallback;

        /// <summary>
        /// 引用计数
        /// 跟踪钩子的启动次数，确保多次调用Start时不会重复安装，仅在计数为0时卸载
        /// </summary>
        private static int Count;

        /// <summary>
        /// 启动鼠标钩子
        /// 安装低级鼠标钩子，开始监听系统鼠标事件
        /// 多次调用会增加引用计数，不会重复安装
        /// </summary>
        public static void Start()
        {
            // 仅当钩子未安装时执行安装逻辑
            if (HookId == IntPtr.Zero)
            {
                HookId = SetHook(Proc);
            }

            // 安装成功则增加引用计数
            if (HookId != IntPtr.Zero)
            {
                Count++;
            }
        }

        /// <summary>
        /// 停止鼠标钩子
        /// 减少引用计数，当计数小于1时卸载钩子，停止监听
        /// </summary>
        public static void Stop()
        {
            Count--;
            // 仅当引用计数小于1且钩子已安装时执行卸载
            if (Count < 1)
            {
                InteropMethods.UnhookWindowsHookEx(HookId);
                HookId = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 安装钩子
        /// 调用Windows API设置低级鼠标钩子，关联回调函数
        /// </summary>
        /// <param name="proc">钩子回调函数委托</param>
        /// <returns>钩子句柄（IntPtr），失败时返回IntPtr.Zero</returns>
        private static IntPtr SetHook(InteropValues.HookProc proc)
        {
            // 获取当前进程和主模块信息
            var curProcess = Process.GetCurrentProcess();
            var curModule = curProcess.MainModule;

            // 主模块不为空时，调用API安装钩子
            if (curModule != null)
            {
                return InteropMethods.SetWindowsHookEx(
                    (int)InteropValues.HookType.WH_MOUSE_LL, // 低级鼠标钩子类型
                    proc, // 回调函数
                    InteropMethods.GetModuleHandle(curModule.ModuleName), // 当前模块句柄
                    0 // 0表示监听所有线程（全局钩子）
                );
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 钩子回调函数
        /// 当有鼠标事件发生时，Windows系统会调用此函数
        /// 解析事件信息并触发StatusChanged事件
        /// </summary>
        /// <param name="nCode">钩子代码，决定是否处理事件（小于0时需传递给下一个钩子）</param>
        /// <param name="wParam">事件类型标识（如左键按下、移动等）</param>
        /// <param name="lParam">鼠标事件详细信息的结构体指针</param>
        /// <returns>下一个钩子的返回值，确保钩子链正常工作</returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // 钩子代码小于0时，不处理事件，直接传递给下一个钩子
            if (nCode < 0)
                return InteropMethods.CallNextHookEx(HookId, nCode, wParam, lParam);

            // 将lParam指针转换为MOUSEHOOKSTRUCT结构体，获取鼠标位置等信息
            var hookStruct = (InteropValues.MOUSEHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(InteropValues.MOUSEHOOKSTRUCT));

            // 触发状态变化事件，传递事件类型和鼠标位置
            StatusChanged?.Invoke(null, new MouseHookEventArgs
            {
                MessageType = (MouseHookMessageType)wParam, // 转换事件类型
                Point = new InteropValues.POINT(hookStruct.pt.X, hookStruct.pt.Y) // 鼠标位置
            });

            // 将事件传递给下一个钩子，确保系统钩子链正常
            return InteropMethods.CallNextHookEx(HookId, nCode, wParam, lParam);
        }
    }

    /// <summary>
    /// 鼠标钩子事件参数类
    /// 包含鼠标事件的详细信息，用于StatusChanged事件传递数据
    /// </summary>
    internal class MouseHookEventArgs : EventArgs
    {
        /// <summary>
        /// 鼠标消息类型
        /// 标识当前触发的鼠标事件类型（如左键按下、移动等）
        /// </summary>
        public MouseHookMessageType MessageType { get; set; }

        /// <summary>
        /// 鼠标位置
        /// 事件发生时鼠标在屏幕上的坐标（POINT结构体）
        /// </summary>
        public InteropValues.POINT Point { get; set; }
    }

    /// <summary>
    /// 鼠标钩子消息类型枚举
    /// 定义低级鼠标钩子可捕获的鼠标事件类型，对应Windows消息常量
    /// </summary>
    public enum MouseHookMessageType
    {
        /// <summary>左键按下</summary>
        LeftButtonDown = 0x0201,
        /// <summary>左键释放</summary>
        LeftButtonUp = 0x0202,
        /// <summary>鼠标移动</summary>
        MouseMove = 0x0200,
        /// <summary>鼠标滚轮</summary>
        MouseWheel = 0x020A,
        /// <summary>右键按下</summary>
        RightButtonDown = 0x0204,
        /// <summary>右键释放</summary>
        RightButtonUp = 0x0205
    }

    /// <summary>
    /// 互操作数据结构与枚举定义类
    /// 包含与Windows API交互时所需的结构体、枚举和委托定义
    /// 对应Win32 API中的原生类型，确保P/Invoke调用正确
    /// </summary>
    internal class InteropValues
    {
        /// <summary>
        /// 点坐标结构体
        /// 表示屏幕上的一个点坐标（X,Y），对应Win32的POINT结构体
        /// </summary>
        internal struct POINT
        {
            /// <summary>X坐标</summary>
            public int X;
            /// <summary>Y坐标</summary>
            public int Y;

            /// <summary>初始化POINT结构体</summary>
            /// <param name="x">X坐标</param>
            /// <param name="y">Y坐标</param>
            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        /// <summary>
        /// 钩子回调函数委托
        /// 定义钩子事件处理函数的签名，与Windows API要求一致
        /// </summary>
        /// <param name="code">钩子代码</param>
        /// <param name="wParam">事件类型参数</param>
        /// <param name="lParam">事件详细信息指针</param>
        /// <returns>处理结果</returns>
        internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 外部DLL名称常量类
        /// 存储需要调用的Windows系统DLL名称，用于DllImport特性
        /// </summary>
        internal static class ExternDll
        {
            public const string
                User32 = "user32.dll",       // 包含鼠标钩子相关API
                Gdi32 = "gdi32.dll",         // 图形设备接口相关
                GdiPlus = "gdiplus.dll",     // GDI+相关
                Kernel32 = "kernel32.dll",   // 内核相关API
                Shell32 = "shell32.dll",     // 外壳相关API
                MsImg = "msimg32.dll",       // 图像相关API
                NTdll = "ntdll.dll",         // NT内核相关
                DwmApi = "dwmapi.dll";       // 桌面窗口管理器相关
        }

        /// <summary>
        /// 钩子类型枚举
        /// 定义Windows支持的钩子类型，此处使用低级键盘和鼠标钩子
        /// </summary>
        internal enum HookType
        {
            /// <summary>低级键盘钩子</summary>
            WH_KEYBOARD_LL = 13,
            /// <summary>低级鼠标钩子</summary>
            WH_MOUSE_LL = 14
        }

        /// <summary>
        /// 鼠标钩子结构体
        /// 包含鼠标事件的详细信息，对应Win32的MOUSEHOOKSTRUCT结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)] // 按顺序布局，与原生结构体一致
        internal struct MOUSEHOOKSTRUCT
        {
            /// <summary>鼠标坐标</summary>
            public POINT pt;
            /// <summary>鼠标事件关联的窗口句柄</summary>
            public IntPtr hwnd;
            /// <summary>命中测试代码（标识鼠标在窗口中的位置）</summary>
            public uint wHitTestCode;
            /// <summary>额外信息</summary>
            public IntPtr dwExtraInfo;
        }
    }

    /// <summary>
    /// 互操作方法类
    /// 声明与Windows API交互的P/Invoke方法
    /// 封装钩子安装、卸载、模块句柄获取等系统调用
    /// </summary>
    internal class InteropMethods
    {
        /// <summary>
        /// 安装钩子
        /// 调用Windows API设置钩子，返回钩子句柄
        /// </summary>
        /// <param name="idHook">钩子类型（如WH_MOUSE_LL）</param>
        /// <param name="lpfn">钩子回调函数</param>
        /// <param name="hMod">模块句柄</param>
        /// <param name="dwThreadId">监听的线程ID（0表示所有线程）</param>
        /// <returns>钩子句柄，失败时为0</returns>
        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, InteropValues.HookProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>
        /// 卸载钩子
        /// 调用Windows API移除已安装的钩子
        /// </summary>
        /// <param name="hhk">要卸载的钩子句柄</param>
        /// <returns>成功返回true，失败返回false</returns>
        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// 获取模块句柄
        /// 调用Windows API获取指定模块的句柄
        /// </summary>
        /// <param name="lpModuleName">模块名称（如进程主模块名）</param>
        /// <returns>模块句柄</returns>
        [DllImport(InteropValues.ExternDll.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// 传递钩子事件
        /// 将钩子事件传递给钩子链中的下一个钩子
        /// </summary>
        /// <param name="hhk">当前钩子句柄</param>
        /// <param name="nCode">钩子代码</param>
        /// <param name="wParam">事件类型参数</param>
        /// <param name="lParam">事件详细信息指针</param>
        /// <returns>下一个钩子的处理结果</returns>
        [DllImport(InteropValues.ExternDll.User32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    }
}