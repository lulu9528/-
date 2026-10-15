using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ToolBlock;
using Jabil.Lib.Common;
using Jabil.Lib;
using Jabil.Lib.Parameter;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Log.Lib;
using Jabil.Module.Vision.Common.Dtos;
using Jabil.Module.Vision.Entity;
using Jabil.Module.Vision.Lib;
using Jabil.Module.Vision.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoMapper;

namespace Jabil.Module.Vision
{
    /// <summary>
    /// 视觉系统核心管理器
    /// 负责视觉工具、检测点位的集中管理及业务逻辑协调
    /// 采用单例模式确保系统内唯一实例，避免资源竞争和状态不一致
    /// 继承BindableBase以支持数据绑定和属性变更通知
    /// </summary>
    public class VisionSystem : BindableBase
    {
        #region 单例模式实现
        /// <summary>
        /// 单例实例
        /// </summary>
        private static VisionSystem _instance;

        /// <summary>
        /// 线程同步锁，确保单例创建的线程安全性
        /// </summary>
        private static readonly object _syncLock = new object();

        /// <summary>
        /// 线程安全的单例实例访问属性
        /// 采用双重检查锁定(Double-Check Locking)模式实现高效线程安全
        /// </summary>
        public static VisionSystem Instance
        {
            get
            {
                // 第一次检查，避免不必要的锁获取
                if (_instance == null)
                {
                    // 锁定操作，确保只有一个线程进入实例创建代码
                    lock (_syncLock)
                    {
                        // 第二次检查，防止多个线程同时通过第一次检查后重复创建实例
                        if (_instance == null)
                        {
                            _instance = new VisionSystem();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 私有字段
        /// <summary>
        /// 视觉工具服务实例，负责视觉工具的数据库操作
        /// </summary>
        private readonly VisionToolService _visionToolService;

        /// <summary>
        /// 视觉检测点位服务实例，负责检测点位的数据库操作
        /// </summary>
        private readonly VisionInspectionService _visionInspectionService;
        #endregion

        #region 公共属性
        /// <summary>
        /// 视觉工具列表
        /// 存储所有视觉工具的数据传输对象(DTO)
        /// 数据变更时通过RaisePropertyChanged()自动通知UI更新
        /// </summary>
        private List<VisionToolDto> _listVisionTool = new List<VisionToolDto>();
        public List<VisionToolDto> ListVisionTool
        {
            get => _listVisionTool;
            set { _listVisionTool = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 视觉检测点位列表
        /// 存储所有检测点位的数据传输对象(DTO)
        /// 数据变更时通过RaisePropertyChanged()自动通知UI更新
        /// </summary>
        private List<VisionInspectionDto> _listVisionInspection = new List<VisionInspectionDto>();
        public List<VisionInspectionDto> ListVisionInspection
        {
            get => _listVisionInspection;
            set { _listVisionInspection = value; RaisePropertyChanged(); }
        }
        #endregion

        #region 视觉工具实例管理
        /// <summary>
        /// 图像采集工具数组
        /// 索引对应不同相机，最大支持6个相机设备
        /// CogAcqFifoTool用于从相机或图像采集卡获取图像数据
        /// </summary>
        public static CogAcqFifoTool[] AcqFifoTools { get; } = new CogAcqFifoTool[6];

        /// <summary>
        /// 检测工具块数组
        /// 最大支持20个工具块，用于执行复杂视觉检测流程
        /// CogToolBlock可包含多个视觉工具，按预设流程执行并返回结果
        /// </summary>
        public static CogToolBlock[] ToolBlocks { get; } = new CogToolBlock[20];

        /// <summary>
        /// N点标定工具数组
        /// 最大支持20个标定工具，用于坐标转换标定
        /// 通过已知的N对点坐标计算坐标转换关系，实现不同坐标系间的转换
        /// </summary>
        public static CogCalibNPointToNPointTool[] CalibNPointTools { get; } = new CogCalibNPointToNPointTool[20];

        /// <summary>
        /// 棋盘格标定工具数组
        /// 最大支持20个标定工具，用于相机内参标定
        /// 通过拍摄棋盘格图案计算相机的畸变参数、焦距等内参
        /// </summary>
        public static CogCalibCheckerboardTool[] CalibCheckerboardTools { get; } = new CogCalibCheckerboardTool[20];

        /// <summary>
        /// 本地图像文件工具
        /// 用于加载和处理本地图像文件（如BMP、PNG、JPG等）
        /// 主要用于离线测试和算法验证
        /// </summary>
        public static CogImageFileTool ImageFileTool { get; } = new CogImageFileTool();

        /// <summary>
        /// 文件打开对话框
        /// 用于选择图像文件或配置文件（如.vpp格式的视觉工具配置）
        /// </summary>
        public static OpenFileDialog OpenFileDialog { get; } = new OpenFileDialog();

        /// <summary>
        /// 文件保存对话框
        /// 用于保存处理后的图像或视觉工具配置
        /// </summary>
        public static SaveFileDialog SaveFileDialog { get; } = new SaveFileDialog();





        ///<summary>
        ///用于存放飞拍图片
        ///</summary>
        ///
        public  List<ICogImage> List_飞拍图片 = new List<ICogImage>();
        #endregion

        #region 构造函数
        /// <summary>
        /// 私有构造函数，确保只能通过Instance属性获取实例
        /// 初始化服务层实例并加载初始数据
        /// </summary>
        private VisionSystem()
        {
            try
            {
                // 初始化数据访问服务
                _visionToolService = new VisionToolService();
                _visionInspectionService = new VisionInspectionService();

                // 异步加载数据，避免阻塞UI线程
                // 使用_ = 忽略返回的Task，因为构造函数不能是async
                _ = RefreshDataAsync();
            }
            catch (Exception ex)
            {
                // 记录初始化错误
                RLog.SaveError(ex);
                // 包装异常并重新抛出，提供更明确的错误信息
                throw new InvalidOperationException("视觉系统初始化失败", ex);
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 添加或更新视觉工具
        /// 当工具ID存在时执行更新操作，否则执行添加操作
        /// </summary>
        /// <param name="dto">视觉工具数据传输对象，包含工具的所有属性信息</param>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task AddOrUpdateVisionToolAsync(VisionToolDto dto)
        {
            // 参数验证
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "视觉工具数据不能为空");

            try
            {
                // 调用服务层执行添加或更新操作
                var result = await _visionToolService.AddOrUpdateAsync(dto);

                // 操作成功则刷新数据
                if (result.Status)
                {
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // 记录异常并继续抛出，由调用者处理
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 批量添加或更新视觉工具
        /// 循环调用服务层单条写入，全部成功后统一刷新一次数据源（避免 N 次写入 N 次全量刷新）
        /// </summary>
        /// <param name="dtos">要添加或更新的视觉工具DTO集合</param>
        /// <exception cref="ArgumentNullException">当dtos或其中任一元素为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当任一工具保存失败时抛出</exception>
        public async Task AddOrUpdateVisionToolsAsync(IEnumerable<VisionToolDto> dtos)
        {
            // 参数验证
            if (dtos == null)
                throw new ArgumentNullException(nameof(dtos), "视觉工具数据不能为空");

            var list = dtos.ToList();
            if (list.Count == 0)
                return;

            try
            {
                // 逐条写入（服务层单条 upsert，不刷新），全部成功后统一刷新一次
                foreach (var dto in list)
                {
                    if (dto == null)
                        throw new ArgumentNullException(nameof(dto), "视觉工具数据不能为空");

                    var result = await _visionToolService.AddOrUpdateAsync(dto);
                    if (!result.Status)
                        throw new InvalidOperationException($"视觉工具保存失败：{dto.检测工具名称}");
                }

                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                // 记录异常并继续抛出，由调用者处理
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 添加或更新视觉检测点位
        /// 当点位ID存在时执行更新操作，否则执行添加操作
        /// </summary>
        /// <param name="dto">检测点位数据传输对象，包含点位的所有属性信息</param>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task AddOrUpdateVisionInspectionAsync(VisionInspectionDto dto)
        {
            // 参数验证
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "检测点位数据不能为空");

            try
            {
                // 调用服务层执行添加或更新操作
                var result = await _visionInspectionService.AddOrUpdateAsync(dto);

                // 操作成功则刷新数据
                if (result.Status)
                {
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // 记录异常并继续抛出
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 从数据库刷新检测点位数据到DataGridView
        /// 用于UI界面的数据展示更新
        /// </summary>
        /// <param name="dataGridView">目标数据网格控件，若为null则仅刷新内部数据</param>
        /// <param name="columnWidth">列宽设置，默认为120</param>
        public async Task RefreshInspectionGridViewAsync(DataGridView dataGridView = null, int columnWidth = 120)
        {
            try
            {
                // 调用服务层从数据库刷新数据到DataGridView
                var result = await _visionInspectionService.RefreshDataGridViewFromSqlAsync(dataGridView);

                // 配置数据网格列样式
                if (dataGridView != null)
                {
                    ConfigureGridViewColumns(dataGridView);
                }

                // 操作成功则刷新内部数据列表
                if (result.Status)
                {
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // 记录异常，不抛出以避免UI崩溃
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 从数据库刷新视觉工具数据到DataGridView
        /// 用于UI界面的数据展示更新
        /// </summary>
        /// <param name="dataGridView">目标数据网格控件，若为null则仅刷新内部数据</param>
        /// <param name="columnWidth">列宽设置，默认为120</param>
        public async Task RefreshToolGridViewAsync(DataGridView dataGridView = null, int columnWidth = 120)
        {
            try
            {
                // 调用服务层从数据库刷新数据到DataGridView
                var result = await _visionToolService.RefreshDataGridViewFromSqlAsync(dataGridView);

                // 配置数据网格列样式
                if (dataGridView != null)
                {
                    ConfigureGridViewColumns(dataGridView);
                }

                // 操作成功则刷新内部数据列表
                if (result.Status)
                {
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // 记录异常，不抛出以避免UI崩溃
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 删除单个视觉工具
        /// </summary>
        /// <param name="dto">要删除的视觉工具DTO</param>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task DeleteVisionToolAsync(VisionToolDto dto)
        {
            // 参数验证
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "请指定要删除的视觉工具");

            try
            {
                // 调用服务层执行删除操作
                var result = await _visionToolService.DeleteAsync(dto);

                // 操作成功则刷新数据
                if (result.Status)
                {
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // 记录异常并继续抛出
                RLog.SaveError(ex);
                throw;
            }
        }

        /// <summary>
        /// 批量删除视觉工具
        /// 提高删除多个工具时的效率
        /// </summary>
        /// <param name="dtos">要删除的视觉工具DTO集合</param>
        public async Task DeleteVisionToolsAsync(IEnumerable<VisionToolDto> dtos)
        {
            // 空值或空集合检查，直接返回
            if (dtos == null || !dtos.Any())
            {
                return;
            }

            try
            {
                // 调用服务层执行批量删除操作
                var result = await _visionToolService.DeleteAsync(dtos);

                // 操作成功则刷新数据
                if (result.Status)
                {
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // 记录异常，不抛出以避免影响其他操作
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 批量删除检测点位
        /// 提高删除多个点位时的效率
        /// </summary>
        /// <param name="dtos">要删除的检测点位DTO集合</param>
        public async Task DeleteVisionInspectionsAsync(IEnumerable<VisionInspectionDto> dtos)
        {
            // 空值或空集合检查，直接返回
            if (dtos == null || !dtos.Any())
            {
                return;
            }

            try
            {
                // 调用服务层执行批量删除操作
                var result = await _visionInspectionService.DeleteAsync(dtos);

                // 操作成功则刷新数据
                if (result.Status)
                {
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // 记录异常，不抛出以避免影响其他操作
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 刷新所有数据
        /// 从数据库重新加载视觉工具和检测点位数据
        /// 采用并行加载提高效率
        /// </summary>
        public async Task RefreshDataAsync()
        {
            try
            {
                // 并行执行两个数据加载任务，提高效率
                var toolTask = _visionToolService.GetAllAsync();
                var inspectionTask = _visionInspectionService.GetAllAsync();

                // 等待所有任务完成
                await Task.WhenAll(toolTask, inspectionTask);

                // 更新视觉工具列表（带空值检查）
                if (toolTask.Result?.Status == true)
                {
                    ListVisionTool = toolTask.Result.Result ?? new List<VisionToolDto>();
                }

                // 更新检测点位列表（带空值检查）
                if (inspectionTask.Result?.Status == true)
                {
                    ListVisionInspection = inspectionTask.Result.Result ?? new List<VisionInspectionDto>();
                }
            }
            catch (Exception ex)
            {
                // 记录异常，不抛出以避免UI崩溃
                RLog.SaveError(ex);
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 配置DataGridView列样式
        /// 统一设置列的自动大小模式和排序模式
        /// </summary>
        /// <param name="grid">要配置的DataGridView控件</param>
        private void ConfigureGridViewColumns(DataGridView grid)
        {
            // 空值检查
            if (grid?.Columns == null) return;

            // 遍历所有列并设置统一样式
            foreach (DataGridViewColumn column in grid.Columns)
            {
                // 列宽自动填充
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                // 禁用排序功能
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
        #endregion
    }
}
