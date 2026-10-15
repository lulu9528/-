using AutoMapper;
using Jabil.Lib;
using Jabil.Lib.Common;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Alarm.Common.Dtos;
using Jabil.Module.Alarm.Entity;
using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Alarm.Service
{
    /// <summary>
    /// 过程错误服务类
    /// 负责处理过程错误数据的CRUD操作，采用单例模式确保全局唯一实例
    /// </summary>
    public class ProcessErrorService
    {
        // 单例模式线程安全锁
        private static readonly object _instanceLock = new object();
        private static ProcessErrorService _instance;

        // 提示信息常量 - 集中管理所有用户提示，便于统一修改和国际化
        private const string Msg_AddSuccess = "添加成功";
        private const string Msg_AddFail = "添加失败";
        private const string Msg_DeleteSuccess = "删除完成";
        private const string Msg_DeleteFail = "删除失败";
        private const string Msg_NoDataToDelete = "请选择要删除的数据";
        private const string Msg_FileCopyFailed = "文件复制失败";
        private const string Msg_SourcePathEmpty = "源文件路径不能为空";
        private const string Msg_TargetPathEmpty = "目标文件路径不能为空";
        private const string Msg_QueryFailed = "查询失败：";
        private const string Msg_OperationFailed = "操作失败：";

        // 数据库文件相关常量
        private const string DbFileName = "ProcessError.accdb";
        private const int HResultFileNotFound = -2146233088; // 数据库文件未找到的错误码

        /// <summary>
        /// AutoMapper实例，用于DTO与实体间的映射
        /// </summary>
        protected readonly IMapper _mapper;

        /// <summary>
        /// 单例实例访问属性
        /// 采用双重检查锁定确保多线程环境下的线程安全
        /// </summary>
        public static ProcessErrorService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ProcessErrorService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数，确保只能通过单例属性获取实例
        /// 初始化AutoMapper实例
        /// </summary>
        private ProcessErrorService()
        {
            _mapper = AutoMapperHelper.Instance.Mapper;
        }

        #region 查询操作

        /// <summary>
        /// 异步获取所有过程错误数据
        /// </summary>
        /// <returns>包含过程错误DTO列表的ApiResponse</returns>
        public async Task<ApiResponse<List<ProcessErrorDto>>> GetAllAsync()
        {
            try
            {
                // 异步查询所有实体并转换为DTO
                var entities = await SugarSQLProcessHelper.Instance.DB
                    .Queryable<ProcessErrorEntity>()
                    .ToListAsync()
                    .ConfigureAwait(false);

                var dtos = _mapper.Map<List<ProcessErrorDto>>(entities);
                return new ApiResponse<List<ProcessErrorDto>>(null, true, dtos);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse<List<ProcessErrorDto>>($"{Msg_QueryFailed}{ex.Message}");
            }
        }

        #endregion

        #region 添加操作

        /// <summary>
        /// 异步添加过程错误记录
        /// </summary>
        /// <param name="dto">待添加的过程错误DTO</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task<ApiResponse> AddAsync(ProcessErrorDto dto)
        {
            // 参数验证：使用卫语句提前返回异常
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "添加的数据不能为空");

            try
            {
                // DTO转换为实体并执行插入
                var entity = _mapper.Map<ProcessErrorEntity>(dto);
                var affectRows = await SugarSQLProcessHelper.Instance.DB
                    .Insertable(entity)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                return affectRows > 0
                    ? new ApiResponse(Msg_AddSuccess, true)
                    : new ApiResponse(Msg_AddFail);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"{Msg_OperationFailed}{ex.Message}");
            }
        }

        /// <summary>
        /// 同步添加过程错误记录
        /// 包含数据库文件自动修复逻辑（当文件不存在时）
        /// </summary>
        /// <param name="dto">待添加的过程错误DTO</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public ApiResponse Add(ProcessErrorDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "添加的数据不能为空");

            try
            {
                var entity = _mapper.Map<ProcessErrorEntity>(dto);
                var affectRows = SugarSQLProcessHelper.Instance.DB
                    .Insertable(entity)
                    .ExecuteCommand();

                return affectRows > 0
                    ? new ApiResponse(Msg_AddSuccess, true)
                    : new ApiResponse(Msg_AddFail);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);

                // 处理数据库文件不存在的特定异常（HResult对应文件未找到）
                if (ex.HResult == HResultFileNotFound)
                {
                    // 构建源文件和目标文件路径（使用Path.Combine确保跨平台兼容性）
                    var targetDir = Path.Combine(
                        Project.Instance.ParameterPath.DataPath,
                        "Config", "Log", "ProcessError"
                    );
                    var targetPath = Path.Combine(targetDir, DbFileName);
                    var sourcePath = Path.Combine(
                        Application.StartupPath,
                        "Config", "Project", "Profile", "SQL", DbFileName
                    );

                    // 当目标文件不存在时，从源路径复制文件
                    if (!File.Exists(targetPath))
                    {
                        Directory.CreateDirectory(targetDir); // 确保目标目录存在
                        FileCopy(sourcePath, targetPath);
                    }
                }

                return new ApiResponse($"{Msg_OperationFailed}{ex.Message}");
            }
        }

        #endregion

        #region 删除操作

        /// <summary>
        /// 异步删除单个过程错误记录
        /// </summary>
        /// <param name="dto">待删除的过程错误DTO</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        /// <exception cref="ArgumentNullException">当dto为null时抛出</exception>
        public async Task<ApiResponse> DeleteAsync(ProcessErrorDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "删除的数据不能为空");

            try
            {
                var entity = _mapper.Map<ProcessErrorEntity>(dto);
                var affectRows = await SugarSQLProcessHelper.Instance.DB
                    .Deleteable<ProcessErrorEntity>(u => u.报警ID == entity.报警ID)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                return affectRows > 0
                    ? new ApiResponse($"{Msg_DeleteSuccess}，Process {dto.报警ID}已删除", true)
                    : new ApiResponse($"{Msg_DeleteFail}，Process {dto.报警ID}不存在");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"{Msg_OperationFailed}{ex.Message}");
            }
        }

        /// <summary>
        /// 异步批量删除过程错误记录
        /// </summary>
        /// <param name="dtos">待删除的过程错误DTO集合</param>
        /// <returns>包含操作结果的ApiResponse</returns>
        public async Task<ApiResponse> DeleteAsync(IEnumerable<ProcessErrorDto> dtos)
        {
            // 验证删除数据是否为空
            if (dtos == null || !dtos.Any())
                return new ApiResponse(Msg_NoDataToDelete);

            try
            {
                var entities = _mapper.Map<List<ProcessErrorEntity>>(dtos);
                var affectRows = await SugarSQLProcessHelper.Instance.DB
                    .Deleteable(entities)
                    .ExecuteCommandAsync()
                    .ConfigureAwait(false);

                return affectRows > 0
                    ? new ApiResponse($"{Msg_DeleteSuccess}，共删除{affectRows}条记录", true)
                    : new ApiResponse($"{Msg_DeleteFail}，Process不存在或已删除");
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                return new ApiResponse($"{Msg_OperationFailed}{ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 安全复制文件（带路径验证和异常封装）
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="targetPath">目标文件路径</param>
        /// <exception cref="ArgumentException">当源路径或目标路径为空时抛出</exception>
        /// <exception cref="IOException">当文件复制失败时抛出</exception>
        private void FileCopy(string sourcePath, string targetPath)
        {
            // 路径合法性验证
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException(Msg_SourcePathEmpty, nameof(sourcePath));

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(Msg_TargetPathEmpty, nameof(targetPath));

            try
            {
                // 覆盖模式复制文件
                File.Copy(sourcePath, targetPath, overwrite: true);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                // 封装异常为更具体的IOException，便于上层处理
                throw new IOException(Msg_FileCopyFailed, ex);
            }
        }
    }
}
