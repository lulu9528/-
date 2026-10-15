using Jabil.Lib;
using Jabil.Lib.Parameter;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using Jabil.Module.Setting.Control;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;

namespace Jabil.Module.Setting.Frm
{
    public partial class Frm_Project : UIForm
    {
        // 用于存储参数修改记录的列表
        private List<string> _parameterChangeLogs = new List<string>();

        public Frm_Project()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static Frm_Project _instance;
        public static Frm_Project Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_Project();
                return _instance;
            }
        }

        public HttpClient _httpClient1 = new HttpClient();

        public HttpClient _httpClient2 = new HttpClient();


        private void Frm_Project_Load(object sender, EventArgs e)
        {
            try
            {
                #region 其他参数

             


               // Project.Instance.LoadSystemConfig(ref Project.Instance.weldParm, ParameterPath.StationPostion);

                Project.Instance.LoadSystemConfig(ref Project.Instance.postion左Epson, ParameterPath.StationPostion左Epson);


                Project.Instance.LoadSystemConfig(ref Project.Instance.postion右Epson, ParameterPath.StationPostion右Epson);


                Project.Instance.LoadSystemConfig(ref Project.Instance.飞拍基准位, ParameterPath.飞拍Postion);


                #region 左Epson数据加载
                xyr1.P1_X.Text = Project.Instance.postion左Epson.X1.ToString();
                xyr1.P1_Y.Text = Project.Instance.postion左Epson.Y1.ToString();
                xyr1.P1_R.Text = Project.Instance.postion左Epson.R1.ToString();

                xyr1.P2_X.Text = Project.Instance.postion左Epson.X2.ToString();
                xyr1.P2_Y.Text = Project.Instance.postion左Epson.Y2.ToString();
                xyr1.P2_R.Text = Project.Instance.postion左Epson.R2.ToString();

                xyr1.P3_X.Text = Project.Instance.postion左Epson.X3.ToString();
                xyr1.P3_Y.Text = Project.Instance.postion左Epson.Y3.ToString();
                xyr1.P3_R.Text = Project.Instance.postion左Epson.R3.ToString();


                xyr1.P4_X.Text = Project.Instance.postion左Epson.X4.ToString();
                xyr1.P4_Y.Text = Project.Instance.postion左Epson.Y4.ToString();
                xyr1.P4_R.Text = Project.Instance.postion左Epson.R4.ToString();


                xyr1.P5_X.Text = Project.Instance.postion左Epson.X5.ToString();
                xyr1.P5_Y.Text = Project.Instance.postion左Epson.Y5.ToString();
                xyr1.P5_R.Text = Project.Instance.postion左Epson.R5.ToString();



                xyr1.P6_X.Text = Project.Instance.postion左Epson.X6.ToString();
                xyr1.P6_Y.Text = Project.Instance.postion左Epson.Y6.ToString();
                xyr1.P6_R.Text = Project.Instance.postion左Epson.R6.ToString();



                rotateXY1.P1_X.Text = Project.Instance.postion左Epson.X11.ToString();
                rotateXY1.P1_Y.Text = Project.Instance.postion左Epson.Y11.ToString();


                rotateXY1.P2_X.Text = Project.Instance.postion左Epson.X12.ToString();
                rotateXY1.P2_Y.Text = Project.Instance.postion左Epson.Y12.ToString();



                rotateXY1.P3_X.Text = Project.Instance.postion左Epson.X13.ToString();
                rotateXY1.P3_Y.Text = Project.Instance.postion左Epson.Y13.ToString();


                rotateXY1.P4_X.Text = Project.Instance.postion左Epson.X14.ToString();
                rotateXY1.P4_Y.Text = Project.Instance.postion左Epson.Y14.ToString();


                rotateXY1.P5_X.Text = Project.Instance.postion左Epson.X15.ToString();
                rotateXY1.P5_Y.Text = Project.Instance.postion左Epson.Y15.ToString();


                rotateXY1.P6_X.Text = Project.Instance.postion左Epson.X16.ToString();
                rotateXY1.P6_Y.Text = Project.Instance.postion左Epson.Y16.ToString();



                xyr2.P1_X.Text = Project.Instance.postion左Epson.X21.ToString();
                xyr2.P1_Y.Text = Project.Instance.postion左Epson.Y21.ToString();
                xyr2.P1_R.Text = Project.Instance.postion左Epson.R21.ToString();



                xyr2.P2_X.Text = Project.Instance.postion左Epson.X22.ToString();
                xyr2.P2_Y.Text = Project.Instance.postion左Epson.Y22.ToString();
                xyr2.P2_R.Text = Project.Instance.postion左Epson.R22.ToString();



                xyr2.P3_X.Text = Project.Instance.postion左Epson.X23.ToString();
                xyr2.P3_Y.Text = Project.Instance.postion左Epson.Y23.ToString();
                xyr2.P3_R.Text = Project.Instance.postion左Epson.R23.ToString();



                xyr2.P4_X.Text = Project.Instance.postion左Epson.X24.ToString();
                xyr2.P4_Y.Text = Project.Instance.postion左Epson.Y24.ToString();
                xyr2.P4_R.Text = Project.Instance.postion左Epson.R24.ToString();




                xyr2.P5_X.Text = Project.Instance.postion左Epson.X25.ToString();
                xyr2.P5_Y.Text = Project.Instance.postion左Epson.Y25.ToString();
                xyr2.P5_R.Text = Project.Instance.postion左Epson.R25.ToString();



                xyr2.P6_X.Text = Project.Instance.postion左Epson.X26.ToString();
                xyr2.P6_Y.Text = Project.Instance.postion左Epson.Y26.ToString();
                xyr2.P6_R.Text = Project.Instance.postion左Epson.R26.ToString();
                #endregion



                #region 右Epson数据加载
                xyr3.P1_X.Text = Project.Instance.postion右Epson.X1.ToString();
                xyr3.P1_Y.Text = Project.Instance.postion右Epson.Y1.ToString();
                xyr3.P1_R.Text = Project.Instance.postion右Epson.R1.ToString();

                xyr3.P2_X.Text = Project.Instance.postion右Epson.X2.ToString();
                xyr3.P2_Y.Text = Project.Instance.postion右Epson.Y2.ToString();
                xyr3.P2_R.Text = Project.Instance.postion右Epson.R2.ToString();

                xyr3.P3_X.Text = Project.Instance.postion右Epson.X3.ToString();
                xyr3.P3_Y.Text = Project.Instance.postion右Epson.Y3.ToString();
                xyr3.P3_R.Text = Project.Instance.postion右Epson.R3.ToString();


                xyr3.P4_X.Text = Project.Instance.postion右Epson.X4.ToString();
                xyr3.P4_Y.Text = Project.Instance.postion右Epson.Y4.ToString();
                xyr3.P4_R.Text = Project.Instance.postion右Epson.R4.ToString();


                xyr3.P5_X.Text = Project.Instance.postion右Epson.X5.ToString();
                xyr3.P5_Y.Text = Project.Instance.postion右Epson.Y5.ToString();
                xyr3.P5_R.Text = Project.Instance.postion右Epson.R5.ToString();



                xyr3.P6_X.Text = Project.Instance.postion右Epson.X6.ToString();
                xyr3.P6_Y.Text = Project.Instance.postion右Epson.Y6.ToString();
                xyr3.P6_R.Text = Project.Instance.postion右Epson.R6.ToString();



                rotateXY2.P1_X.Text = Project.Instance.postion右Epson.X11.ToString();
                rotateXY2.P1_Y.Text = Project.Instance.postion右Epson.Y11.ToString();


                rotateXY2.P2_X.Text = Project.Instance.postion右Epson.X12.ToString();
                rotateXY2.P2_Y.Text = Project.Instance.postion右Epson.Y12.ToString();



                rotateXY2.P3_X.Text = Project.Instance.postion右Epson.X13.ToString();
                rotateXY2.P3_Y.Text = Project.Instance.postion右Epson.Y13.ToString();


                rotateXY2.P4_X.Text = Project.Instance.postion右Epson.X14.ToString();
                rotateXY2.P4_Y.Text = Project.Instance.postion右Epson.Y14.ToString();


                rotateXY2.P5_X.Text = Project.Instance.postion右Epson.X15.ToString();
                rotateXY2.P5_Y.Text = Project.Instance.postion右Epson.Y15.ToString();


                rotateXY2.P6_X.Text = Project.Instance.postion右Epson.X16.ToString();
                rotateXY2.P6_Y.Text = Project.Instance.postion右Epson.Y16.ToString();



                xyr4.P1_X.Text = Project.Instance.postion右Epson.X21.ToString();
                xyr4.P1_Y.Text = Project.Instance.postion右Epson.Y21.ToString();
                xyr4.P1_R.Text = Project.Instance.postion右Epson.R21.ToString();



                xyr4.P2_X.Text = Project.Instance.postion右Epson.X22.ToString();
                xyr4.P2_Y.Text = Project.Instance.postion右Epson.Y22.ToString();
                xyr4.P2_R.Text = Project.Instance.postion右Epson.R22.ToString();



                xyr4.P3_X.Text = Project.Instance.postion右Epson.X23.ToString();
                xyr4.P3_Y.Text = Project.Instance.postion右Epson.Y23.ToString();
                xyr4.P3_R.Text = Project.Instance.postion右Epson.R23.ToString();



                xyr4.P4_X.Text = Project.Instance.postion右Epson.X24.ToString();
                xyr4.P4_Y.Text = Project.Instance.postion右Epson.Y24.ToString();
                xyr4.P4_R.Text = Project.Instance.postion右Epson.R24.ToString();




                xyr4.P5_X.Text = Project.Instance.postion右Epson.X25.ToString();
                xyr4.P5_Y.Text = Project.Instance.postion右Epson.Y25.ToString();
                xyr4.P5_R.Text = Project.Instance.postion右Epson.R25.ToString();



                xyr4.P6_X.Text = Project.Instance.postion右Epson.X26.ToString();
                xyr4.P6_Y.Text = Project.Instance.postion右Epson.Y26.ToString();
                xyr4.P6_R.Text = Project.Instance.postion右Epson.R26.ToString();
                #endregion


                #region 飞拍
                xyr5.P1_X.Text = Project.Instance.飞拍基准位.X1.ToString();
                xyr5.P1_Y.Text = Project.Instance.飞拍基准位.Y1.ToString();
                xyr5.P1_R.Text = Project.Instance.飞拍基准位.R1.ToString();

                xyr5.P2_X.Text = Project.Instance.飞拍基准位.X2.ToString();
                xyr5.P2_Y.Text = Project.Instance.飞拍基准位.Y2.ToString();
                xyr5.P2_R.Text = Project.Instance.飞拍基准位.R2.ToString();

                xyr5.P3_X.Text = Project.Instance.飞拍基准位.X3.ToString();
                xyr5.P3_Y.Text = Project.Instance.飞拍基准位.Y3.ToString();
                xyr5.P3_R.Text = Project.Instance.飞拍基准位.R3.ToString();


                xyr5.P4_X.Text = Project.Instance.飞拍基准位.X4.ToString();
                xyr5.P4_Y.Text = Project.Instance.飞拍基准位.Y4.ToString();
                xyr5.P4_R.Text = Project.Instance.飞拍基准位.R4.ToString();


                xyr5.P5_X.Text = Project.Instance.飞拍基准位.X5.ToString();
                xyr5.P5_Y.Text = Project.Instance.飞拍基准位.Y5.ToString();
                xyr5.P5_R.Text = Project.Instance.飞拍基准位.R5.ToString();



                xyr5.P6_X.Text = Project.Instance.飞拍基准位.X6.ToString();
                xyr5.P6_Y.Text = Project.Instance.飞拍基准位.Y6.ToString();
                xyr5.P6_R.Text = Project.Instance.飞拍基准位.R6.ToString();



                //rotateXY2.P1_X.Text = Project.Instance.postion右Epson.X11.ToString();
                //rotateXY2.P1_Y.Text = Project.Instance.postion右Epson.Y11.ToString();


                //rotateXY2.P2_X.Text = Project.Instance.postion右Epson.X12.ToString();
                //rotateXY2.P2_Y.Text = Project.Instance.postion右Epson.Y12.ToString();



                //rotateXY2.P3_X.Text = Project.Instance.postion右Epson.X13.ToString();
                //rotateXY2.P3_Y.Text = Project.Instance.postion右Epson.Y13.ToString();


                //rotateXY2.P4_X.Text = Project.Instance.postion右Epson.X14.ToString();
                //rotateXY2.P4_Y.Text = Project.Instance.postion右Epson.Y14.ToString();


                //rotateXY2.P5_X.Text = Project.Instance.postion右Epson.X15.ToString();
                //rotateXY2.P5_Y.Text = Project.Instance.postion右Epson.Y15.ToString();


                //rotateXY2.P6_X.Text = Project.Instance.postion右Epson.X16.ToString();
                //rotateXY2.P6_Y.Text = Project.Instance.postion右Epson.Y16.ToString();



                xyr6.P1_X.Text = Project.Instance.飞拍基准位.X21.ToString();
                xyr6.P1_Y.Text = Project.Instance.飞拍基准位.Y21.ToString();
                xyr6.P1_R.Text = Project.Instance.飞拍基准位.R21.ToString();



                xyr6.P2_X.Text = Project.Instance.飞拍基准位.X22.ToString();
                xyr6.P2_Y.Text = Project.Instance.飞拍基准位.Y22.ToString();
                xyr6.P2_R.Text = Project.Instance.飞拍基准位.R22.ToString();



                xyr6.P3_X.Text = Project.Instance.飞拍基准位.X23.ToString();
                xyr6.P3_Y.Text = Project.Instance.飞拍基准位.Y23.ToString();
                xyr6.P3_R.Text = Project.Instance.飞拍基准位.R23.ToString();



                xyr6.P4_X.Text = Project.Instance.飞拍基准位.X24.ToString();
                xyr6.P4_Y.Text = Project.Instance.飞拍基准位.Y24.ToString();
                xyr6.P4_R.Text = Project.Instance.飞拍基准位.R24.ToString();




                xyr6.P5_X.Text = Project.Instance.飞拍基准位.X25.ToString();
                xyr6.P5_Y.Text = Project.Instance.飞拍基准位.Y25.ToString();
                xyr6.P5_R.Text = Project.Instance.飞拍基准位.R25.ToString();



                xyr6.P6_X.Text = Project.Instance.飞拍基准位.X26.ToString();
                xyr6.P6_Y.Text = Project.Instance.飞拍基准位.Y26.ToString();
                xyr6.P6_R.Text = Project.Instance.飞拍基准位.R26.ToString();
                #endregion
                #endregion
                // 切换所有输入控件的可编辑状态
                SetControlsEditable(false);

            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        /// <summary>
        /// 记录参数修改日志
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        private void LogParameterChange(string parameterName, string oldValue, string newValue)
        {
            if (oldValue != newValue)
            {
                var currentUser = PermissionSystem.Instance.CurrentUser;
                string userName = currentUser != null ? currentUser.UserName : "未登录";
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 用户 {userName} 修改参数: {parameterName}，旧值: {oldValue}，新值: {newValue}";
                _parameterChangeLogs.Add(logEntry);
            }
        }

        /// <summary>
        /// 保存参数修改日志到文件
        /// </summary>
        private void SaveChangeLogs()
        {
            if (_parameterChangeLogs.Count == 0)
                return;

            try
            {
                string logDirectory = Project.Instance.ParameterPath.DataPath + "\\Config\\Data\\参数修改记录\\";
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string logFileName = $"ParamChangeLog_{DateTime.Now:yyyyMMdd}.csv";
                string logFilePath = Path.Combine(logDirectory, logFileName);

                // 追加日志到文件
                using (StreamWriter sw = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    foreach (string log in _parameterChangeLogs)
                    {
                        sw.WriteLine(log);
                    }
                }

                _parameterChangeLogs.Clear(); // 清空日志列表
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }

        private void uiSymbolButton_保存_Click(object sender, EventArgs e)
        {
            try
            {
                _parameterChangeLogs.Clear(); // 清空之前的日志记录



                // 保存修改日志到文件
                SaveChangeLogs();

                // 更新按钮文本
                uiSymbolButton_编辑.Text = "编辑";

                // 切换所有输入控件的可编辑状态
                SetControlsEditable(false);

                #region 左Epson数据
                Project.Instance.postion左Epson.X1 = Convert.ToDouble(xyr1.P1_X.Text);
                Project.Instance.postion左Epson.Y1 = Convert.ToDouble(xyr1.P1_Y.Text);
                Project.Instance.postion左Epson.R1 = Convert.ToDouble(xyr1.P1_R.Text);

                Project.Instance.postion左Epson.X2 = Convert.ToDouble(xyr1.P2_X.Text);
                Project.Instance.postion左Epson.Y2 = Convert.ToDouble(xyr1.P2_Y.Text);
                Project.Instance.postion左Epson.R2 = Convert.ToDouble(xyr1.P2_R.Text);


                Project.Instance.postion左Epson.X3 = Convert.ToDouble(xyr1.P3_X.Text);
                Project.Instance.postion左Epson.Y3 = Convert.ToDouble(xyr1.P3_Y.Text);
                Project.Instance.postion左Epson.R3 = Convert.ToDouble(xyr1.P3_R.Text);



                Project.Instance.postion左Epson.X4 = Convert.ToDouble(xyr1.P4_X.Text);
                Project.Instance.postion左Epson.Y4 = Convert.ToDouble(xyr1.P4_Y.Text);
                Project.Instance.postion左Epson.R4 = Convert.ToDouble(xyr1.P4_R.Text);


                Project.Instance.postion左Epson.X5 = Convert.ToDouble(xyr1.P5_X.Text);
                Project.Instance.postion左Epson.Y5 = Convert.ToDouble(xyr1.P5_Y.Text);
                Project.Instance.postion左Epson.R5 = Convert.ToDouble(xyr1.P5_R.Text);


                Project.Instance.postion左Epson.X6 = Convert.ToDouble(xyr1.P6_X.Text);
                Project.Instance.postion左Epson.Y6 = Convert.ToDouble(xyr1.P6_Y.Text);
                Project.Instance.postion左Epson.R6 = Convert.ToDouble(xyr1.P6_R.Text);


                Project.Instance.postion左Epson.X11 = Convert.ToDouble(rotateXY1.P1_X.Text);
                Project.Instance.postion左Epson.Y11 = Convert.ToDouble(rotateXY1.P1_Y.Text);


                Project.Instance.postion左Epson.X12 = Convert.ToDouble(rotateXY1.P2_X.Text);
                Project.Instance.postion左Epson.Y12 = Convert.ToDouble(rotateXY1.P2_Y.Text);


                Project.Instance.postion左Epson.X13 = Convert.ToDouble(rotateXY1.P3_X.Text);
                Project.Instance.postion左Epson.Y13 = Convert.ToDouble(rotateXY1.P3_Y.Text);


                Project.Instance.postion左Epson.X14 = Convert.ToDouble(rotateXY1.P4_X.Text);
                Project.Instance.postion左Epson.Y14 = Convert.ToDouble(rotateXY1.P4_Y.Text);


                Project.Instance.postion左Epson.X15 = Convert.ToDouble(rotateXY1.P5_X.Text);
                Project.Instance.postion左Epson.Y15 = Convert.ToDouble(rotateXY1.P5_Y.Text);


                Project.Instance.postion左Epson.X16 = Convert.ToDouble(rotateXY1.P6_X.Text);
                Project.Instance.postion左Epson.Y16 = Convert.ToDouble(rotateXY1.P6_Y.Text);




                Project.Instance.postion左Epson.X21 = Convert.ToDouble(xyr2.P1_X.Text);
                Project.Instance.postion左Epson.Y21 = Convert.ToDouble(xyr2.P1_Y.Text);
                Project.Instance.postion左Epson.R21 = Convert.ToDouble(xyr2.P1_R.Text);



                Project.Instance.postion左Epson.X22 = Convert.ToDouble(xyr2.P2_X.Text);
                Project.Instance.postion左Epson.Y22 = Convert.ToDouble(xyr2.P2_Y.Text);
                Project.Instance.postion左Epson.R22 = Convert.ToDouble(xyr2.P2_R.Text);


                Project.Instance.postion左Epson.X23 = Convert.ToDouble(xyr2.P3_X.Text);
                Project.Instance.postion左Epson.Y23 = Convert.ToDouble(xyr2.P3_Y.Text);
                Project.Instance.postion左Epson.R23 = Convert.ToDouble(xyr2.P3_R.Text);


                Project.Instance.postion左Epson.X24 = Convert.ToDouble(xyr2.P4_X.Text);
                Project.Instance.postion左Epson.Y24 = Convert.ToDouble(xyr2.P4_Y.Text);
                Project.Instance.postion左Epson.R24 = Convert.ToDouble(xyr2.P4_R.Text);



                Project.Instance.postion左Epson.X25 = Convert.ToDouble(xyr2.P5_X.Text);
                Project.Instance.postion左Epson.Y25 = Convert.ToDouble(xyr2.P5_Y.Text);
                Project.Instance.postion左Epson.R25 = Convert.ToDouble(xyr2.P5_R.Text);



                Project.Instance.postion左Epson.X26 = Convert.ToDouble(xyr2.P6_X.Text);
                Project.Instance.postion左Epson.Y26 = Convert.ToDouble(xyr2.P6_Y.Text);
                Project.Instance.postion左Epson.R26 = Convert.ToDouble(xyr2.P6_R.Text);
                #endregion

                #region 右Epson数据
                Project.Instance.postion右Epson.X1 = Convert.ToDouble(xyr3.P1_X.Text);
                Project.Instance.postion右Epson.Y1 = Convert.ToDouble(xyr3.P1_Y.Text);
                Project.Instance.postion右Epson.R1 = Convert.ToDouble(xyr3.P1_R.Text);

                Project.Instance.postion右Epson.X2 = Convert.ToDouble(xyr3.P2_X.Text);
                Project.Instance.postion右Epson.Y2 = Convert.ToDouble(xyr3.P2_Y.Text);
                Project.Instance.postion右Epson.R2 = Convert.ToDouble(xyr3.P2_R.Text);


                Project.Instance.postion右Epson.X3 = Convert.ToDouble(xyr3.P3_X.Text);
                Project.Instance.postion右Epson.Y3 = Convert.ToDouble(xyr3.P3_Y.Text);
                Project.Instance.postion右Epson.R3 = Convert.ToDouble(xyr3.P3_R.Text);



                Project.Instance.postion右Epson.X4 = Convert.ToDouble(xyr3.P4_X.Text);
                Project.Instance.postion右Epson.Y4 = Convert.ToDouble(xyr3.P4_Y.Text);
                Project.Instance.postion右Epson.R4 = Convert.ToDouble(xyr3.P4_R.Text);


                Project.Instance.postion右Epson.X5 = Convert.ToDouble(xyr3.P5_X.Text);
                Project.Instance.postion右Epson.Y5 = Convert.ToDouble(xyr3.P5_Y.Text);
                Project.Instance.postion右Epson.R5 = Convert.ToDouble(xyr3.P5_R.Text);


                Project.Instance.postion右Epson.X6 = Convert.ToDouble(xyr3.P6_X.Text);
                Project.Instance.postion右Epson.Y6 = Convert.ToDouble(xyr3.P6_Y.Text);
                Project.Instance.postion右Epson.R6 = Convert.ToDouble(xyr3.P6_R.Text);


                Project.Instance.postion右Epson.X11 = Convert.ToDouble(rotateXY2.P1_X.Text);
                Project.Instance.postion右Epson.Y11 = Convert.ToDouble(rotateXY2.P1_Y.Text);


                Project.Instance.postion右Epson.X12 = Convert.ToDouble(rotateXY2.P2_X.Text);
                Project.Instance.postion右Epson.Y12 = Convert.ToDouble(rotateXY2.P2_Y.Text);


                Project.Instance.postion右Epson.X13 = Convert.ToDouble(rotateXY2.P3_X.Text);
                Project.Instance.postion右Epson.Y13 = Convert.ToDouble(rotateXY2.P3_Y.Text);


                Project.Instance.postion右Epson.X14 = Convert.ToDouble(rotateXY2.P4_X.Text);
                Project.Instance.postion右Epson.Y14 = Convert.ToDouble(rotateXY2.P4_Y.Text);


                Project.Instance.postion右Epson.X15 = Convert.ToDouble(rotateXY2.P5_X.Text);
                Project.Instance.postion右Epson.Y15 = Convert.ToDouble(rotateXY2.P5_Y.Text);


                Project.Instance.postion右Epson.X16 = Convert.ToDouble(rotateXY2.P6_X.Text);
                Project.Instance.postion右Epson.Y16 = Convert.ToDouble(rotateXY2.P6_Y.Text);




                Project.Instance.postion右Epson.X21 = Convert.ToDouble(xyr4.P1_X.Text);
                Project.Instance.postion右Epson.Y21 = Convert.ToDouble(xyr4.P1_Y.Text);
                Project.Instance.postion右Epson.R21 = Convert.ToDouble(xyr4.P1_R.Text);



                Project.Instance.postion右Epson.X22 = Convert.ToDouble(xyr4.P2_X.Text);
                Project.Instance.postion右Epson.Y22 = Convert.ToDouble(xyr4.P2_Y.Text);
                Project.Instance.postion右Epson.R22 = Convert.ToDouble(xyr4.P2_R.Text);


                Project.Instance.postion右Epson.X23 = Convert.ToDouble(xyr4.P3_X.Text);
                Project.Instance.postion右Epson.Y23 = Convert.ToDouble(xyr4.P3_Y.Text);
                Project.Instance.postion右Epson.R23 = Convert.ToDouble(xyr4.P3_R.Text);


                Project.Instance.postion右Epson.X24 = Convert.ToDouble(xyr4.P4_X.Text);
                Project.Instance.postion右Epson.Y24 = Convert.ToDouble(xyr4.P4_Y.Text);
                Project.Instance.postion右Epson.R24 = Convert.ToDouble(xyr4.P4_R.Text);



                Project.Instance.postion右Epson.X25 = Convert.ToDouble(xyr4.P5_X.Text);
                Project.Instance.postion右Epson.Y25 = Convert.ToDouble(xyr4.P5_Y.Text);
                Project.Instance.postion右Epson.R25 = Convert.ToDouble(xyr4.P5_R.Text);



                Project.Instance.postion右Epson.X26 = Convert.ToDouble(xyr4.P6_X.Text);
                Project.Instance.postion右Epson.Y26 = Convert.ToDouble(xyr4.P6_Y.Text);
                Project.Instance.postion右Epson.R26 = Convert.ToDouble(xyr4.P6_R.Text);
                #endregion

                #region 飞拍
                Project.Instance.飞拍基准位.X1 = Convert.ToDouble(xyr5.P1_X.Text);
                Project.Instance.飞拍基准位.Y1 = Convert.ToDouble(xyr5.P1_Y.Text);
                Project.Instance.飞拍基准位.R1 = Convert.ToDouble(xyr5.P1_R.Text);

                Project.Instance.飞拍基准位.X2 = Convert.ToDouble(xyr5.P2_X.Text);
                Project.Instance.飞拍基准位.Y2 = Convert.ToDouble(xyr5.P2_Y.Text);
                Project.Instance.飞拍基准位.R2 = Convert.ToDouble(xyr5.P2_R.Text);


                Project.Instance.飞拍基准位.X3 = Convert.ToDouble(xyr5.P3_X.Text);
                Project.Instance.飞拍基准位.Y3 = Convert.ToDouble(xyr5.P3_Y.Text);
                Project.Instance.飞拍基准位.R3 = Convert.ToDouble(xyr5.P3_R.Text);



                Project.Instance.飞拍基准位.X4 = Convert.ToDouble(xyr5.P4_X.Text);
                Project.Instance.飞拍基准位.Y4 = Convert.ToDouble(xyr5.P4_Y.Text);
                Project.Instance.飞拍基准位.R4 = Convert.ToDouble(xyr5.P4_R.Text);


                Project.Instance.飞拍基准位.X5 = Convert.ToDouble(xyr5.P5_X.Text);
                Project.Instance.飞拍基准位.Y5 = Convert.ToDouble(xyr5.P5_Y.Text);
                Project.Instance.飞拍基准位.R5 = Convert.ToDouble(xyr5.P5_R.Text);


                Project.Instance.飞拍基准位.X6 = Convert.ToDouble(xyr5.P6_X.Text);
                Project.Instance.飞拍基准位.Y6 = Convert.ToDouble(xyr5.P6_Y.Text);
                Project.Instance.飞拍基准位.R6 = Convert.ToDouble(xyr5.P6_R.Text);







                Project.Instance.飞拍基准位.X21 = Convert.ToDouble(xyr6.P1_X.Text);
                Project.Instance.飞拍基准位.Y21 = Convert.ToDouble(xyr6.P1_Y.Text);
                Project.Instance.飞拍基准位.R21 = Convert.ToDouble(xyr6.P1_R.Text);



                Project.Instance.飞拍基准位.X22 = Convert.ToDouble(xyr6.P2_X.Text);
                Project.Instance.飞拍基准位.Y22 = Convert.ToDouble(xyr6.P2_Y.Text);
                Project.Instance.飞拍基准位.R22 = Convert.ToDouble(xyr6.P2_R.Text);


                Project.Instance.飞拍基准位.X23 = Convert.ToDouble(xyr6.P3_X.Text);
                Project.Instance.飞拍基准位.Y23 = Convert.ToDouble(xyr6.P3_Y.Text);
                Project.Instance.飞拍基准位.R23 = Convert.ToDouble(xyr6.P3_R.Text);


                Project.Instance.飞拍基准位.X24 = Convert.ToDouble(xyr6.P4_X.Text);
                Project.Instance.飞拍基准位.Y24 = Convert.ToDouble(xyr6.P4_Y.Text);
                Project.Instance.飞拍基准位.R24 = Convert.ToDouble(xyr6.P4_R.Text);



                Project.Instance.飞拍基准位.X25 = Convert.ToDouble(xyr6.P5_X.Text);
                Project.Instance.飞拍基准位.Y25 = Convert.ToDouble(xyr6.P5_Y.Text);
                Project.Instance.飞拍基准位.R25 = Convert.ToDouble(xyr6.P5_R.Text);



                Project.Instance.飞拍基准位.X26 = Convert.ToDouble(xyr6.P6_X.Text);
                Project.Instance.飞拍基准位.Y26 = Convert.ToDouble(xyr6.P6_Y.Text);
                Project.Instance.飞拍基准位.R26 = Convert.ToDouble(xyr6.P6_R.Text);
                #endregion

                // Project.Instance.SaveSystemConfig(Project.Instance.weldParm, ParameterPath.StationPostion);
                Project.Instance.SaveSystemConfig(Project.Instance.postion左Epson, ParameterPath.StationPostion左Epson);

                Project.Instance.SaveSystemConfig(Project.Instance.postion右Epson, ParameterPath.StationPostion右Epson);


                Project.Instance.SaveSystemConfig(Project.Instance.飞拍基准位, ParameterPath.飞拍Postion);
                // 提示保存成功
                UIMessageBox.Show("参数保存成功，修改记录已保存", "提示", (UIStyle)MessageBoxButtons.OK, (UIMessageBoxButtons)MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                UIMessageBox.Show($"参数保存失败: {ex.Message}", "错误", (UIStyle)MessageBoxButtons.OK, (UIMessageBoxButtons)MessageBoxIcon.Error);
            }
        }




        private bool _isEditing = false; // 标记当前是否处于编辑状态

        private void uiSymbolButton_编辑_Click(object sender, EventArgs e)
        {
            try
            {
                // 切换编辑状态
                _isEditing = !_isEditing;

                // 更新按钮文本
                uiSymbolButton_编辑.Text = _isEditing ? "取消编辑" : "编辑";

                // 切换所有输入控件的可编辑状态
                SetControlsEditable(_isEditing);
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
                UIMessageBox.Show($"操作失败: {ex.Message}", "错误", (UIStyle)MessageBoxButtons.OK, (UIMessageBoxButtons)MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 设置所有输入控件的可编辑状态
        /// </summary>
        /// <param name="editable">是否可编辑</param>
        private void SetControlsEditable(bool editable)
        {


            //#region 其他参数控件权限
            //// 复选框可编辑（根据编辑状态切换）
            //checkBox_结果强制NG.Enabled = editable;
            //checkBox_屏蔽上传.Enabled = editable;
            //checkBox_组装标准位刷新.Enabled = editable;
            //checkBox_自动点检开启.Enabled = editable;

            //// 点检设定时间可编辑
            //dateTimePicker_点检设定时间.Enabled = editable;

            //// 点检间隔时间可编辑
            //nUD_点检间隔时间.Enabled = editable;

            //// 上次点检时间（label仅显示，始终不可编辑）
            //label_上次点检时间.Enabled = false;
            //#endregion

            //// 保存按钮状态跟随编辑状态
            //uiSymbolButton_保存.Enabled = editable;
        }



        private void uiButton4_Click(object sender, EventArgs e)
        {
            if (!登录焊接机(_httpClient1, "https://192.168.10.105/Services/SystemLogin"))
            {
                Thread.Sleep(100);
                if (!登出焊接机(_httpClient1, "https://192.168.10.105/Services/SystemLogout"))
                {
                    MessageBox.Show("登陆失败!!!");
                    return;
                }
                Thread.Sleep(100);
                if (!登录焊接机(_httpClient1, "https://192.168.10.105/Services/SystemLogin"))
                {
                    MessageBox.Show("登陆失败!!!");
                    return;
                }
                else
                {
                    MessageBox.Show("登陆成功!!!");

                }
            }
            else
            {
                MessageBox.Show("登陆成功!!!");
            }
        }



        public bool 登录焊接机(HttpClient httpClient, string uri)
        {
            Login login = new Login("ADMIN", "123456Aa#", "1234567890QWERTYUIOPASDFGHJKLZXC");
            string jsonData = JsonConvert.SerializeObject(login);
            ServicePointManager.ServerCertificateValidationCallback += (sender1, certificate, chain, sslPolicyErrors) => true;
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(uri, content).Result;
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().Result;
            var loginResponse = JsonConvert.DeserializeObject<LoginBackup>(responseString);
            if (loginResponse.StatusCode != 0)
            {
                return false;
            }
            return true;
        }


        public bool 登出焊接机(HttpClient httpClient, string uri)
        {
            Post post = new Post(12345);
            string json = JsonConvert.SerializeObject(post);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(uri, content).Result;
            response.EnsureSuccessStatusCode();
            var responseString = response.Content.ReadAsStringAsync().Result;
            var loginResponse = JsonConvert.DeserializeObject<LogoutBackup>(responseString);
            if (loginResponse.StatusCode != 0)
            {
                return false;
            }
            return true;
        }

        private void uiButton3_Click(object sender, EventArgs e)
        {
            if (!登出焊接机(_httpClient1, "https://192.168.10.105/Services/SystemLogout"))
            {
                Thread.Sleep(100);
                if (!登录焊接机(_httpClient1, "https://192.168.10.105/Services/SystemLogin"))
                {
                    MessageBox.Show("登出失败!!!");
                    return;
                }
                Thread.Sleep(100);
                if (!登出焊接机(_httpClient1, "https://192.168.10.105/Services/SystemLogout"))
                {
                    MessageBox.Show("登出失败!!!");
                    return;
                }
                else
                {
                    MessageBox.Show("登出成功!!!");

                }
            }
            else
            {
                MessageBox.Show("登出成功!!!");
            }
        }

        private void uiButton6_Click(object sender, EventArgs e)
        {
            if (!登录焊接机(_httpClient2, "https://192.168.10.106/Services/SystemLogin"))
            {
                Thread.Sleep(100);
                if (!登出焊接机(_httpClient2, "https://192.168.10.106/Services/SystemLogout"))
                {
                    MessageBox.Show("登陆失败!!!");
                    return;
                }
                Thread.Sleep(100);
                if (!登录焊接机(_httpClient2, "https://192.168.10.106/Services/SystemLogin"))
                {
                    MessageBox.Show("登陆失败!!!");
                    return;
                }
                else
                {
                    MessageBox.Show("登陆成功!!!");

                }
            }
            else
            {
                MessageBox.Show("登陆成功!!!");
            }
        }

        private void uiButton5_Click(object sender, EventArgs e)
        {
            if (!登出焊接机(_httpClient2, "https://192.168.10.106/Services/SystemLogout"))
            {
                Thread.Sleep(1000);
                if (!登录焊接机(_httpClient2, "https://192.168.10.106/Services/SystemLogin"))
                {
                    MessageBox.Show("登出失败!!!");
                    return;
                }
                Thread.Sleep(1000);
                if (!登出焊接机(_httpClient2, "https://192.168.10.106/Services/SystemLogout"))
                {
                    MessageBox.Show("登出失败!!!");
                    return;
                }
                else
                {
                    MessageBox.Show("登出成功!!!");

                }
            }
            else
            {
                MessageBox.Show("登出成功!!!");
            }
        }

        private void uiButton8_Click(object sender, EventArgs e)
        {
            Project.Instance.GlobalVariables.手动流程选择 = 1;
            Project.Instance.GlobalVariables.手动流程选择等待 = 1;
        }

        private void uiButton9_Click(object sender, EventArgs e)
        {
            Project.Instance.GlobalVariables.手动流程选择 = 2;
            Project.Instance.GlobalVariables.手动流程选择等待 = 1;
        }
    }
}