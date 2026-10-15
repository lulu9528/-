﻿using Jabil.Lib;
using Jabil.Module.Log.Frm;
using Jabil.Module.Log.Lib;
using Jabil.Module.Motion.Common.Dtos;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jabil.Module.Motion.Frm
{
    public partial class Frm_MotionManual : UIPage
    {
        public Frm_MotionManual()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }
        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static Frm_MotionManual _instance;
        public static Frm_MotionManual Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                    _instance = new Frm_MotionManual();
                return _instance;
            }
        }


        #region  Fields
        MotionAxisDto AxisParam;
        public double _选定轴_步径值 = 1;
        public double _选定轴_速度值 = 5;
        private bool _homed = false;
        #endregion
        #region  Methods
        #endregion
        #region  Control Event Handler
        private  void Frm_MotionManual_Load(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    Instance.Enabled = false;
                    #region Frm_MotionManual_Load
                    this.uiImageButton_X轴_正向移动.Click += new System.EventHandler(this.uiImageButton_轴_正负向移动_Click);
                    this.uiImageButton_X轴_负向移动.Click += new System.EventHandler(this.uiImageButton_轴_正负向移动_Click);
                    this.uiImageButton_Y轴_正向移动.Click += new System.EventHandler(this.uiImageButton_轴_正负向移动_Click);
                    this.uiImageButton_Y轴_负向移动.Click += new System.EventHandler(this.uiImageButton_轴_正负向移动_Click);
                    this.uiImageButton_Z轴_正向移动.Click += new System.EventHandler(this.uiImageButton_轴_正负向移动_Click);
                    this.uiImageButton_Z轴_负向移动.Click += new System.EventHandler(this.uiImageButton_轴_正负向移动_Click);
                    uiListBox_选中运动轴.Items.Clear();
                    if (MotionSystem.Instance.ListMotionAxis != null)
                    {
                        //刷入点位定义表
                        if (MotionSystem.Instance.ListMotionAxis.Count > 0)
                        {
                            foreach (var item in MotionSystem.Instance.ListMotionAxis)
                            {
                                if (!uiListBox_选中运动轴.Items.Contains(item.轴名称))
                                {
                                    uiListBox_选中运动轴.Items.Add(item.轴名称);
                                }
                            }
                        }
                    }
                    uiListBox_选中运动轴.Items.Remove("未绑定");
                    #endregion
                    Instance.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                Instance.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiDoubleUpDown_步径_ValueChanged(object sender, double value)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiDoubleUpDown_步径.Enabled = false;
                    _选定轴_步径值 = (double)uiDoubleUpDown_步径.Value;
                    uiDoubleUpDown_步径.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiDoubleUpDown_步径.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiDoubleUpDown_速度_ValueChanged(object sender, double value)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiDoubleUpDown_速度.Enabled = false;
                    _选定轴_速度值 = (double)uiDoubleUpDown_速度.Value;
                    uiDoubleUpDown_速度.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiDoubleUpDown_速度.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiListBox_选中运动轴_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiListBox_选中运动轴.Enabled = false;
                    if (this.uiListBox_选中运动轴.SelectedItem != null)
                    {
                        if (MotionSystem.Instance.ListMotionAxis != null)
                        {
                            if (MotionSystem.Instance.ListMotionAxis.Count > 0)
                            {
                                foreach (var item in MotionSystem.Instance.ListMotionAxis)
                                {
                                    if (item.轴名称 == this.uiListBox_选中运动轴.SelectedItem.ToString())
                                    {
                                        AxisParam = item;
                                        switch (AxisParam.轴类型)
                                        {
                                            case "X轴":
                                                this.uiImageButton_X轴_正向移动.Visible = true;
                                                this.uiImageButton_X轴_负向移动.Visible = true;
                                                this.uiImageButton_X轴_正向移动.Enabled = true;
                                                this.uiImageButton_X轴_负向移动.Enabled = true;


                                                this.uiImageButton_Y轴_正向移动.Visible = false;
                                                this.uiImageButton_Y轴_负向移动.Visible = false;
                                                this.uiImageButton_Z轴_正向移动.Visible = false;
                                                this.uiImageButton_Z轴_负向移动.Visible = false;

                                                break;
                                            case "Y轴":
                                                this.uiImageButton_X轴_正向移动.Visible = false;
                                                this.uiImageButton_X轴_负向移动.Visible = false;
                                                this.uiImageButton_Y轴_正向移动.Visible = true;
                                                this.uiImageButton_Y轴_负向移动.Visible = true;
                                                this.uiImageButton_Y轴_正向移动.Enabled = true;
                                                this.uiImageButton_Y轴_负向移动.Enabled = true;


                                                this.uiImageButton_Z轴_正向移动.Visible = false;
                                                this.uiImageButton_Z轴_负向移动.Visible = false;

                                                break;
                                            case "Z轴":
                                                this.uiImageButton_X轴_正向移动.Visible = false;
                                                this.uiImageButton_X轴_负向移动.Visible = false;
                                                this.uiImageButton_Y轴_正向移动.Visible = false;
                                                this.uiImageButton_Y轴_负向移动.Visible = false;
                                                this.uiImageButton_Z轴_正向移动.Visible = true;
                                                this.uiImageButton_Z轴_负向移动.Visible = true;

                                                this.uiImageButton_Z轴_正向移动.Enabled = true;
                                                this.uiImageButton_Z轴_负向移动.Enabled = true;

                                                break;

                                        }

                                        if (MotionSystem.Instance.Ltdmc_Motion.ServoRead(AxisParam) == 0)
                                        {
                                            uiButton_使能.Text = "使能：ON";
                                            uiButton_使能.FillColor = Color.FromArgb(80, 160, 255);
                                        }
                                        else
                                        {
                                            uiButton_使能.Text = "使能：OFF";
                                            uiButton_使能.FillColor = Color.Red;
                                        }


                                        break;
                                    }
                                }
                            }
                        }
                    }
                    uiListBox_选中运动轴.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiListBox_选中运动轴.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiListBox_快速步径_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiListBox_快速步径.Enabled = false;
                    if (uiListBox_快速步径.SelectedItem != null)
                    {
                        switch (uiListBox_快速步径.SelectedItem)
                        {
                            case "0.01":
                                _选定轴_步径值 = 0.01;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "0.02":
                                _选定轴_步径值 = 0.02;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "0.05":
                                _选定轴_步径值 = 0.05;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "0.1":
                                _选定轴_步径值 = 0.1;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "0.2":
                                _选定轴_步径值 = 0.2;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "0.5":
                                _选定轴_步径值 = 0.5;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "1":
                                _选定轴_步径值 = 1;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "2":
                                _选定轴_步径值 = 2;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "5":
                                _选定轴_步径值 = 5;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "10":
                                _选定轴_步径值 = 10;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;
                            case "20":
                                _选定轴_步径值 = 20;
                                uiDoubleUpDown_步径.Value = _选定轴_步径值;

                                break;

                        }

                    }
                    uiListBox_快速步径.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiListBox_快速步径.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiRadioButton_低速_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiRadioButton_低速.Enabled = false;
                    if (uiRadioButton_低速.Checked == true)
                    {
                        uiRadioButton_中速.Checked = false;
                        uiRadioButton_高速.Checked = false;
                        uiRadioButton_速度自定义.Checked = false;
                        Project.Instance._工位_Info._运行速度比例 = 10;
                        uiTrackBar_速度比例.Value = (int)(Project.Instance._工位_Info._运行速度比例);
                        uiLabel_速度比例显示.Text = "速度比例：+" + (Project.Instance._工位_Info._运行速度比例).ToString() + "%";
                    }
                    uiRadioButton_低速.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiRadioButton_低速.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiRadioButton_中速_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiRadioButton_中速.Enabled = false;
                    if (uiRadioButton_中速.Checked == true)
                    {
                        uiRadioButton_低速.Checked = false;
                        uiRadioButton_高速.Checked = false;
                        uiRadioButton_速度自定义.Checked = false;

                        Project.Instance._工位_Info._运行速度比例 = 50;
                        uiTrackBar_速度比例.Value = (int)(Project.Instance._工位_Info._运行速度比例);
                        uiLabel_速度比例显示.Text = "速度比例：+" + (Project.Instance._工位_Info._运行速度比例).ToString() + "%";
                    }
                    uiRadioButton_中速.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiRadioButton_中速.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiRadioButton_高速_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiRadioButton_高速.Enabled = false;
                    if (uiRadioButton_高速.Checked == true)
                    {
                        uiRadioButton_低速.Checked = false;
                        uiRadioButton_中速.Checked = false;
                        uiRadioButton_速度自定义.Checked = false;

                        Project.Instance._工位_Info._运行速度比例 = 100;
                        uiTrackBar_速度比例.Value = (int)(Project.Instance._工位_Info._运行速度比例);
                        uiLabel_速度比例显示.Text = "速度比例：+" + (Project.Instance._工位_Info._运行速度比例).ToString() + "%";
                    }
                    uiRadioButton_高速.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiRadioButton_高速.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiRadioButton_速度自定义_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiRadioButton_速度自定义.Enabled = false;
                    if (uiRadioButton_速度自定义.Checked == true)
                    {
                        uiRadioButton_低速.Checked = false;
                        uiRadioButton_中速.Checked = false;
                        uiRadioButton_高速.Checked = false;
                    }
                    uiRadioButton_速度自定义.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiRadioButton_速度自定义.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiTrackBar_速度比例_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiTrackBar_速度比例.Enabled = false;
                    #region uiTrackBar_速度比例_ValueChanged
                    Project.Instance._工位_Info._运行速度比例 = uiTrackBar_速度比例.Value;
                    uiLabel_速度比例显示.Text = "速度比例：+" + (Project.Instance._工位_Info._运行速度比例).ToString() + "%";
                    uiRadioButton_低速.Checked = false;
                    uiRadioButton_中速.Checked = false;
                    uiRadioButton_高速.Checked = false;
                    uiRadioButton_速度自定义.Checked = true;
                    if (Frm_SpeedBox.Instance.InvokeRequired)
                    {
                        Frm_SpeedBox.Instance.Invoke(new Action(() =>
                        {
                            if (Frm_SpeedBox.Instance.uiTrackBar_速度比例.Created)
                            {
                                Frm_SpeedBox.Instance.uiTrackBar_速度比例.Value = Project.Instance._工位_Info._运行速度比例;
                                Frm_SpeedBox.Instance.lab_速度比例显示.Text = (Project.Instance._工位_Info._运行速度比例).ToString() + "%";

                            }
                        }));
                    }
                    else
                    {
                        if (Frm_SpeedBox.Instance.uiTrackBar_速度比例.Created)
                        {
                            Frm_SpeedBox.Instance.uiTrackBar_速度比例.Value = Project.Instance._工位_Info._运行速度比例;
                            Frm_SpeedBox.Instance.lab_速度比例显示.Text = (Project.Instance._工位_Info._运行速度比例).ToString() + "%";

                        }
                    }

                    #endregion
                    uiTrackBar_速度比例.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiTrackBar_速度比例.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiImageButton_轴_正负向移动_Click(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    if (sender is UIImageButton)
                    {
                        UIImageButton item = (UIImageButton)sender;
                        if (Project.Instance._工位_Info._OperateType != OperateType.手动)
                        {
                            RLog.SaveLog(LogType.ControlOperation, LogLevel.Warn, Color.Yellow, "工位未处于调试模式，不允许手动执行操作！！！");
                            return;
                        }
                        if (AxisParam != null)
                        {
                            AxisParam.运行速度 = _选定轴_速度值;

                            if (item.Text.Contains("+"))
                            {
                                MotionSystem.Instance.Ltdmc_Motion.RelMove(AxisParam, _选定轴_步径值);
                            }
                            else if (item.Text.Contains("-"))
                            {
                                MotionSystem.Instance.Ltdmc_Motion.RelMove(AxisParam, -_选定轴_步径值);
                            }

                        }
                    }
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }

        }
        private  void uiButton_急停_Click(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiButton_急停.Enabled = false;
                    Project.Instance.globalVariables.机台手动急停 = true;
                    if (AxisParam != null)
                    {
                        MotionSystem.Instance.Ltdmc_Motion.SmtStop(AxisParam, 0);
                    }
                    uiButton_急停.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiButton_急停.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiButton_使能_Click(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiButton_使能.Enabled = false;
                    if (AxisParam == null)
                    {
                        uiButton_使能.Enabled = true;
                        return;
                    }

                    if (uiButton_使能.Text == "使能：OFF")
                    {
                        uiButton_使能.Text = "使能：ON";
                        uiButton_使能.FillColor = Color.FromArgb(80, 160, 255);
                        MotionSystem.Instance.Ltdmc_Motion.ServoSet(AxisParam, 0);
                    }
                    else if (uiButton_使能.Text == "使能：ON")
                    {
                        uiButton_使能.Text = "使能：OFF";
                        uiButton_使能.FillColor = Color.Red;
                        MotionSystem.Instance.Ltdmc_Motion.ServoSet(AxisParam, 1);
                    }
                    uiButton_使能.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiButton_使能.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private  void uiButton_回原点_Click(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    uiButton_回原点.Enabled = false;
                    if (AxisParam == null)
                    {
                        uiButton_回原点.Enabled = true;
                        return;
                    }
                    Frm_ConfirmBox.Instance.lbl_info.Text = ("请确认是否手动执行<" + AxisParam.轴名称 + ">回原点！！！");
                    Frm_ConfirmBox.Instance.Visible = false;
                    Frm_ConfirmBox.Instance.TopMost = true;
                    Frm_ConfirmBox.Instance.ShowDialog();
                    if (Frm_ConfirmBox.Instance.Result == ConfirmBoxResult.Cancel || Frm_ConfirmBox.Instance.Result == ConfirmBoxResult.No)
                    {
                        uiButton_回原点.Enabled = true;
                        return;
                    }

                    if (timer_Home.Enabled)
                    {
                        timer_Home.Enabled = false;
                        MotionSystem.Instance.Ltdmc_Motion.SmtStop(AxisParam, 0);
                        Frm_MessageBox.Instance.MessageBoxShow("\r\n手动回原停止");
                    }
                    else
                    {
                        timer_Home.Enabled = true;
                        _homed = false;
                    }
                    uiButton_回原点.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                uiButton_回原点.Enabled = true;
                RLog.SaveError(ex);
            }
        }
        private void timer_Home_Tick(object sender, EventArgs e)
        {
            try
            {
                WFUtil.updateMainThreadUI(this, () =>
                {
                    if (AxisParam == null)
                    {
                        return;
                    }
                    bool[] myBitArray_MIO = new bool[32];
                    myBitArray_MIO = MotionSystem.Instance.Ltdmc_Motion.GetMotionSts(AxisParam);
                    if (!_homed)
                    {
                        _homed = true;
                        MotionSystem.Instance.Ltdmc_Motion.GoHome(AxisParam);
                    }
                    if (_homed & MotionSystem.Instance.Ltdmc_Motion.Check_Move_Done(AxisParam) & myBitArray_MIO[4])
                    {
                        System.Threading.Thread.Sleep(500);
                        timer_Home.Enabled = false;
                        _homed = false;
                        MotionSystem.Instance.Ltdmc_Motion.SetPos(0, AxisParam);
                        Frm_MessageBox.Instance.MessageBoxShow("\r\n手动回原完成");
                    }
                });
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }

        }
        #endregion
    }
}
