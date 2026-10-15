using AutoMapper;
using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.Entity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Service
{
    /// <summary>
    /// 登录服务类
    /// 负责处理用户登录相关的业务逻辑，包括不同登录方式的验证
    /// 与数据库交互并返回登录结果
    /// </summary>
    public class LoginService
    {
        /// <summary>
        /// 服务名称标识
        /// 用于标识当前服务（可用于日志、监控等场景）
        /// </summary>
        private readonly string serviceName = "Login";

        /// <summary>
        /// AutoMapper实例
        /// 用于实体与DTO之间的对象映射
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造函数
        /// 初始化AutoMapper实例
        /// </summary>
        public LoginService()
        {
            // 获取单例的AutoMapper实例
            _mapper = AutoMapperHelper.Instance.Mapper;
        }

        /// <summary>
        /// 用户登录方法
        /// 支持多种登录方式（管理员、RFID、账号密码）
        /// </summary>
        /// <param name="user">登录用户DTO，包含登录所需信息</param>
        /// <returns>登录结果的API响应对象，包含用户信息或错误提示</returns>
        public async Task<ApiResponse<UserDto>> Login(LoginUserDto user)
        {
            try
            {
                // 存储查询到的用户实体
                UserEntity query = null;

                // 管理员登录逻辑
                if (user.Account == "Admin")
                {
                    // 查询管理员账号
                    query = await SugarSQLHelper.Instance.DB
                        .Queryable<UserEntity>()
                        .SingleAsync(s => s.Account.Equals(user.Account));

                    // 验证管理员密码（格式为当前日期：年+月+日+*）
                    if (user.PassWord != $"{DateTime.Now.Year}{DateTime.Now.Month}{DateTime.Now.Day}*")
                    {
                        query = null; // 密码错误，置空查询结果
                    }
                }
                // RFID卡登录逻辑（通过员工号登录）
                else if (user.LoginMode == LoginMode.RFID && !string.IsNullOrEmpty(user.EmpNo))
                {
                    query = await SugarSQLHelper.Instance.DB
                        .Queryable<UserEntity>()
                        .SingleAsync(s => s.EmpNo.Equals(user.EmpNo));
                }
                // 账号密码登录逻辑
                else if (user.LoginMode == LoginMode.Account)
                {
                    query = await SugarSQLHelper.Instance.DB
                        .Queryable<UserEntity>()
                        .SingleAsync(s => s.Account.Equals(user.Account) && s.PassWord.Equals(user.PassWord));
                }

                // 验证查询结果
                if (query == null)
                {
                    // 登录失败：账号或密码错误
                    return new ApiResponse<UserDto>("账号或密码错误,请重试！");
                }

                // 将用户实体映射为DTO
                var userDto = _mapper.Map<UserDto>(query);

                // 登录成功：返回用户信息
                return new ApiResponse<UserDto>(null, true, userDto);
            }
            catch (Exception ex)
            {
                // 捕获异常并返回错误信息
                return new ApiResponse<UserDto>("操作失败：" + ex.Message);
            }
        }
    }
}