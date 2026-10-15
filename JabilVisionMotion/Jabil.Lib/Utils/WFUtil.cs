using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Lib
{
    /// <summary>
    /// Windows Forms UI操作工具类
    /// 提供跨线程安全更新UI控件的方法，解决多线程操作UI的线程安全问题
    /// </summary>
    public static class WFUtil
    {
        /// <summary>
        /// 以线程安全的方式异步更新UI控件
        /// 不会阻塞当前线程，通过Task异步等待UI操作完成
        /// </summary>
        /// <param name="control">需要操作的UI控件</param>
        /// <param name="action">要在UI线程执行的操作</param>
        /// <returns>表示异步操作的Task</returns>
        public static Task UpdateMainThreadUIAsync(this Control control, Action action, CancellationToken cancellationToken = default)
        {
           
            if (control == null)
                throw new ArgumentNullException(nameof(control), "UI控件不能为null");
            if (action == null)
                throw new ArgumentNullException(nameof(action), "要执行的操作委托不能为null");
            if (control.IsDisposed)
                throw new ObjectDisposedException(nameof(control), "控件已被释放，无法执行UI操作");

            // 如果已取消，直接返回已取消的任务
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            // 如果在UI线程，直接执行并返回已完成的任务
            //当当前线程是创建控件的线程时，InvokeRequired 返回 false
            if (!control.InvokeRequired)
            {
                action();
                return Task.CompletedTask;
            }

            // 非UI线程，使用BeginInvoke进行异步调用
            return Task.Factory.FromAsync(
                control.BeginInvoke(action),
                result =>
                {
                    try
                    {
                        control.EndInvoke(result);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("异步UI操作执行失败", ex);
                    }
                }
            ).ContinueWith(task =>
            {
                // 处理取消请求
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException("UI操作已被取消", cancellationToken);

                // 传播可能的异常
                if (task.Exception != null)
                    throw task.Exception;
            }, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        /// <summary>
        /// 以线程安全的方式异步更新UI控件并返回结果
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="control">需要操作的UI控件</param>
        /// <param name="func">要在UI线程执行的函数</param>
        /// <returns>包含执行结果的Task</returns>
        public static Task<T> UpdateMainThreadUIAsync<T>(this Control control, Func<T> func, CancellationToken cancellationToken = default)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            if (control.IsDisposed)
                throw new ObjectDisposedException(nameof(control));

            // 如果已取消，直接返回已取消的任务
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<T>(cancellationToken);

            // 如果在UI线程，直接执行并返回结果
            if (!control.InvokeRequired)
            {
                return Task.FromResult(func());
            }

            // 非UI线程，使用BeginInvoke进行异步调用
            return Task.Factory.FromAsync<T>(
                control.BeginInvoke(func),
                result =>
                {
                    try
                    {
                        return (T)control.EndInvoke(result);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("异步UI操作执行失败", ex);
                    }
                }
            ).ContinueWith(task =>
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException("UI操作已被取消", cancellationToken);

                if (task.Exception != null)
                    throw task.Exception;

                return task.Result;
            }, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
