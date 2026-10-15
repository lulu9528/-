using Jabil.Module.IO.Common.Dtos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Jabil.Module.IO.Lib
{
    /// <summary>
    /// IO点位实时数据容器类
    /// 封装单个IO点位的完整信息：基础配置、实时值、时间戳及有效性状态
    /// 作为IO数据传输和存储的基本单元，实现配置与实时数据的关联管理
    /// </summary>
    public class IoPointRealTimeData
    {
        /// <summary>
        /// 点位基础配置信息
        /// 包含卡名称、点位索引、数据类型等静态配置
        /// </summary>
        public IOCardDIODto PointConfig { get; set; }

        /// <summary>
        /// 点位实时值
        /// 存储IO点的实时状态（主要为bool类型，表示通断状态）
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
        public bool IsValid { get; set; } = false;

        /// <summary>
        /// 卡名称（从配置获取，只读）
        /// 关联的IO卡唯一标识，用于卡级数据分组
        /// </summary>
        public string CardName => PointConfig?.卡名称;

        /// <summary>
        /// 点位索引（从配置获取，只读）
        /// 卡内的点位唯一标识，与卡名称组合形成全局唯一键
        /// </summary>
        public int PointIndex => PointConfig?.点位索引 ?? -1;

        /// <summary>
        /// 点位类型（输入/输出，从配置获取，只读）
        /// 用于区分输入点位和输出点位
        /// </summary>
        public string PointType => PointConfig?.点位类型;

        /// <summary>
        /// 点位名称（从配置获取，只读）
        /// 点位的业务名称（如"急停"、"安全门左"等）
        /// </summary>
        public string PointName => PointConfig?.点位名称;

        /// <summary>
        /// 转换为bool值（安全转换）
        /// 针对IO点常用的布尔状态提供便捷访问
        /// </summary>
        public bool? AsBoolean => RealTimeValue as bool?;
    }

    /// <summary>
    /// IO点位实时数据全局管理器
    /// 核心功能：
    /// 1. 线程安全地管理所有IO点位的实时数据存储与更新
    /// 2. 提供高效的配置缓存与数据查询能力
    /// 3. 支持超时数据检测、异常状态监控等增强功能
    /// 设计特点：
    /// - 采用ConcurrentDictionary确保多线程环境下的数据操作安全
    /// - 分离配置缓存与实时数据，减少重复查询开销
    /// - 提供丰富的查询接口，满足不同维度的数据访问需求
    /// </summary>
    public static class IoPointRealTimeManager
    {
        #region 核心存储容器
        /// <summary>
        /// 全局实时数据字典（线程安全）
        /// 键：由"卡名称_点位索引"组成的唯一标识（通过GetKey方法生成）
        /// 值：包含实时值、时间戳等信息的IoPointRealTimeData对象
        /// </summary>
        private static readonly ConcurrentDictionary<string, IoPointRealTimeData> _realtimePoints =
            new ConcurrentDictionary<string, IoPointRealTimeData>();

        /// <summary>
        /// 点位配置缓存（线程安全）
        /// 缓存所有IO点位的静态配置信息，避免频繁访问数据源
        /// 键：与实时数据字典一致的唯一标识
        /// 值：点位配置数据传输对象IoPointConfigDto
        /// </summary>
        private static readonly ConcurrentDictionary<string, IOCardDIODto> _pointConfigCache =
            new ConcurrentDictionary<string, IOCardDIODto>();
        #endregion

        #region 初始化状态管理
        /// <summary>
        /// 初始化状态标记
        /// 用于判断管理器是否已完成初始化，避免未初始化时的数据操作
        /// </summary>
        private static int _initialized = 0; // 使用int而非bool实现原子操作

        /// <summary>
        /// 公开的初始化状态查询属性
        /// 外部模块可通过此属性判断是否可进行数据操作
        /// </summary>
        public static bool IsInitialized => Interlocked.CompareExchange(ref _initialized, 0, 0) == 1;
        #endregion

        #region 初始化与配置刷新
        /// <summary>
        /// 初始化实时数据管理器
        /// 流程：
        /// 1. 清空现有数据与缓存，避免脏数据残留
        /// 2. 从IO系统获取所有点位配置
        /// 3. 为每个点位初始化实时数据条目（设置默认值、标记初始状态）
        /// 4. 同步更新配置缓存
        /// </summary>
        /// <returns>初始化是否成功（true：成功；false：失败，如无配置数据）</returns>
        public static bool Initialize()
        {
            // 确保初始化只执行一次
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 1)
                return true;

            try
            {
                // 清空现有数据，确保初始化环境干净
                _realtimePoints.Clear();
                _pointConfigCache.Clear();

                // 从系统获取所有点位配置
                var allPoints = IOSystem.Instance.IOCardDios;
                if (allPoints == null || !allPoints.Any())
                {
                    Interlocked.Exchange(ref _initialized, 0);
                    return false;
                }

                // 遍历所有配置，初始化实时数据和缓存
                foreach (var point in allPoints)
                {
                    string key = GetKey(point.卡名称, point.点位索引);

                    // 缓存配置信息
                    _pointConfigCache.TryAdd(key, point);

                    // 初始化实时数据条目
                    _realtimePoints.TryAdd(key, new IoPointRealTimeData
                    {
                        PointConfig = point,
                        RealTimeValue = GetDefaultValue("int"),
                        LastUpdateTime = DateTime.MinValue,
                        IsValid = false
                    });
                }

                return true;
            }
            catch
            {
                Interlocked.Exchange(ref _initialized, 0);
                return false;
            }
        }

        /// <summary>
        /// 刷新配置缓存（动态更新配置时使用）
        /// 适用场景：IO卡配置变更后，无需重启即可更新管理器中的配置信息
        /// 特点：保留现有实时值，仅更新配置关联，减少数据中断
        /// </summary>
        public static void RefreshConfigCache()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("IO点位实时数据管理器尚未初始化");

            // 保存当前实时值，用于刷新后恢复
            var currentValues = _realtimePoints.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.RealTimeValue);

            // 清空现有缓存和实时数据容器
            _pointConfigCache.Clear();
            _realtimePoints.Clear();

            // 从系统重新获取配置
            var allPoints = IOSystem.Instance.IOCardDios;
            if (allPoints == null)
                return;

            // 重新加载配置并恢复实时值
            foreach (var point in allPoints)
            {
                string key = GetKey(point.卡名称, point.点位索引);
                _pointConfigCache.TryAdd(key, point);

                // 尝试从保存的实时值中恢复，无历史值则使用默认值
                currentValues.TryGetValue(key, out var lastValue);

                _realtimePoints.TryAdd(key, new IoPointRealTimeData
                {
                    PointConfig = point,
                    RealTimeValue = lastValue ?? GetDefaultValue("int"),
                    LastUpdateTime = lastValue != null ? DateTime.Now : DateTime.MinValue,
                    IsValid = lastValue != null
                });
            }
        }
        #endregion

        #region 实时数据更新
        /// <summary>
        /// 批量更新指定卡的IO点位实时值（线程安全）
        /// 高效处理整卡数据更新，适用于周期性读取IO卡数据后同步
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="pointValues">点位值字典（键：点位索引，值：点位状态）</param>
        /// <param name="isInput">是否为输入点位（用于筛选配置）</param>
        public static void BatchUpdateCardPoints(string cardName, Dictionary<int, bool> pointValues, bool isInput)
        {
            if (string.IsNullOrEmpty(cardName) || pointValues == null || !pointValues.Any())
                return;

            var timestamp = DateTime.Now;
            var cardPoints = GetCardConfigByType(cardName, isInput ? "输入" : "输出");

            foreach (var point in cardPoints)
            {
                if (pointValues.TryGetValue(point.点位索引, out bool value))
                {
                    string key = GetKey(cardName, point.点位索引);

                    _realtimePoints.AddOrUpdate(
                        key,
                        (k) => new IoPointRealTimeData
                        {
                            PointConfig = point,
                            RealTimeValue = value,
                            LastUpdateTime = timestamp,
                            IsValid = true
                        },
                        (k, existing) =>
                        {
                            existing.RealTimeValue = value;
                            existing.LastUpdateTime = timestamp;
                            existing.IsValid = true;
                            return existing;
                        });
                }
            }
        }

        /// <summary>
        /// 更新单个点位的实时值（线程安全）
        /// 适用于单独控制某个输出点位或处理特殊输入点位
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="pointIndex">点位索引</param>
        /// <param name="value">待更新的实时值</param>
        /// <param name="isValid">数据有效性标记（默认true）</param>
        public static void UpdatePointValue(string cardName, int pointIndex, bool value, bool isValid = true)
        {
            if (string.IsNullOrEmpty(cardName) || pointIndex < 0)
                return;

            string key = GetKey(cardName, pointIndex);
            var timestamp = DateTime.Now;

            _realtimePoints.AddOrUpdate(
                key,
                (k) => new IoPointRealTimeData
                {
                    PointConfig = GetPointConfig(cardName, pointIndex),
                    RealTimeValue = value,
                    LastUpdateTime = timestamp,
                    IsValid = isValid
                },
                (k, existing) =>
                {
                    existing.RealTimeValue = value;
                    existing.LastUpdateTime = timestamp;
                    existing.IsValid = isValid;
                    return existing;
                });
        }

        /// <summary>
        /// 标记点位为无效（用于通信异常时）
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        public static void MarkCardPointsAsInvalid(string cardName)
        {
            if (string.IsNullOrEmpty(cardName))
                return;

            var keys = _realtimePoints.Keys.Where(k => k.StartsWith($"{cardName}_")).ToList();
            foreach (var key in keys)
            {
                if (_realtimePoints.TryGetValue(key, out var pointData))
                {
                    pointData.IsValid = false;
                }
            }
        }
        #endregion

        #region 配置查询（基于缓存）
        /// <summary>
        /// 获取指定卡的所有点位配置
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <returns>该卡下所有点位的配置列表</returns>
        public static List<IOCardDIODto> GetCardConfig(string cardName)
        {
            return _pointConfigCache.Values
                .Where(p => p.卡名称.Equals(cardName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 获取指定卡的指定类型（输入/输出）点位配置
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="pointType">点位类型（输入/输出）</param>
        /// <returns>筛选后的点位配置列表</returns>
        public static List<IOCardDIODto> GetCardConfigByType(string cardName, string pointType)
        {
            return _pointConfigCache.Values
                .Where(p => p.卡名称.Equals(cardName, StringComparison.OrdinalIgnoreCase) &&
                           p.点位类型.Equals(pointType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 获取指定点位的配置信息
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="pointIndex">点位索引</param>
        /// <returns>点位配置对象（null表示未找到）</returns>
        public static IOCardDIODto GetPointConfig(string cardName, int pointIndex)
        {
            string key = GetKey(cardName, pointIndex);
            _pointConfigCache.TryGetValue(key, out var config);
            return config;
        }

        /// <summary>
        /// 根据点位名称查找配置（支持模糊匹配）
        /// </summary>
        /// <param name="pointName">点位名称（或部分名称）</param>
        /// <returns>匹配的点位配置列表</returns>
        public static List<IOCardDIODto> FindPointConfigsByName(string pointName)
        {
            if (string.IsNullOrEmpty(pointName))
                return new List<IOCardDIODto>();

            return _pointConfigCache.Values
                .Where(p => p.点位名称.IndexOf(pointName, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }
        #endregion

        #region 实时数据查询（多维度）
        /// <summary>
        /// 获取指定点位的实时数据
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="pointIndex">点位索引</param>
        /// <returns>实时数据对象（null表示未找到）</returns>
        public static IoPointRealTimeData GetPointData(string cardName, int pointIndex)
        {
            _realtimePoints.TryGetValue(GetKey(cardName, pointIndex), out var data);
            return data;
        }

        /// <summary>
        /// 根据点位名称获取实时数据（适用于业务逻辑中按名称访问）
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="pointName">点位名称</param>
        /// <returns>实时数据对象（null表示未找到）</returns>
        public static IoPointRealTimeData GetPointDataByName(string cardName, string pointName)
        {
            return _realtimePoints.Values
                .FirstOrDefault(d => d.CardName.Equals(cardName, StringComparison.OrdinalIgnoreCase) &&
                                    d.PointName.Equals(pointName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取指定卡的所有点位实时数据
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <returns>只读的实时数据列表</returns>
        public static IReadOnlyList<IoPointRealTimeData> GetCardPointData(string cardName)
        {
            var query = _realtimePoints.Values
                .Where(d => d.CardName.Equals(cardName, StringComparison.OrdinalIgnoreCase));

            return query.ToList().AsReadOnly();
        }

        /// <summary>
        /// 获取指定卡的指定类型（输入/输出）点位实时数据
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="pointType">点位类型（输入/输出）</param>
        /// <returns>只读的实时数据列表</returns>
        public static IReadOnlyList<IoPointRealTimeData> GetCardPointDataByType(string cardName, string pointType)
        {
            var query = _realtimePoints.Values
                .Where(d => d.CardName.Equals(cardName, StringComparison.OrdinalIgnoreCase) &&
                           d.PointType.Equals(pointType, StringComparison.OrdinalIgnoreCase));

            return query.ToList().AsReadOnly();
        }

        /// <summary>
        /// 获取所有处于特定状态的点位数据
        /// </summary>
        /// <param name="state">要筛选的状态（true/false）</param>
        /// <returns>只读的实时数据列表</returns>
        public static IReadOnlyList<IoPointRealTimeData> GetPointsByState(bool state)
        {
            var query = _realtimePoints.Values
                .Where(d => d.IsValid && d.AsBoolean == state);

            return query.ToList().AsReadOnly();
        }

        /// <summary>
        /// 获取超时未更新的点位数据
        /// 用于检测通信异常或数据停滞的点位
        /// </summary>
        /// <param name="timeout">超时阈值</param>
        /// <returns>所有超时点位的实时数据列表（只读）</returns>
        public static IReadOnlyList<IoPointRealTimeData> GetTimeoutPoints(TimeSpan timeout)
        {
            var cutoffTime = DateTime.Now.Subtract(timeout);

            var query = _realtimePoints.Values
                .Where(d => d.LastUpdateTime < cutoffTime);

            return query.ToList().AsReadOnly();
        }

        /// <summary>
        /// 获取所有无效点位数据
        /// 用于监控系统中异常的IO点
        /// </summary>
        /// <returns>所有无效点位的实时数据列表（只读）</returns>
        public static IReadOnlyList<IoPointRealTimeData> GetInvalidPoints()
        {
            var query = _realtimePoints.Values
                .Where(d => !d.IsValid);

            return query.ToList().AsReadOnly();
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 生成点位全局唯一键
        /// 格式："卡名称_点位索引"，确保不同卡的同索引点位不冲突
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="pointIndex">点位索引</param>
        /// <returns>唯一键字符串</returns>
        public static string GetKey(string cardName, int pointIndex)
        {
            return $"{cardName}_{pointIndex}";
        }

        /// <summary>
        /// 根据数据类型获取默认值
        /// 用于初始化点位时设置初始值
        /// </summary>
        /// <param name="dataType">数据类型字符串</param>
        /// <returns>对应类型的默认值</returns>
        private static object GetDefaultValue(string dataType)
        {
            return dataType?.ToLower() switch
            {
                "bool" => false,
                "int" => 0,
                _ => false // IO点默认按bool处理
            };
        }

        /// <summary>
        /// 检查指定卡的所有点位是否正常（未超时且有效）
        /// 用于快速判断整个IO卡的通信状态
        /// </summary>
        /// <param name="cardName">IO卡名称</param>
        /// <param name="maxAllowedTimeout">允许的最大超时时间</param>
        /// <returns>是否所有点位都正常</returns>
        public static bool IsCardHealthy(string cardName, TimeSpan maxAllowedTimeout)
        {
            var cardPoints = GetCardPointData(cardName);
            if (!cardPoints.Any())
                return false;

            var cutoffTime = DateTime.Now.Subtract(maxAllowedTimeout);
            return cardPoints.All(p => p.IsValid && p.LastUpdateTime >= cutoffTime);
        }
        #endregion
    }
}
