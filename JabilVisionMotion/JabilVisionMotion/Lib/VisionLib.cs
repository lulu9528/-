using Jabil.Module.Vision;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Drawing.Imaging;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Jabil.Lib.Parameter;
using Jabil.Module.Log.Lib;
using System.Collections.Concurrent;

namespace JabilVisionMotion.Lib
{
    /// <summary>
    /// 视觉处理核心库（单例模式）<br/>
    /// 提供线程安全的图像异步保存功能，通过生产者-消费者模式实现图像采集与保存的解耦<br/>
    /// 主要职责：
    /// - 管理图像保存线程的生命周期
    /// - 提供安全的图像入队接口
    /// - 统一资源释放与异常处理
    /// </summary>
    public class VisionLib : IDisposable
    {
        /// <summary>
        /// 私有构造函数，防止外部直接实例化（保证单例唯一性）<br/>
        /// 初始化图像保存器并启动保存线程
        /// </summary>
        private VisionLib()
        {
            try
            {
                // 初始化图像保存器实例
                _imageSaver = new ImageSaver();
                // 启动图像保存线程
                SaveImgThread_Start();
            }
            catch (Exception ex)
            {
                // 建议在实际环境中启用日志记录
                // RLog.SaveError(ex);
                throw new InvalidOperationException("Failed to initialize VisionLib", ex);
            }
        }

        /// <summary>单例实例</summary>
        private static VisionLib _instance;
        /// <summary>线程安全锁对象，用于单例初始化同步</summary>
        private static readonly object _lock = new object();
        /// <summary>资源释放状态标记</summary>
        private bool _isDisposed;
        /// <summary>图像保存器实例，负责实际的图像持久化工作</summary>
        private readonly ImageSaver _imageSaver;

