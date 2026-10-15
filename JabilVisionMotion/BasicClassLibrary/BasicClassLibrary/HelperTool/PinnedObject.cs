using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// Object pinning class 
    /// 在C#中，pinned object是一个具有固定地址的对象，
    /// 它不会被垃圾回收器移动。这通常用于与非托管代码交互时，
    /// 因为非托管代码需要持续引用同一内存区域
    /// 
    /// 在调用Alloc方法固定对象后，你可以通过GCHandle.ToIntPtr(handle)获取该对象的固定地址。然后，你可以将这个地址传递给需要使用该对象的非托管代码。
    ///请注意，固定对象应该谨慎使用，因为它会阻止对象被移动，这可能会影响到垃圾回收器的性能。
    ///只有当你需要与非托管代码交互时，并且非托管代码需要引用对象的固定内存地址时，才应该使用固定对象
    /// </summary>
    public sealed class PinnedObject : IDisposable
    {
        private GCHandle _handle;

        /// <summary>
        /// Constructor （来固定对象，并且获取地址
        /// </summary>
        /// <param name="target">Target to protect from the garbage collector </param>
        public PinnedObject(object target)
        {
            //为指定的对象分配句柄
            this._handle = GCHandle.Alloc(target, GCHandleType.Pinned);
        }

        /// <summary>
        /// Interface 不需要的时候手动释放
        /// </summary>
        public void Dispose()
        {
            this._handle.Free();
            this._handle = new GCHandle();
        }

        /// <summary>
        /// Get the address.(获取对象地址)
        /// </summary>
        public IntPtr Pointer
        {
            get
            {
                return this._handle.AddrOfPinnedObject();
            }
        }
    }
}
