using AutoMapper;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Alarm.Common;
using Jabil.Module.Alarm.Common.Dtos;
using Jabil.Module.Alarm.Entity;
using Jabil.Module.Alarm.Service;
using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Alarm
{
    /// <summary>
    /// 报警系统核心管理类
    /// 负责协调报警通信DIO（数字输入输出）相关的所有业务操作
    /// 采用单例模式设计，确保系统中只有一个实例进行集中管理
    /// 继承自BindableBase，支持属性变化通知（适用于MVVM模式）
    /// </summary>
    public class AlarmSystem : BindableBase
    {
        // 单例模式的线程安全锁
        // 用于在多线程环境下保证实例创建的原子性
        private static readonly object _instanceLock = new object();

        // 单例实例变量
        private static AlarmSystem _instance;

        // 提示信息常量 - 集中管理用户提示文本，便于统一修改和国际化
        private const string Msg_NoDataToDelete = "请选择要删除的数据";
        private const string Msg_NoDataToUpdate = "请选择要保存的数据";

        /// <summary>
        /// 单例实例访问属性
        /// 采用双重检查锁定(Double-Check Locking)实现线程安全的单例模式
        /// 确保高并发场景下只会创建一个实例
        /// </summary>
        public static AlarmSystem Instance
        {
            get
            {
                // 第一次检查：避免每次访问都加锁，提高性能
                if (_instance == null)
                {
                    // 加锁：确保只有一个线程进入实例创建逻辑
                    lock (_instanceLock)
                    {
                        // 第二次检查：防止多个线程同时通过第一次检查后重复创建实例
                        if (_instance == null)
                        {
                            _instance = new AlarmSystem();
                        }
                    }
                }
                return _instance;
            }
        }

        #region Fields

        /// <summary>
        /// 报警通信DIO数据集合的私有存储字段
        /// </summary>
        private List<AlarmCommunicationDIODto> _listAlarmCommunicationDIO;

        /// <summary>
        /// 报警通信DIO数据集合的公共属性
        /// 提供外部访问，并在值变化时触发属性变更通知
        /// 适用于UI绑定场景，当数据更新时自动刷新界面
        /// </summary>
        public List<AlarmCommunicationDIODto> ListAlarmCommunicationDIO
        {
            get => _listAlarmCommunicationDIO;
            set { _listAlarmCommunicationDIO = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 报警通信DIO服务的私有实例
        /// 负责实际的数据访问操作，通过构造函数注入实现解耦
        /// </summary>
        private readonly AlarmCommunicationDIOService _alarmDIOService;

        #endregion

        #region Constructors

        /// <summary>
        /// 构造函数（支持依赖注入）
        /// 采用依赖注入模式，允许外部传入服务实例，便于单元测试和扩展
        /// </summary>
        /// <param name="alarmDIOService">
        /// 可选的AlarmCommunicationDIOService实例
        /// 如未提供，则使用默认实例
        /// </param>
        public AlarmSystem(AlarmCommunicationDIOService alarmDIOService = null)
        {
            try
            {
                // 依赖注入：如果外部提供了服务实例则使用，否则创建默认实例
                // 这种设计使得在测试时可以传入模拟(Mock)服务，隔离数据层
                _alarmDIOService = alarmDIOService ?? new AlarmCommunicationDIOService();

                // 初始化时异步加载数据，使用下划线忽略Task返回值
                // 避免构造函数阻塞，同时不使用await防止构造函数变为async
                _ = RefreshDataAsync();
            }
            catch (Exception ex)
            {
                // 捕获并记录初始化过程中的异常
                // 确保程序不会因初始化失败而崩溃
                RLog.SaveError(ex);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 异步刷新数据
        /// 从数据库加载最新的报警通信DIO数据并更新到集合中
        /// 采用异步设计避免UI线程阻塞
        /// </summary>
        public async Task RefreshDataAsync()
        {
            try
            {
                // 服务实例为空时直接返回，避免空引用异常
                if (_alarmDIOService == null) return;

                // 异步获取所有报警通信DIO数据
                // ConfigureAwait(false)：避免上下文切换，提高性能
                // 适用于非UI线程的异步操作
                var alarmDioResponse = await _alarmDIOService.GetAllAsync().ConfigureAwait(false);

                // 验证响应的有效性：状态为成功且结果不为空
                if (alarmDioResponse?.Status == true && alarmDioResponse.Result != null)
                {
                    // 更新数据集合，会自动触发属性变更通知
                    ListAlarmCommunicationDIO = alarmDioResponse.Result;
                }
            }
            catch (Exception ex)
            {
                // 记录异常但不向上抛出，防止影响调用者
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 显示消息给用户
        /// 封装消息显示逻辑，便于统一管理和修改显示方式
        /// </summary>
        /// <param name="message">要显示的消息内容</param>
        public void ShowMessage(string message)
        {
            // 空消息或空白消息直接返回，避免无效操作
            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                // 可根据实际需求实现消息显示逻辑
                // 示例：使用MessageBox显示消息
                // MessageBox.Show(message);

                // 也可扩展为日志记录或其他UI通知方式
            }
            catch (Exception ex)
            {
                // 记录消息显示失败的异常
                RLog.SaveError(ex);
            }
        }

        #endregion

        #region CRUD Operations

        /// <summary>
        /// 添加或更新报警通信DIO记录
        /// 根据记录是否存在自动判断是执行添加还是更新操作
        /// </summary>
        /// <param name="dto">要添加或更新的数据传输对象</param>
        public async Task AddOrUpdateAlarmCommunicationDIOAsync(AlarmCommunicationDIODto dto)
        {
            // 参数验证：检查DTO是否为空
            if (dto == null)
            {
                ShowMessage("数据不能为空");
                return;
            }

            try
            {
                // 调用服务层执行添加或更新操作
                var result = await _alarmDIOService.AddOrUpdateAsync(dto).ConfigureAwait(false);

                // 显示操作结果消息
                ShowMessage(result.Message);

                // 操作成功时刷新数据，确保UI显示最新状态
                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // 记录异常并显示错误信息
                RLog.SaveError(ex);
                ShowMessage("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量删除报警通信DIO记录
        /// </summary>
        /// <param name="dtos">要删除的数据传输对象集合</param>
        public async Task DeleteAlarmCommunicationDIOsAsync(IEnumerable<AlarmCommunicationDIODto> dtos = null)
        {
            try
            {
                // 验证删除数据是否为空
                if (dtos == null || !dtos.Any())
                {
                    ShowMessage(Msg_NoDataToDelete);
                    return;
                }

                // 调用服务层执行删除操作
                var result = await _alarmDIOService.DeleteAsync(dtos).ConfigureAwait(false);

                // 显示操作结果消息
                ShowMessage(result.Message);

                // 操作成功时刷新数据
                if (result.Status)
                {
                    await RefreshDataAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowMessage("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 更新单个报警通信DIO记录
        /// </summary>
        /// <param name="dto">要更新的数据传输对象</param>
        public async Task UpdateAlarmCommunicationDIOAsync(AlarmCommunicationDIODto dto)
        {
            try
            {
                // 验证更新数据是否为空
                if (dto == null)
                {
                    ShowMessage(Msg_NoDataToUpdate);
                    return;
                }

                // 调用服务层执行更新操作
                var result = await _alarmDIOService.UpdateAsync(dto).ConfigureAwait(false);

                // 显示操作结果消息
                ShowMessage(result.Message);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowMessage("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量更新报警通信DIO记录
        /// </summary>
        /// <param name="dtos">要更新的数据传输对象集合</param>
        public async Task UpdateAlarmCommunicationDIOsAsync(IEnumerable<AlarmCommunicationDIODto> dtos = null)
        {
            try
            {
                // 验证更新数据是否为空
                if (dtos == null || !dtos.Any())
                {
                    ShowMessage(Msg_NoDataToUpdate);
                    return;
                }

                // 调用服务层执行批量更新操作
                var result = await _alarmDIOService.UpdateAsync(dtos).ConfigureAwait(false);

                // 显示操作结果消息
                ShowMessage(result.Message);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                ShowMessage("操作失败：" + ex.Message);
            }
        }

        #endregion
    }
}
