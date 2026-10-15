using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Motion.Entity;
using Jabil.Module.Motion.Lib;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jabil.Module.Motion.Service;

namespace Jabil.Module.Motion
{
    /// <summary>
    /// 运动控制系统核心类
    /// 提供单例访问，统一管理运动轴、轴卡和点位的操作
    /// </summary>
    public class MotionSystem : BindableBase
    {
        #region 单例模式（线程安全实现）
        private static readonly Lazy<MotionSystem> _lazyInstance = new Lazy<MotionSystem>(() => new MotionSystem());
        public static MotionSystem Instance => _lazyInstance.Value;

        private MotionSystem()
        {
            try
            {
                DefaultMotionMode = MotionMode.LingChen;
                _motionCardService = new MotionCardService();
                _motionAxisService = new MotionAxisService();
                _motionPointService = new MotionPointService();

                MotionController = DefaultMotionMode switch
                {
                    MotionMode.LingChen => new EcatMotion(),
                    _ => throw new NotImplementedException("未实现的运动控制器类型")
                };
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 字段与属性
        private List<MotionCardDto> _motionCards;
        private List<MotionAxisDto> _motionAxes;
        private List<MotionPointDto> _motionPoints;

        /// <summary>
        /// 轴卡列表
        /// </summary>
        public List<MotionCardDto> MotionCards
        {
            get => _motionCards;
            set { _motionCards = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 运动轴列表
        /// </summary>
        public List<MotionAxisDto> MotionAxes
        {
            get => _motionAxes;
            set { _motionAxes = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 运动点位列表
        /// </summary>
        public List<MotionPointDto> MotionPoints
        {
            get => _motionPoints;
            set { _motionPoints = value; RaisePropertyChanged(); }
        }

        private readonly MotionCardService _motionCardService;
        private readonly MotionAxisService _motionAxisService;
        private readonly MotionPointService _motionPointService;

        /// <summary>
        /// 运动控制器实例
        /// </summary>
        public IMotionController MotionController { get; }

        /// <summary>
        /// 默认运动控制器类型
        /// </summary>
        public MotionMode DefaultMotionMode { get; }
        #endregion

        #region 公共操作方法
        /// <summary>
        /// 添加或更新轴卡
        /// </summary>
        public async Task AddOrUpdateMotionCardAsync(MotionCardDto dto)
        {
            await ExecuteWithLogging(async () =>
            {
                var result = await _motionCardService.AddOrUpdateAsync(dto);
                ShowMessage(result.Message);
                if (result.Status) await RefreshDataAsync();
            });
        }

        /// <summary>
        /// 添加或更新运动轴
        /// </summary>
        public async Task AddOrUpdateMotionAxisAsync(MotionAxisDto dto)
        {
            await ExecuteWithLogging(async () =>
            {
                var result = await _motionAxisService.AddOrUpdateAsync(dto);
                ShowMessage(result.Message);
                if (result.Status) await RefreshDataAsync();
            });
        }

        /// <summary>
        /// 添加或更新运动点位
        /// </summary>
        public async Task AddOrUpdateMotionPointAsync(MotionPointDto dto)
        {
            await ExecuteWithLogging(async () =>
            {
                var result = await _motionPointService.AddOrUpdateAsync(dto);
                ShowMessage(result.Message);
                if (result.Status) await RefreshDataAsync();
            });
        }

        /// <summary>
        /// 删除轴卡
        /// </summary>
        public async Task DeleteMotionCardsAsync(IEnumerable<MotionCardDto> dtos = null)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dtos?.Any() != true)
                {
                    ShowMessage("请选择要删除的轴卡数据");
                    return;
                }

                var result = await _motionCardService.DeleteAsync(dtos);
                ShowMessage(result.Message);
                if (result.Status) await RefreshDataAsync();
            });
        }

        /// <summary>
        /// 删除运动轴
        /// </summary>
        public async Task DeleteMotionAxesAsync(IEnumerable<MotionAxisDto> dtos = null)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dtos?.Any() != true)
                {
                    ShowMessage("请选择要删除的运动轴数据");
                    return;
                }

                var result = await _motionAxisService.DeleteAsync(dtos);
                ShowMessage(result.Message);
                if (result.Status) await RefreshDataAsync();
            });
        }

        /// <summary>
        /// 删除运动点位
        /// </summary>
        public async Task DeleteMotionPointsAsync(IEnumerable<MotionPointDto> dtos = null)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dtos?.Any() != true)
                {
                    ShowMessage("请选择要删除的运动点位数据");
                    return;
                }

                var result = await _motionPointService.DeleteAsync(dtos);
                ShowMessage(result.Message);
                if (result.Status) await RefreshDataAsync();
            });
        }

        /// <summary>
        /// 更新单个轴卡
        /// </summary>
        public async Task UpdateMotionCardAsync(MotionCardDto dto)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dto == null)
                {
                    ShowMessage("请选择要保存的数据");
                    return;
                }
                var result = await _motionCardService.UpdateAsync(dto);
                ShowMessage(result.Message);
            });
        }

        /// <summary>
        /// 批量更新轴卡
        /// </summary>
        public async Task UpdateMotionCardsAsync(IEnumerable<MotionCardDto> dtos = null)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dtos?.Any() != true)
                {
                    ShowMessage("请选择要保存的数据");
                    return;
                }
                var result = await _motionCardService.UpdateAsync(dtos);
                ShowMessage(result.Message);
            });
        }

        /// <summary>
        /// 更新单个运动轴
        /// </summary>
        public async Task UpdateMotionAxisAsync(MotionAxisDto dto)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dto == null)
                {
                    ShowMessage("请选择要保存的数据");
                    return;
                }
                var result = await _motionAxisService.UpdateAsync(dto);
                ShowMessage(result.Message);
            });
        }

        /// <summary>
        /// 批量更新运动轴
        /// </summary>
        public async Task UpdateMotionAxesAsync(IEnumerable<MotionAxisDto> dtos = null)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dtos?.Any() != true)
                {
                    ShowMessage("请选择要保存的数据");
                    return;
                }
                var result = await _motionAxisService.UpdateAsync(dtos);
                ShowMessage(result.Message);
            });
        }

        /// <summary>
        /// 更新单个运动点位
        /// </summary>
        public async Task UpdateMotionPointAsync(MotionPointDto dto)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dto == null)
                {
                    ShowMessage("请选择要保存的数据");
                    return;
                }
                var result = await _motionPointService.UpdateAsync(dto);
                ShowMessage(result.Message);
            });
        }

        /// <summary>
        /// 批量更新运动点位
        /// </summary>
        public async Task UpdateMotionPointsAsync(IEnumerable<MotionPointDto> dtos = null)
        {
            await ExecuteWithLogging(async () =>
            {
                if (dtos?.Any() != true)
                {
                    ShowMessage("请选择要保存的数据");
                    return;
                }
                var result = await _motionPointService.UpdateAsync(dtos);
                ShowMessage(result.Message);
            });
        }

        /// <summary>
        /// 刷新所有数据
        /// </summary>
        public async Task RefreshDataAsync()
        {
            await ExecuteWithLogging(async () =>
            {
                var cardResult = await _motionCardService.GetAllAsync();
                if (cardResult.Status) MotionCards = cardResult.Result;

                var axisResult = await _motionAxisService.GetAllAsync();
                if (axisResult.Status) MotionAxes = axisResult.Result;

                var pointResult = await _motionPointService.GetAllAsync();
                if (pointResult.Status) MotionPoints = pointResult.Result;
            });
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 带日志记录的异步执行方法
        /// </summary>
        private async Task ExecuteWithLogging(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowMessage($"操作失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        private void ShowMessage(string message)
        {
            // 实现消息显示逻辑，如弹窗或状态栏显示
            // 示例：MessageBox.Show(message);
        }
        #endregion

        /// <summary>
        /// 运动控制器类型
        /// </summary>
        public enum MotionMode
        {
            /// <summary>
            /// 雷赛控制器
            /// </summary>
            LeiSai,
            /// <summary>
            /// 凌臣控制器
            /// </summary>
            LingChen,
            /// <summary>
            /// 凌华控制器
            /// </summary>
            LingHua
        }
    }
}
