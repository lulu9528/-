using Jabil.Module.Log.Lib;
using Sunny.UI;
using System;
using System.IO;
using System.Windows.Forms;


namespace Jabil.Module.Help.Frm
{
    /// <summary>
    /// 故障处理帮助窗体
    /// 提供各类设备文档查看功能，包括电路图、流程图、操作手册等
    /// </summary>
    public partial class Frm_故障处理 : UIPage
    {
        /// <summary>
        /// 单例锁对象，确保线程安全
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// 窗体单例实例
        /// </summary>
        private static Frm_故障处理 _instance;

        /// <summary>
        /// 基础文档路径
        /// </summary>
        private readonly string _baseDocPath = Path.Combine(Environment.CurrentDirectory, "Config", "Project", "Profile", "Help");

        /// <summary>
        /// 文档路径集合
        /// </summary>
        private readonly string _circuitDiagramPath;    // 电路图路径
        private readonly string _drawerFlowPath;        // 抽屉流程图路径
        private readonly string _operationGuidePath;    // 设备操作说明书路径
        private readonly string _inspectionGuidePath;   // 点检操作说明书路径
        private readonly string _epsonAlarmCodePath;    // Epson报警代码路径

        /// <summary>
        /// 单例属性，确保全局唯一实例
        /// </summary>
        public static Frm_故障处理 Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    lock (_lock)
                    {
                        if (_instance == null || _instance.IsDisposed)
                        {
                            _instance = new Frm_故障处理();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 构造函数
        /// 初始化路径和组件
        /// </summary>
        public Frm_故障处理()
        {
            // 初始化文档路径（使用Path.Combine确保跨平台路径兼容性）
            _circuitDiagramPath = Path.Combine(_baseDocPath, "电路图.pdf");
            _drawerFlowPath = Path.Combine(_baseDocPath, "抽屉流程图.pdf");
            _operationGuidePath = Path.Combine(_baseDocPath, "设备操作说明书.pdf");
            _inspectionGuidePath = Path.Combine(_baseDocPath, "点检操作说明书.pdf");
            _epsonAlarmCodePath = Path.Combine(_baseDocPath, "Epson报警代码.pdf");

            InitializeComponent();
        }

        /// <summary>
        /// 打开电路图
        /// </summary>
        private void TsbtnOpenCircuitDiagram_Click(object sender, EventArgs e)
        {
            OpenDocument(_circuitDiagramPath, "电路图");
        }

        /// <summary>
        /// 打开抽屉流程图
        /// </summary>
        private void TsbtnOpenDrawerFlow_Click(object sender, EventArgs e)
        {
            OpenDocument(_drawerFlowPath, "抽屉流程图");
        }

        /// <summary>
        /// 打开Epson报警代码文档
        /// </summary>
        private void TsbtnOpenEpsonAlarmCode_Click(object sender, EventArgs e)
        {
            OpenDocument(_epsonAlarmCodePath, "Epson报警代码");
        }

        /// <summary>
        /// 打开文档文件夹
        /// </summary>
        private void TsbtnOpenDocFolder_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(_baseDocPath))
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", _baseDocPath);
                }
                catch (Exception ex)
                {
                    RLog.SaveError(ex);
                }
            }
        }

        /// <summary>
        /// 打开设备操作说明书
        /// </summary>
        private void TsbtnOpenOperationGuide_Click(object sender, EventArgs e)
        {
            OpenDocument(_operationGuidePath, "设备操作说明书");
        }

        /// <summary>
        /// 打开点检操作说明书
        /// </summary>
        private void TsbtnOpenInspectionGuide_Click(object sender, EventArgs e)
        {
            OpenDocument(_inspectionGuidePath, "点检操作说明书");
        }

        /// <summary>
        /// 通用文档打开方法
        /// 封装文件检查、加载和错误提示逻辑
        /// </summary>
        /// <param name="filePath">文档路径</param>
        /// <param name="docName">文档名称（用于错误提示）</param>
        private void OpenDocument(string filePath, string docName)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                webView21.Source = new Uri(filePath);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
    }
}
