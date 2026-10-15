﻿using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.IO.Common.Dtos;
using Jabil.Module.IO.Entity;
using Jabil.Module.IO.Service;
using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.IO
{
    /// <summary>
    /// IO系统管理核心类（单例模式）
    /// 核心职责：
    /// 1. 作为IO卡和点位数据的统一管理入口，封装CRUD操作
    /// 2. 维护内存缓存，减少数据库访问，提升UI响应速度
    /// 3. 协调服务层与UI层的数据交互，处理线程安全和异步操作
    /// 设计模式：单例模式（确保全局数据一致性）+ 观察者模式（属性变更通知）
    /// </summary>
    public class IOSystem : BindableBase // 继承BindableBase以支持UI数据绑定（属性变更通知）
    {
        #region 单例模式实现（线程安全）
        /// <summary>
        /// 懒加载单例实例
        /// 特性：
        /// - 线程安全：Lazy<T>默认使用ExecutionAndPublication模式，多线程环境下仅初始化一次
        /// - 延迟初始化：首次访问Instance时才创建对象，减少应用启动时间
        /// </summary>
        private static readonly Lazy<IOSystem> _lazyInstance = new Lazy<IOSystem>(() => new IOSystem());

        /// <summary>
        /// 全局唯一访问点
        /// 所有外部调用通过IOSystem.Instance访问，确保系统中只有一个IO管理实例
        /// </summary>
        public static IOSystem Instance => _lazyInstance.Value;

        /// <summary>
        /// 私有构造函数
        /// 禁止外部通过new创建实例，强制通过单例入口访问，保证数据一致性
        /// </summary>
        private IOSystem()
        {
            try
            {
                // 初始化服务实例（实际项目中可替换为依赖注入）
                _ioCardService = new IOCardService();
                _ioCardDIOService = new IOCardDIOService();

                // 初始化内存缓存集合（避免null引用异常）
                _ioCards = new List<IOCardDto>();
                _ioCardDios = new List<IOCardDIODto>();

                // 初始化信号量（控制并发刷新，确保同时只有一个刷新操作）
                _refreshSemaphore = new SemaphoreSlim(1, 1);
            }
            catch (Exception ex)
            {
                // 捕获初始化异常并记录，避免程序崩溃（单例初始化失败会影响整个模块）
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 私有字段与公共属性
        /// <summary>IO卡服务实例，封装IO卡的数据库操作</summary>
        private readonly IOCardService _ioCardService;

        /// <summary>IO卡点位服务实例，封装点位的数据库操作</summary>
        private readonly IOCardDIOService _ioCardDIOService;

        /// <summary>
        /// 刷新操作的信号量（SemaphoreSlim）
        /// 作用：控制RefreshDataAsync方法的并发执行，避免多线程同时刷新导致的数据冲突
        /// 初始化为1，表示同时只允许1个线程执行刷新
        /// </summary>
        private readonly SemaphoreSlim _refreshSemaphore;

        /// <summary>IO卡数据内存缓存（私有字段，不直接暴露给外部）</summary>
        private List<IOCardDto> _ioCards;

        /// <summary>
        /// IO卡列表（公共属性，支持UI数据绑定）
        /// 特性：
        /// - 对外只读（get公开，set私有），确保数据只能通过内部方法更新
        /// - 变更时自动触发PropertyChanged事件，通知UI控件刷新
        /// </summary>
        public List<IOCardDto> IOCards
        {
            get => _ioCards;
            private set
            {
                _ioCards = value;
                RaisePropertyChanged(); // 触发属性变更通知（UI绑定的基础）
            }
        }

        /// <summary>IO卡点位数据内存缓存（私有字段）</summary>
        private List<IOCardDIODto> _ioCardDios;

        /// <summary>
        /// IO卡点位列表（公共属性，支持UI数据绑定）
        /// 特性同IOCards属性
        /// </summary>
        public List<IOCardDIODto> IOCardDios
        {
            get => _ioCardDios;
            private set
            {
                _ioCardDios = value;
                RaisePropertyChanged(); // 通知UI刷新
            }
        }

        /// <summary>是否正在刷新数据（私有字段）</summary>
        private bool _isRefreshing;

        /// <summary>
        /// 是否正在刷新数据（公共属性）
        /// 用途：UI层可绑定此属性，显示加载动画或禁用操作按钮，提升用户体验
        /// </summary>
        public bool IsRefreshing
        {
            get => _isRefreshing;
            private set
            {
                _isRefreshing = value;
                RaisePropertyChanged(); // 状态变更时通知UI
            }
        }
        #endregion

        #region 核心数据刷新方法
        /// <summary>
        /// 异步刷新所有IO数据（IO卡+点位）
        /// 核心功能：从数据库加载最新数据到内存缓存，同步UI显示
        /// </summary>
        /// <param name="cancellationToken">取消令牌，支持在操作未完成时终止（如用户取消）</param>
        public async Task RefreshDataAsync()
        {
            try
            {
                // 并行发起设备和DIO点位的数据加载请求
                // 同时执行两个异步操作，减少总体等待时间
                var deviceTask = _ioCardService.GetAllAsync();
                var dioTask = _ioCardDIOService.GetAllAsync();

                // 等待两个异步操作都完成
                await Task.WhenAll(deviceTask, dioTask);

                // 处理设备数据结果
                var deviceResult = await deviceTask;
                IOCards = deviceResult?.Status == true
                    // 操作成功时使用返回结果，若结果为null则初始化空集合
                    ? deviceResult.Result ?? new List<IOCardDto>()
                    // 操作失败时使用空集合
                    : new List<IOCardDto>();

                // 处理DIO点位数据结果
                var dioResult = await dioTask;
                IOCardDios = dioResult?.Status == true
                    // 操作成功时使用返回结果，若结果为null则初始化空集合
                    ? dioResult.Result ?? new List<IOCardDIODto>()
                    // 操作失败时使用空集合
                    : new List<IOCardDIODto>();
            }
            catch (Exception ex)
            {
                // 记录数据刷新过程中的异常
                RLog.SaveError(ex);
                // 重新抛出异常，允许调用者捕获并处理（如显示错误提示）
                throw;
            }
        }

        /// <summary>
        /// 显示操作消息（统一消息处理入口）
        /// 设计目的：
        /// - 集中管理操作结果提示，便于后续统一修改为UI弹窗、状态栏提示等
        /// - 过滤空消息，避免无效日志
        /// </summary>
        /// <param name="message">消息内容（如"新增成功"、"删除失败"等）</param>
        public void ShowMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return; // 过滤空消息或纯空白消息

            // 当前实现为记录日志，实际项目可扩展为：
            // 1. 调用UI层弹窗（如MessageBox.Show）
            // 2. 更新状态栏文本
            // 3. 结合消息类型（成功/警告/错误）显示不同样式
            RLog.SaveLog(LogType.System, LogLevel.Info, System.Drawing.Color.Gray, message);
        }
        #endregion

        #region IO卡管理（增删改查）
        /// <summary>
        /// 新增或更新IO卡（自动判断操作类型）
        /// 逻辑：根据DTO中的ID判断（ID=0则新增，ID>0则更新）
        /// </summary>
        /// <param name="dto">IO卡数据传输对象（包含新增/更新的字段）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <exception cref="ArgumentNullException">当dto为null时抛出（防止无效操作）</exception>
        public async Task AddOrUpdateIOCardAsync(IOCardDto dto, CancellationToken cancellationToken = default)
        {
            // 参数校验：防止空对象传入（避免后续映射和数据库操作异常）
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "IO卡数据不能为空");

            try
            {
                // 调用服务层执行新增/更新（异步等待操作完成）
                var result = await _ioCardService.AddOrUpdateAsync(dto, cancellationToken).ConfigureAwait(false);
                // 显示操作结果（成功/失败提示）
                ShowMessage(result.Message);

                // 若操作成功，刷新内存缓存（确保UI显示最新数据）
                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // 记录取消日志，重新抛出供上层处理
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡新增/更新已取消");
                throw;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw; // 抛出异常，允许UI层捕获并显示错误
            }
        }

        /// <summary>
        /// 批量删除IO卡
        /// </summary>
        /// <param name="dtos">要删除的IO卡列表（可为null，内部会校验）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task DeleteIOCardsAsync(IEnumerable<IOCardDto> dtos = null, CancellationToken cancellationToken = default)
        {
            // 校验：若未选择数据，提示用户并返回（避免无效数据库操作）
            if (dtos?.Any() != true)
            {
                ShowMessage("请选择要删除的IO卡");
                return;
            }

            try
            {
                // 调用服务层执行批量删除
                var result = await _ioCardService.DeleteAsync(dtos, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                // 若删除成功，刷新缓存
                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡删除已取消");
                throw;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 单条更新IO卡（仅更新已有记录，不支持新增）
        /// 与AddOrUpdateIOCardAsync的区别：此方法强制要求DTO包含有效ID
        /// </summary>
        /// <param name="dto">要更新的IO卡数据（必须包含有效ID）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task UpdateIOCardAsync(IOCardDto dto, CancellationToken cancellationToken = default)
        {
            // 校验：若对象为空，提示用户（不执行数据库操作）
            if (dto == null)
            {
                ShowMessage("请选择要更新的IO卡");
                return;
            }

            try
            {
                // 调用服务层执行单条更新
                var result = await _ioCardService.UpdateAsync(dto, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                // 若更新成功，刷新缓存（确保UI同步）
                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡更新已取消");
                throw;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 批量更新IO卡（仅更新已有记录）
        /// 用途：一次性修改多条IO卡信息（如批量修改卡描述）
        /// </summary>
        /// <param name="dtos">要更新的IO卡列表（必须包含有效ID）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task UpdateIOCardsAsync(IEnumerable<IOCardDto> dtos = null, CancellationToken cancellationToken = default)
        {
            // 校验：若无有效数据，提示用户
            if (dtos?.Any() != true)
            {
                ShowMessage("请选择要更新的IO卡");
                return;
            }

            try
            {
                // 调用服务层执行批量更新
                var result = await _ioCardService.UpdateAsync(dtos, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                // 若更新成功，刷新缓存
                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "IO卡批量更新已取消");
                throw;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }
        #endregion

        #region IO卡点位管理（增删改查）
        /// <summary>
        /// 新增或更新IO卡点位（自动判断操作类型）
        /// 逻辑：ID=0则新增，ID>0则更新
        /// </summary>
        /// <param name="dto">点位数据传输对象</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task AddOrUpdateIOCardDIOAsync(IOCardDIODto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "点位数据不能为空");

            try
            {
                var result = await _ioCardDIOService.AddOrUpdateAsync(dto, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位新增/更新已取消");
                throw;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 批量删除IO卡点位
        /// </summary>
        /// <param name="dtos">要删除的点位列表（可为null）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task DeleteIOCardDIOsAsync(IEnumerable<IOCardDIODto> dtos = null, CancellationToken cancellationToken = default)
        {
            if (dtos?.Any() != true)
            {
                ShowMessage("请选择要删除的点位");
                return;
            }

            try
            {
                var result = await _ioCardDIOService.DeleteAsync(dtos, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位删除已取消");
                throw;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 单条更新IO卡点位（仅更新已有记录）
        /// </summary>
        /// <param name="dto">要更新的点位数据（必须包含有效ID）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task UpdateIOCardDIOAsync(IOCardDIODto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                ShowMessage("请选择要更新的点位");
                return;
            }

            try
            {
                var result = await _ioCardDIOService.UpdateAsync(dto, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位更新已取消");
                throw;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 批量更新IO卡点位（仅更新已有记录）
        /// </summary>
        /// <param name="dtos">要更新的点位列表（必须包含有效ID）</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task UpdateIOCardDIOsAsync(IEnumerable<IOCardDIODto> dtos = null, CancellationToken cancellationToken = default)
        {
            if (dtos?.Any() != true)
            {
                ShowMessage("请选择要更新的点位");
                return;
            }

            try
            {
                var result = await _ioCardDIOService.UpdateAsync(dtos, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, System.Drawing.Color.Blue, "点位批量更新已取消");
                throw;
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }
        #endregion
    }

    /// <summary>
    /// IO卡实时状态数据类（用于实时监控）
    /// 设计目的：存储IO卡的实时输入输出状态，供UI层显示当前设备状态
    /// 数据更新源：通常由硬件通信模块（如IO驱动）定时更新
    /// </summary>
    public class IoCardStatus
    {
        /// <summary>
        /// IO卡名称（与IOCardDto.CardName对应）
        /// 作用：关联IO卡配置信息与实时状态
        /// </summary>
        public string CardName { get; set; }

        /// <summary>
        /// 输入点（DI）状态列表
        /// 索引：对应点位的PointIndex（物理点位编号）
        /// 值：true表示导通，false表示断开
        /// </summary>
        public List<bool> DiStatus { get; set; } = new List<bool>();

        /// <summary>
        /// 输出点（DO）状态列表
        /// 索引与值的含义同DiStatus
        /// </summary>
        public List<bool> DoStatus { get; set; } = new List<bool>();

        /// <summary>
        /// 最后更新时间
        /// 作用：标记数据的新鲜度，UI层可据此判断是否通信异常（如长时间未更新）
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}