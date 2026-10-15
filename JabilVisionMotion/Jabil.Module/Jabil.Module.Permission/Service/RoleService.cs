using Jabil.Module.Permission.Common;
using Jabil.Module.Permission.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Permission.Service
{
    /// <summary>
    /// 角色服务类
    /// 继承自权限服务基类，实现角色管理的核心业务逻辑
    /// 负责角色的查询、添加、更新、删除及初始化操作
    /// </summary>
    public class RoleService : PermissionServiceBase
    {
        #region 查询操作
        /// <summary>
        /// 获取所有角色信息
        /// 从数据库查询所有角色记录并转换为DTO列表
        /// </summary>
        /// <returns>包含所有角色DTO的API响应</returns>
        internal async Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync()
        {
            try
            {
                // 从数据库查询所有角色实体
                var roles = await SugarSQLHelper.Instance.DB.Queryable<RoleEntity>().ToListAsync();

                // 映射为DTO列表
                var dto = _mapper.Map<List<RoleDto>>(roles);

                return new ApiResponse<List<RoleDto>>(null, true, dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<RoleDto>>("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 根据ID查询角色
        /// 获取指定ID的角色详情
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns>包含角色DTO的API响应</returns>
        internal async Task<ApiResponse<RoleDto>> GetRoleById(int id)
        {
            try
            {
                // 根据ID查询角色实体
                var role = await SugarSQLHelper.Instance.DB.Queryable<RoleEntity>()
                    .SingleAsync(r => r.Id == id);

                // 映射为DTO
                var dto = _mapper.Map<RoleDto>(role);

                return new ApiResponse<RoleDto>(null, true, dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<RoleDto>("操作失败：" + ex.Message);
            }
        }
        #endregion

        #region 添加与更新操作
        /// <summary>
        /// 新增角色
        /// 验证角色名称唯一性后添加新角色
        /// </summary>
        /// <param name="model">角色DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> AddAsync(RoleDto model)
        {
            try
            {
                // 查询所有角色用于验证
                var roles = await SugarSQLHelper.Instance.DB.Queryable<RoleEntity>().ToListAsync();
                // 映射为实体
                var role = _mapper.Map<RoleEntity>(model);

                // 检查角色名称是否已存在
                var exist = roles.Any(x => x.RoleName == model.RoleName);
                if (exist)
                {
                    return new ApiResponse("已存在角色账号或角色名");
                }

                // 设置时间戳
                role.CreateTime = DateTime.Now;
                role.UpdateTime = DateTime.Now;

                // 执行插入操作
                var relt = await SugarSQLHelper.Instance.DB.Insertable<RoleEntity>(role).ExecuteCommandAsync();
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
        /// 添加或更新角色
        /// 根据角色名称判断是新增还是更新操作
        /// </summary>
        /// <param name="roleDto">角色DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> AddOrUpdateRole(RoleDto roleDto)
        {
            try
            {
                // 映射为实体
                var entity = _mapper.Map<RoleEntity>(roleDto);

                // 确保权限键不为null
                if (entity.PermissionKey == null)
                {
                    entity.PermissionKey = string.Empty;
                }

                // 查询是否已存在该角色名称的角色
                var queryRole = await SugarSQLHelper.Instance.DB.Queryable<RoleEntity>()
                    .SingleAsync(u => u.RoleName.Equals(roleDto.RoleName));

                int relt = 0;
                if (queryRole == null)
                {
                    // 新增操作：设置创建和更新时间
                    entity.CreateTime = DateTime.Now;
                    entity.UpdateTime = DateTime.Now;
                    relt = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();

                    if (relt >= 0)
                    {
                        return new ApiResponse("添加角色完成", true);
                    }
                    return new ApiResponse("添加角色失败");
                }
                else
                {
                    // 更新操作：只更新时间戳
                    entity.UpdateTime = DateTime.Now;
                    relt = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();

                    if (relt > 0)
                    {
                        return new ApiResponse("更新角色完成", true);
                    }
                    return new ApiResponse("更新角色失败");
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量更新角色
        /// 同时更新多个角色记录
        /// </summary>
        /// <param name="roles">角色DTO集合</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> UpdateRoles(IEnumerable<RoleDto> roles)
        {
            try
            {
                // 验证输入
                if (roles is null || roles.Count() < 1)
                {
                    return new ApiResponse("无更新对象");
                }

                // 映射为实体列表
                var entity = _mapper.Map<List<RoleEntity>>(roles);

                // 执行批量更新
                var relt = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();
                if (relt > 0)
                {
                    return new ApiResponse($"更新角色完成{relt}", true);
                }
                else
                {
                    return new ApiResponse("更新角色失败");
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
        /// 删除单个角色
        /// 根据角色名称删除指定角色
        /// </summary>
        /// <param name="roleDel">要删除的角色DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> DeleteRoleAsync(RoleDto roleDel)
        {
            try
            {
                // 映射为实体
                var role = _mapper.Map<RoleEntity>(roleDel);

                // 执行删除操作
                var relt = await SugarSQLHelper.Instance.DB
                    .Deleteable<RoleDto>(u => u.RoleName == role.RoleName)
                    .ExecuteCommandAsync();

                if (relt > 0)
                {
                    return new ApiResponse($"删除完成,角色{roleDel.RoleName}已删除");
                }

                return new ApiResponse($"删除失败,角色{roleDel.RoleName}不存在");
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量删除角色
        /// 同时删除多个角色记录
        /// </summary>
        /// <param name="roles">要删除的角色DTO集合</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> DeleteRoleAsync(IEnumerable<RoleDto> roles)
        {
            try
            {
                // 映射为实体列表
                var list = new List<RoleEntity>();
                roles.ToList().ForEach(x =>
                {
                    list.Add(_mapper.Map<RoleEntity>(x));
                });

                // 执行批量删除
                var relt = await SugarSQLHelper.Instance.DB.Deleteable(list).ExecuteCommandAsync();
                if (relt > 0)
                {
                    return new ApiResponse("删除角色完成", true);
                }
                return new ApiResponse("删除失败,角色不存在或已删除");
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 确保角色表存在并初始化默认角色数据
        /// 系统启动时调用，创建角色表和预设角色（超级管理员、技术员、操作员）
        /// </summary>
        public override void EnsureTable()
        {
            // 初始化角色表，如果不存在则创建
            SugarSQLHelper.Instance.DB.CodeFirst.InitTables<RoleEntity>();

            // 检查是否已存在基础角色数据，不存在则添加默认角色
            if (SugarSQLHelper.Instance.DB.Queryable<RoleEntity>()
                .Where(r => r.RoleName == "超级管理员")
                .ToList().Count < 1)
            {
                // 添加超级管理员角色（只读，拥有全部权限）
                _ = AddOrUpdateRole(new RoleDto()
                {
                    RoleName = "超级管理员",
                    IsReadOnly = true,
                    PermissionKey = PermissionService.GetPermissKeyAdmin()
                });

                // 添加技术员角色（可编辑，拥有技术员权限）
                _ = AddOrUpdateRole(new RoleDto()
                {
                    RoleName = "技术员",
                    IsReadOnly = false,
                    PermissionKey = PermissionService.GetPermissKeyTE()
                });

                // 添加操作员角色（可编辑，拥有操作员权限）
                _ = AddOrUpdateRole(new RoleDto()
                {
                    RoleName = "操作员",
                    IsReadOnly = false,
                    PermissionKey = PermissionService.GetPermissKeyOp()
                });
            }
        }
        #endregion
    }
}