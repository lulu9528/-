using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jabil.Module.IO.Entity;
using Jabil.Module.Log.Lib;

namespace Jabil.Module.IO
{
    /// <summary>
    /// IO系统优化建议示例
    /// 展示如何更合理地使用不同类型的日志
    /// </summary>
    public class IOSystemOptimizationExample
    {
        #region IO卡管理优化示例

        /// <summary>
        /// 新增或更新IO卡 - 优化日志使用
        /// </summary>
        public async Task AddOrUpdateIOCardOptimizedAsync(IOCardDto dto, CancellationToken cancellationToken = default)
        {
            // 参数校验日志 - 使用Debug级别
            RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Gray,
                $"开始新增或更新IO卡，DTO ID: {dto?.Id ?? 0}, 名称: {dto?.卡名称 ?? "未知"}");

            if (dto == null)
            {
                // 错误日志 - 使用Error级别
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red, 
                    "IO卡数据不能为空", "ERR003");
                throw new ArgumentNullException(nameof(dto), "IO卡数据不能为空");
            }

            try
            {
                // 记录操作开始日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                    $"开始{(dto.Id == 0 ? "新增" : "更新")}IO卡: {dto.卡名称}", "OPR003");

                // 调用服务层执行新增/更新
                var result = await _ioCardService.AddOrUpdateAsync(dto, cancellationToken).ConfigureAwait(false);
                
                // 显示操作结果
                ShowMessage(result.Message);

                // 若操作成功，刷新内存缓存
                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                    
                    // 记录操作成功日志
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                        $"{(dto.Id == 0 ? "新增" : "更新")}IO卡成功: {dto.卡名称}", "OPR004");
                }
                else
                {
                    // 警告日志 - 使用Warn级别
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                        $"{(dto.Id == 0 ? "新增" : "更新")}IO卡失败: {dto.卡名称}, 原因: {result.Message}", "WRN001");
                }
            }
            catch (OperationCanceledException)
            {
                // 操作取消日志 - 使用Info级别
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue, 
                    $"{(dto.Id == 0 ? "新增" : "更新")}IO卡已取消: {dto.卡名称}", "OPR005");
                throw;
            }
            catch (Exception ex)
            {
                // 错误日志 - 记录异常详情
                RLog.SaveError(ex, $"{(dto.Id == 0 ? "新增" : "更新")}IO卡失败: {dto.卡名称}");
                throw;
            }
        }

        /// <summary>
        /// 批量删除IO卡 - 优化日志使用
        /// </summary>
        public async Task DeleteIOCardsOptimizedAsync(IEnumerable<IOCardDto> dtos = null, CancellationToken cancellationToken = default)
        {
            // 记录操作开始日志
            RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Gray,
                "开始批量删除IO卡");

            // 校验：若未选择数据，提示用户并返回
            if (dtos?.Any() != true)
            {
                // 警告日志 - 使用Warn级别
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                    "未选择要删除的IO卡", "WRN002");
                ShowMessage("请选择要删除的IO卡");
                return;
            }

            try
            {
                // 记录操作开始日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                    $"开始批量删除 {dtos.Count()} 个IO卡", "OPR006");

                // 调用服务层执行批量删除
                var result = await _ioCardService.DeleteAsync(dtos, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                // 若删除成功，刷新缓存
                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                    
                    // 记录操作成功日志
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                        $"批量删除 {dtos.Count()} 个IO卡成功", "OPR007");
                }
                else
                {
                    // 警告日志 - 使用Warn级别
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                        $"批量删除IO卡失败，原因: {result.Message}", "WRN003");
                }
            }
            catch (OperationCanceledException)
            {
                // 操作取消日志 - 使用Info级别
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                    "批量删除IO卡已取消", "OPR008");
                throw;
            }
            catch (Exception ex)
            {
                // 错误日志 - 记录异常详情
                RLog.SaveError(ex, "批量删除IO卡失败");
                throw;
            }
        }

        #endregion

        #region IO卡点位管理优化示例

        /// <summary>
        /// 新增或更新IO卡点位 - 优化日志使用
        /// </summary>
        public async Task AddOrUpdateIOCardDIOOptimizedAsync(IOCardDIODto dto, CancellationToken cancellationToken = default)
        {
            // 参数校验日志 - 使用Debug级别
            RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Gray,
                $"开始新增或更新IO卡点位，DTO ID: {dto?.Id ?? 0}, 点位名称: {dto?.点位名称 ?? "未知"}");

            if (dto == null)
            {
                // 错误日志 - 使用Error级别
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    "点位数据不能为空", "ERR004");
                throw new ArgumentNullException(nameof(dto), "点位数据不能为空");
            }

            try
            {
                // 记录操作开始日志
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                    $"开始{(dto.Id == 0 ? "新增" : "更新")}IO卡点位: {dto.点位名称}", "OPR009");

                var result = await _ioCardDIOService.AddOrUpdateAsync(dto, cancellationToken).ConfigureAwait(false);
                ShowMessage(result.Message);

                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                    
                    // 记录操作成功日志
                    RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Green,
                        $"{(dto.Id == 0 ? "新增" : "更新")}IO卡点位成功: {dto.点位名称}", "OPR010");
                }
                else
                {
                    // 警告日志 - 使用Warn级别
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                        $"{(dto.Id == 0 ? "新增" : "更新")}IO卡点位失败: {dto.点位名称}, 原因: {result.Message}", "WRN004");
                }
            }
            catch (OperationCanceledException)
            {
                // 操作取消日志 - 使用Info级别
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                    $"{(dto.Id == 0 ? "新增" : "更新")}IO卡点位已取消: {dto.点位名称}", "OPR011");
                throw;
            }
            catch (Exception ex)
            {
                // 错误日志 - 记录异常详情
                RLog.SaveError(ex, $"{(dto.Id == 0 ? "新增" : "更新")}IO卡点位失败: {dto.点位名称}");
                throw;
            }
        }

        #endregion

        #region 系统消息优化示例

        /// <summary>
        /// 显示系统消息 - 优化日志使用
        /// </summary>
        public void ShowMessageOptimized(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // 记录系统消息日志 - 使用Info级别
            RLog.SaveLog(LogType.System, LogLevel.Info, Color.Gray, message, "SYS002");
        }

        #endregion

        #region 辅助方法占位符

        // 以下为占位符，实际实现请参考原代码
        private IOCardService _ioCardService;
        private IOCardDIOService _ioCardDIOService;

        private async Task RefreshDataAsync() => await Task.CompletedTask;
        private void ShowMessage(string message) { }

        #endregion
    }

    // 占位符类
    public class IOCardDto 
    { 
        public int Id { get; set; } 
        public string 卡名称 { get; set; } 
    }
    
    public class IOCardDIODto 
    { 
        public int Id { get; set; } 
        public string 点位名称 { get; set; } 
    }
    
    public class IOCardService 
    {
        public async Task<OperationResult> AddOrUpdateAsync(IOCardDto dto, CancellationToken cancellationToken) => 
            await Task.FromResult(new OperationResult { Status = true, Message = "操作成功" });
            
        public async Task<OperationResult> DeleteAsync(IEnumerable<IOCardDto> dtos, CancellationToken cancellationToken) => 
            await Task.FromResult(new OperationResult { Status = true, Message = "删除成功" });
            
        public async Task<OperationResult> UpdateAsync(IOCardDto dto, CancellationToken cancellationToken) => 
            await Task.FromResult(new OperationResult { Status = true, Message = "更新成功" });
            
        public async Task<OperationResult> UpdateAsync(IEnumerable<IOCardDto> dtos, CancellationToken cancellationToken) => 
            await Task.FromResult(new OperationResult { Status = true, Message = "批量更新成功" });
    }
    
    public class IOCardDIOService 
    {
        public async Task<OperationResult> AddOrUpdateAsync(IOCardDIODto dto, CancellationToken cancellationToken) => 
            await Task.FromResult(new OperationResult { Status = true, Message = "操作成功" });
    }
    
    public class OperationResult 
    { 
        public bool Status { get; set; } 
        public string Message { get; set; } 
    }
}