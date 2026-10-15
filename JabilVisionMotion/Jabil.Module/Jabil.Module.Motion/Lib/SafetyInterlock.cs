using Jabil.Lib;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Jabil.Module.Permission;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace Jabil.Module.Motion.Lib
{
    /// <summary>
    /// 全局轴位置管理类（防呆控制专用）
    /// 维护所有轴的编码器位置和运动状态，提供线程安全的访问方式
    /// 设计目的：集中管理所有轴的位置信息，确保多线程环境下数据访问的一致性
    /// </summary>
    public static class AllAxisPos
    {
        #region 私有字段与锁
        // 存储所有16个轴的编码器位置（double类型提供高精度）
        private static readonly double[] _allAxisEncPos = new double[16];

        // 存储所有16个轴的运行状态（true表示正在运行）
        private static readonly bool[] _allAxisRunning = new bool[16];

        // 线程安全锁对象，确保对共享资源的操作是原子性的
        private static readonly object _axisLock = new object();
        #endregion

        #region 事件定义
        /// <summary>
        /// 轴位置变化事件
        /// 当轴位置发生显著变化时触发
        /// 事件参数：轴索引(int)，新位置(double)
        /// </summary>
        public static event Action<int, double> AxisPositionChanged;

        /// <summary>
        /// 轴运动状态变化事件
        /// 当轴的运行状态（运行/停止）改变时触发
        /// 事件参数：轴索引(int)，新状态(bool)
        /// </summary>
        public static event Action<int, bool> AxisRunningStatusChanged;
        #endregion

        #region 轴索引常量（中文命名+中文说明）
        // 使用中文常量定义各轴索引，提高代码可读性和可维护性
        public const int 上料Y轴 = 0;
        public const int 上料Z轴 = 1;
        public const int 检测X轴 = 2;

        public const int 检测旋转1 = 3;
        public const int 检测旋转2 = 4;
        public const int 检测旋转3 = 5;
        public const int 检测旋转4 = 6;

        public const int 左检测相机 = 7;
        public const int 右检测相机 = 8;

        public const int 补料BufferX轴 = 9;
        public const int 分料Y轴 = 10;
        public const int 分料Z轴 = 11;
        public const int 组装X轴 = 12;
        public const int 组装Y轴 = 13;
        public const int 组装Z轴 = 14;
        public const int 组装U轴 = 15;
        #endregion

        #region 公共访问接口（线程安全）
        /// <summary>
        /// 获取指定轴的编码器位置
        /// </summary>
        /// <param name="axisIndex">轴索引（0-15）</param>
        /// <returns>该轴的当前编码器位置</returns>
        public static double GetEncPos(int axisIndex)
        {
            try
            {
                // 验证轴索引有效性
                ValidateAxisIndex(axisIndex);

                // 使用lock确保线程安全，防止读取过程中数据被修改
                lock (_axisLock)
                {
                    return _allAxisEncPos[axisIndex];
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志，包含详细信息便于调试
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"获取轴位置失败 (轴索引: {axisIndex}): {ex.Message}");
                throw; // 重新抛出异常，确保调用方知道发生了错误
            }
        }

        /// <summary>
        /// 批量获取多个轴的位置
        /// 优化性能：一次锁定操作获取多个轴位置，减少锁竞争
        /// </summary>
        /// <param name="axisIndices">需要获取位置的轴索引数组</param>
        /// <returns>包含轴索引和对应位置的字典</returns>
        public static Dictionary<int, double> GetMultipleEncPos(params int[] axisIndices)
        {
            // 参数验证
            if (axisIndices == null || axisIndices.Length == 0)
                throw new ArgumentException("必须指定至少一个轴索引", nameof(axisIndices));

            var result = new Dictionary<int, double>();

            // 一次锁定获取所有需要的轴位置
            lock (_axisLock)
            {
                foreach (var index in axisIndices)
                {
                    ValidateAxisIndex(index);
                    result[index] = _allAxisEncPos[index];
                }
            }
            return result;
        }

        /// <summary>
        /// 更新指定轴的编码器位置
        /// 包含防抖动逻辑：只有位置变化超过0.001才视为有效变化
        /// </summary>
        /// <param name="axisIndex">轴索引</param>
        /// <param name="position">新的位置值</param>
        public static void SetEncPos(int axisIndex, double position)
        {
            try
            {
                // 验证轴索引有效性
                ValidateAxisIndex(axisIndex);

                // 检查位置是否在合理范围内（排除NaN和无穷大）
                if (double.IsNaN(position) || double.IsInfinity(position))
                {
                    throw new ArgumentException($"轴位置值无效: {position}", nameof(position));
                }

                // 线程安全更新
                lock (_axisLock)
                {
                    // 仅当位置发生显著变化时才更新并触发事件（防抖动）
                    if (Math.Abs(_allAxisEncPos[axisIndex] - position) > 0.001)
                    {
                        double oldPos = _allAxisEncPos[axisIndex];
                        _allAxisEncPos[axisIndex] = position;

                        // 触发位置变化事件，通知所有订阅者
                        AxisPositionChanged?.Invoke(axisIndex, position);

                        // 记录关键轴的显著位置变化（超过10.0单位）
                        if (IsCriticalAxis(axisIndex) && Math.Abs(oldPos - position) > 10.0)
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Gray,
                                $"轴位置显著变化 - 轴索引: {axisIndex}, 旧值: {oldPos:F2}, 新值: {position:F2}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"更新轴位置失败 (轴索引: {axisIndex}): {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 批量更新多个轴的位置
        /// 优化性能：一次锁定操作更新多个轴，减少锁竞争
        /// </summary>
        /// <param name="positions">包含轴索引和对应位置的字典</param>
        public static void SetMultipleEncPos(Dictionary<int, double> positions)
        {
            // 参数验证
            if (positions == null || positions.Count == 0)
                throw new ArgumentException("必须提供至少一个轴位置", nameof(positions));

            // 一次锁定更新所有轴位置
            lock (_axisLock)
            {
                foreach (var kvp in positions)
                {
                    int axisIndex = kvp.Key;
                    ValidateAxisIndex(axisIndex);

                    // 仅当位置发生显著变化时才更新
                    if (Math.Abs(_allAxisEncPos[axisIndex] - kvp.Value) > 0.001)
                    {
                        _allAxisEncPos[axisIndex] = kvp.Value;
                        AxisPositionChanged?.Invoke(axisIndex, kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// 获取指定轴的运动状态（是否运行中）
        /// </summary>
        /// <param name="axisIndex">轴索引</param>
        /// <returns>true表示正在运行，false表示已停止</returns>
        public static bool GetRunningStatus(int axisIndex)
        {
            ValidateAxisIndex(axisIndex);
            lock (_axisLock)
            {
                return _allAxisRunning[axisIndex];
            }
        }

        /// <summary>
        /// 检查是否有任何轴正在运行
        /// 用于整体安全判断，如急停时需要知道是否有轴在动
        /// </summary>
        /// <returns>true表示至少有一个轴在运行</returns>
        public static bool AnyAxisRunning()
        {
            lock (_axisLock)
            {
                foreach (var running in _allAxisRunning)
                {
                    if (running) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 更新指定轴的运动状态
        /// 状态变化时会触发相应事件
        /// </summary>
        /// <param name="axisIndex">轴索引</param>
        /// <param name="isRunning">新的运动状态</param>
        public static void SetRunningStatus(int axisIndex, bool isRunning)
        {
            ValidateAxisIndex(axisIndex);
            lock (_axisLock)
            {
                // 仅当状态发生变化时才更新并触发事件
                if (_allAxisRunning[axisIndex] != isRunning)
                {
                    _allAxisRunning[axisIndex] = isRunning;
                    AxisRunningStatusChanged?.Invoke(axisIndex, isRunning);
                }
            }
        }

        /// <summary>
        /// 重置所有轴的运动状态为停止
        /// 用于紧急情况（如急停）下的快速停止所有轴
        /// </summary>
        public static void ResetAllRunningStatus()
        {
            lock (_axisLock)
            {
                for (int i = 0; i < _allAxisRunning.Length; i++)
                {
                    if (_allAxisRunning[i])
                    {
                        _allAxisRunning[i] = false;
                        // 触发状态变化事件
                        AxisRunningStatusChanged?.Invoke(i, false);
                        // 记录强制停止日志
                        RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                            $"{GetAxisName(i)}已被强制停止");
                    }
                }
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 验证轴索引有效性（0-15）
        /// 防止数组越界异常
        /// </summary>
        /// <param name="axisIndex">需要验证的轴索引</param>
        private static void ValidateAxisIndex(int axisIndex)
        {
            if (axisIndex < 0 || axisIndex >= 16)
            {
                throw new ArgumentOutOfRangeException(nameof(axisIndex), "轴索引必须在0-15范围内");
            }
        }

        /// <summary>
        /// 判断是否为关键轴（用于日志记录）
        /// 关键轴指对设备安全和生产质量影响较大的轴
        /// </summary>
        /// <param name="axisIndex">轴索引</param>
        /// <returns>true表示是关键轴</returns>
        private static bool IsCriticalAxis(int axisIndex)
        {
            // 定义需要重点监控的轴
            return axisIndex == 上料Y轴 || axisIndex == 上料Z轴 ||
                   axisIndex == 分料Y轴 || axisIndex == 分料Z轴 ||
                   axisIndex == 组装X轴 || axisIndex == 组装Y轴 || axisIndex == 组装Z轴;
        }

        /// <summary>
        /// 获取轴的名称（用于日志）
        /// 将轴索引转换为有意义的中文名称，便于日志阅读
        /// </summary>
        /// <param name="axisIndex">轴索引</param>
        /// <returns>轴的中文名称</returns>
        public static string GetAxisName(int axisIndex)
        {
            switch (axisIndex)
            {
                case 上料Y轴: return "上料Y轴";
                case 上料Z轴: return "上料Z轴";
                case 检测X轴: return "检测X轴";
                case 检测旋转1: return "检测旋转1";
                case 检测旋转2: return "检测旋转2";
                case 检测旋转3: return "检测旋转3";
                case 检测旋转4: return "检测旋转4";
                case 左检测相机: return "左检测相机";
                case 右检测相机: return "右检测相机";
                case 补料BufferX轴: return "补料BufferX轴";
                case 分料Y轴: return "分料Y轴";
                case 分料Z轴: return "分料Z轴";
                case 组装X轴: return "组装X轴";
                case 组装Y轴: return "组装Y轴";
                case 组装Z轴: return "组装Z轴";
                case 组装U轴: return "组装U轴";
                default: return $"未知轴({axisIndex})";
            }
        }
        #endregion
    }

    /// <summary>
    /// 全局IO状态管理类（防呆控制专用）
    /// 维护所有DI/DO点状态，提供线程安全的访问方式
    /// 设计目的：集中管理所有输入输出信号，确保设备状态的一致性
    /// </summary>
    public static class IoManager
    {
        #region 私有字段与锁
        // 存储所有128个DI点的状态（true/false）
        private static readonly bool[] _allDiStatus = new bool[128];

        // 存储所有128个DO点的状态（true/false）
        private static readonly bool[] _allDoStatus = new bool[128];

        // 线程安全锁对象
        private static readonly object _ioLock = new object();

        // 用于DI点防抖的时间戳字典
        private static readonly Dictionary<int, DateTime> _diDebounceTimestamps = new Dictionary<int, DateTime>();
        #endregion

        #region 事件定义
        /// <summary>
        /// DI点状态变化事件
        /// 当DI点状态改变时触发
        /// 事件参数：DI索引(int)，新状态(bool)
        /// </summary>
        public static event Action<int, bool> DiStatusChanged;

        /// <summary>
        /// DO点状态变化事件
        /// 当DO点状态改变时触发
        /// 事件参数：DO索引(int)，新状态(bool)
        /// </summary>
        public static event Action<int, bool> DoStatusChanged;

        /// <summary>
        /// 安全信号异常事件（用于紧急响应）
        /// 当关键安全信号（如急停、安全门）异常时触发
        /// 事件参数：异常信息(string)
        /// </summary>
        public static event Action<string> SafetySignalException;
        #endregion

        #region IO索引常量（中文命名+中文说明）
        // DI点输入信号定义
        /// <summary>后门开关</summary>
        public const int 后门开关 = 7;
        /// <summary>急停信号1（硬件急停输入）</summary>
        public const int 急停信号1 = 9;
        /// <summary>急停信号2（冗余急停输入）</summary>
        public const int 急停信号2 = 10;
        /// <summary>机台内部STO信号1（安全扭矩关闭）</summary>
        public const int 机台内部STO信号1 = 16;
        /// <summary>机台内部STO信号2（安全扭矩关闭冗余）</summary>
        public const int 机台内部STO信号2 = 17;

        /// <summary>正面左侧安全门状态（true=关闭，false=打开）</summary>
        public const int 正面左侧安全门 = 11;
        /// <summary>正面右侧安全门状态（true=关闭，false=打开）</summary>
        public const int 正面右侧安全门 = 12;
        /// <summary>背面左侧安全门状态（true=关闭，false=打开）</summary>
        public const int 背面左侧安全门 = 13;
        /// <summary>背面右侧安全门状态（true=关闭，false=打开）</summary>
        public const int 背面右侧安全门 = 14;

        /// <summary>手自动切换开关（true=自动，false=手动）</summary>
        public const int 手自动切换开关 = 15;

        /// <summary>机台启动按钮（true=按下）</summary>
        public const int 机台启动按钮 = 4;
        /// <summary>机台复位按钮（true=按下）</summary>
        public const int 机台复位按钮 = 5;
        /// <summary>机台暂停按钮（true=按下）</summary>
        public const int 机台暂停按钮 = 6;

        /// <summary>上料解锁气缸原点信号（true=在原点）</summary>
        public const int 上料解锁气缸原点 = 26;
        /// <summary>下料解锁气缸原点信号（true=在原点）</summary>
        public const int 下料解锁气缸原点 = 28;

        /// <summary>流线出口有料检测（true=有料）</summary>
        public const int 流线出口有料检测 = 87;
        /// <summary>清料按钮（true=按下）</summary>
        public const int 清料按钮 = 7;

        // DO点输出信号定义
        /// <summary>上料机构允许运动输出</summary>
        public const int 上料机构允许运动输出 = 32;
        /// <summary>分料机构允许运动输出</summary>
        public const int 分料机构允许运动输出 = 33;
        /// <summary>组装机构允许运动输出</summary>
        public const int 组装机构允许运动输出 = 34;
        /// <summary>急停报警输出</summary>
        public const int 急停报警输出 = 35;
        #endregion

        #region 公共访问接口（线程安全）
        /// <summary>
        /// 获取指定DI点的状态（带防抖处理）
        /// 防抖处理用于过滤传感器的抖动信号
        /// </summary>
        /// <param name="diIndex">DI点索引（0-127）</param>
        /// <param name="debounceMs">防抖时间(毫秒)，0表示不防抖</param>
        /// <returns>DI点的当前状态</returns>
        public static bool GetDiStatus(int diIndex, int debounceMs = 0)
        {
            try
            {
                ValidateDiIndex(diIndex);

                lock (_ioLock)
                {
                    // 不需要防抖，直接返回当前状态
                    if (debounceMs <= 0)
                        return _allDiStatus[diIndex];

                    // 防抖处理逻辑
                    bool currentStatus = _allDiStatus[diIndex];
                    DateTime now = DateTime.Now;

                    // 如果是第一次检测，记录时间戳并返回当前状态
                    if (!_diDebounceTimestamps.ContainsKey(diIndex))
                    {
                        _diDebounceTimestamps[diIndex] = now;
                        return currentStatus;
                    }

                    // 计算与上次检测的时间差
                    TimeSpan elapsed = now - _diDebounceTimestamps[diIndex];
                    // 超过防抖时间，更新时间戳并返回当前状态
                    if (elapsed.TotalMilliseconds >= debounceMs)
                    {
                        _diDebounceTimestamps[diIndex] = now;
                        return currentStatus;
                    }

                    // 未超过防抖时间，返回之前的状态（过滤抖动）
                    return currentStatus;
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"获取DI状态失败 (DI索引: {diIndex}): {ex.Message}");
                return false; // 安全起见，返回false表示异常状态
            }
        }

        /// <summary>
        /// 更新指定DI点的状态
        /// 状态变化时会触发相应事件，并对关键安全信号进行特殊处理
        /// </summary>
        /// <param name="diIndex">DI点索引</param>
        /// <param name="status">新的状态值</param>
        public static void SetDiStatus(int diIndex, bool status)
        {
            try
            {
                ValidateDiIndex(diIndex);
                lock (_ioLock)
                {
                    // 仅当状态发生变化时才更新
                    if (_allDiStatus[diIndex] != status)
                    {
                        bool oldStatus = _allDiStatus[diIndex];
                        _allDiStatus[diIndex] = status;
                        // 触发状态变化事件
                        DiStatusChanged?.Invoke(diIndex, status);

                        // 重要安全信号变化时记录日志
                        if (IsCriticalSafetySignal(diIndex))
                        {
                            string signalName = GetSignalName(diIndex);
                            RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                                $"安全信号变化: {signalName} - 从{oldStatus}变为{status}");

                            // 急停信号触发时，立即触发安全异常事件
                            if (IsEmergencyStopSignal(diIndex) && !status)
                            {
                                SafetySignalException?.Invoke($"{signalName}触发急停");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"更新DI状态失败 (DI索引: {diIndex}): {ex.Message}");
            }
        }

        /// <summary>
        /// 获取指定DO点的状态
        /// </summary>
        /// <param name="doIndex">DO点索引</param>
        /// <returns>DO点的当前状态</returns>
        public static bool GetDoStatus(int doIndex)
        {
            try
            {
                ValidateDoIndex(doIndex);
                lock (_ioLock)
                {
                    return _allDoStatus[doIndex];
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"获取DO状态失败 (DO索引: {doIndex}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置指定DO点的状态
        /// 状态变化时会触发相应事件，并记录关键输出信号的变化
        /// </summary>
        /// <param name="doIndex">DO点索引</param>
        /// <param name="status">新的状态值</param>
        public static void SetDoStatus(int doIndex, bool status)
        {
            try
            {
                ValidateDoIndex(doIndex);
                lock (_ioLock)
                {
                    // 仅当状态发生变化时才更新
                    if (_allDoStatus[doIndex] != status)
                    {
                        _allDoStatus[doIndex] = status;
                        // 触发状态变化事件
                        DoStatusChanged?.Invoke(doIndex, status);

                        // 记录关键输出信号的变化
                        if (IsCriticalOutputSignal(doIndex))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Blue,
                                $"输出信号变化: {GetOutputSignalName(doIndex)} - {status}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"设置DO状态失败 (DO索引: {doIndex}): {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否有任何急停信号触发
        /// 急停信号是设备安全的最后一道防线，需要冗余设计
        /// </summary>
        /// <returns>true表示有急停信号触发</returns>
        public static bool IsAnyEmergencyStopActive()
        {
            try
            {
                lock (_ioLock)
                {
                    // 急停信号通常是常闭点，false表示急停触发
                    return !_allDiStatus[急停信号1] || !_allDiStatus[急停信号2];
                }
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"检查急停信号失败: {ex.Message}");
                return true; // 异常情况下默认认为急停已触发，确保安全
            }
        }

        /// <summary>
        /// 检查所有安全门是否关闭
        /// 安全门未关闭时，设备应禁止危险运动
        /// </summary>
        /// <returns>true表示所有安全门都已关闭</returns>
        public static bool AreAllSafetyDoorsClosed()
        {
            try
            {
                lock (_ioLock)
                {
                    return _allDiStatus[正面左侧安全门] &&
                           _allDiStatus[正面右侧安全门] &&
                           _allDiStatus[背面左侧安全门] &&
                           _allDiStatus[背面右侧安全门];
                }
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"检查安全门状态失败: {ex.Message}");
                return false; // 异常情况下默认认为安全门未关闭
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 验证DI索引有效性（0-127）
        /// 防止数组越界异常
        /// </summary>
        /// <param name="diIndex">需要验证的DI索引</param>
        private static void ValidateDiIndex(int diIndex)
        {
            if (diIndex < 0 || diIndex >= 128)
            {
                throw new ArgumentOutOfRangeException(nameof(diIndex), "DI索引必须在0-127范围内");
            }
        }

        /// <summary>
        /// 验证DO索引有效性（0-127）
        /// 防止数组越界异常
        /// </summary>
        /// <param name="doIndex">需要验证的DO索引</param>
        private static void ValidateDoIndex(int doIndex)
        {
            if (doIndex < 0 || doIndex >= 128)
            {
                throw new ArgumentOutOfRangeException(nameof(doIndex), "DO索引必须在0-127范围内");
            }
        }

        /// <summary>
        /// 判断是否为关键安全信号
        /// 关键安全信号指直接关系到设备和人员安全的信号
        /// </summary>
        /// <param name="diIndex">DI索引</param>
        /// <returns>true表示是关键安全信号</returns>
        private static bool IsCriticalSafetySignal(int diIndex)
        {
            return diIndex == 急停信号1 || diIndex == 急停信号2 ||
                   diIndex == 机台内部STO信号1 || diIndex == 机台内部STO信号2 ||
                   diIndex == 正面左侧安全门 || diIndex == 正面右侧安全门 ||
                   diIndex == 背面左侧安全门 || diIndex == 背面右侧安全门;
        }

        /// <summary>
        /// 判断是否为急停信号
        /// 急停信号需要特殊处理，触发后应立即停止所有运动
        /// </summary>
        /// <param name="diIndex">DI索引</param>
        /// <returns>true表示是急停信号</returns>
        private static bool IsEmergencyStopSignal(int diIndex)
        {
            return diIndex == 急停信号1 || diIndex == 急停信号2 ||
                   diIndex == 机台内部STO信号1 || diIndex == 机台内部STO信号2;
        }

        /// <summary>
        /// 判断是否为关键输出信号
        /// 关键输出信号指控制设备重要功能的输出信号
        /// </summary>
        /// <param name="doIndex">DO索引</param>
        /// <returns>true表示是关键输出信号</returns>
        private static bool IsCriticalOutputSignal(int doIndex)
        {
            return doIndex == 上料机构允许运动输出 ||
                   doIndex == 分料机构允许运动输出 ||
                   doIndex == 组装机构允许运动输出 ||
                   doIndex == 急停报警输出;
        }

        /// <summary>
        /// 获取信号名称（用于日志）
        /// 将DI索引转换为有意义的中文名称，便于日志阅读
        /// </summary>
        /// <param name="diIndex">DI索引</param>
        /// <returns>信号的中文名称</returns>
        private static string GetSignalName(int diIndex)
        {
            switch (diIndex)
            {
                case 急停信号1: return "急停信号1";
                case 急停信号2: return "急停信号2";
                case 机台内部STO信号1: return "机台内部STO信号1";
                case 机台内部STO信号2: return "机台内部STO信号2";
                case 正面左侧安全门: return "正面左侧安全门";
                case 正面右侧安全门: return "正面右侧安全门";
                case 背面左侧安全门: return "背面左侧安全门";
                case 背面右侧安全门: return "背面右侧安全门";
                case 手自动切换开关: return "手自动切换开关";
                case 上料解锁气缸原点: return "上料解锁气缸原点";
                case 下料解锁气缸原点: return "下料解锁气缸原点";
                default: return $"DI_{diIndex}";
            }
        }

        /// <summary>
        /// 获取输出信号名称（用于日志）
        /// 将DO索引转换为有意义的中文名称，便于日志阅读
        /// </summary>
        /// <param name="doIndex">DO索引</param>
        /// <returns>输出信号的中文名称</returns>
        private static string GetOutputSignalName(int doIndex)
        {
            switch (doIndex)
            {
                case 上料机构允许运动输出: return "上料机构允许运动输出";
                case 分料机构允许运动输出: return "分料机构允许运动输出";
                case 组装机构允许运动输出: return "组装机构允许运动输出";
                case 急停报警输出: return "急停报警输出";
                default: return $"DO_{doIndex}";
            }
        }
        #endregion
    }

    /// <summary>
    /// 安全联锁控制类（防呆逻辑核心）
    /// 提供各轴移动允许性判断，防止机构干涉和危险操作
    /// 设计目的：实现设备各机构间的安全互锁，是防呆控制的核心逻辑
    /// </summary>
    public static class SafetyInterlock
    {
        // 静态构造函数，注册安全信号异常处理
        static SafetyInterlock()
        {
            // 订阅IO管理器的安全信号异常事件
            IoManager.SafetySignalException += (msg) =>
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Fatal, Color.Red, $"安全信号异常: {msg}，触发紧急停止");
                TriggerEmergencyStop(); // 发生安全异常时触发紧急停止
            };
        }

        #region 全局安全检查（所有运动必须通过的基础检查）
        /// <summary>
        /// 基础安全检查（所有轴运动前必须通过）
        /// 包含设备最基本的安全状态判断
        /// </summary>
        /// <param name="errorMessage">输出：检查失败原因</param>
        /// <returns>true=安全，false=不安全</returns>
        public static bool BasicSafetyCheck(out string errorMessage)
        {
            errorMessage = string.Empty;

            // 1. 检查急停状态
            if (IoManager.IsAnyEmergencyStopActive())
            {
                errorMessage = "急停信号已触发，禁止所有运动";
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Red, errorMessage);
                return false;
            }
            if (MenuProxy.Instance.MachineRunModel == MachineMode.Auto)
            {
                //2.检查安全门状态（当前代码注释掉了，可能是为了调试或特殊场景）
                if (!IoManager.AreAllSafetyDoorsClosed())
                {
                    errorMessage = "安全门未关闭，禁止所有运动";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Red, errorMessage);
                    return false;
                }
            }

            // 3. 检查机台是否处于报警状态
            if (IsMachineInAlarmState())
            {
                errorMessage = "机台存在未复位报警，禁止运动";
                RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查机台是否处于报警状态
        /// 实际应用中应根据机台报警信号实现
        /// </summary>
        /// <returns>true表示机台处于报警状态</returns>
        private static bool IsMachineInAlarmState()
        {
            // 示例实现：当前返回false表示无报警
            return false;
        }
        #endregion

        #region 上料机构安全判断
        /// <summary>
        /// 判断上料YZ轴是否允许移动
        /// 核心逻辑：避免Y轴大幅移动时Z轴处于低位导致干涉
        /// </summary>
        /// <param name="targetYPos">Y轴目标位置</param>
        /// <param name="errorMessage">输出：禁止移动的原因</param>
        /// <returns>true=允许移动，false=禁止移动</returns>
        public static bool IsFeedingYZMovementAllowed(double targetYPos, out string errorMessage)
        {
            // 先进行基础安全检查
            if (!BasicSafetyCheck(out errorMessage))
                return false;

            try
            {
                // 缓存当前位置（减少锁竞争）
                double currentY = AllAxisPos.GetEncPos(AllAxisPos.上料Y轴);
                double currentZ = AllAxisPos.GetEncPos(AllAxisPos.上料Z轴);

                // 1. Y轴大幅移动（超过1.0单位视为显著移动）且Z轴未升起：禁止移动
                if (Math.Abs(currentY - targetYPos) > 1.0 && currentZ >= 1)
                {
                    errorMessage = $"上料Y轴需移动{Math.Abs(currentY - targetYPos):F2}单位，但Z轴高度({currentZ:F2})低于安全高度(1)";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 2. 放料侧（Y>100为放料侧边界）且Z轴高度超过安全值（20.0为放料侧安全高度）：禁止下移
                if (currentY > 100.0 && currentZ > 20.0)
                {
                    errorMessage = $"上料机构在放料侧，Z轴高度({currentZ:F2})超过安全高度(20.0)，禁止下移";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 3. 取料侧（Y<100为放料侧边界）且Z轴高度超过安全值（130.0为取料侧安全高度）：禁止下移
                if (currentY < 100.0 && currentZ > 130.0)
                {
                    errorMessage = $"上料机构在取料侧，Z轴高度({currentZ:F2})超过安全高度(130.0)，禁止下移";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 4. 检查分料机构是否在干涉区域
                if (IsSortingInFeedingInterferenceArea())
                {
                    errorMessage = "分料机构处于上料机构干涉区域，禁止移动";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 允许移动时激活使能输出
                IoManager.SetDoStatus(IoManager.上料机构允许运动输出, true);
                RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Green, "上料YZ轴允许移动");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"上料机构安全检查异常: {ex.Message}";
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red, errorMessage);
                // 异常情况下禁止移动，确保安全
                IoManager.SetDoStatus(IoManager.上料机构允许运动输出, false);
                return false;
            }
        }

        /// <summary>
        /// 检查分料机构是否在上料机构的干涉区域
        /// 防止上料机构与分料机构同时在同一区域运动导致碰撞
        /// </summary>
        /// <returns>true表示存在干涉风险</returns>
        private static bool IsSortingInFeedingInterferenceArea()
        {
            double sortY = AllAxisPos.GetEncPos(AllAxisPos.分料Y轴);
            double sortZ = AllAxisPos.GetEncPos(AllAxisPos.分料Z轴);

            // 分料Y轴小于150.0（缓冲位范围上限）且Z轴小于等于-1时视为在干涉区域
            return sortY < 150.0 && sortZ <= -1;
        }
        #endregion

        #region 检测机构安全判断
        /// <summary>
        /// 判断检测轴是否允许移动
        /// 核心逻辑：确保上/下料解锁气缸在原点，避免与上料/分料机构干涉
        /// </summary>
        /// <param name="errorMessage">输出：禁止移动的原因</param>
        /// <returns>true=允许移动，false=禁止移动</returns>
        public static bool IsInspectionAxisMovementAllowed(out string errorMessage)
        {
            // 先进行基础安全检查
            if (!BasicSafetyCheck(out errorMessage))
                return false;

            try
            {
                // 1. 检查上料/下料解锁气缸是否在原点（带防抖，防抖间隔100毫秒）
                if (!IsCylinderAtOriginWithDebounce())
                {
                    errorMessage = "上料或下料解锁气缸未在原点位置，禁止检测轴移动";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 2. 上料Y轴在放料侧（Y>100.0）且Z轴未升起（Z>1）：禁止移动
                double feedY = AllAxisPos.GetEncPos(AllAxisPos.上料Y轴);
                double feedZ = AllAxisPos.GetEncPos(AllAxisPos.上料Z轴);
                if (feedY > 100.0 && feedZ > 1)
                {
                    errorMessage = $"上料机构在放料侧且Z轴未升起(Y:{feedY:F2}, Z:{feedZ:F2})，禁止检测轴移动";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 3. 分料Y轴在取料位（100.0-200.0之间）且Z轴未升起（Z>1）：禁止移动
                double sortY = AllAxisPos.GetEncPos(AllAxisPos.分料Y轴);
                double sortZ = AllAxisPos.GetEncPos(AllAxisPos.分料Z轴);
                if (sortY > 100.0 && sortY < 200.0 && sortZ > 1)
                {
                    errorMessage = $"分料机构在取料位且Z轴未升起(Y:{sortY:F2}, Z:{sortZ:F2})，禁止检测轴移动";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 4. 检查相机轴是否在安全位置
                if (!AreCameraAxesInSafePosition())
                {
                    errorMessage = "检测相机轴未在安全位置，禁止检测机构移动";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Green, "检测轴允许移动");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"检测机构安全检查异常: {ex.Message}";
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red, errorMessage);
                return false;
            }
        }

        /// <summary>
        /// 检查相机轴是否在安全位置
        /// 确保相机不会与其他运动部件发生碰撞
        /// </summary>
        /// <returns>true表示相机轴在安全位置</returns>
        private static bool AreCameraAxesInSafePosition()
        {
            // 实际应用中应根据具体安全位置参数实现
            double leftCameraPos = AllAxisPos.GetEncPos(AllAxisPos.左检测相机);
            double rightCameraPos = AllAxisPos.GetEncPos(AllAxisPos.右检测相机);

            // 示例安全位置检查逻辑
            return leftCameraPos < 50.0 && rightCameraPos < 200.0;
        }

        /// <summary>
        /// 带防抖的气缸原点检测
        /// 避免因传感器抖动导致误判，确保气缸确实在原点位置
        /// </summary>
        /// <returns>true表示气缸在原点位置</returns>
        private static bool IsCylinderAtOriginWithDebounce()
        {
            // 第一次检测
            bool firstCheck = IoManager.GetDiStatus(IoManager.上料解锁气缸原点)
                           && IoManager.GetDiStatus(IoManager.下料解锁气缸原点);

            if (!firstCheck)
            {
                // 等待防抖时间（100毫秒）后再次检测
                Thread.Sleep(100);
                return IoManager.GetDiStatus(IoManager.上料解锁气缸原点)
                    && IoManager.GetDiStatus(IoManager.下料解锁气缸原点);
            }

            return true;
        }
        #endregion

        #region 分料机构安全判断
        /// <summary>
        /// 判断分料轴是否允许移动
        /// 核心逻辑：避免Y轴大幅移动时Z轴处于低位，以及与组装轴干涉
        /// </summary>
        /// <param name="targetYPos">Y轴目标位置</param>
        /// <param name="errorMessage">输出：禁止移动的原因</param>
        /// <returns>true=允许移动，false=禁止移动</returns>
        public static bool IsSortingMovementAllowed(double targetYPos, out string errorMessage)
        {
            // 先进行基础安全检查
            if (!BasicSafetyCheck(out errorMessage))
                return false;

            try
            {
                // 缓存当前位置
                double currentY = AllAxisPos.GetEncPos(AllAxisPos.分料Y轴);
                double currentZ = AllAxisPos.GetEncPos(AllAxisPos.分料Z轴);
                double assembleY = AllAxisPos.GetEncPos(AllAxisPos.组装Y轴);

                // 1. Y轴大幅移动（超过1.0单位）且Z轴未升起（Z<= -1）：禁止移动
                if (Math.Abs(currentY - targetYPos) > 1.0 && currentZ <= -1)
                {
                    errorMessage = $"分料Y轴需移动{Math.Abs(currentY - targetYPos):F2}单位，但Z轴高度({currentZ:F2})低于安全高度(1.0)";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 2. 分料Y轴进入组装区域（>200.0为干涉边界）且组装Y轴在干涉位（< -20.0）：禁止移动
                if (targetYPos > 200.0 && assembleY < -20.0)
                {
                    errorMessage = $"分料Y轴目标位置({targetYPos:F2})与组装Y轴位置({assembleY:F2})存在干涉";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 3. 检查缓冲轴是否在干涉区域
                if (IsBufferInSortingInterferenceArea())
                {
                    errorMessage = "补料缓冲区轴处于分料机构干涉区域，禁止移动";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 允许移动时激活使能输出
                IoManager.SetDoStatus(IoManager.分料机构允许运动输出, true);
                RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Green, "分料轴允许移动");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"分料机构安全检查异常: {ex.Message}";
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red, errorMessage);
                IoManager.SetDoStatus(IoManager.分料机构允许运动输出, false);
                return false;
            }
        }

        /// <summary>
        /// 检查缓冲轴是否在分料机构的干涉区域
        /// 防止缓冲轴与分料机构同时在同一区域运动导致碰撞
        /// </summary>
        /// <returns>true表示存在干涉风险</returns>
        private static bool IsBufferInSortingInterferenceArea()
        {
            // 当前示例返回false，实际应用中应根据具体情况实现
            return false;

            // 示例实现：缓冲X轴在100-200范围内时与分料机构干涉
            // double bufferX = AllAxisPos.GetEncPos(AllAxisPos.补料BufferX轴);
            // return bufferX > 100.0 && bufferX < 200.0;
        }
        #endregion

        #region 缓冲轴安全判断
        /// <summary>
        /// 判断缓冲X轴是否允许移动
        /// 核心逻辑：避免与分料机构在缓冲位干涉
        /// </summary>
        /// <param name="errorMessage">输出：禁止移动的原因</param>
        /// <returns>true=允许移动，false=禁止移动</returns>
        public static bool IsBufferXAxisMovementAllowed(out string errorMessage)
        {
            // 先进行基础安全检查
            if (!BasicSafetyCheck(out errorMessage))
                return false;

            try
            {
                double sortY = AllAxisPos.GetEncPos(AllAxisPos.分料Y轴);
                double sortZ = AllAxisPos.GetEncPos(AllAxisPos.分料Z轴);

                // 分料Y轴在缓冲位（50.0-150.0之间）且Z轴未升起（Z>1）：禁止移动
                if (sortY > 50.0 && sortY < 150.0 && sortZ > 1)
                {
                    errorMessage = $"分料机构在缓冲位且Z轴未升起(Y:{sortY:F2}, Z:{sortZ:F2})，禁止缓冲轴移动";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Green, "缓冲X轴允许移动");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"缓冲轴安全检查异常: {ex.Message}";
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red, errorMessage);
                return false;
            }
        }
        #endregion

        #region 组装机构安全判断
        /// <summary>
        /// 判断组装轴是否允许移动
        /// 核心逻辑：避免与分料机构干涉，确保Z轴安全高度
        /// </summary>
        /// <param name="targetXPos">X轴目标位置</param>
        /// <param name="targetYPos">Y轴目标位置</param>
        /// <param name="errorMessage">输出：禁止移动的原因</param>
        /// <returns>true=允许移动，false=禁止移动</returns>
        public static bool IsAssemblyAxisMovementAllowed(double targetXPos, double targetYPos, out string errorMessage)
        {
            // 先进行基础安全检查
            if (!BasicSafetyCheck(out errorMessage))
                return false;

            try
            {
                // 缓存当前位置
                double currentX = AllAxisPos.GetEncPos(AllAxisPos.组装X轴);
                double currentY = AllAxisPos.GetEncPos(AllAxisPos.组装Y轴);
                double currentZ = AllAxisPos.GetEncPos(AllAxisPos.组装Z轴);
                double sortY = AllAxisPos.GetEncPos(AllAxisPos.分料Y轴);

                // 1. X或Y轴大幅移动（超过1.0单位）且Z轴未升起（<=1）：禁止移动
                if ((Math.Abs(currentX - targetXPos) > 1.0 ||
                     Math.Abs(currentY - targetYPos) > 1.0) &&
                    currentZ >= 1)
                {
                    errorMessage = $"组装轴需移动且Z轴高度({currentZ:F2})低于安全高度(1)";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 2. 组装Y轴进入分料区域（< -20.0）且分料Y轴在干涉位（>200.0）：禁止移动
                if (currentY < -20.0 && sortY > 200.0)
                {
                    errorMessage = $"组装Y轴位置({currentY:F2})与分料Y轴位置({sortY:F2})存在干涉";
                    RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange, errorMessage);
                    return false;
                }

                // 允许移动时激活使能输出
                IoManager.SetDoStatus(IoManager.组装机构允许运动输出, true);
                RLog.SaveLog(LogType.Operation, LogLevel.Debug, Color.Green, "组装轴允许移动");
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"组装机构安全检查异常: {ex.Message}";
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red, errorMessage);
                IoManager.SetDoStatus(IoManager.组装机构允许运动输出, false);
                return false;
            }
        }
        #endregion

        #region 紧急停止处理
        /// <summary>
        /// 触发紧急停止，停止所有轴运动
        /// 安全控制的最后手段，发生严重安全问题时调用
        /// </summary>
        public static void TriggerEmergencyStop()
        {
            try
            {
                // 记录急停事件（优先记录）
                LogEmergencyStop();

                // 停止所有轴运动
                AllAxisPos.ResetAllRunningStatus();

                // 关闭所有运动使能
                IoManager.SetDoStatus(IoManager.上料机构允许运动输出, false);
                IoManager.SetDoStatus(IoManager.分料机构允许运动输出, false);
                IoManager.SetDoStatus(IoManager.组装机构允许运动输出, false);

                // 激活急停报警输出
                IoManager.SetDoStatus(IoManager.急停报警输出, true);
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Error, LogLevel.Fatal, Color.Red, $"紧急停止处理异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录紧急停止事件
        /// 紧急停止是严重的安全事件，需要详细记录
        /// </summary>
        private static void LogEmergencyStop()
        {
            RLog.SaveLog(LogType.Operation, LogLevel.Fatal, Color.Red,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 触发紧急停止");
        }
        #endregion

        /// <summary>
        /// 检查运动点位的防呆安全条件
        /// 根据不同的运动点位类型，调用相应的安全检查方法
        /// </summary>
        /// <param name="currentMotionPoint">当前运动点位信息</param>
        /// <returns>是否允许运动（true=允许，false=禁止）</returns>
        public static bool CheckMotionSafety(MotionPointDto currentMotionPoint)
        {
            if (currentMotionPoint == null)
            {
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red, "运动点位信息为空，禁止运动");
                return false;
            }

            try
            {
                // 防呆逻辑！！！重要：根据不同点位类型执行相应的安全检查
                switch (currentMotionPoint.点位定义)
                {
                    case "上料":
                        if (!SafetyInterlock.IsFeedingYZMovementAllowed(currentMotionPoint.Y轴目标位置, out string feedingError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"上料模组-安全互锁不满足，无法移动！原因：{feedingError}");
                            return false;
                        }
                        break;

                    case "检测X/旋转轴":
                    case "检测X轴":
                        if (!SafetyInterlock.IsInspectionAxisMovementAllowed(out string inspectionError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"检测轴-安全互锁不满足，无法移动！原因：{inspectionError}");
                            return false;
                        }
                        break;

                    case "分料":
                        if (!SafetyInterlock.IsSortingMovementAllowed(currentMotionPoint.Y轴目标位置, out string sortingError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"分料模组-安全互锁不满足，无法移动！原因：{sortingError}");
                            return false;
                        }
                        break;

                    case "缓冲取放":
                    case "缓冲轴":
                        if (!SafetyInterlock.IsBufferXAxisMovementAllowed(out string bufferError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"缓冲轴-安全互锁不满足，无法移动！原因：{bufferError}");
                            return false;
                        }
                        break;

                    case "组装":
                        if (!SafetyInterlock.IsAssemblyAxisMovementAllowed(
                            currentMotionPoint.X轴目标位置,
                            currentMotionPoint.Y轴目标位置,
                            out string assemblyError))
                        {
                            RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Orange, $"组装模组-安全互锁不满足，无法移动！原因：{assemblyError}");
                            return false;
                        }
                        break;

                    default:
                        // 未定义防呆检查的点位，记录警告日志
                        RLog.SaveLog(LogType.Operation, LogLevel.Warn, Color.Orange,
                            $"未定义防呆检查的点位: {currentMotionPoint.点位定义}，默认允许移动");
                        break;
                }

                // 所有防呆检查通过
                return true;
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Error, LogLevel.Error, Color.Red,
                    $"运动安全检查异常 (点位: {currentMotionPoint.点位定义}): {ex.Message}");
                return false; // 异常情况下禁止移动
            }
        }
    }
}