using Jabil.Lib;
using System;
/// <summary>
/// 流程基类
/// 提供所有流程的通用属性和行为，是各类具体流程的抽象父类
/// 实现流程的基本生命周期管理（状态跟踪、标识管理等）
/// </summary>
public class ProcessBase : ICloneable
{
    /// <summary>
    /// 当前自动流程的运行状态
    /// </summary>
    private GeneralStatus _autoRunStatus = GeneralStatus.Wait;
    private readonly object _statusLock = new object(); // 只读字段，只能在构造函数中初始化

    public GeneralStatus CurrentStatus
    {
        get
        {
            lock (_statusLock)
            {
                return _autoRunStatus;
            }
        }
        set
        {
            lock (_statusLock)
            {
                _autoRunStatus = value;
            }
        }
    }

    /// <summary>
    /// 流程执行过程中的消息/日志信息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 流程的名称
    /// </summary>
    public string ProcessName { get; protected set; }

    /// <summary>
    /// 流程的唯一标识符
    /// </summary>
    public string ProcessId { get; protected set; }

    /// <summary>
    /// 流程的类型
    /// </summary>
    public string ProcessType { get; protected set; }

    /// <summary>
    /// 初始化流程基类实例
    /// </summary>
    public ProcessBase(string processName, string processType)
    {
        ProcessName = processName ?? throw new ArgumentNullException(nameof(processName), "流程名称不能为空");
        ProcessType = processType ?? throw new ArgumentNullException(nameof(processType), "流程类型不能为空");

        CurrentStatus = GeneralStatus.Wait;
        ProcessId = "未指定";
        Message = string.Empty;
    }

    /// <summary>
    /// 启动流程执行
    /// </summary>
    public virtual void Run()
    {
        if (CurrentStatus == GeneralStatus.Wait)
        {
            CurrentStatus = GeneralStatus.Running;
            Message = $"流程 [{ProcessName}] 已启动";
        }
        else
        {
            Message = $"流程 [{ProcessName}] 无法启动，当前状态：{CurrentStatus}";
        }
    }

    /// <summary>
    /// 标记流程为完成状态
    /// </summary>
    public virtual void Complete()
    {
        if (CurrentStatus == GeneralStatus.Running)
        {
            CurrentStatus = GeneralStatus.NoError;
            Message = $"流程 [{ProcessName}] 已完成";
        }
        else
        {
            Message = $"流程 [{ProcessName}] 无法标记为完成，当前状态：{CurrentStatus}";
        }
    }

    /// <summary>
    /// 标记流程为失败状态
    /// </summary>
    public virtual void Fail(string errorMessage)
    {
        if (CurrentStatus != GeneralStatus.NoError)
        {
            CurrentStatus = GeneralStatus.Error;
            Message = $"流程 [{ProcessName}] 执行失败：{errorMessage}";
        }
        else
        {
            Message = $"流程 [{ProcessName}] 已完成，无法标记为失败";
        }
    }

    /// <summary>
    /// 克隆流程实例（修正版）
    /// </summary>
    public virtual object Clone()
    {
        // 使用反射创建新实例，绕过只读字段的限制
        var clone = (ProcessBase)Activator.CreateInstance(GetType(), true);

        // 复制所有属性值
        clone._autoRunStatus = this._autoRunStatus;
        clone.ProcessName = this.ProcessName;
        clone.ProcessId = this.ProcessId;
        clone.ProcessType = this.ProcessType;
        clone.Message = string.Copy(this.Message);

        return clone;
    }
}

/// <summary>
/// 自定义流程类
/// </summary>
public class CustomProcess : ProcessBase
{
    public CustomProcess(string processName, string processType)
        : base(processName, processType)
    {
    }

    public override void Run()
    {
        base.Run();
        if (CurrentStatus == GeneralStatus.Running)
        {
            // 自定义执行逻辑
        }
    }

    public override object Clone()
    {
        return (CustomProcess)base.Clone();
    }
}
