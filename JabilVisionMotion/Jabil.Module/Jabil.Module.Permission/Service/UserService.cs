using AutoMapper;
using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Service
{
    /// <summary>
    /// 用户服务类
    /// 继承自权限服务基类，实现用户管理的核心业务逻辑
    /// 负责用户的查询、添加、更新、删除及初始化操作
    /// </summary>
    public class UserService : PermissionServiceBase
    {
        /// <summary>
        /// 构造函数
        /// 继承基类的AutoMapper实例
        /// </summary>
        public UserService()
        {
        }

        /// <summary>
        /// 确保用户表存在并初始化默认用户数据
        /// 系统启动时调用，创建用户表和预设用户（管理员、操作员、技术员）
        /// </summary>
        public override void EnsureTable()
        {
            // 初始化用户表，如果不存在则创建
            SugarSQLHelper.Instance.DB.CodeFirst.InitTables<UserEntity>();

            // 检查是否已存在管理员用户，不存在则添加默认用户
            if (SugarSQLHelper.Instance.DB.Queryable<UserEntity>()
                .Where(r => r.Account == "Admin")
                .ToList().Count < 1)
            {
                // 添加管理员用户（角色ID=1，账号密码123）
                _ = AddOrUpdateUser(new UserDto()
                {
                    Account = "Admin",
                    RoleId = 1,
                    PassWord = "123",
                    UserName = "Admin",
                    EmpNo = string.Empty,
                    LoginMode = LoginMode.Account
                });

                // 添加操作员用户（角色ID=3，账号密码123）
                _ = AddOrUpdateUser(new UserDto()
                {
                    Account = "OP",
                    RoleId = 3,
                    PassWord = "123",
                    UserName = "OP",
                    EmpNo = string.Empty,
                    LoginMode = LoginMode.Account
                });

                // 添加技术员用户（角色ID=2，账号密码TE123）
                _ = AddOrUpdateUser(new UserDto()
                {
                    Account = "TE",
                    RoleId = 2,
                    PassWord = "TE123",
                    UserName = "TE",
                    EmpNo = string.Empty,
                    LoginMode = LoginMode.Account
                });
            }
        }

        #region 查询操作
        /// <summary>
        /// 获取所有用户信息
        /// 从数据库查询所有用户记录并转换为DTO列表
        /// </summary>
        /// <returns>包含所有用户DTO的API响应</returns>
        internal async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync()
        {
            try
            {
                // 从数据库查询所有用户实体
                var users = await SugarSQLHelper.Instance.DB.Queryable<UserEntity>().ToListAsync();

                // 映射为DTO列表
                var dto = _mapper.Map<List<UserDto>>(users);

                // 标记超级管理员为只读
                var super = dto.FirstOrDefault(u => u.Account.Equals("superAdmin"));
                if (super != null)
                {
                    super.IsReadOnly = true;
                }

                return new ApiResponse<List<UserDto>>(null, true, dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<UserDto>>("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 通过账号获取用户实体
        /// 用于内部验证和查询
        /// </summary>
        /// <param name="account">用户账号</param>
        /// <returns>用户实体，查询失败返回null</returns>
        private async Task<UserEntity> GetUserByIdAsync(string account)
        {
            try
            {
                return await SugarSQLHelper.Instance.DB.Queryable<UserEntity>()
                    .SingleAsync(s => s.Account.Equals(account));
            }
            catch (Exception)
            {
                return null;
            }
        }
        #endregion

        #region 添加与更新操作
        /// <summary>
        /// 新增用户
        /// 验证用户账号唯一性后添加新用户
        /// </summary>
        /// <param name="model">用户DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> AddAsync(UserDto model)
        {
            try
            {
                // 查询所有用户用于验证
                var users = await SugarSQLHelper.Instance.DB.Queryable<UserEntity>().ToListAsync();
                // 映射为实体
                var user = _mapper.Map<UserEntity>(model);

                // 检查账号是否已存在
                var exist = users.Any(x => x.Account == model.Account);
                if (exist)
                {
                    return new ApiResponse("已存在用户账号或用户名");
                }

                // 设置时间戳
                user.CreateTime = DateTime.Now;
                user.UpdateTime = DateTime.Now;

                // 执行插入操作
                var relt = await SugarSQLHelper.Instance.DB.Insertable<UserEntity>(user).ExecuteCommandAsync();
                if (relt > 0)
                    return new ApiResponse("添加成功", true);

                return new ApiResponse("添加失败");
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 添加或更新用户
        /// 根据登录方式（账号/ RFID）判断是新增还是更新操作
        /// </summary>
        /// <param name="user">用户DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> AddOrUpdateUser(UserDto user)
        {
            try
            {
                // 映射为实体
                var entity = _mapper.Map<UserEntity>(user);
                int relt = 0;
                UserEntity query;

                // 根据登录方式区分查询条件
                if (user.LoginMode == LoginMode.Account)
                {
                    // 账号登录：清空员工号，按账号查询
                    entity.EmpNo = "";
                    query = SugarSQLHelper.Instance.DB.Queryable<UserEntity>()
                        .Single(u => u.Account.Equals(user.Account));
                }
                else
                {
                    // RFID登录：清空账号密码，按员工号查询
                    entity.Account = "";
                    entity.PassWord = "";
                    query = SugarSQLHelper.Instance.DB.Queryable<UserEntity>()
                        .Single(u => u.EmpNo.Equals(user.EmpNo));
                }

                if (query == null)
                {
                    // 新增操作：设置创建和更新时间
                    entity.CreateTime = DateTime.Now;
                    entity.UpdateTime = DateTime.Now;
                    relt = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();

                    if (relt >= 0)
                    {
                        return new ApiResponse("添加用户完成", true);
                    }
                    return new ApiResponse("添加用户失败");
                }
                else
                {
                    // 更新操作：只更新时间戳
                    entity.UpdateTime = DateTime.Now;
                    relt = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();

                    if (relt > 0)
                    {
                        return new ApiResponse("更新用户完成", true);
                    }
                    return new ApiResponse("更新用户失败");
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量更新用户
        /// 同时更新多个用户记录，自动设置更新时间
        /// </summary>
        /// <param name="users">用户DTO集合</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> UpdateUsers(IEnumerable<UserDto> users)
        {
            try
            {
                // 验证输入
                if (users is null || users.Count() < 1)
                {
                    return new ApiResponse("无更新对象");
                }

                // 映射为实体列表并设置更新时间
                var entity = _mapper.Map<List<UserEntity>>(users);
                entity.ForEach(e => e.UpdateTime = DateTime.Now);

                // 执行批量更新
                var relt = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();
                if (relt > 0)
                {
                    return new ApiResponse($"更新用户完成{relt}", true);
                }
                else
                {
                    return new ApiResponse("更新用户失败");
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }
        #endregion

        #region 删除操作
        /// <summary>
        /// 删除单个用户
        /// 根据用户账号删除指定用户
        /// </summary>
        /// <param name="userDel">要删除的用户DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> DeleteUserAsync(UserDto userDel)
        {
            try
            {
                // 映射为实体
                var user = _mapper.Map<UserEntity>(userDel);

                // 执行删除操作
                var relt = await SugarSQLHelper.Instance.DB
                    .Deleteable<UserDto>(u => u.Account == user.Account)
                    .ExecuteCommandAsync();

                if (relt > 0)
                {
                    return new ApiResponse($"删除完成,账户{userDel.UserName}已删除");
                }

                return new ApiResponse($"删除失败,账户{userDel.UserName}不存在");
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量删除用户
        /// 同时删除多个用户记录
        /// </summary>
        /// <param name="users">要删除的用户DTO集合</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> DeleteUserAsync(IEnumerable<UserDto> users)
        {
            try
            {
                // 映射为实体列表
                var list = new List<UserEntity>();
                users.ToList().ForEach(x =>
                {
                    list.Add(_mapper.Map<UserEntity>(x));
                });

                // 执行批量删除
                var relt = await SugarSQLHelper.Instance.DB.Deleteable(list).ExecuteCommandAsync();
                if (relt > 0)
                {
                    return new ApiResponse("删除用户完成", true);
                }
                return new ApiResponse("删除失败,用户不存在或已删除");
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }
        #endregion
    }
}