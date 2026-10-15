using SqlSugar;
using System;
using System.IO;

namespace Jabil.Lib.SQLHelper
{
    /// <summary>
    /// SQL操作助手基类（抽象类）
    /// 封装SqlSugar访问Access数据库的通用逻辑，子类只需提供具体连接字符串
    /// </summary>
    public abstract class BaseSugarSQLHelper
    {
        /// <summary>
        /// SqlSugar客户端实例（延迟初始化）
        /// </summary>
        private SqlSugarClient _db;

        /// <summary>
        /// 数据库连接配置（子类必须实现，提供具体连接字符串）
        /// </summary>
        protected abstract ConnectionConfig ConnectionConfig { get; }

        /// <summary>
        /// 获取SqlSugar客户端实例（线程安全，自动初始化）
        /// </summary>
        public SqlSugarClient DB
        {
            get
            {
                if (_db == null)
                {
                    // 确保数据库文件目录存在，避免连接失败
                    EnsureDbDirectoryExists(ConnectionConfig.ConnectionString);
                    _db = new SqlSugarClient(ConnectionConfig);
                }
                return _db;
            }
        }

        /// <summary>
        /// 执行更新类SQL语句（INSERT/UPDATE/DELETE）
        /// </summary>
        /// <param name="sql">SQL命令</param>
        /// <returns>受影响的行数</returns>
        public int ExecuteNonQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL语句不能为空", nameof(sql));

            try
            {
                // 利用SqlSugar的Ado执行SQL，自动管理连接（IsAutoCloseConnection=true）
                return DB.Ado.ExecuteCommand(sql);
            }
            catch (Exception ex)
            {
                // 包装异常信息，便于排查问题（包含执行的SQL）
                throw new InvalidOperationException($"执行SQL失败: {sql}", ex);
            }
        }

        /// <summary>
        /// 确保数据库文件所在目录存在，不存在则创建
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        private void EnsureDbDirectoryExists(string connectionString)
        {
            // 从Access连接字符串中提取数据库文件路径（格式：Provider=...;Data Source=文件路径）
            string dataSourceKey = "Data Source=";
            int index = connectionString.IndexOf(dataSourceKey, StringComparison.OrdinalIgnoreCase);
            if (index > -1)
            {
                string dbPath = connectionString.Substring(index + dataSourceKey.Length);
                string dbDirectory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                }
            }
        }
    }

    /// <summary>
    /// 通用SQL数据库操作助手
    /// 连接SQL_ALL.accdb数据库，提供单例访问和数据操作能力
    /// </summary>
    public class SugarSQLHelper : BaseSugarSQLHelper
    {
        /// <summary>
        /// 单例实例（线程安全的延迟初始化）
        /// Lazy<T>确保多线程环境下仅创建一个实例
        /// </summary>
        private static readonly Lazy<SugarSQLHelper> _instance = new Lazy<SugarSQLHelper>(() => new SugarSQLHelper());

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static SugarSQLHelper Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数，防止外部实例化（单例模式）
        /// </summary>
        private SugarSQLHelper() { }

        /// <summary>
        /// 数据库连接配置（重写基类，提供具体连接信息）
        /// </summary>
        protected override ConnectionConfig ConnectionConfig => new ConnectionConfig
        {
            // 数据库连接字符串（Access数据库路径）
            ConnectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={GetDbPath()}",
            DbType = DbType.Access,          // 数据库类型：Access
            IsAutoCloseConnection = true,    // 自动关闭连接（无需手动Close）
            InitKeyType = InitKeyType.SystemTable // 从系统表初始化实体主键信息
        };

        /// <summary>
        /// 获取数据库文件路径
        /// </summary>
        private string GetDbPath()
        {
            // 数据库文件位置：程序运行目录\Config\Project\Profile\SQL\SQL_ALL.accdb
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Project", "Profile", "SQL", "SQL_ALL.accdb");
        }
    }

    /// <summary>
    /// 工艺错误日志数据库操作助手
    /// 连接ProcessError.accdb数据库，专门用于处理工艺错误日志的读写
    /// </summary>
    public class SugarSQLProcessHelper : BaseSugarSQLHelper
    {
        /// <summary>
        /// 单例实例（线程安全的延迟初始化）
        /// </summary>
        private static readonly Lazy<SugarSQLProcessHelper> _instance = new Lazy<SugarSQLProcessHelper>(() => new SugarSQLProcessHelper());

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static SugarSQLProcessHelper Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数，防止外部实例化（单例模式）
        /// </summary>
        private SugarSQLProcessHelper() { }

        /// <summary>
        /// 数据库连接配置（重写基类，提供具体连接信息）
        /// </summary>
        protected override ConnectionConfig ConnectionConfig => new ConnectionConfig
        {
            // 数据库连接字符串（工艺错误日志数据库路径）
            ConnectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={GetDbPath()}",
            DbType = DbType.Access,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.SystemTable
        };

        /// <summary>
        /// 获取工艺错误日志数据库文件路径
        /// </summary>
        private string GetDbPath()
        {
            // 数据库文件位置：项目数据路径\Config\Log\ProcessError\ProcessError.accdb
            return Path.Combine(Project.Instance.ParameterPath.DataPath, "Config", "Log", "ProcessError", "ProcessError.accdb");
        }
    }

    /// <summary>
    /// pgsql数据库操作助手
    /// </summary>
    public class SugarSQLPgHelper : BaseSugarSQLHelper
    {
        /// <summary>
        /// 单例实例（线程安全的延迟初始化）
        /// </summary>
        private static readonly Lazy<SugarSQLPgHelper> _instance = new Lazy<SugarSQLPgHelper>(() => new SugarSQLPgHelper());

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static SugarSQLPgHelper Instance => _instance.Value;

        /// <summary>
        /// 私有构造函数，防止外部实例化（单例模式）
        /// </summary>
        private SugarSQLPgHelper() { }

        public new SqlSugarClient DB
        {
            get
            {
                return new SqlSugarClient(ConnectionConfig);
            }
        }

        /// <summary>
        /// 数据库连接配置
        /// </summary>
        protected override ConnectionConfig ConnectionConfig => new ConnectionConfig
        {
            ConnectionString = "PORT=5432;DATABASE=l15db;HOST=192.168.1.13;PASSWORD=654321;USER ID=postgres",
            DbType = DbType.PostgreSQL,
            IsAutoCloseConnection = true
        };
    }
}
