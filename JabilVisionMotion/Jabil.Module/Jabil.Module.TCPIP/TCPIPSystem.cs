using AutoMapper;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.TCPIP.Common;
using Jabil.Module.TCPIP.Common.Dtos;
using Jabil.Module.TCPIP.Entity;
using Jabil.Module.TCPIP.Lib;
using Jabil.Module.TCPIP.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.TCPIP
{
    /// <summary>
    /// TCP/IP系统管理类，负责TCP客户端/服务器管理和数据通信
    /// </summary>
    public class TcpIpSystem : BindableBase
    {
        #region 单例模式
        private static readonly Lazy<TcpIpSystem> _instance = new Lazy<TcpIpSystem>(() => new TcpIpSystem(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static TcpIpSystem Instance => _instance.Value;

        private TcpIpSystem()
        {
            _communicationService = new TcpIpCommunicationService();
            _dioService = new TcpIpCommunicationDioService();
        }
        #endregion

        #region 私有字段
        private readonly List<TCPClient> _tcpClients = new List<TCPClient>();
        private readonly List<TCPServer> _tcpServers = new List<TCPServer>();
        private bool _isDataFormatValid;

        private List<TcpIpCommunicationDto> _tcpIpCommunications;
        private List<TcpIpCommunicationDioDto> _tcpIpCommunicationDios;

        private readonly TcpIpCommunicationService _communicationService;
        private readonly TcpIpCommunicationDioService _dioService;
        #endregion

        #region 公共属性
        /// <summary>
        /// 获取TCP客户端列表（只读）
        /// </summary>
        public List<TCPClient> TcpClients => _tcpClients;

        /// <summary>
        /// 获取TCP服务器列表（只读）
        /// </summary>
        public List<TCPServer> TcpServers => _tcpServers;

        /// <summary>
        /// 数据格式验证状态
        /// </summary>
        public bool IsDataFormatValid
        {
            get => _isDataFormatValid;
            set => SetProperty(ref _isDataFormatValid, value);
        }

        /// <summary>
        /// TCP/IP通信配置列表
        /// </summary>
        public List<TcpIpCommunicationDto> TcpIpCommunications
        {
            get => _tcpIpCommunications;
            private set => SetProperty(ref _tcpIpCommunications, value);
        }

        /// <summary>
        /// TCP/IP通信DIO配置列表
        /// </summary>
        public List<TcpIpCommunicationDioDto> TcpIpCommunicationDios
        {
            get => _tcpIpCommunicationDios;
            private set => SetProperty(ref _tcpIpCommunicationDios, value);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 刷新数据，从数据库加载最新配置
        /// </summary>
        public async Task RefreshDataAsync()
        {
            try
            {
                // 并行加载数据，提高效率
                var communicationTask = _communicationService.GetAllAsync();
                var dioTask = _dioService.GetAllAsync();

                await Task.WhenAll(communicationTask, dioTask);

                var communicationResult = await communicationTask;
                if (communicationResult.Status)
                {
                    TcpIpCommunications = communicationResult.Result;
                }

                var dioResult = await dioTask;
                if (dioResult.Status)
                {
                    TcpIpCommunicationDios = dioResult.Result;
                }
            }
            catch (Exception ex)
            {
                OnMessageShown($"数据刷新失败: {ex.Message}");
                throw; // 允许上层处理异常
            }
        }

        /// <summary>
        /// 显示消息（可被重写或订阅事件处理）
        /// </summary>
        public event Action<string> MessageShown;
        protected virtual void OnMessageShown(string message)
        {
            MessageShown?.Invoke(message);
        }
        #endregion

        #region 配置管理（增删改查）
        /// <summary>
        /// 添加或更新IO卡配置
        /// </summary>
        public async Task AddOrUpdateCommunicationAsync(TcpIpCommunicationDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var result = await _communicationService.AddOrUpdateAsync(dto);
            OnMessageShown(result.Message);

            if (result.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 添加或更新DIO配置
        /// </summary>
        public async Task AddOrUpdateDioAsync(TcpIpCommunicationDioDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var result = await _dioService.AddOrUpdateAsync(dto);
            OnMessageShown(result.Message);

            if (result.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 删除IO卡配置
        /// </summary>
        public async Task DeleteCommunicationsAsync(IEnumerable<TcpIpCommunicationDto> dtos)
        {
            if (dtos?.Any() != true)
            {
                OnMessageShown("请选择要删除的数据");
                return;
            }

            var result = await _communicationService.DeleteAsync(dtos);
            OnMessageShown(result.Message);

            if (result.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 删除DIO配置
        /// </summary>
        public async Task DeleteDiosAsync(IEnumerable<TcpIpCommunicationDioDto> dtos)
        {
            if (dtos?.Any() != true)
            {
                OnMessageShown("请选择要删除的数据");
                return;
            }

            var result = await _dioService.DeleteAsync(dtos);
            OnMessageShown(result.Message);

            if (result.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 更新IO卡配置
        /// </summary>
        public async Task UpdateCommunicationsAsync(IEnumerable<TcpIpCommunicationDto> dtos)
        {
            if (dtos?.Any() != true)
            {
                OnMessageShown("请选择要更新的数据");
                return;
            }

            var result = await _communicationService.UpdateAsync(dtos);
            OnMessageShown(result.Message);
        }

        /// <summary>
        /// 更新DIO配置
        /// </summary>
        public async Task UpdateDiosAsync(IEnumerable<TcpIpCommunicationDioDto> dtos)
        {
            if (dtos?.Any() != true)
            {
                OnMessageShown("请选择要更新的数据");
                return;
            }

            var result = await _dioService.UpdateAsync(dtos);
            OnMessageShown(result.Message);
        }
        #endregion
    }




}

