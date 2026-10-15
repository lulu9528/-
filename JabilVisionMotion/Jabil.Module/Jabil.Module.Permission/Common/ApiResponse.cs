using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Common
{
    /// <summary>
    /// API响应基类
    /// 用于封装API接口的统一返回格式
    /// 包含操作状态、消息和通用结果对象
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// 响应消息
        /// 存储操作结果的描述信息，如"操作成功"、"账号不存在"等
        /// 通常在操作失败时提供详细的错误原因
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 操作状态
        /// 布尔值表示操作是否成功，true为成功，false为失败
        /// 用于前端快速判断操作结果
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// 结果数据
        /// 存储操作返回的具体数据，类型为object，可容纳任意数据
        /// 通常在操作成功时返回业务数据
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// 构造函数
        /// 初始化API响应对象
        /// </summary>
        /// <param name="message">响应消息，默认为null</param>
        /// <param name="status">操作状态，默认为false（失败）</param>
        /// <param name="result">结果数据，默认为默认值</param>
        public ApiResponse(string message = null, bool status = false, object result = default)
        {
            Message = message;
            Status = status;
            Result = result;
        }
    }

    /// <summary>
    /// 泛型API响应类
    /// 继承自API响应的思想，提供强类型的结果数据
    /// 用于需要明确返回数据类型的API接口
    /// </summary>
    /// <typeparam name="T">结果数据的类型</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// 响应消息
        /// 同基类，存储操作结果的描述信息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 操作状态
        /// 同基类，布尔值表示操作是否成功
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// 泛型结果数据
        /// 强类型的结果数据，类型由T指定
        /// 确保返回数据类型的一致性和类型安全
        /// </summary>
        public T Result { get; set; }

        /// <summary>
        /// 构造函数
        /// 初始化泛型API响应对象
        /// </summary>
        /// <param name="message">响应消息，默认为null</param>
        /// <param name="status">操作状态，默认为false（失败）</param>
        /// <param name="result">泛型结果数据，默认为T的默认值</param>
        public ApiResponse(string message = null, bool status = false, T result = default)
        {
            Message = message;
            Status = status;
            Result = result;
        }
    }
}