using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 安全缓存扩展类
    /// </summary>
    public class SafeBufferExt : SafeBuffer
    {
        /// <summary>
        /// 构造函数 创建 SafeBuffer 类的新实例，并指定是否可靠地释放缓冲区句柄。
        /// </summary>
        /// <param name="size"></param>
        public SafeBufferExt(int size) : base(true)
        {
            base.SetHandle(Marshal.AllocHGlobal(size));
            base.Initialize((ulong)size);
        }

        /// <summary>
        /// 转换为IntPtr
        /// </summary>
        /// <param name="SB"></param>
        public static implicit operator IntPtr(SafeBufferExt SB)
        {
            return SB.handle;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <returns></returns>
        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(base.handle);
            return true;
        }
    }
}
