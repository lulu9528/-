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
using System.Threading;
using System.Windows.Forms;

namespace Jabil.Module.ProductData.Lib
{
    /// <summary>
    /// 产品检测结果枚举
    /// 定义产品在串线生产中的四种状态，用于区分统计维度
    /// </summary>
    public enum ProductResult
    {
        /// <summary>
        /// 投入状态：产品进入生产流程但未完成检测
        /// 仅计入投入数量，不影响产出和良率计算
        /// </summary>
        投入数,
        /// <summary>
        /// 合格状态：产品通过所有检测标准
        /// 计入产出数量，参与良率计算
        /// </summary>
        Ok,
        /// <summary>
        /// 不合格状态：产品未通过至少一项检测标准
        /// 计入NG数量，参与良率计算
        /// </summary>
        Ng,
        /// <summary>
        /// 未定义状态：检测流程异常或数据无效
        /// 不计入任何统计，避免影响数据准确性
        /// </summary>
        Undefined,
    }

    /// <summary>
    /// 串线生产良率统计管理器
    /// 功能：实时统计生产数据（投入/产出/NG/良率），按白班/夜班自动切换，数据持久化到CSV
    /// 设计：线程安全单例模式，确保全系统数据一致性
    /// </summary>
    public class YieldManager : IDisposable
    {
        #region 单例模式实现（线程安全）
        /// <summary>
        /// 懒加载单例实例
        /// 使用Lazy<T>确保多线程环境下仅初始化一次，避免并发冲突
        /// LazyThreadSafetyMode.ExecutionAndPublication：保证初始化过程线程安全
        /// </summary>
        private static readonly Lazy<YieldManager> _lazyInstance = new Lazy<YieldManager>(() => new YieldManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 全局唯一实例访问入口
        /// 外部通过此属性获取管理器，确保全系统使用同一统计上下文
        /// </summary>
        public static YieldManager Instance => _lazyInstance.Value;

        /// <summary>
        /// 私有构造函数：防止外部实例化，确保单例唯一性
        /// 初始化工作：配置班次时间、创建存储目录、加载初始数据、启动定时检查
        /// </summary>
        private YieldManager()
        {
            // 班次切换基准时间（早上8点，行业通用白班起始时间）
            _shiftRestartHour = OutputStatistics.Instance.RestartTime;
            // 构建统计数据存储路径：基于系统DataPath + 自定义目录
            _baseStatisticsPath = Path.Combine(Project.Instance.ParameterPath.DataPath, "Config", "Data", "串线生产统计");
            // 确保存储目录存在，不存在则自动创建（避免文件写入失败）
            Directory.CreateDirectory(_baseStatisticsPath);

            // 初始化班次跟踪信息：记录当前班次文件路径和类型，用于后续对比
            _lastShiftFilePath = GetCurrentShiftFilePath();
            _lastCheckedShiftType = GetCurrentShiftType();

            // 加载初始数据：当前班次和上一班次的数据（从CSV读取或新建）
            CurrentShiftData = LoadShiftData(GetCurrentShiftFilePath());
            PreviousShiftData = LoadShiftData(GetPreviousShiftFilePath());

            // 启动定时检查计时器：每30秒检查一次班次是否切换（核心机制）
            StartShiftCheckTimer();
        }
        #endregion

        #region 私有字段（含新增班次切换相关）
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
        /// 统计数据存储根目录
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
        /// 每隔30秒触发一次班次检查，确保无数据录入时也能自动切换
        /// </summary>
        private System.Threading.Timer _shiftCheckTimer;
        #endregion

        #region 公共数据集合
        /// <summary>
        /// 当前班次统计数据列表
        /// 结构：12个小时段数据（索引0-11） + 1条总计数据（索引12）
        /// 实时性：随生产数据录入动态更新，用于UI展示当前生产状态
        /// </summary>
        public List<ProductionTimeSlot> CurrentShiftData { get; private set; }

        /// <summary>
        /// 上一班次统计数据列表
        /// 结构同当前班次，存储已结束班次的历史数据，不实时更新
        /// 用途：生产对比分析、历史数据追溯
        /// </summary>
        public List<ProductionTimeSlot> PreviousShiftData { get; private set; }

        /// <summary>
        /// 历史查询-白班数据缓存
        /// 临时存储用户查询的指定日期白班数据，供UI展示
        /// </summary>
        public List<ProductionTimeSlot> HistoryDayShiftData { get; private set; } = new List<ProductionTimeSlot>();

        /// <summary>
        /// 历史查询-夜班数据缓存
        /// 临时存储用户查询的指定日期夜班数据，供UI展示
        /// </summary>
        public List<ProductionTimeSlot> HistoryNightShiftData { get; private set; } = new List<ProductionTimeSlot>();
        #endregion

        #region 核心业务方法（含班次切换检查）
        /// <summary>
        /// 录入单个产品的检测结果
        /// 适用场景：自动化流水线，产品逐个检测并实时统计
        /// 流程：检查班次切换 → 定位时间段 → 更新数据 → 计算良率 → 持久化
        /// </summary>
        /// <param name="result">产品检测结果（投入数/Ok/Ng/Undefined）</param>
        public void AddSingleProduct(ProductResult result)
        {
            try
            {
                lock (_syncRoot)
                {
                    // 关键步骤：先检查是否需要切换班次，若已到切换时间则更新数据
                    if (CheckShiftChanged())
                    {
                        SwitchShiftData();
                    }

                    // 获取当前班次的CSV文件路径（用于数据持久化）
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

                    // 若文件不存在（新班次首次录入），初始化空数据
                    if (!File.Exists(currentFilePath))
                    {
                        CurrentShiftData = InitializeShiftData(GetCurrentShiftType());
                    }

                    // 获取当前时间对应的小时段索引（0-11，对应12个小时）
                    int timeSlotIndex = GetCurrentTimeSlotIndex();
                    // 索引校验：必须在0-11范围内（排除总计行）
                    if (timeSlotIndex < 0 || timeSlotIndex >= CurrentShiftData.Count - 1)
                        throw new IndexOutOfRangeException("无效的时间段索引，超出0-11范围");

                    // 获取当前小时段的数据对象
                    ProductionTimeSlot currentSlot = CurrentShiftData[timeSlotIndex];

                    // 根据检测结果更新对应统计字段
                    switch (result)
                    {
                        case ProductResult.投入数:
                            currentSlot.投入数量++; // 仅投入数+1
                            break;
                        case ProductResult.Ok:
                            currentSlot.产出数量++; // 合格产品，产出数+1
                            break;
                        case ProductResult.Ng:
                            currentSlot.NG数++; // 不合格产品，NG数+1
                            break;
                        case ProductResult.Undefined:
                            return; // 无效数据，不统计
                    }

                    // 计算当前时段良率：(投入数量 - NG数) / 投入数量 × 100%
                    // 边界处理：
                    // 1. 投入数量为0 → 良率0（避免除以零）
                    // 2. 投入数量 < NG数 → 良率0（业务逻辑合理性）
                    // 保留2位小数，确保统计精度
                    double yieldValue = currentSlot.投入数量 > 0
                        ? Math.Max(0, Math.Round((double)(currentSlot.投入数量 - currentSlot.NG数) / currentSlot.投入数量, 2))
                        : 0;
                    currentSlot.良率 = yieldValue;

                    // 更新当前班次的总计数据（累加12个时段的结果）
                    UpdateTotalStatistics();
                    // 持久化数据到CSV文件（防止程序异常退出导致数据丢失）
                    OutputStatistics.Instance.WriteCsv(currentFilePath, CurrentShiftData);
                }
            }
            catch (Exception ex)
            {
                // 记录异常日志，包含堆栈信息便于排查
                RLog.SaveError(ex);
                // 向上抛出异常，由调用方处理（如弹窗提示）
                throw;
            }
        }

        /// <summary>
        /// 批量录入生产数据
        /// 适用场景：批次生产，一次性录入多个产品的统计结果
        /// 特性：支持批量投入/产出/NG数，包含参数有效性校验
        /// </summary>
        /// <param name="inputQty">批量投入数量（≥0）</param>
        /// <param name="outputQty">批量产出数量（≥0）</param>
        /// <param name="ngQty">批量NG数量（≥0）</param>
        /// <exception cref="ArgumentOutOfRangeException">参数无效时抛出（如负数、产出+NG>投入）</exception>
        public void AddBatchProduct(int inputQty, int outputQty, int ngQty)
        {
            // 参数有效性校验：确保数据符合业务逻辑
            if (inputQty < 0 || outputQty < 0 || ngQty < 0 || outputQty + ngQty > inputQty)
                throw new ArgumentOutOfRangeException("批量数据无效：投入数量需≥0，产出+NG数量不能超过投入数量");

            try
            {
                lock (_syncRoot)
                {
                    // 先检查班次是否切换，确保数据录入到正确的班次
                    if (CheckShiftChanged())
                    {
                        SwitchShiftData();
                    }

                    string currentFilePath = GetCurrentShiftFilePath();

                    // 处理数据列表为空的异常情况
                    if (CurrentShiftData.Count == 0)
                    {
                        if (File.Exists(currentFilePath))
                            File.Delete(currentFilePath);
                        CurrentShiftData = InitializeShiftData(GetCurrentShiftType());
                    }

                    // 新班次首次录入，初始化空数据
                    if (!File.Exists(currentFilePath))
                    {
                        CurrentShiftData = InitializeShiftData(GetCurrentShiftType());
                    }

                    // 获取当前时段索引并校验
                    int timeSlotIndex = GetCurrentTimeSlotIndex();
                    if (timeSlotIndex < 0 || timeSlotIndex >= CurrentShiftData.Count - 1)
                        throw new IndexOutOfRangeException("无效的时间段索引");

                    // 累加批量数据到当前时段
                    ProductionTimeSlot currentSlot = CurrentShiftData[timeSlotIndex];
                    currentSlot.投入数量 += inputQty;
                    currentSlot.产出数量 += outputQty;
                    currentSlot.NG数 += ngQty;

                    // 计算批量数据后的良率（逻辑同单个录入）
                    double yieldValue = currentSlot.投入数量 > 0
                        ? Math.Max(0, Math.Round((double)(currentSlot.投入数量 - currentSlot.NG数) / currentSlot.投入数量, 2))
                        : 0;
                    currentSlot.良率 = yieldValue;

                    // 更新总计并持久化
                    UpdateTotalStatistics();
                    OutputStatistics.Instance.WriteCsv(currentFilePath, CurrentShiftData);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 查询指定日期的历史生产数据
        /// 用途：生成生产报表、分析历史良率趋势、追溯特定日期的生产情况
        /// </summary>
        /// <param name="date">目标查询日期</param>
        public void QueryHistoryData(DateTime date)
        {
            try
            {
                // 加载指定日期的白班数据
                string dayShiftPath = GetShiftFilePath(date, ShiftType.Day);
                HistoryDayShiftData = File.Exists(dayShiftPath)
                    ? OutputStatistics.Instance.ReadCsv<ProductionTimeSlot>(dayShiftPath) // 存在则读取
                    : InitializeShiftData(ShiftType.Day); // 不存在则初始化空数据

                // 加载指定日期的夜班数据（逻辑同上）
                string nightShiftPath = GetShiftFilePath(date, ShiftType.Night);
                HistoryNightShiftData = File.Exists(nightShiftPath)
                    ? OutputStatistics.Instance.ReadCsv<ProductionTimeSlot>(nightShiftPath)
                    : InitializeShiftData(ShiftType.Night);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex); // 记录查询异常
            }
        }

        /// <summary>
        /// 清空当前班次的所有生产数据
        /// 此操作会重置内存中的数据并覆盖CSV文件，请谨慎使用。
        /// </summary>
        public void ClearCurrentShiftData()
        {
            try
            {
                lock (_syncRoot)
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
                        $"已手动清空当前{shiftName}的所有生产数据。文件路径: {currentFilePath}");
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(new Exception("执行清空当前班次数据操作失败。", ex));
                throw; // 向上抛出异常，通知调用方操作失败
            }
        }
        #endregion

        #region 新增：班次切换核心逻辑
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
            PreviousShiftData = new List<ProductionTimeSlot>(CurrentShiftData);
            CurrentShiftData = LoadShiftData(currentFilePath);
        }

        /// <summary>
        /// 启动定时检查计时器
        /// 作用：即使没有生产数据录入（如夜间设备空闲），也能在切换时间点（8:00/20:00）自动切换班次
        /// 周期：每30秒检查一次，平衡实时性和性能消耗
        /// </summary>
        private void StartShiftCheckTimer()
        {
            _shiftCheckTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    lock (_syncRoot)
                    {
                        // 检查班次是否变化，若变化则执行切换
                        if (CheckShiftChanged())
                        {
                            SwitchShiftData();
                        }
                    }
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

        #region 私有辅助方法
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
        /// 计算当前时间在所属班次中的小时段索引（0-11）
        /// 每个班次分为12个小时段，每个时段对应一个索引，用于定位数据存储位置
        /// </summary>
        /// <returns>当前时段索引（0-11）</returns>
        private int GetCurrentTimeSlotIndex()
        {
            int currentHour = DateTime.Now.Hour;
            ShiftType currentShift = GetCurrentShiftType();

            if (currentShift == ShiftType.Day)
            {
                // 白班时段索引：8点→0，9点→1，...，19点→11
                return currentHour - _shiftRestartHour;
            }
            else
            {
                // 夜班时段分两部分：
                // 1. 20:00-23:59 → 20点→0，21点→1，23点→3
                if (currentHour >= _shiftRestartHour + 12)
                {
                    return currentHour - (_shiftRestartHour + 12);
                }
                // 2. 0:00-7:59 → 0点→4，1点→5，...，7点→11
                else
                {
                    return currentHour + (24 - (_shiftRestartHour + 12));
                }
            }
        }

        /// <summary>
        /// 更新当前班次的总计统计数据
        /// 逻辑：累加前12个时段的投入/产出/NG数量，计算总良率，更新到总计行
        /// 确保总计数据与各时段数据一致，反映班次整体生产状态
        /// </summary>
        private void UpdateTotalStatistics()
        {
            // 初始化总计变量
            int totalInput = 0;       // 总投入数量
            int totalOutput = 0;      // 总产出数量
            int totalNg = 0;          // 总NG数量

            // 遍历前12个时段（排除最后一条总计行），累加数据
            for (int i = 0; i < CurrentShiftData.Count - 1; i++)
            {
                totalInput += CurrentShiftData[i].投入数量;
                totalOutput += CurrentShiftData[i].产出数量;
                totalNg += CurrentShiftData[i].NG数;
            }

            // 获取总计行并更新数据
            ProductionTimeSlot totalSlot = CurrentShiftData[CurrentShiftData.Count - 1];
            totalSlot.投入数量 = totalInput;
            totalSlot.产出数量 = totalOutput;
            totalSlot.NG数 = totalNg;

            // 计算总良率（逻辑同单个时段）
            totalSlot.良率 = totalInput > 0
                ? Math.Max(0, Math.Round((double)(totalInput - totalNg) / totalInput, 2))
                : 0;
        }

        /// <summary>
        /// 初始化班次数据结构（空数据）
        /// 生成12个小时段的空数据 + 1条总计行，用于新班次或新文件
        /// </summary>
        /// <param name="shiftType">班次类型（白班/夜班）</param>
        /// <returns>初始化后的空数据列表</returns>
        private List<ProductionTimeSlot> InitializeShiftData(ShiftType shiftType)
        {
            var shiftData = new List<ProductionTimeSlot>();
            // 总计行描述文本（区分白班/夜班）
            string totalLabel = shiftType == ShiftType.Day ? "白班总计" : "夜班总计";

            if (shiftType == ShiftType.Day)
            {
                // 白班时段：8:00-20:00，每小时一个时段（共12个）
                for (int i = _shiftRestartHour; i < _shiftRestartHour + 12; i++)
                {
                    shiftData.Add(new ProductionTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00", // 时段描述（如"8:00-9:00"）
                        投入数量 = 0,
                        产出数量 = 0,
                        NG数 = 0,
                        良率 = 0
                    });
                }
            }
            else
            {
                // 夜班第一部分：20:00-23:59（4个时段）
                for (int i = _shiftRestartHour + 12; i < 24; i++)
                {
                    shiftData.Add(new ProductionTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        投入数量 = 0,
                        产出数量 = 0,
                        NG数 = 0,
                        良率 = 0
                    });
                }
                // 夜班第二部分：0:00-7:59（8个时段，跨天）
                for (int i = 0; i < _shiftRestartHour; i++)
                {
                    shiftData.Add(new ProductionTimeSlot
                    {
                        时间段 = $"{i}:00-{i + 1}:00",
                        投入数量 = 0,
                        产出数量 = 0,
                        NG数 = 0,
                        良率 = 0
                    });
                }
            }

            // 添加总计行（列表最后一条）
            shiftData.Add(new ProductionTimeSlot
            {
                时间段 = totalLabel,
                投入数量 = 0,
                产出数量 = 0,
                NG数 = 0,
                良率 = 0
            });

            return shiftData;
        }

