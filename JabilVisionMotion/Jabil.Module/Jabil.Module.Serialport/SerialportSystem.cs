using AutoMapper;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Serialport.Common;
using Jabil.Module.Serialport.Common.Dtos;
using Jabil.Module.Serialport.Entity;
using Jabil.Module.Serialport.Lib;
using Jabil.Module.Serialport.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.Serialport
{
    /// <summary>
    /// 串口管理系统核心类，负责串口设备的整体管理和数据交互
    /// </summary>
    public class SerialportSystem : BindableBase
    {
        #region 单例模式
        private static readonly Lazy<SerialportSystem> _instance = new Lazy<SerialportSystem>(
            () => new SerialportSystem(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 获取串口系统的唯一实例
        /// </summary>
        public static SerialportSystem Instance => _instance.Value;
        #endregion

        #region 私有字段
        private readonly SerialportService _serialportService;
        private readonly SerialportDIOService _serialportDIOService;
        private bool _isDataFormatValid;
        private string _transferReceivedString;
        private string _transferSentString;
        private string _communicationDeviceName = "未定义";
        private readonly object _lockObj = new object();
        #endregion

        #region 公共属性
        /// <summary>
        /// 串口通讯器列表
        /// </summary>
        public List<SerialPortCommunicator> SerialPorts { get; } = new List<SerialPortCommunicator>();

        /// <summary>
        /// 数据格式判断结果
        /// </summary>
        public bool IsDataFormatValid
        {
            get => _isDataFormatValid;
            set => SetProperty(ref _isDataFormatValid, value);
        }

        /// <summary>
        /// 中转接收字符串
        /// </summary>
        public string TransferReceivedString
        {
            get => _transferReceivedString;
            set
            {
                if (_transferReceivedString != value)
                {
                    DataChangedReceivedString?.Invoke(_communicationDeviceName, value);
                    _transferReceivedString = value;
                }
            }
        }

        /// <summary>
        /// 中转发送字符串
        /// </summary>
        public string TransferSentString
        {
            get => _transferSentString;
            set
            {
                if (_transferSentString != value)
                {
                    DataChangedSentString?.Invoke(_communicationDeviceName, value);
                    _transferSentString = value;
                }
            }
        }

        /// <summary>
        /// 通讯设备名称
        /// </summary>
        public string CommunicationDeviceName
        {
            get => _communicationDeviceName;
            set => SetProperty(ref _communicationDeviceName, value);
        }

        /// <summary>
        /// 串口配置列表
        /// </summary>
        private List<SerialportDto> _serialportList;
        public List<SerialportDto> SerialportList
        {
            get => _serialportList;
            set => SetProperty(ref _serialportList, value);
        }

        /// <summary>
        /// 串口DIO配置列表
        /// </summary>
        private List<SerialportDIODto> _serialportDIOList;
        public List<SerialportDIODto> SerialportDIOList
        {
            get => _serialportDIOList;
            set => SetProperty(ref _serialportDIOList, value);
        }
        #endregion

        #region 事件定义
        /// <summary>
        /// 接收数据变化事件委托
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="data">数据内容</param>
        public delegate void DataChangedEventHandler(string deviceName, string data);

        /// <summary>
        /// 接收数据变化事件
        /// </summary>
        public event DataChangedEventHandler DataChangedReceivedString;

        /// <summary>
        /// 发送数据变化事件
        /// </summary>
        public event DataChangedEventHandler DataChangedSentString;
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化串口管理系统
        /// </summary>
        private SerialportSystem()
        {
            _serialportService = new SerialportService();
            _serialportDIOService = new SerialportDIOService();
        }
        #endregion

        #region 数据操作
        /// <summary>
        /// 刷新所有串口数据
        /// </summary>
        public async Task RefreshDataAsync()
        {
            if (_serialportService == null || _serialportDIOService == null)
            {
                ShowMessage("服务未初始化");
                return;
            }

            try
            {
                // 并行加载数据提高效率
                var serialportTask = _serialportService.GetAllAsync();
                var dioTask = _serialportDIOService.GetAllAsync();

                await Task.WhenAll(serialportTask, dioTask);

                var serialportResult = await serialportTask;
                if (serialportResult != null && serialportResult.Status)
                {
                    SerialportList = serialportResult.Result;
                }

                var dioResult = await dioTask;
                if (dioResult != null && dioResult.Status)
                {
                    SerialportDIOList = dioResult.Result;
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"数据刷新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="message">消息内容</param>
        public void ShowMessage(string message)
        {
            // 消息显示逻辑实现
        }
        #endregion

        #region 串口配置管理 (CRUD操作)
        /// <summary>
        /// 添加或更新串口配置
        /// </summary>
        public async Task AddOrUpdateSerialportAsync(SerialportDto dto)
        {
            if (dto == null)
            {
                ShowMessage("配置数据不能为空");
                return;
            }

            var result = await _serialportService.AddOrUpdateAsync(dto);
            ShowMessage(result.Message);

            if (result.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 添加或更新串口DIO配置
        /// </summary>
        public async Task AddOrUpdateSerialportDIOAsync(SerialportDIODto dto)
        {
            if (dto == null)
            {
                ShowMessage("DIO配置数据不能为空");
                return;
            }

            var result = await _serialportDIOService.AddOrUpdateAsync(dto);
            ShowMessage(result.Message);

            if (result.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 删除串口配置
        /// </summary>
        public async Task DeleteSerialportsAsync(IEnumerable<SerialportDto> dtos = null)
        {
            if (dtos?.Any() != true)
            {
                ShowMessage("请选择要删除的串口配置");
                return;
            }

            var result = await _serialportService.DeleteAsync(dtos);
            ShowMessage(result.Message);

            if (result.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 删除串口DIO配置
        /// </summary>
        public async Task DeleteSerialportDIOsAsync(IEnumerable<SerialportDIODto> dtos = null)
        {
            if (dtos?.Any() != true)
            {
                ShowMessage("请选择要删除的DIO配置");
                return;
            }

            var result = await _serialportDIOService.DeleteAsync(dtos);
            ShowMessage(result.Message);

            if (result.Status)
            {
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// 更新单个串口配置
        /// </summary>
        public async Task UpdateSerialportAsync(SerialportDto dto)
        {
            if (dto == null)
            {
                ShowMessage("请选择要保存的数据");
                return;
            }

            var result = await _serialportService.UpdateAsync(dto);
            ShowMessage(result.Message);
        }

        /// <summary>
        /// 更新单个串口DIO配置
        /// </summary>
        public async Task UpdateSerialportDIOAsync(SerialportDIODto dto)
        {
            if (dto == null)
            {
                ShowMessage("请选择要保存的数据");
                return;
            }

            var result = await _serialportDIOService.UpdateAsync(dto);
            ShowMessage(result.Message);
        }

        /// <summary>
        /// 批量更新串口配置
        /// </summary>
        public async Task UpdateSerialportsAsync(IEnumerable<SerialportDto> dtos = null)
        {
            if (dtos?.Any() != true)
            {
                ShowMessage("请选择要更新的串口配置");
                return;
            }

            var result = await _serialportService.UpdateAsync(dtos);
            ShowMessage(result.Message);
        }

        /// <summary>
        /// 批量更新串口DIO配置
        /// </summary>
        public async Task UpdateSerialportDIOsAsync(IEnumerable<SerialportDIODto> dtos = null)
        {
            if (dtos?.Any() != true)
            {
                ShowMessage("请选择要更新的DIO配置");
                return;
            }

            var result = await _serialportDIOService.UpdateAsync(dtos);
            ShowMessage(result.Message);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 设置属性值并触发属性变更通知
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
        #endregion
    }




}
