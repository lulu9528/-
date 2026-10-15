using Newtonsoft.Json;
using System;
using System.IO;

namespace Jabil.Module.Setting.Lib.JVS
{
    /// <summary>
    /// JVS workspace → 点胶机期望结果的 Pin 映射配置（进程内嵌入模式）
    /// 配置文件：{应用根目录}\Config\JVS\JvsWorkspaceMapping.json（不存在时使用内置默认值）
    /// 默认值依据真实方案"筛选分拣程序.jvws"实测：ToolBlock0 / input_image / data
    /// </summary>
    public class JvsWorkspaceMapping
    {
        /// <summary>ToolBlock 名称</summary>
        public string ToolBlock { get; set; } = "ToolBlock0";

        /// <summary>输入 pin 名（显式传图给 ToolBlock）</summary>
        public string InputPin { get; set; } = "input_image";

        /// <summary>输出 pin 名（结果从此 pin 读取）</summary>
        public string OutputPin { get; set; } = "data";

        /// <summary>多检测点分隔符（真实方案为分号）</summary>
        public string PointSeparator { get; set; } = ";";

        /// <summary>字段分隔符（真实方案为逗号）</summary>
        public string FieldSeparator { get; set; } = ",";

        /// <summary>字段顺序（真实方案输出为 X,Y,R 角度）</summary>
        public string FieldOrder { get; set; } = "X,Y,R";

        /// <summary>
        /// 取图工具文件（.jvt，JVS 导出的 CameraTool 工具实例，绝对路径）。
        /// 取图由该工具触发（run → output_image），点胶机进程不驱动任何硬件相机。
        /// 默认空：须在 JVS 设置页配置；正常由 VisionSystem 工具配置（JvsCameraTool 记录）提供。
        /// </summary>
        public string CaptureToolPath { get; set; } = "";

        /// <summary>默认方案文件（兜底；正常由 VisionSystem 工具配置的检测工具路径提供）</summary>
        public string WorkspacePath { get; set; } = "Config\\JVS\\筛选分拣程序.jvws";

        private static JvsWorkspaceMapping _instance;
        private static readonly object _sync = new object();

        /// <summary>全局映射配置（懒加载）</summary>
        public static JvsWorkspaceMapping Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_sync)
                    {
                        if (_instance == null)
                        {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 重新加载配置（设置页保存后调用，使新配置即时生效）。
        /// </summary>
        public static void Reload()
        {
            lock (_sync)
            {
                _instance = Load();
            }
        }

        private static JvsWorkspaceMapping Load()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "Config", "JVS", "JvsWorkspaceMapping.json");
                if (File.Exists(path))
                {
                    var cfg = JsonConvert.DeserializeObject<JvsWorkspaceMapping>(File.ReadAllText(path));
                    if (cfg != null) return cfg;
                }
            }
            catch (Exception)
            {
                // 配置损坏时回退默认值，不阻塞集成
            }
            return new JvsWorkspaceMapping();
        }

        /// <summary>
        /// 保存当前映射配置到 {应用根目录}\Config\JVS\JvsWorkspaceMapping.json
        /// （先确保目录存在；写失败抛出异常由调用方处理）
        /// </summary>
        public void Save()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "JVS");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var path = Path.Combine(dir, "JvsWorkspaceMapping.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