        /// <summary>
        /// 加载班次数据（从CSV文件或初始化新数据）
        /// 核心逻辑：优先读取现有文件；文件不存在/损坏时，创建新文件并初始化空数据
        /// </summary>
        /// <param name="filePath">班次数据文件路径</param>
        /// <returns>加载完成的数据列表</returns>
        public List<ProductionTimeSlot> LoadShiftData(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    // 从CSV文件读取数据并反序列化为列表
                    return OutputStatistics.Instance.ReadCsv<ProductionTimeSlot>(filePath);
                }
                catch (Exception ex)
                {
                    // 文件损坏时，删除无效文件并记录日志
                    RLog.SaveError(new Exception($"读取班次数据失败，重建文件：{filePath}", ex));
                    File.Delete(filePath);
                }
            }

            // 文件不存在或损坏时，初始化新数据
            ShiftType shiftType = filePath.Contains("白班") ? ShiftType.Day : ShiftType.Night;
            var newData = InitializeShiftData(shiftType);
            // 将空数据写入新文件，为后续录入做准备
            OutputStatistics.Instance.WriteCsv(filePath, newData);
            return newData;
        }

        /// <summary>
        /// 获取当前班次的CSV文件路径
        /// 关键处理：夜班跨天场景（如凌晨3点属于前一天的夜班），调整日期为前一天
        /// 确保文件命名与实际班次周期一致
        /// </summary>
        /// <returns>当前班次文件的完整路径</returns>
        public string GetCurrentShiftFilePath()
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
        public string GetPreviousShiftFilePath()
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
        /// 统一命名规范："yyyy年MM月dd日 串线生产_白班/夜班.csv"
        /// 确保文件按日期和班次分类，便于管理和查询
        /// </summary>
        /// <param name="date">目标日期</param>
        /// <param name="shiftType">班次类型</param>
        /// <returns>指定班次文件的完整路径</returns>
        private string GetShiftFilePath(DateTime date, ShiftType shiftType)
        {
            string shiftName = shiftType == ShiftType.Day ? "白班" : "夜班";
            string fileName = $"{date:yyyy年MM月dd日} 串线生产_{shiftName}.csv";
            return Path.Combine(_baseStatisticsPath, fileName);
        }

        /// <summary>
        /// 内部班次类型枚举（仅用于管理器内部逻辑）
        /// 区分白班和夜班，用于数据统计和文件命名
        /// </summary>
        private enum ShiftType
        {
            /// <summary>
            /// 白班：8:00-20:00（12小时）
            /// </summary>
            Day,

            /// <summary>
            /// 夜班：20:00-次日8:00（12小时）
            /// </summary>
            Night
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放资源（实现IDisposable接口）
        /// 主要释放定时计时器，防止内存泄漏
        /// </summary>
        public void Dispose()
        {
            _shiftCheckTimer?.Dispose(); // 安全释放计时器（?. 避免空引用）
        }
        #endregion
    }

    /// <summary>
    /// 串线生产时间段数据模型
    /// 存储单个小时段或班次总计的统计指标，与CSV文件列一一对应
    /// 是数据序列化、UI展示、统计计算的基础单元
    /// </summary>
    public class ProductionTimeSlot
    {
        /// <summary>
        /// 时间段描述
        /// 两种类型：1. 小时段（如"8:00-9:00"）；2. 班次总计（如"白班总计"）
        /// </summary>
        public string 时间段 { get; set; }

        /// <summary>
        /// 投入数量
        /// 进入当前生产环节的产品总数量（包含后续产出和NG的产品）
        /// </summary>
        public int 投入数量 { get; set; }

        /// <summary>
        /// 产出数量
        /// 通过所有检测标准的合格产品数量
        /// </summary>
        public int 产出数量 { get; set; }

        /// <summary>
        /// NG数量
        /// 未通过检测的不合格产品数量
        /// </summary>
        public int NG数 { get; set; }

        /// <summary>
        /// 良率（百分比）
        /// 计算公式：(投入数量 - NG数) / 投入数量 × 100%
        /// 保留2位小数，产出为0时良率为0
        /// </summary>
        public double 良率 { get; set; }
    }
}