        /// <summary>
        /// 初始化单例实例<br/>
        /// 线程安全的单例创建方式，确保全局唯一实例
        /// </summary>
        public static void Initialize()
        {
            // 双重检查锁定模式（double-checked locking），兼顾线程安全与性能
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new VisionLib();
                }
            }
        }

        /// <summary>
        /// 获取单例实例<br/>
        /// 需先调用Initialize()方法初始化，否则会抛出异常
        /// </summary>
        public static VisionLib Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("VisionLib has not been initialized. Call Initialize() first.");
                }
                return _instance;
            }
        }

        /// <summary>
        /// 启动图像保存线程<br/>
        /// 调用图像保存器的启动方法，开始处理队列中的图像
        /// </summary>
        public void SaveImgThread_Start()
        {
            CheckDisposed();
            _imageSaver.Start();
        }

        /// <summary>
        /// 停止图像保存线程<br/>
        /// 确保队列中剩余图像处理完成后再停止
        /// </summary>
        public void SaveImgThread_Stop()
        {
            CheckDisposed();
            _imageSaver.Stop();
        }

        /// <summary>
        /// 非阻塞方式将图像添加到保存队列<br/>
        /// 避免主线程因IO操作阻塞，提高系统响应性
        /// </summary>
        /// <param name="image">待保存的图像对象</param>
        /// <param name="name">保存的文件名</param>
        /// <param name="millisecondsTimeout">入队超时时间(毫秒)</param>
        /// <returns>成功入队返回true，超时返回false</returns>
        public bool TryEnqueueImage(Bitmap image, string name, int millisecondsTimeout = 100)
        {
            CheckDisposed();
            return _imageSaver.TryEnqueueImage(image, name, millisecondsTimeout);
        }

        /// <summary>
        /// 释放资源（实现IDisposable接口）
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 资源释放的核心实现<br/>
        /// 区分托管资源和非托管资源的释放逻辑
        /// </summary>
        /// <param name="disposing">true表示主动调用释放，false表示GC自动回收</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                // 释放托管资源
                _imageSaver.Dispose();
                lock (_lock)
                {
                    _instance = null;
                }
            }

            _isDisposed = true;
        }

        /// <summary>
        /// 析构函数<br/>
        /// 用于释放未被手动释放的资源
        /// </summary>
        ~VisionLib()
        {
            Dispose(false);
        }

        /// <summary>
        /// 检查对象是否已释放<br/>
        /// 防止对已释放对象进行操作
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(VisionLib));
        }
    }

    /// <summary>
    /// 图像保存器（实现IDisposable接口）<br/>
    /// 采用生产者-消费者模式，通过后台线程异步保存图像<br/>
    /// 核心特点：
    /// - 线程安全的队列管理
    /// - 自动处理队列中的图像保存
    /// - 优雅的线程停止与资源释放机制
    /// </summary>
    public class ImageSaver : IDisposable
    {
        /// <summary>
        /// 线程安全的图像队列<br/>
        /// 使用BlockingCollection包装ConcurrentQueue，提供阻塞式入队/出队操作<br/>
        /// 限制最大容量为1000，防止内存溢出
        /// </summary>
        private readonly BlockingCollection<(Bitmap Image, string Name)> _imageQueue =
            new BlockingCollection<(Bitmap, string)>(new ConcurrentQueue<(Bitmap, string)>(), 1000);

        /// <summary>取消令牌源，用于通知线程停止</summary>
        private CancellationTokenSource _cancellationTokenSource;
        /// <summary>图像保存后台任务</summary>
        private Task _savingTask;
        /// <summary>保存线程运行状态标记</summary>
        private bool _isRunning;
        /// <summary>资源释放状态标记</summary>
        private bool _isDisposed;

        /// <summary>
        /// 启动图像保存线程<br/>
        /// 初始化取消令牌并启动后台任务
        /// </summary>
        public void Start()
        {
            CheckDisposed();

            if (_isRunning) return;

            _cancellationTokenSource = new CancellationTokenSource();
            // 启动后台任务处理图像保存，传入取消令牌支持任务取消
            _savingTask = Task.Run(() => SaveImages(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            _isRunning = true;
        }

        /// <summary>
        /// 停止图像保存线程<br/>
        /// 确保处理完队列中剩余图像后再停止，避免数据丢失
        /// </summary>
        public void Stop()
        {
            CheckDisposed();

            if (!_isRunning) return;

            // 标记队列不再接受新元素
            _imageQueue.CompleteAdding();

            // 发送取消信号
            _cancellationTokenSource?.Cancel();

            // 等待任务完成，设置5秒超时防止无限等待
            if (_savingTask != null)
            {
                try
                {
                    _savingTask.Wait(5000, _cancellationTokenSource.Token);
                }
                catch (AggregateException ex)
                {
                    // 处理取消异常，忽略正常取消
                    ex.Handle(e => e is OperationCanceledException);
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，无需处理
                }
            }

            // 清理资源
            CleanupResources();
            _isRunning = false;
        }

        /// <summary>
        /// 非阻塞方式将图像添加到保存队列<br/>
        /// 带超时机制，防止入队操作无限阻塞
        /// </summary>
        /// <param name="image">待保存的图像</param>
        /// <param name="name">保存的文件名</param>
        /// <param name="millisecondsTimeout">超时时间(毫秒)</param>
        /// <returns>成功入队返回true，否则返回false</returns>
        public bool TryEnqueueImage(Bitmap image, string name, int millisecondsTimeout = 100)
        {
            CheckDisposed();

            if (!_isRunning)
                throw new InvalidOperationException("ImageSaver is not running. Call Start() first.");

            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Image name cannot be empty or whitespace.", nameof(name));

            // 尝试添加到队列，超时返回失败
            return _imageQueue.TryAdd((image, name), millisecondsTimeout);
        }

        /// <summary>
        /// 图像保存核心逻辑<br/>
        /// 在后台线程中循环处理队列中的图像，实现异步保存
        /// </summary>
        /// <param name="cancellationToken">取消令牌，用于接收停止信号</param>
        private void SaveImages(CancellationToken cancellationToken)
        {
            try
            {
                // GetConsumingEnumerable会自动阻塞等待新元素，直到队列完成添加或被取消
                foreach (var (image, name) in _imageQueue.GetConsumingEnumerable(cancellationToken))
                {
                    try
                    {
                        // 确保文件名以.bmp结尾
                        var fileName = name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                            ? name
                            : $"{name}.bmp";

                        // 使用using确保图像资源在保存后释放
                        using (image)
                        {
                            image.Save(fileName, ImageFormat.Bmp);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 建议记录保存失败的日志
                        // RLog.SaveError($"Failed to save image {name}: {ex.Message}", ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，无需处理
            }
            catch (Exception ex)
            {
                // 记录意外异常
                // RLog.SaveError($"Image saving thread failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 释放资源（实现IDisposable接口）
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 资源释放的核心实现
        /// </summary>
        /// <param name="disposing">true表示主动调用释放，false表示GC自动回收</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                // 释放托管资源
                Stop();
                _imageQueue.Dispose();
            }

            _isDisposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~ImageSaver()
        {
            Dispose(false);
        }

        /// <summary>
        /// 清理线程相关资源
        /// </summary>
        private void CleanupResources()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _savingTask = null;
        }

        /// <summary>
        /// 检查对象是否已释放
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ImageSaver));
        }
    }
}
