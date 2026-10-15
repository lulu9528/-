using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// 泛型委托命令类
    /// 继承自DelegateCommandBase，实现带参数的命令模式
    /// 用于MVVM模式中绑定UI元素（如按钮）与业务逻辑方法
    /// </summary>
    /// <typeparam name="T">命令参数类型</typeparam>
    public class DelegateCommand<T> : DelegateCommandBase
    {
        /// <summary>
        /// 命令执行方法
        /// 存储具体的业务逻辑执行委托
        /// </summary>
        private readonly Action<T> _executeMethod;

        /// <summary>
        /// 命令可执行判断方法
        /// 存储判断命令是否可执行的委托
        /// </summary>
        private Func<T, bool> _canExecuteMethod;

        /// <summary>
        /// 构造函数（仅指定执行方法）
        /// 初始化命令时仅提供执行逻辑，默认命令始终可执行
        /// </summary>
        /// <param name="executeMethod">命令执行的委托方法</param>
        public DelegateCommand(Action<T> executeMethod)
            : this(executeMethod, (Func<T, bool>)((T o) => true))
        {
        }

        /// <summary>
        /// 构造函数（指定执行方法和可执行判断方法）
        /// 初始化命令时同时提供执行逻辑和可执行判断逻辑
        /// </summary>
        /// <param name="executeMethod">命令执行的委托方法</param>
        /// <param name="canExecuteMethod">判断命令是否可执行的委托方法</param>
        /// <exception cref="Exception">当执行方法或可执行方法为null时抛出</exception>
        /// <exception cref="InvalidCastException">当参数类型为非可空值类型时抛出</exception>
        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            // 验证委托方法不为null
            if (executeMethod == null || canExecuteMethod == null)
            {
                throw new Exception("无效参数");
            }

            // 验证参数类型（不支持非可空值类型，避免绑定时空值转换问题）
            TypeInfo typeInfo = typeof(T).GetTypeInfo();
            if (typeInfo.IsValueType && (!typeInfo.IsGenericType || !typeof(Nullable<>).GetTypeInfo().IsAssignableFrom(typeInfo.GetGenericTypeDefinition().GetTypeInfo())))
            {
                throw new InvalidCastException("参数类型无效");
            }

            _executeMethod = executeMethod;
            _canExecuteMethod = canExecuteMethod;
        }

        /// <summary>
        /// 执行命令
        /// 调用构造函数中指定的执行方法
        /// </summary>
        /// <param name="parameter">命令参数（类型为T）</param>
        public void Execute(T parameter)
        {
            _executeMethod(parameter);
        }

        /// <summary>
        /// 判断命令是否可执行
        /// 调用构造函数中指定的可执行判断方法
        /// </summary>
        /// <param name="parameter">命令参数（类型为T）</param>
        /// <returns>true表示命令可执行，false表示不可执行</returns>
        public bool CanExecute(T parameter)
        {
            return _canExecuteMethod(parameter);
        }

        /// <summary>
        /// 重写基类执行方法
        /// 实现ICommand接口的Execute方法，处理参数类型转换
        /// </summary>
        /// <param name="parameter">原始参数（将被转换为T类型）</param>
        protected override void Execute(object parameter)
        {
            Execute((T)parameter);
        }

        /// <summary>
        /// 重写基类可执行判断方法
        /// 实现ICommand接口的CanExecute方法，处理参数类型转换
        /// </summary>
        /// <param name="parameter">原始参数（将被转换为T类型）</param>
        /// <returns>true表示命令可执行，false表示不可执行</returns>
        protected override bool CanExecute(object parameter)
        {
            return CanExecute((T)parameter);
        }
    }

    /// <summary>
    /// 委托命令基类（抽象）
    /// 实现ICommand和IActiveAware接口，提供命令的基础功能
    /// 作为所有具体命令类的基类，封装公共逻辑
    /// </summary>
    public abstract class DelegateCommandBase : ICommand, IActiveAware
    {
        /// <summary>
        /// 激活状态标识
        /// 指示当前命令是否处于激活状态
        /// </summary>
        private bool _isActive;

        /// <summary>
        /// 同步上下文
        /// 存储命令创建时的线程上下文，用于线程安全的事件触发
        /// </summary>
        private readonly SynchronizationContext _synchronizationContext;

        /// <summary>
        /// 观察的属性表达式集合
        /// 存储需要监控的属性，用于自动更新命令可执行状态
        /// </summary>
        private readonly HashSet<string> _observedPropertiesExpressions = new HashSet<string>();

        /// <summary>
        /// 激活状态属性
        /// 实现IActiveAware接口，获取或设置命令是否激活
        /// 状态变化时触发IsActiveChanged事件
        /// </summary>
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnIsActiveChanged();
                }
            }
        }

        /// <summary>
        /// 可执行状态变化事件
        /// 实现ICommand接口，当命令可执行状态变化时触发
        /// UI元素（如按钮）会监听此事件以更新自身状态（启用/禁用）
        /// </summary>
        public virtual event EventHandler CanExecuteChanged;

        /// <summary>
        /// 激活状态变化事件
        /// 实现IActiveAware接口，当命令激活状态变化时触发
        /// </summary>
        public virtual event EventHandler IsActiveChanged;

        /// <summary>
        /// 构造函数
        /// 初始化同步上下文（通常为UI线程上下文）
        /// </summary>
        protected DelegateCommandBase()
        {
            _synchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// 触发可执行状态变化事件
        /// 确保在正确的线程（通常是UI线程）上触发事件
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChanged;
            if (handler == null)
            {
                return;
            }

            // 跨线程安全触发事件
            if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
            {
                _synchronizationContext.Post(delegate
                {
                    handler(this, EventArgs.Empty);
                }, null);
            }
            else
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 手动触发可执行状态变化
        /// 供外部调用，强制UI更新命令状态
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        /// 实现ICommand接口的Execute方法
        /// 委托给抽象的Execute方法由子类实现
        /// </summary>
        /// <param name="parameter">命令参数</param>
        void ICommand.Execute(object parameter)
        {
            Execute(parameter);
        }

        /// <summary>
        /// 实现ICommand接口的CanExecute方法
        /// 委托给抽象的CanExecute方法由子类实现
        /// </summary>
        /// <param name="parameter">命令参数</param>
        /// <returns>true表示命令可执行，false表示不可执行</returns>
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute(parameter);
        }

        /// <summary>
        /// 抽象执行方法
        /// 由子类实现具体的命令执行逻辑
        /// </summary>
        /// <param name="parameter">命令参数</param>
        protected abstract void Execute(object parameter);

        /// <summary>
        /// 抽象可执行判断方法
        /// 由子类实现具体的可执行判断逻辑
        /// </summary>
        /// <param name="parameter">命令参数</param>
        /// <returns>true表示命令可执行，false表示不可执行</returns>
        protected abstract bool CanExecute(object parameter);

        /// <summary>
        /// 触发激活状态变化事件
        /// 当IsActive属性变化时调用
        /// </summary>
        protected virtual void OnIsActiveChanged()
        {
            this.IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 激活状态感知接口
    /// 定义对象激活状态的属性和变化事件
    /// 用于跟踪命令或视图模型的激活状态
    /// </summary>
    public interface IActiveAware
    {
        /// <summary>
        /// 激活状态属性
        /// true表示对象处于激活状态，false表示未激活
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// 激活状态变化事件
        /// 当IsActive属性变化时触发
        /// </summary>
        event EventHandler IsActiveChanged;
    }

    /// <summary>
    /// 异步命令接口
    /// 扩展ICommand接口，支持异步执行命令
    /// </summary>
    public interface IAsyncCommand : ICommand
    {
        /// <summary>
        /// 异步执行命令
        /// </summary>
        /// <param name="parameter">命令参数</param>
        /// <returns>任务对象</returns>
        Task ExecuteAsync(object parameter);

        /// <summary>
        /// 取消令牌源
        /// 用于取消异步操作
        /// </summary>
        CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// 运行状态标识
        /// true表示命令正在执行，false表示未执行
        /// </summary>
        bool IsRunning { get; }
    }

    /// <summary>
    /// 异步命令类
    /// 实现IAsyncCommand和INotifyPropertyChanged接口
    /// 支持异步执行长时间运行的操作，不阻塞UI线程
    /// </summary>
    public class AsyncCommand : IAsyncCommand, INotifyPropertyChanged
    {
        /// <summary>
        /// 异步执行方法
        /// 存储具体的异步业务逻辑委托
        /// </summary>
        private readonly Func<object, Task> _excute;

        /// <summary>
        /// 可执行判断方法
        /// 存储判断命令是否可执行的委托
        /// </summary>
        private readonly Func<object, bool> _canExcute;

        /// <summary>
        /// 取消令牌源
        /// 用于取消当前正在执行的异步操作
        /// </summary>
        private CancellationTokenSource _cts;

        /// <summary>
        /// 运行状态标识
        /// 指示命令是否正在执行
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// 构造函数
        /// 初始化异步执行方法和可执行判断方法
        /// </summary>
        /// <param name="excute">异步执行的委托方法</param>
        /// <param name="canExcute">判断命令是否可执行的委托方法（可为null）</param>
        public AsyncCommand(Func<object, Task> excute, Func<object, bool> canExcute = null)
        {
            _excute = excute;
            _canExcute = canExcute;
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// 取消令牌源属性
        /// 提供外部访问以取消异步操作
        /// </summary>
        public CancellationTokenSource CancellationTokenSource => _cts;

        /// <summary>
        /// 运行状态属性
        /// 实现INotifyPropertyChanged，状态变化时通知UI
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;
                // 通知属性变化
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            }
        }

        /// <summary>
        /// 可执行状态变化事件
        /// 实现ICommand接口，当命令可执行状态变化时触发
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// 属性变化事件
        /// 实现INotifyPropertyChanged，当属性变化时触发
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 判断命令是否可执行
        /// 实现ICommand接口，调用构造函数中指定的可执行判断方法
        /// </summary>
        /// <param name="parameter">命令参数</param>
        /// <returns>true表示命令可执行，false表示不可执行</returns>
        public bool CanExecute(object parameter)
        {
            return _canExcute == null || _canExcute(parameter);
        }

        /// <summary>
        /// 执行命令（同步入口）
        /// 实现ICommand接口，调用异步执行方法并忽略返回的任务
        /// </summary>
        /// <param name="parameter">命令参数</param>
        public void Execute(object parameter)
        {
            ExecuteAsync(parameter).ConfigureAwait(false);
        }

        /// <summary>
        /// 异步执行命令
        /// 实现IAsyncCommand接口，处理异步执行逻辑
        /// </summary>
        /// <param name="parameter">命令参数</param>
        /// <returns>任务对象</returns>
        public async Task ExecuteAsync(object parameter)
        {
            // 检查命令是否可执行且未在运行中
            if (!CanExecute(parameter) || IsRunning) return;
            try
            {
                // 更新运行状态
                IsRunning = true;
                // 通知可执行状态变化（通常用于禁用UI元素）
                OnCanExecuteChanged();
                // 重置取消令牌源
                _cts = new CancellationTokenSource();
                // 执行异步业务逻辑
                await _excute(parameter);
            }
            catch (Exception)
            {
                // 异常处理（可根据需要扩展）
            }
            finally
            {
                // 恢复运行状态
                IsRunning = false;
                // 释放取消令牌源
                _cts.Dispose();
                // 通知可执行状态变化（通常用于启用UI元素）
                OnCanExecuteChanged();
            }
        }

        /// <summary>
        /// 触发可执行状态变化事件
        /// 通知UI更新命令关联的元素状态
        /// </summary>
        protected void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}