using Jabil.Module.Modbus.Common.Dtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Modbus.Lib
{
    /// <summary>
    /// Modbus点位实时数据容器类
    /// 封装单个Modbus点位的完整信息：基础配置、实时值、时间戳及有效性状态
    /// 作为数据传输和存储的基本单元，实现配置与实时数据的关联管理
    /// </summary>
    public class ModbusDioRealTimeData
    {
        /// <summary>
        /// 点位基础配置信息（数据传输对象）
        /// 包含设备名称、点位索引、数据类型等静态配置
        /// </summary>
        public ModbusCommunicationDioDto DioConfig { get; set; }

        /// <summary>
        /// 点位实时值（支持多类型存储）
        /// 根据配置的 DataType 可存储 int16、bool 等类型的实时采集/控制值
        /// </summary>
        public object RealTimeValue { get; set; }

        /// <summary>
        /// 最后一次数据更新时间戳
        /// 用于判断数据是否超时、计算刷新频率等时间相关逻辑
        /// </summary>
        public DateTime LastUpdateTime { get; set; }

        /// <summary>
        /// 数据有效性标记
        /// true：数据已成功读取/写入，状态可靠
        /// false：数据读取失败、超时或未初始化，不可用于业务判断
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// 数据类型（只读，从配置中获取）
        /// 可能的值："int16"、"bool"等，用于数据解析和类型转换
        /// </summary>
        public string DataType => DioConfig?.数据类型;

        /// <summary>
        /// 设备名称（只读，从配置中获取）
        /// 关联的Modbus设备唯一标识，用于设备级数据分组
        /// </summary>
        public string DeviceName => DioConfig?.设备名称;

        /// <summary>
        /// 点位索引（只读，从配置中获取）
        /// 设备内的点位唯一标识，与设备名称组合形成全局唯一键
        /// </summary>
        public string PointIndex => DioConfig?.点位索引;
    }

    /// <summary>
    /// Modbus点位实时数据全局管理器
    /// 核心功能：
    /// 1. 线程安全地管理所有Modbus点位的实时数据存储与更新
    /// 2. 提供高效的配置缓存与数据查询能力
    /// 3. 支持超时数据检测、配置动态刷新等增强功能
    /// 设计特点：
    /// - 采用ConcurrentDictionary确保多线程环境下的数据操作安全
    /// - 分离配置缓存与实时数据，减少重复查询开销
    /// - 提供丰富的查询接口，满足不同维度的数据访问需求
    /// </summary>
    public static class ModbusDioRealTimeManager
    {
        #region 核心存储容器
        /// <summary>
        /// 全局实时数据字典（线程安全）
        /// 键：由"设备名称_点位索引"组成的唯一标识（通过GetKey方法生成）
        /// 值：包含实时值、时间戳等信息的ModbusDioRealTimeData对象
        /// </summary>
        private static readonly ConcurrentDictionary<string, ModbusDioRealTimeData> _realtimeDios =
            new ConcurrentDictionary<string, ModbusDioRealTimeData>();

        /// <summary>
        /// 点位配置缓存（线程安全）
        /// 缓存所有点位的静态配置信息，避免频繁访问数据源（如数据库/配置文件）
        /// 键：与实时数据字典一致的唯一标识
        /// 值：点位配置数据传输对象ModbusCommunicationDioDto
        /// </summary>
        private static readonly ConcurrentDictionary<string, ModbusCommunicationDioDto> _dioConfigCache =
            new ConcurrentDictionary<string, ModbusCommunicationDioDto>();
        #endregion

        #region 初始化状态管理
        /// <summary>
        /// 初始化状态标记
        /// 用于判断管理器是否已完成初始化，避免未初始化时的数据操作
        /// </summary>
        private static bool _isInitialized = false;

        /// <summary>
        /// 公开的初始化状态查询属性
        /// 外部模块可通过此属性判断是否可进行数据操作
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        #endregion

        #region 初始化与配置刷新
        /// <summary>
        /// 初始化实时数据管理器
        /// 流程：
        /// 1. 清空现有数据与缓存，避免脏数据残留
        /// 2. 从Modbus系统获取所有点位配置
        /// 3. 为每个点位初始化实时数据条目（设置默认值、标记初始状态）
        /// 4. 同步更新配置缓存
        /// </summary>
        /// <returns>初始化是否成功（true：成功；false：失败，如无配置数据）</returns>
        public static bool Initialize()
        {
            try
            {
                // 清空现有数据，确保初始化环境干净
                _realtimeDios.Clear();
                _dioConfigCache.Clear();

                // 从系统获取所有点位配置（依赖ModbusSystem单例）
                var allDios = ModbusSystem.Instance?.ModbusCommunicationDios;
                if (allDios == null || !allDios.Any())
                {
                    // 无配置数据时初始化失败
                    _isInitialized = false;
                    return false;
                }

                // 遍历所有配置，初始化实时数据和缓存
                foreach (var dio in allDios)
                {
                    // 生成全局唯一键
                    string key = GetKey(dio.设备名称, dio.点位索引);

                    // 缓存配置信息

                    _dioConfigCache.TryAdd(key, dio);


                   

                    // 初始化实时数据条目
                    _realtimeDios.TryAdd(key, new ModbusDioRealTimeData
                    {
                        DioConfig = dio,
                        RealTimeValue = GetDefaultValue(dio.数据类型), // 设置类型默认值
                        LastUpdateTime = DateTime.MinValue, // 初始时间戳设为最小，标记未更新
                        IsValid = false // 初始值无效，需等待第一次成功更新
                    });
                }

                // 标记初始化成功
                _isInitialized = true;
                return true;
            }
            catch
            {
                // 异常情况下标记初始化失败
                _isInitialized = false;
                return false;
            }
        }

        /// <summary>
        /// 刷新配置缓存（动态更新配置时使用）
        /// 适用场景：系统配置变更后，无需重启即可更新管理器中的配置信息
        /// 特点：保留现有实时值，仅更新配置关联，减少数据中断
        /// </summary>
        public static void RefreshConfigCache()
        {
            // 保存当前实时值（键：唯一标识，值：实时值），用于刷新后恢复
            var currentValues = _realtimeDios.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.RealTimeValue);

            // 清空现有缓存和实时数据容器（配置将重新加载）
            _dioConfigCache.Clear();
            _realtimeDios.Clear();

            // 从系统重新获取配置
            var allDios = ModbusSystem.Instance?.ModbusCommunicationDios;
            if (allDios == null)
                return;

            // 重新加载配置并恢复实时值
            foreach (var dio in allDios)
            {
                string key = GetKey(dio.设备名称, dio.点位索引);
                _dioConfigCache.TryAdd(key, dio);

                // 尝试从保存的实时值中恢复，无历史值则使用默认值
                currentValues.TryGetValue(key, out var lastValue);

                // 创建新的实时数据条目
                _realtimeDios.TryAdd(key, new ModbusDioRealTimeData
                {
                    DioConfig = dio,
                    RealTimeValue = lastValue ?? GetDefaultValue(dio.数据类型),
                    LastUpdateTime = lastValue != null ? DateTime.Now : DateTime.MinValue,
                    IsValid = lastValue != null // 有历史值则标记为有效
                });
            }

            // 刷新后标记为已初始化
            _isInitialized = true;
        }
        #endregion

        #region 实时数据更新
        /// <summary>
        /// 更新指定点位的实时值（线程安全）
        /// 功能：新增或更新点位的实时数据，同步更新时间戳和有效性
        /// 应用场景：设备数据采集后更新缓存、控制指令下发后同步状态
        /// </summary>
        /// <param name="deviceName">设备名称（唯一标识）</param>
        /// <param name="pointIndex">点位索引（设备内唯一）</param>
        /// <param name="value">待更新的实时值（需与配置的DataType匹配）</param>
        /// <param name="isValid">数据有效性标记（默认true）</param>
        public static void UpdateRealTimeValue(string deviceName, string pointIndex, object value, bool isValid = true)
        {
            // 参数校验：设备名称、点位索引和值不能为空
            if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(pointIndex) || value == null)
                return;

            // 生成唯一键
            string key = GetKey(deviceName, pointIndex);

            // 线程安全地添加或更新数据
            _realtimeDios.AddOrUpdate(
                key,
                // 键不存在时：新增条目（从缓存获取配置，设置初始值）
                (k) => new ModbusDioRealTimeData
                {
                    DioConfig = GetDioConfig(deviceName, pointIndex),
                    RealTimeValue = value,
                    LastUpdateTime = DateTime.Now,
                    IsValid = isValid
                },
                // 键存在时：更新现有条目（保留配置，更新值、时间戳和有效性）
                (k, existing) =>
                {
                    existing.RealTimeValue = value;
                    existing.LastUpdateTime = DateTime.Now;
                    existing.IsValid = isValid;
                    return existing;
                });
        }
        #endregion

        #region 配置查询（基于缓存）
        /// <summary>
        /// 获取指定设备的所有点位配置
        /// 从缓存查询，避免重复访问原始配置源，提升效率
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>该设备下所有点位的配置列表（空列表表示无配置）</returns>
        public static List<ModbusCommunicationDioDto> GetDeviceConfigByDevice(string deviceName)
        {
            return _dioConfigCache.Values
                .Where(d => d.设备名称.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 获取指定点位的配置信息（私有辅助方法）
        /// 供内部更新数据时关联配置使用
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="pointIndex">点位索引</param>
        /// <returns>点位配置对象（null表示未找到）</returns>
        private static ModbusCommunicationDioDto GetDioConfig(string deviceName, string pointIndex)
        {
            string key = GetKey(deviceName, pointIndex);
            _dioConfigCache.TryGetValue(key, out var config);
            return config;
        }
        #endregion

        #region 实时数据查询（多维度）
        /// <summary>
        /// 获取指定点位的实时数据
        /// 用于精确查询单个点位的当前状态
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="pointIndex">点位索引</param>
        /// <returns>实时数据对象（null表示未找到）</returns>
        public static ModbusDioRealTimeData GetRealTimeData(string deviceName, string pointIndex)
        {
            _realtimeDios.TryGetValue(GetKey(deviceName, pointIndex), out var data);
            return data;
        }

        /// <summary>
        /// 获取指定设备的所有点位实时数据
        /// 用于设备级的状态监控、批量操作
        /// </summary>
        /// <param name="deviceName">设备名称（空字符串返回所有设备数据）</param>
        /// <returns>只读的实时数据列表（避免外部修改内部存储）</returns>
        public static IReadOnlyList<ModbusDioRealTimeData> GetDeviceRealTimeData(string deviceName)
        {
            // 筛选指定设备的所有点位数据（忽略大小写）
            if (deviceName == string.Empty)
            {
                var query = _realtimeDios.Values;
                // 转换为只读列表，防止外部修改
                return query.ToList().AsReadOnly();
            }
            else
            {
                var query = _realtimeDios.Values
                   .Where(d => d.DeviceName!=null&&d.DeviceName.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
                // 转换为只读列表，防止外部修改
                return query.ToList().AsReadOnly();
            }

        }

        /// <summary>
        /// 获取指定类型的所有点位实时数据
        /// 用于按点位类型（如"输入"/"输出"）筛选数据
        /// </summary>
        /// <param name="pointType">点位类型（如"输入"、"输出"）</param>
        /// <returns>只读的实时数据列表</returns>
        public static IReadOnlyList<ModbusDioRealTimeData> GetTypeRealTimeData(string pointType)
        {
            // 筛选指定类型的点位数据（忽略大小写）
            var query = _realtimeDios.Values
                .Where(d => d.DioConfig?.点位类型.Equals(pointType, StringComparison.OrdinalIgnoreCase) == true);

            return query.ToList().AsReadOnly();
        }

        /// <summary>
        /// 获取超时未更新的点位数据
        /// 用于检测通信异常或数据停滞的点位，支持系统健康监控
        /// </summary>
        /// <param name="timeout">超时阈值（超过此时长未更新视为超时）</param>
        /// <returns>所有超时点位的实时数据列表（只读）</returns>
        public static IReadOnlyList<ModbusDioRealTimeData> GetTimeoutData(TimeSpan timeout)
        {
            // 计算超时截止时间（当前时间减去超时阈值）
            var cutoffTime = DateTime.Now.Subtract(timeout);

            // 筛选最后更新时间早于截止时间的点位
            var query = _realtimeDios.Values
                .Where(d => d.LastUpdateTime < cutoffTime);

            return query.ToList().AsReadOnly();
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 生成点位全局唯一键
        /// 格式："设备名称_点位索引"，确保不同设备的同索引点位不冲突
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="pointIndex">点位索引</param>
        /// <returns>唯一键字符串</returns>
        public static string GetKey(string deviceName, string pointIndex)
        {
            return $"{deviceName}_{pointIndex}";
        }

        /// <summary>
        /// 根据数据类型获取默认值
        /// 用于初始化点位时设置初始值，避免空引用
        /// </summary>
        /// <param name="dataType">数据类型字符串（如"int16"、"bool"）</param>
        /// <returns>对应类型的默认值（未知类型返回null）</returns>
        private static object GetDefaultValue(string dataType)
        {
            return dataType switch
            {
                "int16" => (short)0,   // 16位整数默认值
                "int32" => (int)0,     // 32位整数默认值
                "bool" => false,       // 布尔类型默认值（false表示关闭/未激活）
                _ => null              // 未知类型返回null
            };
        }
        #endregion
    }
}
