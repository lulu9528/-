using BasicClassLibrary.RexHelps;
using CsvHelper;
using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.ProductData.Frm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.ProductData.Lib
{
    /// <summary>
    /// 设备运行状态枚举（用于稼动率统计）
    /// 定义设备在生产过程中的三种核心状态，用于区分不同状态下的时间消耗
    /// </summary>
    public enum EquipmentStatus
    {
        /// <summary>
        /// 待料状态：设备因缺少原材料、半成品等物料而暂停运行
        /// </summary>
        WaitingForMaterial,

        /// <summary>
        /// 正常运行状态：设备按生产流程正常执行作业（有效生产时间）
        /// </summary>
        Running,

        /// <summary>
        /// 异常状态：设备因故障、报警、人工干预等原因无法正常运行
        /// </summary>
        Abnormal
    }

    /// <summary>
    /// 稼动率统计管理器
    /// 核心功能：记录设备在不同状态（运行/待料/异常）的时间分布，按班次（白班/夜班）进行小时级统计，并持久化到CSV文件
    /// 稼动率=（运行时间÷总时间）×100%，是衡量设备有效利用率的关键指标
    /// 采用线程安全的单例模式，确保全系统数据统计的一致性
    /// 修复点：新增班次切换检测，确保白夜班切换时自动重置数据并切换文件
    /// </summary>
    public class EquipmentUtilizationManager
    {
        #region 单例模式（线程安全）
        /// <summary>
        /// 懒加载单例实例，通过Lazy实现线程安全初始化
        /// LazyThreadSafetyMode.ExecutionAndPublication确保多线程环境下仅初始化一次
        /// </summary>
        private static readonly Lazy<EquipmentUtilizationManager> _lazyInstance =
            new Lazy<EquipmentUtilizationManager>(() => new EquipmentUtilizationManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局唯一实例访问入口
        /// 外部通过此属性获取管理器实例，保证统计逻辑的一致性
        /// </summary>
        public static EquipmentUtilizationManager Instance => _lazyInstance.Value;

        /// <summary>
        /// 私有构造函数，防止外部直接实例化，确保单例模式的唯一性
        /// 初始化时完成基础配置、数据加载和班次跟踪初始化
        /// </summary>
        private EquipmentUtilizationManager()
        {
            // 初始化班次切换时间（默认早8点切换白班）
            _shiftRestartHour = OutputStatistics.Instance.RestartTime;
            // 定义稼动率统计数据的基础存储路径（基于系统配置的DataPath）
            _baseStatisticsPath = Path.Combine(Project.Instance.ParameterPath.DataPath, "Config", "Data", "稼动率统计");
            // 确保存储目录存在，不存在则自动创建
            Directory.CreateDirectory(_baseStatisticsPath);

            // 初始化时加载当前班次和上一班次的数据（从CSV文件或新建）
            CurrentShiftData = LoadShiftData(GetCurrentShiftFilePath());
            PreviousShiftData = LoadShiftData(GetPreviousShiftFilePath());

            // 初始化班次跟踪：记录首次启动时的当前班次（核心修复点）
            _lastRecordedShiftType = GetCurrentShiftType();
        }
        #endregion

        #region 私有字段
        /// <summary>
        /// 用于线程同步的对象
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        /// 班次切换小时（默认8点）
        /// 白班从8点开始，夜班从20点（8+12）开始，用于判断当前属于白班还是夜班
        /// </summary>
        private readonly int _shiftRestartHour;

        /// <summary>
        /// 稼动率统计数据的基础存储路径
        /// 所有班次的CSV统计文件均存储在此目录下，确保数据持久化
        /// </summary>
        private readonly string _baseStatisticsPath;

        /// <summary>
        /// 上一次记录状态时的班次类型（核心修复点）
        /// 用于检测班次是否切换，触发数据重置和文件切换
        /// </summary>
        private ShiftType _lastRecordedShiftType;
        #endregion

        #region 公共数据集合
        /// <summary>
        /// 当班稼动率数据集合（按小时分段统计）
        /// 包含12个小时段数据+1条总计数据（索引12为总计）
        /// 实时更新当前班次的设备状态时间分布，供UI展示和数据持久化
        /// 班次切换时会自动重新加载为新班次的空数据/历史数据
        /// </summary>
        public List<TimeSlotUtilization> CurrentShiftData { get; private set; }

        /// <summary>
        /// 上一班次稼动率数据集合（按小时分段统计）
        /// 包含12个小时段数据+1条总计数据
        /// 记录已结束班次的设备状态时间分布，不实时更新，用于对比分析
        /// 班次切换时会自动更新为刚结束的班次数据
        /// </summary>
        public List<TimeSlotUtilization> PreviousShiftData { get; private set; }

        /// <summary>
        /// 历史查询-白班数据集合
        /// 用于存储查询指定日期的白班稼动率数据，供历史报表展示
        /// </summary>
        public List<TimeSlotUtilization> HistoryDayShiftData { get; private set; } = new List<TimeSlotUtilization>();

        /// <summary>
        /// 历史查询-夜班数据集合
        /// 用于存储查询指定日期的夜班稼动率数据，供历史报表展示
        /// </summary>
        public List<TimeSlotUtilization> HistoryNightShiftData { get; private set; } = new List<TimeSlotUtilization>();
        #endregion

        #region 核心方法
        /// <summary>
        /// 记录设备状态变化，更新对应时间段的稼动率数据
        /// 设计为每秒调用一次，累加当前状态的时间（运行/异常），并自动计算待料时间
        /// 核心优化：
        /// 1. 新增班次切换检测，触发数据重置和文件切换
        /// 2. 确保单小时内运行时间+异常时间+待料时间=3600秒，避免时间溢出
        /// </summary>
        /// <param name="status">设备当前的运行状态（运行/待料/异常）</param>
        public void RecordStatus(EquipmentStatus status)
        {
            try
            {
                // 确保UI操作在主线程执行（避免跨线程UI访问异常）
                WFUtil.UpdateMainThreadUIAsync(Frm_UtilizationRate.Instance, () =>
                {
                    lock (_syncRoot)
                    {
                        // 核心修复点1：检测班次是否切换（当前班次≠上一次记录的班次）
                        ShiftType currentShift = GetCurrentShiftType();
                        if (currentShift != _lastRecordedShiftType)
                        {
                            // 步骤1：保存上一班次的最终数据（确保数据不丢失）
                            string lastShiftFilePath = GetShiftFilePathByShiftType(_lastRecordedShiftType);
                            if (File.Exists(lastShiftFilePath) && CurrentShiftData != null && CurrentShiftData.Count > 0)
                            {
                                OutputStatistics.Instance.WriteCsv(lastShiftFilePath, CurrentShiftData);
                            }

                            // 步骤2：重新加载新班次数据（当前班次=新班次，上一班次=刚结束的班次）
                            CurrentShiftData = LoadShiftData(GetCurrentShiftFilePath()); // 加载新班次数据（自动创建新文件）
                            PreviousShiftData = LoadShiftData(GetPreviousShiftFilePath()); // 加载刚结束的班次数据

                            // 步骤3：更新班次跟踪字段（记录当前班次为上一次班次）
                            _lastRecordedShiftType = currentShift;
                        }

                        // 获取当前班次的CSV文件路径（班次切换后已自动更新为新文件路径）
                        string currentFilePath = GetCurrentShiftFilePath();

                        // 若当前班次数据为空（首次初始化或文件损坏），则重新初始化
                        if (CurrentShiftData.Count == 0)
                        {
                            // 删除无效文件（若存在）
                            if (File.Exists(currentFilePath))
                                File.Delete(currentFilePath);
                            // 初始化当前班次数据（按白班/夜班生成小时段）
                            CurrentShiftData = InitializeShiftData(GetCurrentShiftType());
                            return;
                        }

                        // 检测小时段切换（如从8:59到9:00），并修正上一小时数据
                        HandleHourTransition();

                        // 获取当前时间段索引（对应0-11的小时段）
                        int timeSlotIndex = GetCurrentTimeSlotIndex();
                        // 校验索引有效性（必须在0-11范围内，对应12个小时段）
                        if (timeSlotIndex < 0 || timeSlotIndex >= CurrentShiftData.Count - 1)
                            throw new IndexOutOfRangeException("无效的时间段索引，超出班次时间范围（0-11）");

                        // 获取当前小时段的数据对象
                        TimeSlotUtilization currentSlot = CurrentShiftData[timeSlotIndex];
                        // 解析当前运行时间和异常时间的秒数（便于计算）
                        int currentRunning = SecondsByDateTime(currentSlot.运行时间);
                        int currentAbnormal = SecondsByDateTime(currentSlot.异常时间);
                        const int maxSecondsPerHour = 3600; // 单小时总秒数固定为3600

                        // 基于当前状态更新时间（限制运行+异常时间总和不超过3600秒）
                        switch (status)
                        {
                            case EquipmentStatus.Running:
                                // 计算剩余可分配秒数（确保不超过3600）
                                int remainingRunning = maxSecondsPerHour - currentRunning - currentAbnormal;
                                // 若有剩余时间则累加1秒，否则不再增加
                                currentSlot.运行时间 = SecondsToTimeString(currentRunning + (remainingRunning > 0 ? 1 : 0));
                                break;
                            case EquipmentStatus.Abnormal:
                                // 计算剩余可分配秒数
                                int remainingAbnormal = maxSecondsPerHour - currentRunning - currentAbnormal;
                                // 若有剩余时间则累加1秒，否则不再增加
                                currentSlot.异常时间 = SecondsToTimeString(currentAbnormal + (remainingAbnormal > 0 ? 1 : 0));
                                break;
                                // 待料时间通过总时间-运行-异常计算，无需直接累加
                        }

                        // 重新计算待料时间（确保三者总和恒为3600秒）
                        int runningSeconds = SecondsByDateTime(currentSlot.运行时间);
                        int abnormalSeconds = SecondsByDateTime(currentSlot.异常时间);
                        currentSlot.待料时间 = SecondsToTimeString(maxSecondsPerHour - runningSeconds - abnormalSeconds);

                        // 更新当前班次的总计数据（列表最后一条为总计）
                        UpdateTotalStatistics();

                        // 将更新后的数据持久化到CSV文件（确保数据不丢失）
                        OutputStatistics.Instance.WriteCsv(currentFilePath, CurrentShiftData);
                    }
                });
            }
            catch (Exception ex)
            {
                // 记录异常日志，便于排查问题（不中断程序执行）
                RLog.SaveError(new Exception("[稼动率统计] 记录设备状态失败", ex));
            }
        }

        /// <summary>
        /// 查询指定日期的历史稼动率数据（白班和夜班）
        /// 用于历史数据追溯、报表生成和稼动率趋势分析
        /// </summary>
        /// <param name="date">要查询的日期</param>
        public void QueryHistoryData(DateTime date)
        {
            try
            {
                // 加载指定日期的白班数据
                string dayShiftPath = GetShiftFilePath(date, ShiftType.Day);
                HistoryDayShiftData = File.Exists(dayShiftPath)
                    ? OutputStatistics.Instance.ReadCsv<TimeSlotUtilization>(dayShiftPath) // 从CSV读取
                    : InitializeShiftData(ShiftType.Day); // 文件不存在则初始化空数据

                // 加载指定日期的夜班数据
                string nightShiftPath = GetShiftFilePath(date, ShiftType.Night);
                HistoryNightShiftData = File.Exists(nightShiftPath)
                    ? OutputStatistics.Instance.ReadCsv<TimeSlotUtilization>(nightShiftPath) // 从CSV读取
                    : InitializeShiftData(ShiftType.Night); // 文件不存在则初始化空数据
            }
            catch (Exception ex)
            {
                // 记录查询失败的异常日志
                RLog.SaveError(new Exception("[稼动率统计] 查询历史数据失败", ex));
            }
        }

        /// <summary>
        /// 清空当前班次的所有稼动率数据
        /// 此操作会重置内存中的数据并覆盖CSV文件，请谨慎使用。
        /// </summary>
        public void ClearCurrentShiftUtilization()
        {
            try
            {
                // 使用 lock 确保线程安全
                lock (_syncRoot)
                {
                    // 确保操作在主线程执行，以保持与UI更新的一致性
                    WFUtil.UpdateMainThreadUIAsync(Frm_UtilizationRate.Instance, () =>
                    {
                        string currentFilePath = GetCurrentShiftFilePath();
                        ShiftType currentShiftType = GetCurrentShiftType();

                        // 重新初始化当前班次数据，这会创建一个全新的、干净的数据结构
                        CurrentShiftData = InitializeShiftData(currentShiftType);

                        // 将空数据立即写入文件，确保数据持久化
                        OutputStatistics.Instance.WriteCsv(currentFilePath, CurrentShiftData);

                        // 记录清空操作日志，便于审计和追溯
                        string shiftName = currentShiftType == ShiftType.Day ? "白班" : "夜班";
                        RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Red,
                            $"已手动清空当前{shiftName}的所有稼动率数据。文件路径: {currentFilePath}");
                    });
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception("执行清空当前班次稼动率数据操作失败。", ex));
                throw; // 向上抛出异常，通知调用方操作失败
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 处理小时段切换（如从8:59到9:00）
        /// 确保上一小时的统计数据总和严格等于3600秒，避免跨小时统计误差
        /// </summary>
        private void HandleHourTransition()
        {
            DateTime now = DateTime.Now;
            int currentHour = now.Hour; // 当前小时（如9点）
            int previousHour = now.AddSeconds(-1).Hour; // 上一秒的小时（如8点）

            // 检测是否跨小时（当前小时与上一秒小时不同）
            if (currentHour != previousHour)
            {
                // 计算上一小时对应的时间段索引（如8点对应白班索引0）
                int previousTimeSlotIndex = GetTimeSlotIndexByHour(previousHour, GetCurrentShiftType());
                // 校验索引有效性（必须是当前班次内的有效小时段）
                if (previousTimeSlotIndex >= 0 && previousTimeSlotIndex < CurrentShiftData.Count - 1)
                {
                    TimeSlotUtilization previousSlot = CurrentShiftData[previousTimeSlotIndex];
                    int running = SecondsByDateTime(previousSlot.运行时间);
                    int abnormal = SecondsByDateTime(previousSlot.异常时间);
                    const int maxSeconds = 3600; // 单小时总秒数

                    // 若上一小时的运行+异常时间超过3600秒，进行修正
                    if (running + abnormal > maxSeconds)
                    {
                        // 超出时优先保证运行时间（取运行时间与3600的较小值）
                        previousSlot.运行时间 = SecondsToTimeString(Math.Min(running, maxSeconds));
                        // 异常时间修正为剩余秒数（确保总和=3600）
                        previousSlot.异常时间 = SecondsToTimeString(maxSeconds - SecondsByDateTime(previousSlot.运行时间));
                    }
                    // 重新计算待料时间（确保总和=3600）
                    previousSlot.待料时间 = SecondsToTimeString(maxSeconds - SecondsByDateTime(previousSlot.运行时间) - SecondsByDateTime(previousSlot.异常时间));
                }
            }
        }

        /// <summary>
        /// 根据小时值计算对应的时间段索引（0-11）
        /// 用于小时段切换时定位上一小时的数据位置
        /// </summary>
        /// <param name="hour">小时值（0-23）</param>
        /// <param name="shiftType">当前班次类型（白班/夜班）</param>
        /// <returns>时间段索引（0-11，无效时返回-1）</returns>
        private int GetTimeSlotIndexByHour(int hour, ShiftType shiftType)
        {
            if (shiftType == ShiftType.Day)
            {
                // 白班时间范围：8:00-20:00（8<=hour<20）
                return hour >= _shiftRestartHour && hour < _shiftRestartHour + 12
                    ? hour - _shiftRestartHour // 8点→0，9点→1，...，19点→11
                    : -1; // 超出白班范围返回无效索引
            }
            else
            {
                // 夜班时间范围：20:00-次日8:00
                if (hour >= _shiftRestartHour + 12) // 20:00-23:59（20<=hour<24）
                    return hour - (_shiftRestartHour + 12); // 20点→0，21点→1，...，23点→3
                else if (hour < _shiftRestartHour) // 0:00-7:59（0<=hour<8）
                    return hour + (24 - (_shiftRestartHour + 12)); // 0点→4，1点→5，...，7点→11
                else
                    return -1; // 超出夜班范围（8-19点）返回无效索引
            }
        }

        /// <summary>
        /// 判断当前属于白班还是夜班
        /// 白班：8:00-20:00（共12小时）；夜班：20:00-次日8:00（共12小时）
        /// </summary>
        /// <returns>当前班次类型（Day/Night）</returns>
        private ShiftType GetCurrentShiftType()
        {
            int currentHour = DateTime.Now.Hour;
            // 白班判断：当前小时 >= 8点且 < 20点（8+12）
            return currentHour >= _shiftRestartHour && currentHour < _shiftRestartHour + 12
                ? ShiftType.Day
                : ShiftType.Night;
        }

        /// <summary>
        /// 计算当前时间在所属班次中的时间段索引（0-11）
        /// 每个班次分为12个小时段，每个时段对应一个索引，用于定位当前小时的数据槽
        /// </summary>
        /// <returns>时间段索引（0-11）</returns>
        private int GetCurrentTimeSlotIndex()
        {
            int currentHour = DateTime.Now.Hour;
            ShiftType currentShift = GetCurrentShiftType();

            if (currentShift == ShiftType.Day)
            {
                // 白班：8点对应索引0，9点索引1...19点索引11（共12小时）
                return currentHour - _shiftRestartHour;
            }
            else
            {
                // 夜班：20点对应索引0，21点索引1...23点索引3（前4小时）
                if (currentHour >= _shiftRestartHour + 12)
                {
                    return currentHour - (_shiftRestartHour + 12);
                }
                // 夜班：0点对应索引4，1点索引5...7点索引11（后8小时，跨天部分）
                else
                {
                    return currentHour + (24 - (_shiftRestartHour + 12));
                }
            }
        }

        /// <summary>
        /// 更新当前班次的总计统计数据（列表最后一条为总计行）
        /// 计算12个小时段的时间总和，更新到总计行，用于展示班次整体稼动率
        /// </summary>
        private void UpdateTotalStatistics()
        {
            // 计算前12小时段的时间总和（Take(12)排除最后一条总计行）
            int totalRunning = CurrentShiftData.Take(12).Sum(slot => SecondsByDateTime(slot.运行时间));
            int totalAbnormal = CurrentShiftData.Take(12).Sum(slot => SecondsByDateTime(slot.异常时间));
            int totalWaiting = CurrentShiftData.Take(12).Sum(slot => SecondsByDateTime(slot.待料时间));

            // 更新列表中的总计行（索引12）
            TimeSlotUtilization totalSlot = CurrentShiftData[12];
            totalSlot.运行时间 = SecondsToTimeString(totalRunning);
            totalSlot.异常时间 = SecondsToTimeString(totalAbnormal);
            totalSlot.待料时间 = SecondsToTimeString(totalWaiting);
        }

        /// <summary>
        /// 初始化班次数据结构（按小时分段创建空数据）
        /// 生成12个小时段的空数据+1条总计行，用于新班次或新文件的初始化
        /// </summary>
        /// <param name="shiftType">班次类型（白班/夜班）</param>
        /// <returns>初始化后的班次数据列表</returns>
        private List<TimeSlotUtilization> InitializeShiftData(ShiftType shiftType)
        {
            var shiftData = new List<TimeSlotUtilization>();
            // 总计行的描述文本（白班/夜班区分）
            string totalLabel = shiftType == ShiftType.Day ? "白班总数" : "夜班总数";

            if (shiftType == ShiftType.Day)
            {
                // 白班时段：8:00-20:00（每小时一个段，共12段）
                for (int i = _shiftRestartHour; i < _shiftRestartHour + 12; i++)
                {
                    shiftData.Add(new TimeSlotUtilization
                    {
                        时间段 = $"{i}:00-{i + 1}:00", // 时间段描述（如"8:00-9:00"）
                        运行时间 = "00:00:00", // 初始运行时间0秒
                        待料时间 = "01:00:00", // 初始待料时间1小时（3600秒）
                        异常时间 = "00:00:00"  // 初始异常时间0秒
                    });
                }
            }
            else
            {
                // 夜班时段：20:00-23:59（4个时段）
                for (int i = _shiftRestartHour + 12; i < 24; i++)
                {
                    shiftData.Add(new TimeSlotUtilization
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        运行时间 = "00:00:00",
                        待料时间 = "01:00:00",
                        异常时间 = "00:00:00"
                    });
                }
                // 夜班时段：0:00-7:59（8个时段，跨天部分）
                for (int i = 0; i < _shiftRestartHour; i++)
                {
                    shiftData.Add(new TimeSlotUtilization
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        运行时间 = "00:00:00",
                        待料时间 = "01:00:00",
                        异常时间 = "00:00:00"
                    });
                }
            }

            // 添加总计行（列表最后一条）
            shiftData.Add(new TimeSlotUtilization
            {
                时间段 = totalLabel,
                运行时间 = "00:00:00",
                待料时间 = "12:00:00", // 初始总计待料时间12小时（43200秒）
                异常时间 = "00:00:00"
            });

            return shiftData;
        }

        /// <summary>
        /// 加载班次数据（从CSV文件读取或初始化新数据）
        /// 用于初始化时加载当前/上一班次数据，或历史查询时加载数据
        /// 班次切换时用于加载新班次的空数据/历史数据
        /// </summary>
        /// <param name="filePath">班次数据CSV文件路径</param>
        /// <returns>加载后的班次数据列表</returns>
        private List<TimeSlotUtilization> LoadShiftData(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    // 文件存在时，从CSV读取数据并反序列化为列表
                    var data = OutputStatistics.Instance.ReadCsv<TimeSlotUtilization>(filePath);
                    return data;
                }
                catch (Exception ex)
                {
                    // 读取失败（文件损坏），删除无效文件并初始化新数据
                    RLog.SaveError(new Exception($"[稼动率统计] 读取数据文件失败，将创建新文件：{filePath}", ex));
                    File.Delete(filePath);
                }
            }

            // 文件不存在时，根据文件名称判断班次类型并初始化新数据
            ShiftType shiftType = filePath.Contains("白班") ? ShiftType.Day : ShiftType.Night;
            var newData = InitializeShiftData(shiftType);
            // 将初始化的空数据写入文件（创建新文件）
            OutputStatistics.Instance.WriteCsv(filePath, newData);
            return newData;
        }

        /// <summary>
        /// 获取当前班次的CSV文件路径
        /// 路径格式：[基础路径]\yyyy年MM月dd日 白班.csv 或 [基础路径]\yyyy年MM月dd日 夜班.csv
        /// 核心处理：夜班跨天时，日期取前一天（如凌晨3点的夜班属于前一天）
        /// </summary>
        /// <returns>当前班次数据文件的完整路径</returns>
        private string GetCurrentShiftFilePath()
        {
            DateTime baseDate = DateTime.Now;
            ShiftType currentShift = GetCurrentShiftType();

            // 夜班跨天处理：若当前是夜班且时间在8点前（如凌晨2点），则日期取前一天
            if (currentShift == ShiftType.Night && DateTime.Now.Hour < _shiftRestartHour)
            {
                baseDate = baseDate.AddDays(-1);
            }

            // 生成并返回当前班次的文件路径
            return GetShiftFilePath(baseDate, currentShift);
        }

        /// <summary>
        /// 获取上一班次的CSV文件路径
        /// 上一班次为当前班次的前一个班次（白班的上一班是前一天夜班，夜班的上一班可能是当天/前一天白班）
        /// </summary>
        /// <returns>上一班次数据文件的完整路径</returns>
        private string GetPreviousShiftFilePath()
        {
            DateTime currentDate = DateTime.Now;
            ShiftType currentShift = GetCurrentShiftType();
            // 上一班次类型（与当前班次相反）
            ShiftType previousShift = currentShift == ShiftType.Day ? ShiftType.Night : ShiftType.Day;

            // 计算上一班次的日期：
            // - 白班的上一班是前一天夜班（日期-1）
            // - 夜班的上一班：若当前时间在8点前（凌晨），则是前一天白班；否则是当天白班
            DateTime previousDate = currentShift == ShiftType.Day
                ? currentDate.AddDays(-1)
                : currentDate.Hour < _shiftRestartHour
                    ? currentDate.AddDays(-1)
                    : currentDate;

            // 生成并返回上一班次的文件路径
            return GetShiftFilePath(previousDate, previousShift);
        }

        /// <summary>
        /// 生成指定日期和班次的CSV文件路径
        /// 统一路径生成逻辑，确保文件命名规范一致
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="shiftType">班次类型（白班/夜班）</param>
        /// <returns>指定班次数据文件的完整路径</returns>
        private string GetShiftFilePath(DateTime date, ShiftType shiftType)
        {
            // 班次名称（白班/夜班）
            string shiftName = shiftType == ShiftType.Day ? "白班" : "夜班";
            // 文件名格式：yyyy年MM月dd日 白班.csv 或 yyyy年MM月dd日 夜班.csv
            string fileName = $"{date:yyyy年MM月dd日} {shiftName}.csv";
            // 组合完整路径（基础路径+文件名）
            return Path.Combine(_baseStatisticsPath, fileName);
        }

        /// <summary>
        /// 辅助方法：根据班次类型获取对应的文件路径（用于班次切换时保存上一班次数据）
        /// </summary>
        /// <param name="shiftType">要获取路径的班次类型</param>
        /// <returns>对应班次的CSV文件路径</returns>
        private string GetShiftFilePathByShiftType(ShiftType shiftType)
        {
            DateTime date = DateTime.Now;
            // 夜班跨天处理：若班次是夜班且当前时间在8点前，日期取前一天
            if (shiftType == ShiftType.Night && DateTime.Now.Hour < _shiftRestartHour)
            {
                date = date.AddDays(-1);
            }
            // 白班直接使用当天日期
            else if (shiftType == ShiftType.Day)
            {
                date = date.Date;
            }
            return GetShiftFilePath(date, shiftType);
        }

        /// <summary>
        /// 内部班次类型枚举（仅用于管理器内部判断，不对外暴露）
        /// 区分白班和夜班，用于数据统计和文件命名
        /// </summary>
        private enum ShiftType
        {
            /// <summary>
            /// 白班（8:00-20:00，共12小时）
            /// </summary>
            Day,

            /// <summary>
            /// 夜班（20:00-次日8:00，共12小时）
            /// </summary>
            Night
        }

        /// <summary>
        /// 将秒数转换为HH:mm:ss格式的时间字符串（如3661秒→01:01:01）
        /// 用于格式化显示时间，符合用户阅读习惯
        /// </summary>
        /// <param name="seconds">要转换的秒数</param>
        /// <returns>HH:mm:ss格式的时间字符串</returns>
        public string SecondsToTimeString(int seconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        /// <summary>
        /// 将HH:mm:ss格式的时间字符串转换为总秒数（如01:01:01→3661秒）
        /// 用于时间计算（累加、求和等）
        /// </summary>
        /// <param name="dateTime">HH:mm:ss格式的时间字符串</param>
        /// <returns>对应的总秒数（解析失败返回0）</returns>
        public int SecondsByDateTime(string dateTime)
        {
            // 分割字符串获取时、分、秒（格式必须为HH:mm:ss）
            string[] parts = dateTime.Split(':');
            if (parts.Length != 3) return 0; // 格式错误返回0

            // 安全解析（避免格式错误导致异常）
            int hours = int.TryParse(parts[0], out int h) ? h : 0;
            int minutes = int.TryParse(parts[1], out int m) ? m : 0;
            int seconds = int.TryParse(parts[2], out int s) ? s : 0;

            // 计算总秒数（小时×3600 + 分钟×60 + 秒）
            return hours * 3600 + minutes * 60 + seconds;
        }
        #endregion
    }

    /// <summary>
    /// 时间段稼动率数据模型
    /// 用于存储单个小时段内设备在不同状态（运行/待料/异常）的时间分布
    /// 是数据持久化（CSV）和UI展示的基础数据结构
    /// </summary>
    public class TimeSlotUtilization
    {
        /// <summary>
        /// 时间段描述（如"8:00-9:00"、"白班总数"）
        /// 用于标识当前数据对应的时间范围
        /// </summary>
        public string 时间段 { get; set; }

        /// <summary>
        /// 本时间段内的设备运行时间（格式化：HH:mm:ss）
        /// 反映设备有效生产的时间长度，是计算稼动率的核心指标
        /// </summary>
        public string 运行时间 { get; set; }

        /// <summary>
        /// 本时间段内的设备待料时间（格式化：HH:mm:ss）
        /// 反映设备因缺料而闲置的时间长度，用于分析生产物料管理效率
        /// </summary>
        public string 待料时间 { get; set; }

        /// <summary>
        /// 本时间段内的设备异常时间（格式化：HH:mm:ss）
        /// 反映设备因故障等原因无法运行的时间长度，用于设备维护分析
        /// </summary>
        public string 异常时间 { get; set; }
    }
}