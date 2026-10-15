using BasicClassLibrary.RexHelps;
using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.ProductData.Frm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.ProductData.Lib
{
    /// <summary>
    /// 抛料结果枚举（按吸头编号区分）
    /// 用于记录不同吸头的抛料事件，支持后续按吸头维度统计抛料率
    /// </summary>
    public enum MaterialThrowingResult
    {
        吸头一,   // 吸头1发生抛料
        吸头二,   // 吸头2发生抛料
        吸头三,   // 吸头3发生抛料
        吸头四,   // 吸头4发生抛料
        Undefined // 未定义的抛料（如设备异常导致无法识别吸头）
    }

    /// <summary>
    /// 抛料率统计管理器
    /// 核心功能：
    /// 1. 按白班/夜班（12小时制）统计各时段的抛料数据（总抛料数+各吸头抛料数）
    /// 2. 自动检测班次切换（8:00/20:00），无缝切换统计数据并持久化到CSV
    /// 3. 支持历史抛料数据查询，用于生产质量分析
    /// 设计模式：线程安全单例模式，确保全系统抛料数据的一致性
    /// </summary>
    public class MaterialThrowingManager
    {
        #region 线程安全单例模式
        /// <summary>
        /// 懒加载单例实例
        /// 使用Lazy<T>并指定线程安全模式，确保多线程环境下仅初始化一次实例
        /// 避免并发场景下的数据竞争和实例冲突
        /// </summary>
        private static readonly Lazy<MaterialThrowingManager> _lazyInstance =
            new Lazy<MaterialThrowingManager>(() => new MaterialThrowingManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局唯一实例访问入口
        /// 外部通过此属性获取管理器，确保全系统使用同一统计上下文
        /// </summary>
        public static MaterialThrowingManager Instance => _lazyInstance.Value;

        /// <summary>
        /// 私有构造函数：防止外部实例化，确保单例唯一性
        /// 初始化工作：配置班次时间、创建存储目录、加载初始数据、启动定时检查
        /// </summary>
        private MaterialThrowingManager()
        {
            // 班次切换基准时间（早上8点，行业通用白班起始时间）
            _shiftRestartHour = OutputStatistics.Instance.RestartTime;
            // 构建抛料统计数据存储路径：基于系统DataPath + 自定义目录
            _baseStatisticsPath = Path.Combine(Project.Instance.ParameterPath.DataPath, "Config", "Data", "抛料率统计");
            // 确保存储目录存在，不存在则自动创建（避免文件写入失败）
            Directory.CreateDirectory(_baseStatisticsPath);

            // 初始化班次跟踪信息：记录当前班次文件路径和类型，用于后续对比
            _lastShiftFilePath = GetCurrentShiftFilePath();
            _lastCheckedShiftType = GetCurrentShiftType();

            // 加载初始数据：当前班次和上一班次的抛料数据（从CSV读取或新建）
            CurrentShiftData = LoadShiftData(GetCurrentShiftFilePath());
            PreviousShiftData = LoadShiftData(GetPreviousShiftFilePath());

            // 启动定时检查计时器：每30秒检查一次班次是否切换（核心机制）
            // 确保无抛料事件时也能在切换时间点自动更新班次
            StartShiftCheckTimer();
        }
        #endregion

        #region 私有字段（新增班次跟踪相关）
        /// <summary>
        /// 用于线程同步的对象
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        /// 班次切换小时（固定为8点）
        /// 白班：8:00-20:00（12小时），夜班：20:00-次日8:00（12小时）
        /// </summary>
        private readonly int _shiftRestartHour;

        /// <summary>
        /// 抛料统计数据存储根目录
        /// 所有白班/夜班的CSV文件均存储于此，按日期和班次分类
        /// </summary>
        private readonly string _baseStatisticsPath;

        /// <summary>
        /// 上次记录的班次文件路径
        /// 用于对比当前路径，判断是否发生班次切换（跨天场景关键）
        /// </summary>
        private string _lastShiftFilePath;

        /// <summary>
        /// 上次检查的班次类型（白班/夜班）
        /// 用于快速判断班次是否切换（非跨天场景）
        /// </summary>
        private ShiftType _lastCheckedShiftType;

        /// <summary>
        /// 定时检查计时器
        /// 每隔30秒触发一次班次检查，确保无抛料事件时也能自动切换
        /// </summary>
        private Timer _shiftCheckTimer;
        #endregion

        #region 公共数据集合
        /// <summary>
        /// 当前班次抛料统计数据列表
        /// 结构：12个小时段数据（索引0-11） + 1条总计数据（索引12）
        /// 实时性：随抛料事件动态更新，用于UI展示当前抛料状态
        /// </summary>
        public List<ThrowingTimeSlot> CurrentShiftData { get; private set; }

        /// <summary>
        /// 上一班次抛料统计数据列表
        /// 结构同当前班次，存储已结束班次的历史数据，不实时更新
        /// 用途：生产对比分析、历史抛料趋势追溯
        /// </summary>
        public List<ThrowingTimeSlot> PreviousShiftData { get; private set; }

        /// <summary>
        /// 历史查询-白班抛料数据缓存
        /// 临时存储用户查询的指定日期白班数据，供UI展示
        /// </summary>
        public List<ThrowingTimeSlot> HistoryDayShiftData { get; private set; } = new List<ThrowingTimeSlot>();

        /// <summary>
        /// 历史查询-夜班抛料数据缓存
        /// 临时存储用户查询的指定日期夜班数据，供UI展示
        /// </summary>
        public List<ThrowingTimeSlot> HistoryNightShiftData { get; private set; } = new List<ThrowingTimeSlot>();
        #endregion

        #region 核心方法（新增班次切换逻辑）
        /// <summary>
        /// 记录单次抛料事件并自动检查班次切换
        /// 适用场景：设备发生抛料时实时调用，记录抛料的吸头编号
        /// 流程：检查班次切换 → 定位时间段 → 更新抛料数据 → 计算总计 → 持久化
        /// </summary>
        /// <param name="result">抛料的吸头编号（吸头一至四或未定义）</param>
        public void RecordThrowing(MaterialThrowingResult result)
        {
            try
            {
                // 在UI线程执行操作，避免跨线程访问控件导致的异常
                WFUtil.UpdateMainThreadUIAsync(Frm_MaterialThrowingRate.Instance, () =>
                {
                    lock (_syncRoot)
                    {
                        // 1. 核心修复点：先检查是否需要切换班次，若已到切换时间则更新数据
                        if (CheckShiftChanged())
                        {
                            SwitchShiftData(); // 执行班次切换
                        }

                        // 2. 原有抛料统计逻辑
                        string currentFilePath = GetCurrentShiftFilePath();

                        // 异常处理：若当前数据列表为空（文件损坏或初始化失败）
                        if (CurrentShiftData.Count == 0)
                        {
                            // 删除无效文件，避免残留错误数据
                            if (File.Exists(currentFilePath))
                                File.Delete(currentFilePath);
                            // 重新初始化当前班次数据结构
                            CurrentShiftData = InitializeShiftData(GetCurrentShiftType());
                            return;
                        }

                        // 获取当前时间对应的小时段索引（0-11，对应12个小时）
                        int timeSlotIndex = OutputStatistics.Instance.GetTimeIndex();
                        // 索引校验：必须在0-11范围内（排除总计行）
                        if (timeSlotIndex < 0 || timeSlotIndex >= CurrentShiftData.Count - 1)
                            throw new IndexOutOfRangeException($"无效的时间段索引：{timeSlotIndex}");

                        // 获取当前小时段的抛料数据对象
                        ThrowingTimeSlot currentSlot = CurrentShiftData[timeSlotIndex];
                        currentSlot.总抛料数++; // 先累加总抛料数

                        // 根据抛料的吸头编号，累加对应吸头的抛料数
                        switch (result)
                        {
                            case MaterialThrowingResult.吸头一:
                                currentSlot.吸头一++;
                                break;
                            case MaterialThrowingResult.吸头二:
                                currentSlot.吸头二++;
                                break;
                            case MaterialThrowingResult.吸头三:
                                currentSlot.吸头三++;
                                break;
                            case MaterialThrowingResult.吸头四:
                                currentSlot.吸头四++;
                                break;
                            case MaterialThrowingResult.Undefined:
                                // 未定义抛料需回退总抛料数（不计入统计）
                                currentSlot.总抛料数--;
                                break;
                        }

                        // 更新当前班次的总计数据（累加12个时段的结果）
                        UpdateTotalStatistics();
                        // 持久化数据到CSV文件（防止程序异常退出导致数据丢失）
                        OutputStatistics.Instance.WriteCsv(currentFilePath, CurrentShiftData);
                    }
                });
            }
            catch (Exception ex)
            {
                // 记录异常日志，包含堆栈信息便于排查
                RLog.SaveError(new Exception("更新抛料统计数据失败", ex));
            }
        }

        /// <summary>
        /// 查询指定日期的历史抛料数据
        /// 用途：生成抛料率报表、分析不同班次的抛料趋势、追溯特定日期的设备状态
        /// </summary>
        /// <param name="date">目标查询日期</param>
        public void QueryHistoryData(DateTime date)
        {
            try
            {
                // 加载指定日期的白班抛料数据
                string dayShiftPath = GetShiftFilePath(date, ShiftType.Day);
                HistoryDayShiftData = File.Exists(dayShiftPath)
                    ? OutputStatistics.Instance.ReadCsv<ThrowingTimeSlot>(dayShiftPath) // 存在则读取
                    : InitializeShiftData(ShiftType.Day); // 不存在则初始化空数据

                // 加载指定日期的夜班抛料数据（逻辑同上）
                string nightShiftPath = GetShiftFilePath(date, ShiftType.Night);
                HistoryNightShiftData = File.Exists(nightShiftPath)
                    ? OutputStatistics.Instance.ReadCsv<ThrowingTimeSlot>(nightShiftPath)
                    : InitializeShiftData(ShiftType.Night);
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception("查询历史抛料数据失败", ex));
            }
        }

        /// <summary>
        /// 清空当前班次的所有抛料数据
        /// 此操作会重置内存中的数据并覆盖CSV文件，请谨慎使用。
        /// </summary>
        public void ClearCurrentShiftThrowingData()
        {
            try
            {
                // 使用 lock 确保线程安全
                lock (_syncRoot)
                {
                    // 确保操作在主线程执行，以保持与UI更新的一致性
                    WFUtil.UpdateMainThreadUIAsync(Frm_MaterialThrowingRate.Instance, () =>
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
                            $"已手动清空当前{shiftName}的所有抛料数据。文件路径: {currentFilePath}");
                    });
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception("执行清空当前班次抛料数据操作失败。", ex));
                throw; // 向上抛出异常，通知调用方操作失败
            }
        }
        #endregion

        #region 班次切换核心逻辑（新增）
        /// <summary>
        /// 检查班次是否发生变化
        /// 判定逻辑：
        /// 1. 班次类型改变（白→夜 或 夜→白）
        /// 2. 班次文件路径改变（跨天场景，如夜班从当天20点到次日8点）
        /// 任一条件满足则判定为班次变化
        /// </summary>
        /// <returns>是否发生班次变化（true=是）</returns>
        private bool CheckShiftChanged()
        {
            // 获取当前实际班次类型和文件路径
            ShiftType currentShift = GetCurrentShiftType();
            string currentFilePath = GetCurrentShiftFilePath();

            // 对比上次记录的信息，判断是否变化
            bool isChanged = currentShift != _lastCheckedShiftType || currentFilePath != _lastShiftFilePath;
            return isChanged;
        }

        /// <summary>
        /// 执行班次切换（核心方法）
        /// 流程：保存上一班次数据 → 更新跟踪信息 → 切换数据集合 → 加载新班次数据
        /// 确保班次切换时数据不丢失，统计连续
        /// </summary>
        private void SwitchShiftData()
        {
            // 1. 保存上一班次的最终数据到CSV（避免数据丢失）
            string lastFilePath = _lastShiftFilePath;
            if (File.Exists(lastFilePath) && CurrentShiftData != null)
            {
                OutputStatistics.Instance.WriteCsv(lastFilePath, CurrentShiftData);
            }

            // 2. 更新班次跟踪信息为当前最新值
            string currentFilePath = GetCurrentShiftFilePath();
            _lastShiftFilePath = currentFilePath;
            _lastCheckedShiftType = GetCurrentShiftType();

            // 3. 切换数据集合：
            // - 上一班次数据 = 当前班次数据（深拷贝，避免引用冲突）
            // - 当前班次数据 = 新班次的初始数据（从CSV加载或新建）
            PreviousShiftData = new List<ThrowingTimeSlot>(CurrentShiftData);
            CurrentShiftData = LoadShiftData(currentFilePath);
        }

        /// <summary>
        /// 启动定时检查计时器
        /// 作用：即使没有抛料事件（如设备空闲），也能在切换时间点（8:00/20:00）自动切换班次
        /// 周期：每30秒检查一次，平衡实时性和性能消耗
        /// </summary>
        private void StartShiftCheckTimer()
        {
            _shiftCheckTimer = new Timer(_ =>
            {
                try
                {
                    // 在UI线程执行检查，避免跨线程操作异常
                    WFUtil.UpdateMainThreadUIAsync(Frm_MaterialThrowingRate.Instance, () =>
                    {
                        lock (_syncRoot)
                        {
                            // 检查班次是否变化，若变化则执行切换
                            if (CheckShiftChanged())
                            {
                                SwitchShiftData();
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    // 记录定时检查异常，不中断计时器
                    RLog.SaveError(new Exception("定时班次检查失败", ex));
                }
            },
            null,        // 计时器状态对象（未使用）
            0,           // 首次执行延迟（0=立即执行）
            30000);      // 后续执行间隔（30000毫秒=30秒）
        }
        #endregion

        #region 私有辅助方法（保留原有逻辑，优化细节）
        /// <summary>
        /// 判断当前所属班次类型（白班/夜班）
        /// 基于当前小时和班次切换时间（8点）判定
        /// </summary>
        /// <returns>当前班次类型（Day=白班，Night=夜班）</returns>
        private ShiftType GetCurrentShiftType()
        {
            int currentHour = DateTime.Now.Hour;
            // 白班：8:00-20:00（包含8点，不包含20点）
            // 夜班：20:00-次日8:00（包含20点，不包含次日8点）
            return currentHour >= _shiftRestartHour && currentHour < _shiftRestartHour + 12
                ? ShiftType.Day
                : ShiftType.Night;
        }

        /// <summary>
        /// 更新当前班次的总计统计数据
        /// 逻辑：累加前12个时段的总抛料数和各吸头抛料数，更新到总计行
        /// 确保总计数据与各时段数据一致，反映班次整体抛料情况
        /// </summary>
        private void UpdateTotalStatistics()
        {
            // 计算前12个时段的累计值（排除最后一条总计行）
            var total = new ThrowingTimeSlot
            {
                时间段 = CurrentShiftData[12].时间段, // 保留总计行的描述文本（白班总数/夜班总数）
                总抛料数 = CurrentShiftData.Take(12).Sum(slot => slot.总抛料数), // 累加总抛料数
                吸头一 = CurrentShiftData.Take(12).Sum(slot => slot.吸头一),     // 累加吸头1抛料数
                吸头二 = CurrentShiftData.Take(12).Sum(slot => slot.吸头二),     // 累加吸头2抛料数
                吸头三 = CurrentShiftData.Take(12).Sum(slot => slot.吸头三),     // 累加吸头3抛料数
                吸头四 = CurrentShiftData.Take(12).Sum(slot => slot.吸头四)      // 累加吸头4抛料数
            };
            // 更新总计行数据
            CurrentShiftData[12] = total;
        }

        /// <summary>
        /// 初始化班次抛料数据结构（空数据）
        /// 生成12个小时段的空数据 + 1条总计行，用于新班次或新文件
        /// </summary>
        /// <param name="shiftType">班次类型（白班/夜班）</param>
        /// <returns>初始化后的空数据列表</returns>
        private List<ThrowingTimeSlot> InitializeShiftData(ShiftType shiftType)
        {
            var shiftData = new List<ThrowingTimeSlot>();
            // 总计行描述文本（区分白班/夜班）
            string totalLabel = shiftType == ShiftType.Day ? "白班总数" : "夜班总数";

            if (shiftType == ShiftType.Day)
            {
                // 白班时段：8:00-20:00，每小时一个时段（共12个）
                for (int i = _shiftRestartHour; i < _shiftRestartHour + 12; i++)
                {
                    shiftData.Add(new ThrowingTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00", // 时段描述（如"8:00-9:00"）
                        总抛料数 = 0,
                        吸头一 = 0,
                        吸头二 = 0,
                        吸头三 = 0,
                        吸头四 = 0
                    });
                }
            }
            else
            {
                // 夜班第一部分：20:00-23:59（4个时段）
                for (int i = _shiftRestartHour + 12; i < 24; i++)
                {
                    shiftData.Add(new ThrowingTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        总抛料数 = 0,
                        吸头一 = 0,
                        吸头二 = 0,
                        吸头三 = 0,
                        吸头四 = 0
                    });
                }
                // 夜班第二部分：0:00-7:59（8个时段，跨天）
                for (int i = 0; i < _shiftRestartHour; i++)
                {
                    shiftData.Add(new ThrowingTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        总抛料数 = 0,
                        吸头一 = 0,
                        吸头二 = 0,
                        吸头三 = 0,
                        吸头四 = 0
                    });
                }
            }

            // 添加总计行（列表最后一条）
            shiftData.Add(new ThrowingTimeSlot
            {
                时间段 = totalLabel,
                总抛料数 = 0,
                吸头一 = 0,
                吸头二 = 0,
                吸头三 = 0,
                吸头四 = 0
            });

            return shiftData;
        }

        /// <summary>
        /// 加载班次抛料数据（从CSV文件或初始化新数据）
        /// 核心逻辑：优先读取现有文件；文件不存在/损坏时，创建新文件并初始化空数据
        /// </summary>
        /// <param name="filePath">班次数据文件路径</param>
        /// <returns>加载完成的数据列表</returns>
        private List<ThrowingTimeSlot> LoadShiftData(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    // 从CSV文件读取数据并反序列化为列表
                    return OutputStatistics.Instance.ReadCsv<ThrowingTimeSlot>(filePath);
                }
                catch (Exception ex)
                {
                    // 文件损坏时，删除无效文件并记录日志
                    RLog.SaveError(new Exception($"读取班次数据失败，将创建新文件：{filePath}", ex));
                    File.Delete(filePath);
                }
            }

            // 文件不存在或损坏时，初始化新数据
            ShiftType shiftType = filePath.Contains("白班") ? ShiftType.Day : ShiftType.Night;
            var newData = InitializeShiftData(shiftType);
            // 将空数据写入新文件，为后续抛料记录做准备
            OutputStatistics.Instance.WriteCsv(filePath, newData);
            return newData;
        }

        /// <summary>
        /// 获取当前班次的CSV文件路径
        /// 关键处理：夜班跨天场景（如凌晨3点属于前一天的夜班），调整日期为前一天
        /// 确保文件命名与实际班次周期一致
        /// </summary>
        /// <returns>当前班次文件的完整路径</returns>
        private string GetCurrentShiftFilePath()
        {
            DateTime baseDate = DateTime.Now;
            ShiftType currentShift = GetCurrentShiftType();

            // 夜班跨天处理：若当前是夜班且时间在8点前（如凌晨2点），日期取前一天
            if (currentShift == ShiftType.Night && DateTime.Now.Hour < _shiftRestartHour)
            {
                baseDate = baseDate.AddDays(-1);
            }

            return GetShiftFilePath(baseDate, currentShift);
        }

        /// <summary>
        /// 获取上一班次的CSV文件路径
        /// 逻辑：根据当前班次类型，计算上一班次的日期和类型
        /// 示例：
        /// - 当前白班 → 上一班是前一天夜班
        /// - 当前夜班（凌晨）→ 上一班是前一天白班
        /// - 当前夜班（20:00-23:59）→ 上一班是当天白班
        /// </summary>
        /// <returns>上一班次文件的完整路径</returns>
        private string GetPreviousShiftFilePath()
        {
            DateTime currentDate = DateTime.Now;
            ShiftType currentShift = GetCurrentShiftType();
            // 上一班次类型与当前相反
            ShiftType previousShift = currentShift == ShiftType.Day ? ShiftType.Night : ShiftType.Day;

            // 计算上一班次的日期
            DateTime previousDate = currentShift == ShiftType.Day
                ? currentDate.AddDays(-1) // 白班的上一班是前一天夜班
                : currentDate.Hour < _shiftRestartHour
                    ? currentDate.AddDays(-1) // 凌晨夜班的上一班是前一天白班
                    : currentDate; // 20:00-23:59夜班的上一班是当天白班

            return GetShiftFilePath(previousDate, previousShift);
        }

        /// <summary>
        /// 生成指定日期和班次的CSV文件路径（通用方法）
        /// 统一命名规范："yyyy年MM月dd日 白班/夜班.csv"
        /// 确保文件按日期和班次分类，便于管理和查询
        /// </summary>
        /// <param name="date">目标日期</param>
        /// <param name="shiftType">班次类型</param>
        /// <returns>指定班次文件的完整路径</returns>
        private string GetShiftFilePath(DateTime date, ShiftType shiftType)
        {
            string shiftName = shiftType == ShiftType.Day ? "白班" : "夜班";
            string fileName = $"{date:yyyy年MM月dd日} {shiftName}.csv";
            return Path.Combine(_baseStatisticsPath, fileName);
        }

        /// <summary>
        /// 内部班次类型枚举（仅用于管理器内部逻辑）
        /// 区分白班和夜班，用于数据统计和文件命名
        /// </summary>
        private enum ShiftType
        {
            Day,   // 白班：8:00-20:00
            Night  // 夜班：20:00-次日8:00
        }
        #endregion

        #region 资源释放（新增，防止内存泄漏）
        /// <summary>
        /// 释放资源（实现IDisposable接口）
        /// 主要释放定时计时器，防止长期运行导致的内存泄漏
        /// </summary>
        public void Dispose()
        {
            _shiftCheckTimer?.Dispose(); // 安全释放计时器（?. 避免空引用）
        }
        #endregion
    }

    /// <summary>
    /// 时间段抛料数据模型
    /// 存储单个小时段或班次总计的抛料统计指标，与CSV文件列一一对应
    /// 是抛料数据序列化、UI展示、统计计算的基础单元
    /// </summary>
    public class ThrowingTimeSlot
    {
        /// <summary>
        /// 时间段描述
        /// 两种类型：1. 小时段（如"8:00-9:00"）；2. 班次总计（如"白班总数"）
        /// </summary>
        public string 时间段 { get; set; }

        /// <summary>
        /// 该时段的总抛料数
        /// 所有吸头抛料数的总和（吸头一+吸头二+吸头三+吸头四）
        /// </summary>
        public int 总抛料数 { get; set; }

        /// <summary>
        /// 吸头1的抛料数
        /// 单独统计便于分析特定吸头的异常情况
        /// </summary>
        public int 吸头一 { get; set; }

        /// <summary>
        /// 吸头2的抛料数
        /// </summary>
        public int 吸头二 { get; set; }

        /// <summary>
        /// 吸头3的抛料数
        /// </summary>
        public int 吸头三 { get; set; }

        /// <summary>
        /// 吸头4的抛料数
        /// </summary>
        public int 吸头四 { get; set; }
    }
}