using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Lib
{
    /// <summary>
    /// 配置信息操作结果封装类
    /// 用于统一传递配置文件（如INI、XML等）操作的执行结果、错误信息
    /// 提供标准化的结果处理机制，便于调用方判断操作状态并处理异常
    /// </summary>
    public class IniInfo
    {
        /// <summary>
        /// 操作执行状态标识
        /// true：操作成功完成；false：操作执行失败
        /// 在测试模式下，无论实际结果如何均返回true（便于测试流程顺畅执行）
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 错误信息详情
        /// 仅当Success为false时有效，包含操作失败的具体原因（如文件不存在、权限不足等）
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 初始化配置信息操作结果实例
        /// 根据实际操作结果和测试模式状态，决定最终的成功标识
        /// </summary>
        /// <param name="result">实际操作的执行结果（true为成功，false为失败）</param>
        /// <param name="errorMessage">操作失败时的错误描述信息（可为null）</param>
        public IniInfo(bool result, string errorMessage)
        {
            // 若系统处于测试模式，强制将操作结果视为成功（忽略实际执行结果）
            // 非测试模式下，直接使用实际操作结果
            Success = Project.Instance.GlobalVariables.IsTestMode ? true : result;

            // 错误信息为空时，默认赋值为空字符串（避免null引用）
            ErrorMessage = errorMessage ?? string.Empty;
        }

        /// <summary>
        /// 快捷创建一个"操作成功"的结果实例
        /// 无需传入错误信息（内部自动处理为空字符串）
        /// </summary>
        /// <returns>Success为true的IniInfo实例</returns>
        public static IniInfo CreateSuccess()
        {
            return new IniInfo(true, string.Empty);
        }

        /// <summary>
        /// 快捷创建一个"操作失败"的结果实例
        /// 需要指定具体的错误信息，便于问题定位
        /// </summary>
        /// <param name="errorMessage">操作失败的详细原因描述</param>
        /// <returns>Success为false且包含错误信息的IniInfo实例</returns>
        public static IniInfo CreateFailure(string errorMessage)
        {
            return new IniInfo(false, errorMessage);
        }

        /// <summary>
        /// 重写ToString方法，返回可读性强的结果描述
        /// 便于调试输出和日志记录，快速了解操作状态
        /// </summary>
        /// <returns>操作结果的字符串表示（成功/失败+错误信息）</returns>
        public override string ToString()
        {
            return Success
                ? "操作成功"
                : $"操作失败: {ErrorMessage}";
        }
    }
}