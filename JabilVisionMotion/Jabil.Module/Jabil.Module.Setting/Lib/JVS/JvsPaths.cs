using System;
using System.IO;

namespace Jabil.Module.Setting.Lib.JVS
{
    /// <summary>
    /// 定位 JVS 安装目录、Python 环境与现场工作空间目录。
    /// 路径通过 JVS 安装器设置的环境变量解析（JabilVisionStudioHomePage / JabilPythonEnvHomePage），
    /// 不依赖相对于构建输出的相对路径，工程被复制到其它机器后依然可用。
    /// </summary>
    public static class JvsPaths
    {
        /// <summary>Jabil Vision Studio 安装目录环境变量名</summary>
        public const string HomeVariable = "JabilVisionStudioHomePage";

        /// <summary>Jabil Python 环境目录环境变量名</summary>
        public const string PythonEnvVariable = "JabilPythonEnvHomePage";

        private static string _home;
        private static string _pythonEnv;

        /// <summary>
        /// JVS 安装目录。
        /// </summary>
        public static string Home
        {
            get
            {
                if (_home == null)
                {
                    _home = ResolveDirectoryFromEnvironment(HomeVariable, "Jabil Vision Studio 安装目录");
                }
                return _home;
            }
        }

        /// <summary>
        /// Jabil Python 环境目录（Jvl.Initialize 传入的 Python 引擎根目录）。
        /// </summary>
        public static string PythonEnv
        {
            get
            {
                if (_pythonEnv == null)
                {
                    _pythonEnv = ResolveDirectoryFromEnvironment(PythonEnvVariable, "Jabil Python 环境目录");
                }
                return _pythonEnv;
            }
        }

        /// <summary>
        /// 从环境变量读取目录并校验存在性；缺失时给出可操作的错误信息。
        /// </summary>
        private static string ResolveDirectoryFromEnvironment(string variableName, string description)
        {
            string value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException(
                    "环境变量 " + variableName + " 未设置。" + Environment.NewLine +
                    "它应指向" + description + "；Jabil Vision Studio 安装器通常会设置它。请安装或重启程序后重试。");
            }
            if (!Directory.Exists(value))
            {
                throw new InvalidOperationException(
                    "环境变量 " + variableName + " 指向的目录不存在：" + Environment.NewLine + "  " + value +
                    Environment.NewLine + "应为" + description + "。");
            }
            return value;
        }
    }
}
