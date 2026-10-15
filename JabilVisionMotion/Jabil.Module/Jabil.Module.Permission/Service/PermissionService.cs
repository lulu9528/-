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
    /// 权限服务类
    /// 继承自PermissionServiceBase，实现权限管理的核心业务逻辑
    /// 负责权限的初始化、CRUD操作及权限密钥生成
    /// </summary>
    public class PermissionService : PermissionServiceBase
    {
        /// <summary>
        /// 确保权限表存在并初始化默认权限数据
        /// 系统启动时调用，用于创建权限表和预设基础权限
        /// </summary>
        public override void EnsureTable()
        {
            // 初始化权限表，如果不存在则创建
            SugarSQLHelper.Instance.DB.CodeFirst.InitTables<PermissionEntity>();

            // 检查是否已存在基础权限数据，不存在则添加默认权限
            if (SugarSQLHelper.Instance.DB.Queryable<PermissionEntity>()
                .Where(p => p.TitleName == "权限")
                .ToList().Count < 1)
            {
                // 添加系统默认权限项
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/1", TitleName = "权限", Target = "权限" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/1/2", TitleName = "用户管理", Target = "权限/用户管理" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/1/3", TitleName = "角色管理", Target = "权限/角色管理" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/1/4", TitleName = "权限管理", Target = "权限/权限管理" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/5", TitleName = "管理员权限", Target = "管理员" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/6", TitleName = "技术员权限", Target = "技术员" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/7", TitleName = "操作员权限", Target = "操作员" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/8", TitleName = "视觉", Target = "视觉" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/9", TitleName = "设置", Target = "设置" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/10", TitleName = "手动", Target = "手动" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/11", TitleName = "报警", Target = "报警" });
                _ = AddOrUpdatePermission(new PermissionDto() { Path = "/12", TitleName = "生产统计", Target = "生产统计" });
            }
        }

        /// <summary>
        /// 生成管理员权限密钥
        /// 包含1-99所有权限ID，以逗号分隔
        /// </summary>
        /// <returns>管理员权限密钥字符串</returns>
        internal static string GetPermissKeyAdmin()
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 1; i < 100; i++)
            {
                stringBuilder.Append(i);
                if (i < 99)
                {
                    stringBuilder.Append(','); // 最后一个ID后不加逗号
                }
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// 生成操作员权限密钥
        /// 包含预设的操作员权限ID：7,10,11,12
        /// </summary>
        /// <returns>操作员权限密钥字符串</returns>
        internal static string GetPermissKeyOp()
        {
            return "7,10,11,12";
        }

        /// <summary>
        /// 生成技术员权限密钥
        /// 包含预设的技术员权限ID：6,11,12
        /// </summary>
        /// <returns>技术员权限密钥字符串</returns>
        internal static string GetPermissKeyTE()
        {
            return "6,11,12";
        }

        #region 查询操作
        /// <summary>
        /// 获取所有权限信息
        /// 从数据库查询所有权限记录并转换为DTO列表
        /// </summary>
        /// <returns>包含所有权限DTO的API响应</returns>
        internal async Task<ApiResponse<List<PermissionDto>>> GetAllPermissionsAsync()
        {
            try
            {
                // 从数据库查询所有权限实体
                var permissions = await SugarSQLHelper.Instance.DB.Queryable<PermissionEntity>().ToListAsync();

                // 映射为DTO列表
                var dto = _mapper.Map<List<PermissionDto>>(permissions);

                return new ApiResponse<List<PermissionDto>>(null, true, dto);
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<PermissionDto>>("操作失败：" + ex.Message);
            }
        }

        ///// <summary>
        ///// 根据路径查询权限（已注释，预留方法）
        ///// </summary>
        ///// <param name="path">权限路径</param>
        ///// <returns>权限实体</returns>
        //private async Task<PermissionEntity> GetPermissionByIdAsync(string path)
        //{
        //    try
        //    {
        //        var model = await SugarSQLHelper.Instance.DB.Queryable<PermissionEntity>().SingleAsync(s => s.Path.Equals(account));
        //        return model;
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}
        #endregion

        #region 添加与更新操作
        /// <summary>
        /// 新增权限
        /// 验证权限路径唯一性后添加新权限
        /// </summary>
        /// <param name="model">权限DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> AddAsync(PermissionDto model)
        {
            try
            {
                // 查询所有权限用于验证
                var permissions = await SugarSQLHelper.Instance.DB.Queryable<PermissionEntity>().ToListAsync();
                // 映射为实体
                var permission = _mapper.Map<PermissionEntity>(model);

                // 检查路径是否已存在
                var exist = permissions.Any(x => x.Path == model.Path);
                if (exist)
                {
                    return new ApiResponse("已存在权限路径");
                }

                // 设置时间戳
                permission.CreateTime = DateTime.Now;
                permission.UpdateTime = DateTime.Now;

                // 执行插入操作
                var relt = await SugarSQLHelper.Instance.DB.Insertable<PermissionEntity>(permission).ExecuteCommandAsync();
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
        /// 添加或更新权限
        /// 根据路径判断是新增还是更新操作
        /// </summary>
        /// <param name="permission">权限DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> AddOrUpdatePermission(PermissionDto permission)
        {
            try
            {
                // 映射为实体
                var entity = _mapper.Map<PermissionEntity>(permission);

                // 查询是否已存在该路径的权限
                var querypermission = await SugarSQLHelper.Instance.DB.Queryable<PermissionEntity>()
                    .SingleAsync(u => u.Path.Equals(permission.Path));

                int relt = 0;
                if (querypermission == null)
                {
                    // 新增操作：设置创建和更新时间
                    entity.CreateTime = DateTime.Now;
                    entity.UpdateTime = DateTime.Now;
                    relt = await SugarSQLHelper.Instance.DB.Insertable(entity).ExecuteCommandAsync();

                    if (relt >= 0)
                    {
                        return new ApiResponse("添加权限完成", true);
                    }
                    return new ApiResponse("添加权限失败");
                }
                else
                {
                    // 更新操作：只更新时间戳
                    entity.UpdateTime = DateTime.Now;
                    relt = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();

                    if (relt > 0)
                    {
                        return new ApiResponse("更新权限完成", true);
                    }
                    return new ApiResponse("更新权限失败");
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量更新权限
        /// 同时更新多个权限记录
        /// </summary>
        /// <param name="permissions">权限DTO集合</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> UpdatePermissions(IEnumerable<PermissionDto> permissions)
        {
            try
            {
                // 验证输入
                if (permissions is null || permissions.Count() < 1)
                {
                    return new ApiResponse("无更新对象");
                }

                // 映射为实体列表
                var entity = _mapper.Map<List<PermissionEntity>>(permissions);

                // 执行批量更新
                var relt = await SugarSQLHelper.Instance.DB.Updateable(entity).ExecuteCommandAsync();
                if (relt > 0)
                {
                    return new ApiResponse($"更新权限完成{relt}", true);
                }
                else
                {
                    return new ApiResponse("更新权限失败");
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
        /// 删除单个权限
        /// 根据权限路径删除指定权限
        /// </summary>
        /// <param name="permissionDel">要删除的权限DTO</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> DeletePermissionAsync(PermissionDto permissionDel)
        {
            try
            {
                // 映射为实体
                var permission = _mapper.Map<PermissionEntity>(permissionDel);

                // 执行删除操作
                var relt = await SugarSQLHelper.Instance.DB
                    .Deleteable<PermissionDto>(u => u.Path == permission.Path)
                    .ExecuteCommandAsync();

                if (relt > 0)
                {
                    return new ApiResponse($"删除完成,账户{permissionDel}已删除");
                }

                return new ApiResponse($"添加失败,账户{permissionDel.Path}不存在");
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 批量删除权限
        /// 同时删除多个权限记录
        /// </summary>
        /// <param name="id">要删除的权限DTO集合</param>
        /// <returns>操作结果的API响应</returns>
        internal async Task<ApiResponse> DeletePermissionAsync(IEnumerable<PermissionDto> id)
        {
            try
            {
                // 映射为实体列表
                var list = new List<PermissionEntity>();
                id.ToList().ForEach(x =>
                {
                    list.Add(_mapper.Map<PermissionEntity>(x));
                });

                // 执行批量删除
                var relt = await SugarSQLHelper.Instance.DB.Deleteable(list).ExecuteCommandAsync();
                if (relt > 0)
                {
                    return new ApiResponse("删除权限完成", true);
                }
                return new ApiResponse("删除失败,权限不存在或已删除");
            }
            catch (Exception ex)
            {
                return new ApiResponse("操作失败：" + ex.Message);
            }
        }
        #endregion
    }
}