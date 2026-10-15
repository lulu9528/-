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

namespace Jabil.Module.ProductData.Lib
{
    /// <summary>
    /// NG类型枚举
    /// 与 NgTimeSlot 类的字段一一对应，用于明确NG事件的具体分类
    /// 扩展说明：新增NG类型时，需同步修改此类和 NgTimeSlot 类
    /// </summary>
    public enum NgCategory
    {
        压力NG,       // 因压力异常导致的NG（如真空度不足、压力传感器报警）
        量测NG,       // 因测量结果超出公差导致的NG（如尺寸偏差、位置偏移）
        多种NG,       // 其他未归类的NG类型（兜底分类，避免漏统计）
        Undefined     // 未定义的NG类型
    }

    /// <summary>
    /// NG统计管理器
    /// 核心功能：
    /// 1. 按白班/夜班（12小时制）统计各时段NG数据（总NG数+分类NG数）
    /// 2. 自动检测班次切换（基准时间：OutputStatistics.Instance.RestartTime，默认8点）
    ///    - 白班：RestartTime ~ RestartTime+12小时（如8:00-20:00）
    ///    - 夜班：RestartTime+12小时 ~ 次日RestartTime（如20:00-次日8:00）
    /// 3. 数据持久化：自动保存到CSV文件（路径：Data/Config/Data/NG统计）
    /// 4. 历史数据查询：支持指定日期的白班/夜班数据查询
    /// 设计模式：线程安全单例模式（Lazy初始化，避免多线程冲突）
    /// 线程安全：所有UI相关操作通过WFUtil.UpdateMainThreadUIAsync调度到主线程执行
    /// </summary>
    public class NgStatisticsManager
    {
        #region 线程安全单例模式
        /// <summary>
        /// 懒加载单例实例
        /// LazyThreadSafetyMode.ExecutionAndPublication：确保多线程下仅初始化一次
        /// 优点：延迟初始化，节省资源；线程安全，避免实例冲突
        /// </summary>
        private static readonly Lazy<NgStatisticsManager> _lazyInstance =
            new Lazy<NgStatisticsManager>(() => new NgStatisticsManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局唯一实例访问入口
        /// 外部通过此属性获取管理器，确保全系统使用同一统计上下文
        /// </summary>
        public static NgStatisticsManager Instance => _lazyInstance.Value;

        /// <summary>
        /// 私有构造函数（单例模式核心：防止外部实例化）
        /// 初始化流程：
        /// 1. 读取班次切换基准时间
        /// 2. 创建统计数据存储目录
        /// 3. 初始化班次跟踪信息（上一次班次文件路径+班次类型）
        /// 4. 加载当前/上一班次数据（从CSV读取或新建）
        /// 5. 启动班次切换定时检查计时器
        /// </summary>
        private NgStatisticsManager()
        {
            // 班次切换基准时间（从OutputStatistics读取，默认8点，支持配置修改）
            _shiftRestartHour = OutputStatistics.Instance.RestartTime;
            // 统计数据存储根目录（拼接系统DataPath，确保路径合法）
            _baseStatisticsPath = Path.Combine(Project.Instance.ParameterPath.DataPath, "Config", "Data", "NG统计");
            // 确保目录存在（不存在则自动创建，避免文件写入失败）
            Directory.CreateDirectory(_baseStatisticsPath);

            // 初始化班次跟踪信息：用于后续判断班次是否切换
            _lastShiftFilePath = GetCurrentShiftFilePath(); // 上一次班次文件路径
            _lastCheckedShiftType = GetCurrentShiftType(); // 上一次班次类型（白班/夜班）

            // 加载初始数据：当前班次和上一班次（确保程序启动时已有数据可展示）
            CurrentShiftData = LoadShiftData(GetCurrentShiftFilePath());
            PreviousShiftData = LoadShiftData(GetPreviousShiftFilePath());

            // 启动班次切换定时检查（核心机制：确保无NG事件时也能自动切换班次）
            StartShiftCheckTimer();
        }
        #endregion

        #region 私有字段
        /// <summary>
        /// 用于线程同步的对象
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        /// 班次切换基准小时（如8点）
        /// 白班：_shiftRestartHour ~ _shiftRestartHour+12
        /// 夜班：_shiftRestartHour+12 ~ 次日_shiftRestartHour
        /// </summary>
        private readonly int _shiftRestartHour;

        /// <summary>
        /// 统计数据存储根目录
        /// CSV文件命名规范：{yyyy年MM月dd日} {白班/夜班}.csv（如2024年05月20日 白班.csv）
        /// </summary>
        private readonly string _baseStatisticsPath;

        /// <summary>
        /// 上一次记录的班次文件路径
        /// 用于对比当前路径，判断是否跨天班次切换（如夜班从当天20点到次日8点）
        /// </summary>
        private string _lastShiftFilePath;

        /// <summary>
        /// 上一次检查的班次类型（白班/夜班）
        /// 用于快速判断班次是否切换（非跨天场景）
        /// </summary>
        private ShiftType _lastCheckedShiftType;

        /// <summary>
        /// 班次切换定时检查计时器
        /// 作用：每30秒检查一次班次是否切换，确保无NG事件时也能及时更新
        /// 周期：30秒（平衡实时性和性能，可根据需求调整）
        /// </summary>
        private Timer _shiftCheckTimer;
        #endregion

        #region 公共数据集合
        /// <summary>
        /// 当前班次NG统计数据列表
        /// 结构：12个小时段数据（索引0-11） + 1条总计数据（索引12）
        /// 实时性：随NG事件动态更新，用于UI实时展示
        /// 示例：
        /// - 索引0：8:00-9:00的NG数据
        /// - 索引11：19:00-20:00的NG数据（白班）
        /// - 索引12：当前班次的总计NG数据
        /// </summary>
        public List<NgTimeSlot> CurrentShiftData { get; private set; }

        /// <summary>
        /// 上一班次NG统计数据列表
        /// 结构同CurrentShiftData，存储已结束班次的历史数据
        /// 用途：生产质量对比分析、历史趋势追溯
        /// 更新时机：班次切换时（如白班结束→夜班开始时，将当前班次数据赋值给此属性）
        /// </summary>
        public List<NgTimeSlot> PreviousShiftData { get; private set; }

        /// <summary>
        /// 历史查询-白班NG数据缓存
        /// 用途：存储用户查询的指定日期白班数据，供UI展示
        /// 更新时机：调用QueryHistoryData方法时
        /// </summary>
        public List<NgTimeSlot> HistoryDayShiftData { get; private set; } = new List<NgTimeSlot>();

        /// <summary>
        /// 历史查询-夜班NG数据缓存
        /// 用途：存储用户查询的指定日期夜班数据，供UI展示
        /// 更新时机：调用QueryHistoryData方法时
        /// </summary>
        public List<NgTimeSlot> HistoryNightShiftData { get; private set; } = new List<NgTimeSlot>();
        #endregion

        #region 核心方法
        /// <summary>
        /// 记录单次NG事件（核心业务方法）
        /// 调用场景：设备发生NG时，外部调用此方法传入NG类型
        /// 流程：
        /// 1. 调度到主线程执行（避免跨线程访问UI控件异常）
        /// 2. 检查班次是否切换（若切换则执行班次更新逻辑）
        /// 3. 校验当前班次数据有效性（无效则重建）
        /// 4. 获取当前时间对应的时间段索引（0-11）
        /// 5. 更新对应NG类型的计数和总NG数
        /// 6. 更新当前班次的总计数据
        /// 7. 持久化数据到CSV文件
        /// 异常处理：捕获所有异常，记录日志，确保程序稳定运行
        /// </summary>
        /// <param name="ngCategory">NG事件的具体分类（从NgCategory枚举选择）</param>
        public void RecordNgEvent(NgCategory ngCategory)
        {
            try
            {
                // 调度到主线程执行：所有操作UI控件的逻辑必须在主线程执行
                WFUtil.UpdateMainThreadUIAsync(Frm_MaterialThrowingRate.Instance, () =>
                {
                    lock (_syncRoot)
                    {
                        // 1. 检查班次是否切换（核心：确保统计数据归属正确的班次）
                        if (CheckShiftChanged())
                        {
                            SwitchShiftData(); // 执行班次切换逻辑
                        }

                        // 2. 获取当前班次的CSV文件路径
                        string currentFilePath = GetCurrentShiftFilePath();

                        // 3. 校验当前班次数据有效性（若数据为空，说明文件损坏或未初始化）
                        if (CurrentShiftData.Count == 0)
                        {
                            // 删除损坏的文件（避免残留错误数据）
                            if (File.Exists(currentFilePath))
                                File.Delete(currentFilePath);
                            // 重建当前班次数据结构（空数据）
                            CurrentShiftData = InitializeShiftData(GetCurrentShiftType());
                            return; // 重建后直接返回，等待下一次NG事件记录
                        }

                        // 4. 获取当前时间对应的时间段索引（0-11，对应12个小时段）
                        int timeSlotIndex = OutputStatistics.Instance.GetTimeIndex();
                        // 校验索引有效性（排除总计行，确保不越界）
                        if (timeSlotIndex < 0 || timeSlotIndex >= CurrentShiftData.Count - 1)
                            throw new IndexOutOfRangeException($"无效的时间段索引：{timeSlotIndex}（需在0-11范围内）");

                        // 5. 更新当前时间段的NG数据
                        NgTimeSlot currentSlot = CurrentShiftData[timeSlotIndex];
                        // 根据NG类型累加对应计数（直接操作字段，高效简洁）
                        switch (ngCategory)
                        {
                            case NgCategory.压力NG:
                                currentSlot.压力NG++;
                                break;
                            case NgCategory.量测NG:
                                currentSlot.量测NG++;
                                break;
                            case NgCategory.多种NG:
                                currentSlot.多种NG++;
                                break;
                        }
                        // 手动计算总NG数（确保总计数与分类计数一致）
                        currentSlot.总NG数 = currentSlot.压力NG + currentSlot.量测NG + currentSlot.多种NG;

                        // 6. 更新当前班次的总计数据（累加12个时间段的结果）
                        UpdateTotalStatistics();

                        // 7. 持久化数据到CSV文件（防止程序异常退出导致数据丢失）
                        OutputStatistics.Instance.WriteCsv(currentFilePath, CurrentShiftData);
                    }
                });
            }
            catch (Exception ex)
            {
                // 记录异常日志（包含堆栈信息，便于排查问题）
                RLog.SaveError(new Exception("记录NG事件失败", ex));
            }
        }

        /// <summary>
        /// 查询指定日期的历史NG数据
        /// 调用场景：用户需要查看某一天的白班/夜班NG统计报表
        /// 流程：
        /// 1. 构建指定日期的白班/夜班CSV文件路径
        /// 2. 若文件存在，读取数据；若不存在，初始化空数据
        /// 3. 将数据赋值给历史数据缓存属性（供UI展示）
        /// 异常处理：捕获文件读取异常，记录日志，确保不影响主流程
        /// </summary>
        /// <param name="date">目标查询日期（如2024-05-20）</param>
        public void QueryHistoryNgData(DateTime date)
        {
            try
            {
                // 1. 加载指定日期的白班数据
                string dayShiftPath = GetShiftFilePath(date, ShiftType.Day);
                HistoryDayShiftData = File.Exists(dayShiftPath)
                    ? OutputStatistics.Instance.ReadCsv<NgTimeSlot>(dayShiftPath) // 存在则读取
                    : InitializeShiftData(ShiftType.Day); // 不存在则初始化空数据（便于UI展示默认状态）

                // 2. 加载指定日期的夜班数据（逻辑同上）
                string nightShiftPath = GetShiftFilePath(date, ShiftType.Night);
                HistoryNightShiftData = File.Exists(nightShiftPath)
                    ? OutputStatistics.Instance.ReadCsv<NgTimeSlot>(nightShiftPath)
                    : InitializeShiftData(ShiftType.Night);
            }
            catch (Exception ex)
            {
                // 记录查询异常日志
                RLog.SaveError(new Exception("查询历史NG数据失败", ex));
            }
        }

        /// <summary>
        /// 清空当前班次的所有NG分类数据
        /// 此操作会重置内存中的数据并覆盖CSV文件，请谨慎使用。
        /// </summary>
        public void ClearCurrentShiftNgData()
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
                            $"已手动清空当前{shiftName}的所有NG分类数据。文件路径: {currentFilePath}");
                    });
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception("执行清空当前班次NG分类数据操作失败。", ex));
                throw; // 向上抛出异常，通知调用方操作失败
            }
        }
        #endregion

        #region 班次切换核心逻辑
        /// <summary>
        /// 检查班次是否发生变化（核心判定逻辑）
        /// 判定条件（满足任一即可）：
        /// 1. 班次类型变化（白班→夜班 或 夜班→白班）
        /// 2. 班次文件路径变化（跨天场景：如夜班从当天20点到次日8点，日期会变化）
        /// 作用：确保及时发现班次切换，避免数据统计到错误的班次
        /// </summary>
        /// <returns>是否发生班次变化（true=是，false=否）</returns>
        private bool CheckShiftChanged()
        {
            // 获取当前实际的班次类型和文件路径
            ShiftType currentShift = GetCurrentShiftType();
            string currentFilePath = GetCurrentShiftFilePath();

            // 对比上一次记录的信息，判断是否变化
            bool isChanged = currentShift != _lastCheckedShiftType || currentFilePath != _lastShiftFilePath;
            return isChanged;
        }

        /// <summary>
        /// 执行班次切换逻辑（核心方法）
        /// 切换流程：
        /// 1. 保存上一班次的最终数据到CSV（避免数据丢失）
        /// 2. 更新班次跟踪信息（记录当前班次路径和类型）
        /// 3. 切换数据集合（上一班次数据=当前班次数据，当前班次数据=新班次数据）
        /// 4. 加载新班次数据（从CSV读取或新建）
        /// 注意：数据切换时用深拷贝（new List<NgTimeSlot>(CurrentShiftData)），避免引用冲突
        /// </summary>
        private void SwitchShiftData()
        {
            lock (_syncRoot)
            {
                // 1. 保存上一班次的最终数据（若文件存在且数据有效）
                string lastFilePath = _lastShiftFilePath;
                if (File.Exists(lastFilePath) && CurrentShiftData != null)
                {
                    OutputStatistics.Instance.WriteCsv(lastFilePath, CurrentShiftData);
                }

                // 2. 更新班次跟踪信息为当前最新值
                string currentFilePath = GetCurrentShiftFilePath();
                _lastShiftFilePath = currentFilePath;
                _lastCheckedShiftType = GetCurrentShiftType();

                // 3. 切换数据集合（深拷贝，确保上一班次数据不被后续操作修改）
                PreviousShiftData = new List<NgTimeSlot>(CurrentShiftData);
                // 4. 加载新班次数据（初始化新的统计上下文）
                CurrentShiftData = LoadShiftData(currentFilePath);
            }
        }

        /// <summary>
        /// 启动班次切换定时检查计时器
        /// 作用：即使没有NG事件（如设备空闲），也能在班次切换时间点自动更新
        /// 配置：
        /// - 延迟0秒启动（程序启动后立即开始检查）
        /// - 周期30秒（每30秒检查一次，可根据需求调整）
        /// 异常处理：捕获计时器触发异常，记录日志，确保计时器继续运行
        /// </summary>
        private void StartShiftCheckTimer()
        {
            _shiftCheckTimer = new Timer(_ =>
            {
                try
                {
                    // 调度到主线程执行检查（避免跨线程问题）
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
                    // 记录计时器异常日志（不中断计时器）
                    RLog.SaveError(new Exception("定时班次检查失败", ex));
                }
            },
            null,        // 计时器状态对象（未使用）
            0,           // 首次执行延迟（0秒=立即启动）
            30000);      // 后续执行周期（30000毫秒=30秒）
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 判断当前所属班次类型（白班/夜班）
        /// 判定逻辑：
        /// - 白班：当前小时 >= _shiftRestartHour 且 <= _shiftRestartHour+12
        /// - 夜班：其他情况（当前小时 <= _shiftRestartHour 或 >= _shiftRestartHour+12）
        /// 示例：_shiftRestartHour=8时，8<=当前小时<20→白班，否则→夜班
        /// </summary>
        /// <returns>当前班次类型（ShiftType.Day=白班，ShiftType.Night=夜班）</returns>
        private ShiftType GetCurrentShiftType()
        {
            int currentHour = DateTime.Now.Hour;
            return currentHour >= _shiftRestartHour && currentHour < _shiftRestartHour + 12
                ? ShiftType.Day
                : ShiftType.Night;
        }

        /// <summary>
        /// 更新当前班次的总计数据（索引12的行）
        /// 计算逻辑：
        /// 1. 累加前12个时间段的各分类NG数（压力NG、量测NG、多种NG）
        /// 2. 计算总NG数（各分类NG数之和）
        /// 作用：确保总计行数据与各时段数据一致，便于快速查看班次整体NG情况
        /// </summary>
        private void UpdateTotalStatistics()
        {
            // 初始化总计行数据（保留原时间段描述，如"白班总数"）
            var totalSlot = new NgTimeSlot
            {
                时间段 = CurrentShiftData[12].时间段,
                总NG数 = 0,
                压力NG = 0,
                量测NG = 0,
                多种NG = 0
            };

            // 累加前12个时间段的所有NG数据（Take(12)：排除总计行）
            foreach (var timeSlot in CurrentShiftData.Take(12))
            {
                totalSlot.压力NG += timeSlot.压力NG;
                totalSlot.量测NG += timeSlot.量测NG;
                totalSlot.多种NG += timeSlot.多种NG;
            }

            // 计算总计行的总NG数
            totalSlot.总NG数 = totalSlot.压力NG + totalSlot.量测NG + totalSlot.多种NG;

            // 更新当前班次的总计行（索引12）
            CurrentShiftData[12] = totalSlot;
        }

        /// <summary>
        /// 初始化班次NG数据结构（空数据）
        /// 结构：12个小时段数据 + 1条总计数据
        /// 时间段描述生成：
        /// - 白班：_shiftRestartHour ~ _shiftRestartHour+12（如8:00-9:00，9:00-10:00...）
        /// - 夜班：_shiftRestartHour+12 ~ 24（如20:00-21:00...23:00-0:00） + 0 ~ _shiftRestartHour（如0:00-1:00...7:00-8:00）
        /// 总计行描述：白班→"白班总数"，夜班→"夜班总数"
        /// 用途：新班次初始化、历史数据查询时无文件的默认数据
        /// </summary>
        /// <param name="shiftType">班次类型（白班/夜班）</param>
        /// <returns>初始化后的空数据列表</returns>
        private List<NgTimeSlot> InitializeShiftData(ShiftType shiftType)
        {
            var shiftData = new List<NgTimeSlot>();
            // 总计行描述文本（区分白班/夜班）
            string totalLabel = shiftType == ShiftType.Day ? "白班总数" : "夜班总数";

            // 1. 生成12个小时段的数据（调用辅助方法CreateHourlyTimeSlots）
            IEnumerable<NgTimeSlot> timeSlots = CreateHourlyTimeSlots(shiftType);
            shiftData.AddRange(timeSlots);

            // 2. 添加总计行（初始化所有计数为0）
            shiftData.Add(new NgTimeSlot
            {
                时间段 = totalLabel,
                总NG数 = 0,
                压力NG = 0,
                量测NG = 0,
                多种NG = 0
            });

            return shiftData;
        }

        /// <summary>
        /// 辅助方法：根据班次类型创建12个小时段的空数据
        /// 逻辑：
        /// - 白班：从_shiftRestartHour开始，每小时一个时段，共12个
        /// - 夜班：分两部分：
        ///   ① _shiftRestartHour+12 ~ 24（如20:00-23:00，共4个时段）
        ///   ② 0 ~ _shiftRestartHour（如0:00-8:00，共8个时段）
        /// 每个时段的计数默认为0，时间段描述格式："X:00-X+1:00"
        /// </summary>
        /// <param name="shiftType">班次类型（白班/夜班）</param>
        /// <returns>12个小时段的空数据列表</returns>
        private IEnumerable<NgTimeSlot> CreateHourlyTimeSlots(ShiftType shiftType)
        {
            var slots = new List<NgTimeSlot>();
            if (shiftType == ShiftType.Day)
            {
                // 白班：_shiftRestartHour ~ _shiftRestartHour+12（如8:00-20:00）
                for (int i = _shiftRestartHour; i < _shiftRestartHour + 12; i++)
                {
                    slots.Add(new NgTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        总NG数 = 0,
                        压力NG = 0,
                        量测NG = 0,
                        多种NG = 0
                    });
                }
            }
            else
            {
                // 夜班第一部分：_shiftRestartHour+12 ~ 24（如20:00-23:00）
                for (int i = _shiftRestartHour + 12; i < 24; i++)
                {
                    slots.Add(new NgTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        总NG数 = 0,
                        压力NG = 0,
                        量测NG = 0,
                        多种NG = 0
                    });
                }
                // 夜班第二部分：0 ~ _shiftRestartHour（如0:00-8:00）
                for (int i = 0; i < _shiftRestartHour; i++)
                {
                    slots.Add(new NgTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        总NG数 = 0,
                        压力NG = 0,
                        量测NG = 0,
                        多种NG = 0
                    });
                }
            }
            return slots;
        }

        /// <summary>
        /// 加载班次NG数据（从CSV文件或初始化新数据）
        /// 流程：
        /// 1. 若文件存在，尝试读取数据（OutputStatistics.Instance.ReadCsv）
        /// 2. 若文件不存在或读取失败（如文件损坏），则：
        ///    a. 记录错误日志
        ///    b. 删除损坏的文件（避免影响后续统计）
        ///    c. 初始化新数据并写入文件（为下一次统计做准备）
        /// 作用：确保每次加载的数据都是有效、完整的
        /// </summary>
        /// <param name="filePath">班次数据CSV文件路径</param>
        /// <returns>加载后的班次数据列表</returns>
        private List<NgTimeSlot> LoadShiftData(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    // 从CSV文件读取数据（OutputStatistics封装了CSV序列化/反序列化逻辑）
                    return OutputStatistics.Instance.ReadCsv<NgTimeSlot>(filePath);
                }
                catch (Exception ex)
                {
                    // 记录文件读取失败日志
                    RLog.SaveError(new Exception($"读取班次NG数据失败，将创建新文件：{filePath}", ex));
                    // 删除损坏的文件（避免后续再次读取失败）
                    File.Delete(filePath);
                }
            }

            // 初始化新数据（根据文件路径判断班次类型）
            ShiftType shiftType = filePath.Contains("白班") ? ShiftType.Day : ShiftType.Night;
            var newData = InitializeShiftData(shiftType);
            // 将新数据写入文件（确保后续统计有基础结构）
            OutputStatistics.Instance.WriteCsv(filePath, newData);
            return newData;
        }

        /// <summary>
        /// 获取当前班次的CSV文件路径（核心路径计算方法）
        /// 关键处理：跨天夜班的日期调整
        /// - 若当前是夜班，且当前小时 <= _shiftRestartHour（如凌晨3点），则日期取前一天
        /// 示例：
        /// - 5月20日21:00（夜班）→ 日期=5月20日 → 文件：2024年05月20日 夜班.csv
        /// - 5月21日3:00（夜班）→ 日期=5月20日 → 文件：2024年05月20日 夜班.csv
        /// 作用：确保跨天夜班的数据存储在同一个文件中
        /// </summary>
        /// <returns>当前班次的CSV文件完整路径</returns>
        private string GetCurrentShiftFilePath()
        {
            DateTime baseDate = DateTime.Now;
            // 跨天夜班日期调整：夜班且当前小时 < 基准小时（如凌晨3点 < 8点）
            if (GetCurrentShiftType() == ShiftType.Night && DateTime.Now.Hour < _shiftRestartHour)
            {
                baseDate = baseDate.AddDays(-1); // 日期减1天
            }
            // 调用通用路径生成方法
            return GetShiftFilePath(baseDate, GetCurrentShiftType());
        }

        /// <summary>
        /// 获取上一班次的CSV文件路径
        /// 逻辑：
        /// 1. 上一班次类型 = 当前班次类型的相反（白班→夜班，夜班→白班）
        /// 2. 上一班次日期：
        ///    - 当前白班 → 上一班次日期=当前日期-1天（如5月20日白班的上一班是5月19日夜班）
        ///    - 当前夜班（凌晨）→ 上一班次日期=当前日期-1天（如5月21日凌晨夜班的上一班是5月20日白班）
        ///    - 当前夜班（20:00-23:59）→ 上一班次日期=当前日期（如5月20日21点夜班的上一班是5月20日白班）
        /// 作用：用于程序启动时加载上一班次的历史数据
        /// </summary>
        /// <returns>上一班次的CSV文件完整路径</returns>
        private string GetPreviousShiftFilePath()
        {
            DateTime currentDate = DateTime.Now;
            ShiftType currentShift = GetCurrentShiftType();
            // 上一班次类型与当前相反
            ShiftType previousShift = currentShift == ShiftType.Day ? ShiftType.Night : ShiftType.Day;

            // 计算上一班次日期
            DateTime previousDate = currentShift == ShiftType.Day
                ? currentDate.AddDays(-1) // 当前白班→上一班是前一天夜班
                : currentDate.Hour < _shiftRestartHour
                    ? currentDate.AddDays(-1) // 当前夜班（凌晨）→上一班是前一天白班
                    : currentDate; // 当前夜班（20:00-23:59）→上一班是当天白班

            // 调用通用路径生成方法
            return GetShiftFilePath(previousDate, previousShift);
        }

        /// <summary>
        /// 通用方法：生成指定日期和班次的CSV文件路径
        /// 文件命名规范：{yyyy年MM月dd日} {白班/夜班}.csv
        /// 示例：2024年05月20日 白班.csv、2024年05月20日 夜班.csv
        /// 作用：统一路径生成逻辑，避免重复代码
        /// </summary>
        /// <param name="date">目标日期</param>
        /// <param name="shiftType">班次类型（白班/夜班）</param>
        /// <returns>指定班次的CSV文件完整路径</returns>
        private string GetShiftFilePath(DateTime date, ShiftType shiftType)
        {
            string shiftName = shiftType == ShiftType.Day ? "白班" : "夜班";
            string fileName = $"{date:yyyy年MM月dd日} {shiftName}.csv";
            // 拼接根目录和文件名，生成完整路径
            return Path.Combine(_baseStatisticsPath, fileName);
        }

        /// <summary>
        /// 内部班次类型枚举（仅用于管理器内部逻辑）
        /// 区分白班和夜班，用于路径计算、数据初始化、班次切换判断
        /// 不对外暴露，确保内部逻辑封装性
        /// </summary>
        private enum ShiftType
        {
            Day,   // 白班
            Night  // 夜班
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放资源（实现IDisposable接口的核心方法）
        /// 释放对象：班次切换定时检查计时器（_shiftCheckTimer）
        /// 作用：防止程序退出时计时器未释放导致的内存泄漏
        /// 调用时机：建议在程序关闭时调用（如MainForm.FormClosing事件）
        /// 注意：用?.操作符避免空引用异常（计时器未初始化时）
        /// </summary>
        public void Dispose()
        {
            _shiftCheckTimer?.Dispose();
        }
        #endregion
    }

    /// <summary>
    /// 时间段NG数据模型（与CSV文件列一一对应）
    /// 字段说明：
    /// - 时间段：时段描述（如"8:00-9:00"）或总计描述（如"白班总数"）
    /// - 总NG数：该时段/班次的NG事件总数（压力NG+量测NG+多种NG）
    /// - 压力NG：因压力异常导致的NG数
    /// - 量测NG：因测量结果超出公差导致的NG数
    /// - 多种NG：其他未归类的NG数（兜底分类）
    /// 序列化说明：OutputStatistics通过反射将此类与CSV列映射，字段名需与CSV列名一致
    /// </summary>
    public class NgTimeSlot
    {
        public string 时间段 { get; set; }
        public int 总NG数 { get; set; }
        public int 压力NG { get; set; }
        public int 量测NG { get; set; }
        public int 多种NG { get; set; }
    }
}