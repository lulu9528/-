using Jabil.Module.Log.Lib;
using Sunny.UI;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Jabil.Module.Help.Frm
{
    /// <summary>
    /// 维护保养窗体
    /// 负责加载和展示IP配置信息，支持设备维护相关功能
    /// </summary>
    public partial class Frm_维护保养 : UIPage
    {
        /// <summary>
        /// 单例锁对象，确保线程安全
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// 窗体单例实例
        /// </summary>
        private static Frm_维护保养 _instance;

        /// <summary>
        /// IP配置文件路径
        /// 使用Path.Combine确保跨平台路径兼容性
        /// </summary>
        private readonly string _ipFilePath;

        /// <summary>
        /// 单例属性，确保全局唯一实例
        /// 采用双重检查锁定实现线程安全的单例模式
        /// </summary>
        public static Frm_维护保养 Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    lock (_lock)
                    {
                        if (_instance == null || _instance.IsDisposed)
                        {
                            _instance = new Frm_维护保养();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 构造函数
        /// 初始化组件和IP配置文件路径
        /// </summary>
        public Frm_维护保养()
        {
            // 初始化IP配置文件路径
            _ipFilePath = Path.Combine(
                Environment.CurrentDirectory,
                "Config",
                "Project",
                "Profile",
                "IP",
                "IP.CSV"
            );

            InitializeComponent();
            LoadIpConfigData();
        }

        /// <summary>
        /// 加载IP配置数据到数据网格
        /// 封装文件读取和数据绑定逻辑
        /// </summary>
        private void LoadIpConfigData()
        {
            // 清空现有数据
            uiDataGridView_IP.Rows.Clear();

            // 检查文件是否存在
            if (!File.Exists(_ipFilePath))
            {
                return;
            }

            try
            {
                // 使用using语句自动释放资源，无需手动调用Close和Dispose
                using (var reader = new StreamReader(_ipFilePath, Encoding.Default))
                {
                    // 跳过表头行
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine))
                    {
                        return;
                    }

                    int rowIndex = 0;
                    // 逐行读取数据
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                            continue; // 跳过空行

                        string[] ipData = line.Split(',');

                        // 验证数据格式是否正确
                        if (ipData.Length < 4)
                        {
                            continue;
                        }

                        // 添加数据到网格
                        int index = uiDataGridView_IP.Rows.Add();
                        uiDataGridView_IP.Rows[index].Cells[0].Value = rowIndex.ToString();
                        uiDataGridView_IP.Rows[index].Cells[1].Value = ipData[0];
                        uiDataGridView_IP.Rows[index].Cells[2].Value = ipData[1];
                        uiDataGridView_IP.Rows[index].Cells[3].Value = ipData[2];
                        uiDataGridView_IP.Rows[index].Cells[4].Value = ipData[3];

                        rowIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
    }
}
