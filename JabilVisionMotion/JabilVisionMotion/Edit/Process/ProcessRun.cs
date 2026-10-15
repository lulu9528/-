using Jabil.Lib;
using Jabil.Lib.SQLHelper;
using Jabil.Module.Log.Lib;
using Jabil.Module.Permission;
using Jabil.Module.ProductData.Frm;
using Jabil.Module.ProductData.Lib;
using JabilVisionMotion.Frm;
using JabilVisionMotion.Frm.Dialog;
using JabilVisionMotion.Frm.UserPage;
using JabilVisionMotion.Lib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JabilVisionMotion.Edit.Process
{
    /// <summary>
    /// 副流程运行流程处理类，继承自ProcessBase基类
    /// 支持的流程类型：
    /// 设计特点：通过通用方法封装副流程执行逻辑，减少代码冗余；实时监控流程状态并响应外部中断（如急停）
    /// </summary>
    public class ProcessRun : ProcessBase, ICloneable
    {
        #region 私有字段
        private string _commandName;        // 指令名称（固定为"ProcessRun"）
        private string _processType;        // 流程类型（如"镭雕治具供料流程"，决定要执行的副流程）
        private AutoCore_副流程 _subProcess; // 副流程实例（实际执行的子流程对象）
        private readonly string _process;   // 原始指令字符串
        private readonly string _processTip; // 指令提示信息（用于日志）
        private bool _isWaiting = true;     // 等待标志（控制副流程完成等待循环）


        OP70 now70;   //用于存放码值信息
        #endregion

        /// <summary>
        /// 初始化副流程运行流程
        /// </summary>
        /// <param name="process">副流程指令字符串（如"ProcessRun|左Epson取放料流程"）</param>
        /// <param name="processTip">指令提示信息（用于日志记录）</param>
        public ProcessRun(string process, string processTip) : base(process, processTip)
        {
            try
            {
                _process = process ?? throw new ArgumentNullException(nameof(process));
                _processTip = processTip ?? throw new ArgumentNullException(nameof(processTip));

                ParseCommand(process);  // 解析指令参数

                CurrentStatus = GeneralStatus.Wait; // 初始状态：等待
                Message = "Wait";
            }
            catch (Exception ex)
            {
                Message = ex.Message;       // 记录异常信息
                CurrentStatus = GeneralStatus.Error; // 初始化失败：错误状态
                RLog.SaveError(ex);         // 保存错误日志
                // 初始化错误时直接返回
                return;
            }
        }

        /// <summary>
        /// 克隆方法，支持副流程的缓存机制
        /// 确保多实例并发时的状态独立性
        /// </summary>
        public override object Clone()
        {
            var clone = new ProcessRun(_process, _processTip)
            {
                _commandName = this._commandName,
                _processType = this._processType
            };
            // 初始化克隆实例的状态为"等待"
            clone.CurrentStatus = GeneralStatus.Wait;
            clone.Message = "Wait";
            return clone;
        }

        /// <summary>
        /// 解析副流程指令参数，提取流程类型
        /// </summary>
        /// <param name="process">待解析的指令字符串</param>
        private void ParseCommand(string process)
        {
            // 按竖线分割指令，移除空条目
            var commandParts = process.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证指令格式（至少2个部分：指令名称|流程类型）
            if (commandParts.Length < 2)
            {
                SetErrorStatus($"指令格式错误: {process}，至少需要2个部分，格式为指令名称|流程类型");
                return; // 设置错误状态后立即返回
            }

            // 提取指令名称和流程类型
            _commandName = commandParts[0];
            _processType = commandParts[1];
        }

        /// <summary>
        /// 执行副流程运行逻辑（核心方法）
        /// 根据流程类型调用对应的处理方法，协调副流程的启动、监控和状态切换
        /// </summary>
        public override void Run()
        {
            try
            {
                // 测试模式：直接返回成功（不实际执行副流程）
                if (Project.Instance.GlobalVariables.IsTestMode)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = "NoError-- 测试流程";
                    return;
                }

                // 初始化运行状态
                CurrentStatus = GeneralStatus.Running;
                Message = "Running";

                // 根据流程类型执行对应的副流程
                switch (_processType)
                {
                    case "Epson2上相机流程":
                        Execute_EPSON2上相机流程_Process();
                        break;
                    case "Epson2下相机流程":
                        Execute_EPSON2下相机流程_Process();
                        break;
                    case "Epson2请求最终结果流程":
                        Execute_EPSON2请求最终结果流程_Process();
                        break;
                    case "Epson1上相机流程":
                        Execute_EPSON1上相机流程_Process();
                        break;
                    case "Epson1下相机流程":
                        Execute_EPSON1下相机流程_Process();
                        break;
                    case "Epson1请求最终结果流程":
                        Execute_EPSON1请求最终结果流程_Process();
                        break;
                    case "飞拍组装流程":
                        Execute_飞拍组装流程_Process();
                        break;
                    default:
                        // 未知流程类型：设置错误状态
                        SetErrorStatus($"{_processType} 错误，请检查流程配置表！！");
                        return;
                }

                // 所有流程正常完成后，设置成功状态
                if (CurrentStatus != GeneralStatus.Error)
                {
                    CurrentStatus = GeneralStatus.NoError;
                    Message = $"{_processType}--->>NoError--->>执行完成";
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;       // 记录异常信息
                CurrentStatus = GeneralStatus.Error; // 执行失败：错误状态
                RLog.SaveError(ex);         // 保存错误日志
                return; // 异常处理后立即返回
            }
        }
        #region 流程执行方法（按流程类型分类）
        private void Execute_EPSON2上相机流程_Process()
        {
            // 记录流程启动日志，包含当前模式（如左Epson/右Epson）
            LogProcessStart("Epson2上相机流程跳转顺序", Controller.Instance.StationInfo.Get_EPSON2上相机.ToString());

            // 根据当前模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_EPSON2上相机)
            {
                case EPSON2上相机_ProcessMode.判断是否需要拍照:
                    ExecuteSubProcess(ProcessPath.EPSON2判断是否需要拍照_副流程path,
                        () => EPSON2判断是否需要拍照(),
                        () => Controller.Instance.StationInfo.Get_EPSON2上相机 = EPSON2上相机_ProcessMode.未指定);
                    break;
                case EPSON2上相机_ProcessMode.有点位直接发送:
                    ExecuteSubProcess(ProcessPath.EPSON2有点位直接发送_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2上相机 = EPSON2上相机_ProcessMode.判断是否需要拍照,
                        () => Controller.Instance.StationInfo.Get_EPSON2上相机 = EPSON2上相机_ProcessMode.未指定);
                    break;
                case EPSON2上相机_ProcessMode.无点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON2无点位拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2上相机 = EPSON2上相机_ProcessMode.判断是否需要拍照,
                        () => Controller.Instance.StationInfo.Get_EPSON2上相机 = EPSON2上相机_ProcessMode.未指定);
                    break;
                case EPSON2上相机_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_EPSON2上相机}，请检查流程配置表！！");
                    return;
            }

            
        }


        private void EPSON2判断是否需要拍照()
        {
            if (Controller.Instance.StationInfo.上相机是否需要拍照 == 0)
            {

                Controller.Instance.StationInfo.Get_EPSON2上相机 = EPSON2上相机_ProcessMode.有点位直接发送;
            }
            if (Controller.Instance.StationInfo.上相机是否需要拍照 == 1)
            {
                Controller.Instance.StationInfo.Get_EPSON2上相机 = EPSON2上相机_ProcessMode.无点位拍照;
                Controller.Instance.StationInfo.上相机是否需要拍照 = 0;
            }

        }



        private void Execute_EPSON2下相机流程_Process()
        {
            // 记录流程启动日志，包含当前模式（如左Epson/右Epson）
            LogProcessStart("Epson2下相机流程跳转顺序", Controller.Instance.StationInfo.Get_EPSON2下相机.ToString());

            // 根据当前模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_EPSON2下相机)
            {
                case EPSON2下相机_ProcessMode.下相机拍照点位确认:
                    ExecuteSubProcess(ProcessPath.EPSON2下相机拍照点位确认_副流程path,
                        () => EPSON2下相机拍照(),
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.未指定);
                    break;
                case EPSON2下相机_ProcessMode.一点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON2下相机位置1拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.未指定);
                    break;
                case EPSON2下相机_ProcessMode.二点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON2下相机位置2拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.未指定);
                    break;
                case EPSON2下相机_ProcessMode.三点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON2下相机位置3拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.未指定);
                    break;
                case EPSON2下相机_ProcessMode.四点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON2下相机位置4拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.未指定);
                    break;
                case EPSON2下相机_ProcessMode.五点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON2下相机位置5拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.未指定);
                    break;
                case EPSON2下相机_ProcessMode.六点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON2下相机位置6拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.未指定);
                    break;
                case EPSON2下相机_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_EPSON2下相机}，请检查流程配置表！！");
                    return;
            }


        }

        private void EPSON2下相机拍照()
        {
            if (Controller.Instance.StationInfo.Epson2下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson2下相机请求位置号 == 1)
            {
                Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.一点位拍照;
                Controller.Instance.StationInfo.Epson2下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson2下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson2下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson2下相机请求位置号 == 2)
            {
                Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.二点位拍照;
                Controller.Instance.StationInfo.Epson2下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson2下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson2下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson2下相机请求位置号 == 3)
            {
                Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.三点位拍照;
                Controller.Instance.StationInfo.Epson2下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson2下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson2下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson2下相机请求位置号 == 4)
            {
                Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.四点位拍照;
                Controller.Instance.StationInfo.Epson2下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson2下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson2下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson2下相机请求位置号 == 5)
            {
                Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.五点位拍照;
                Controller.Instance.StationInfo.Epson2下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson2下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson2下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson2下相机请求位置号 == 6)
            {
                Controller.Instance.StationInfo.Get_EPSON2下相机 = EPSON2下相机_ProcessMode.六点位拍照;
                Controller.Instance.StationInfo.Epson2下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson2下相机请求位置号 = 0;
            }
            else
            {
                Controller.Instance.StationInfo.Epson2下相机请求拍照 = 0;
                Controller.Instance.StationInfo.Epson2下相机请求位置号 = 0;
            }






        }



        private void Execute_EPSON2请求最终结果流程_Process()
        {
            // 记录流程启动日志，包含当前模式（如左Epson/右Epson）
            LogProcessStart("Epson2请求最终结果流程跳转顺序", Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果.ToString());

            // 根据当前模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果)
            {
                case EPSON2发送请求最终结果_ProcessMode.等待请求最终结果:
                    ExecuteSubProcess(ProcessPath.EPSON2请求发送最终结果_副流程path,
                        () => EPSON2请求最终结果(),
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON2发送请求最终结果_ProcessMode.请求位置1结果:
                    ExecuteSubProcess(ProcessPath.EPSON2请求结果1_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON2发送请求最终结果_ProcessMode.请求位置2结果:
                    ExecuteSubProcess(ProcessPath.EPSON2请求结果2_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON2发送请求最终结果_ProcessMode.请求位置3结果:
                    ExecuteSubProcess(ProcessPath.EPSON2请求结果3_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON2发送请求最终结果_ProcessMode.请求位置4结果:
                    ExecuteSubProcess(ProcessPath.EPSON2请求结果4_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON2发送请求最终结果_ProcessMode.请求位置5结果:
                    ExecuteSubProcess(ProcessPath.EPSON2请求结果5_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON2发送请求最终结果_ProcessMode.请求位置6结果:
                    ExecuteSubProcess(ProcessPath.EPSON2请求结果6_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON2发送请求最终结果_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果}，请检查流程配置表！！");
                    return;
            }


        }


        private void EPSON2请求最终结果()
        {
            if (Controller.Instance.StationInfo.Epson2皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson2请求安装数据号 == 1)
            {
                Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.请求位置1结果;
                Controller.Instance.StationInfo.Epson2皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson2请求安装数据号 = 1;
            }
            else if (Controller.Instance.StationInfo.Epson2皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson2请求安装数据号 == 2)
            {
                Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.请求位置2结果;
                Controller.Instance.StationInfo.Epson2皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson2请求安装数据号 = 2;
            }
            else if (Controller.Instance.StationInfo.Epson2皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson2请求安装数据号 == 3)
            {
                Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.请求位置3结果;
                Controller.Instance.StationInfo.Epson2皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson2请求安装数据号 = 3;
            }
            else if (Controller.Instance.StationInfo.Epson2皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson2请求安装数据号 == 4)
            {
                Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.请求位置4结果;
                Controller.Instance.StationInfo.Epson2皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson2请求安装数据号 = 4;
            }
            else if (Controller.Instance.StationInfo.Epson2皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson2请求安装数据号 == 5)
            {
                Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.请求位置5结果;
                Controller.Instance.StationInfo.Epson2皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson2请求安装数据号 = 5;
            }
            else if (Controller.Instance.StationInfo.Epson2皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson2请求安装数据号 == 6)
            {
                Controller.Instance.StationInfo.Get_EPSON2发送请求最终结果 = EPSON2发送请求最终结果_ProcessMode.请求位置6结果;
                Controller.Instance.StationInfo.Epson2皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson2请求安装数据号 = 6;
            }
            else
            {
                Controller.Instance.StationInfo.Epson2皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson2请求安装数据号 = 0;
            }






        }








        private void Execute_飞拍组装流程_Process()
        {
            // 记录流程启动日志，包含当前模式（如左Epson/右Epson）
            LogProcessStart("飞拍组装流程跳转顺序", Controller.Instance.StationInfo.Get_飞拍组装.ToString());

            // 根据当前模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_飞拍组装)
            {
                case  飞拍组装流程_ProcessMode.飞拍:
                    ExecuteSubProcess(ProcessPath.飞拍流程_副流程path,
                        () => 飞拍结果(),
                        () => Controller.Instance.StationInfo.Get_飞拍组装 = 飞拍组装流程_ProcessMode.未指定);
                    break;
                case 飞拍组装流程_ProcessMode.组装:
                    ExecuteSubProcess(ProcessPath.组装流程_副流程path,
                        () => Controller.Instance.StationInfo.Get_飞拍组装 = 飞拍组装流程_ProcessMode.复检,
                        () => Controller.Instance.StationInfo.Get_飞拍组装 = 飞拍组装流程_ProcessMode.未指定);
                    break;
                case 飞拍组装流程_ProcessMode.复检:
                    ExecuteSubProcess(ProcessPath.复检流程_副流程path,
                        () => Controller.Instance.StationInfo.Get_飞拍组装 = 飞拍组装流程_ProcessMode.飞拍,
                        () => Controller.Instance.StationInfo.Get_飞拍组装 = 飞拍组装流程_ProcessMode.未指定);
                    break;
                case 飞拍组装流程_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_飞拍组装}，请检查流程配置表！！");
                    return;
            }


        }


        private void 飞拍结果()
        {
            if (Controller.Instance.StationInfo.FlyVisionResult == 1)
            {
                Controller.Instance.StationInfo.Get_飞拍组装 = 飞拍组装流程_ProcessMode.组装;
            }
            else
            {
                Controller.Instance.StationInfo.Get_飞拍组装 = 飞拍组装流程_ProcessMode.飞拍;
            }
        }






        private void Execute_EPSON1上相机流程_Process()
        {
            // 记录流程启动日志，包含当前模式（如左Epson/右Epson）
            LogProcessStart("Epson1上相机流程跳转顺序", Controller.Instance.StationInfo.Get_EPSON1上相机.ToString());

            // 根据当前模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_EPSON1上相机)
            {
                case EPSON1上相机_ProcessMode.判断是否需要拍照:
                    ExecuteSubProcess(ProcessPath.EPSON1判断是否需要拍照_副流程path,
                        () => EPSON1判断是否需要拍照(),
                        () => Controller.Instance.StationInfo.Get_EPSON1上相机 = EPSON1上相机_ProcessMode.未指定);
                    break;
                case EPSON1上相机_ProcessMode.有点位直接发送:
                    ExecuteSubProcess(ProcessPath.EPSON1有点位直接发送_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1上相机 = EPSON1上相机_ProcessMode.判断是否需要拍照,
                        () => Controller.Instance.StationInfo.Get_EPSON1上相机 = EPSON1上相机_ProcessMode.未指定);
                    break;
                case EPSON1上相机_ProcessMode.无点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON1无点位拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1上相机 = EPSON1上相机_ProcessMode.判断是否需要拍照,
                        () => Controller.Instance.StationInfo.Get_EPSON1上相机 = EPSON1上相机_ProcessMode.未指定);
                    break;
                case EPSON1上相机_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_EPSON1上相机}，请检查流程配置表！！");
                    return;
            }


        }


        private void EPSON1判断是否需要拍照()
        {
            if (Controller.Instance.StationInfo.上相机是否需要拍照 == 0)
            {

                Controller.Instance.StationInfo.Get_EPSON1上相机 = EPSON1上相机_ProcessMode.有点位直接发送;
            }
            if (Controller.Instance.StationInfo.上相机是否需要拍照 == 1)
            {
                Controller.Instance.StationInfo.Get_EPSON1上相机 = EPSON1上相机_ProcessMode.无点位拍照;
                Controller.Instance.StationInfo.上相机是否需要拍照 = 0;
            }

        }




        private void Execute_EPSON1下相机流程_Process()
        {
            // 记录流程启动日志，包含当前模式（如左Epson/右Epson）
            LogProcessStart("Epson1下相机流程跳转顺序", Controller.Instance.StationInfo.Get_EPSON1下相机.ToString());

            // 根据当前模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_EPSON1下相机)
            {
                case EPSON1下相机_ProcessMode.下相机拍照点位确认:
                    ExecuteSubProcess(ProcessPath.EPSON1下相机拍照点位确认_副流程path,
                        () => EPSON1下相机拍照(),
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.未指定);
                    break;
                case EPSON1下相机_ProcessMode.一点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON1下相机位置1拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.未指定);
                    break;
                case EPSON1下相机_ProcessMode.二点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON1下相机位置2拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.未指定);
                    break;
                case EPSON1下相机_ProcessMode.三点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON1下相机位置3拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.未指定);
                    break;
                case EPSON1下相机_ProcessMode.四点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON1下相机位置4拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.未指定);
                    break;
                case EPSON1下相机_ProcessMode.五点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON1下相机位置5拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.未指定);
                    break;
                case EPSON1下相机_ProcessMode.六点位拍照:
                    ExecuteSubProcess(ProcessPath.EPSON1下相机位置6拍照_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.下相机拍照点位确认,
                        () => Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.未指定);
                    break;
                case EPSON1下相机_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_EPSON1下相机}，请检查流程配置表！！");
                    return;
            }


        }


        private void EPSON1下相机拍照()
        {
            if (Controller.Instance.StationInfo.Epson1下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson1下相机请求位置号 == 1)
            {
                Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.一点位拍照;
                Controller.Instance.StationInfo.Epson1下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson1下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson1下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson1下相机请求位置号 == 2)
            {
                Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.二点位拍照;
                Controller.Instance.StationInfo.Epson1下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson1下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson1下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson1下相机请求位置号 == 3)
            {
                Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.三点位拍照;
                Controller.Instance.StationInfo.Epson1下相机请求拍照 = 0;
               // Controller.Instance.StationInfo.Epson1下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson1下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson1下相机请求位置号 == 4)
            {
                Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.四点位拍照;
                Controller.Instance.StationInfo.Epson1下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson1下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson1下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson1下相机请求位置号 == 5)
            {
                Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.五点位拍照;
                Controller.Instance.StationInfo.Epson1下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson1下相机请求位置号 = 0;
            }
            else if (Controller.Instance.StationInfo.Epson1下相机请求拍照 == 1 && Controller.Instance.StationInfo.Epson1下相机请求位置号 == 6)
            {
                Controller.Instance.StationInfo.Get_EPSON1下相机 = EPSON1下相机_ProcessMode.六点位拍照;
                Controller.Instance.StationInfo.Epson1下相机请求拍照 = 0;
                //Controller.Instance.StationInfo.Epson1下相机请求位置号 = 0;
            }
            else
            {
                Controller.Instance.StationInfo.Epson1下相机请求拍照 = 0;
                Controller.Instance.StationInfo.Epson1下相机请求位置号 = 0;
            }






        }



        private void Execute_EPSON1请求最终结果流程_Process()
        {
            // 记录流程启动日志，包含当前模式（如左Epson/右Epson）
            LogProcessStart("Epson1请求最终结果流程跳转顺序", Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果.ToString());

            // 根据当前模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果)
            {
                case EPSON1发送请求最终结果_ProcessMode.等待请求最终结果:
                    ExecuteSubProcess(ProcessPath.EPSON1请求发送最终结果_副流程path,
                        () => EPSON1请求最终结果(),
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON1发送请求最终结果_ProcessMode.请求位置1结果:
                    ExecuteSubProcess(ProcessPath.EPSON1请求结果1_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON1发送请求最终结果_ProcessMode.请求位置2结果:
                    ExecuteSubProcess(ProcessPath.EPSON1请求结果2_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON1发送请求最终结果_ProcessMode.请求位置3结果:
                    ExecuteSubProcess(ProcessPath.EPSON1请求结果3_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON1发送请求最终结果_ProcessMode.请求位置4结果:
                    ExecuteSubProcess(ProcessPath.EPSON1请求结果4_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON1发送请求最终结果_ProcessMode.请求位置5结果:
                    ExecuteSubProcess(ProcessPath.EPSON1请求结果5_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON1发送请求最终结果_ProcessMode.请求位置6结果:
                    ExecuteSubProcess(ProcessPath.EPSON1请求结果6_副流程path,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.等待请求最终结果,
                        () => Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.未指定);
                    break;
                case EPSON1发送请求最终结果_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果}，请检查流程配置表！！");
                    return;
            }


        }


        private void EPSON1请求最终结果()
        {
            if (Controller.Instance.StationInfo.Epson1皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson1请求安装数据号 == 1)
            {
                Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.请求位置1结果;
                Controller.Instance.StationInfo.Epson1皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson1请求安装数据号 = 1;
            }
            else if (Controller.Instance.StationInfo.Epson1皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson1请求安装数据号 == 2)
            {
                Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.请求位置2结果;
                Controller.Instance.StationInfo.Epson1皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson1请求安装数据号 = 2;
            }
            else if (Controller.Instance.StationInfo.Epson1皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson1请求安装数据号 == 3)
            {
                Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.请求位置3结果;
                Controller.Instance.StationInfo.Epson1皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson1请求安装数据号 = 3;
            }
            else if (Controller.Instance.StationInfo.Epson1皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson1请求安装数据号 == 4)
            {
                Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.请求位置4结果;
                Controller.Instance.StationInfo.Epson1皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson1请求安装数据号 = 4;
            }
            else if (Controller.Instance.StationInfo.Epson1皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson1请求安装数据号 == 5)
            {
                Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.请求位置5结果;
                Controller.Instance.StationInfo.Epson1皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson1请求安装数据号 = 5;
            }
            else if (Controller.Instance.StationInfo.Epson1皮带线请求结果 == 1 && Controller.Instance.StationInfo.Epson1请求安装数据号 == 6)
            {
                Controller.Instance.StationInfo.Get_EPSON1发送请求最终结果 = EPSON1发送请求最终结果_ProcessMode.请求位置6结果;
                Controller.Instance.StationInfo.Epson1皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson1请求安装数据号 = 6;
            }
            else
            {
                Controller.Instance.StationInfo.Epson1皮带线请求结果 = 0;
                Controller.Instance.StationInfo.Epson1请求安装数据号 = 0;
            }






        }


        private void 搬运轴_流程选择()
        {
            switch (Controller.Instance.StationInfo.搬运轴流程选择)
            {
                case 1:
                    Controller.Instance.StationInfo.Get_搬运轴流程 = 搬运轴_ProcessMode.先取前滑台13再取后滑台24;
                    Controller.Instance.StationInfo.搬运轴流程选择 = 0;
                    break;
                case 2:
                    Controller.Instance.StationInfo.Get_搬运轴流程 = 搬运轴_ProcessMode.先取后滑台24再取前滑台13;
                    Controller.Instance.StationInfo.搬运轴流程选择 = 0;

                    break;
                case 3:
                    Controller.Instance.StationInfo.Get_搬运轴流程 = 搬运轴_ProcessMode.屏蔽后滑台;
                    Controller.Instance.StationInfo.搬运轴流程选择 = 0;

                    break;
                case 4:
                    Controller.Instance.StationInfo.Get_搬运轴流程 = 搬运轴_ProcessMode.屏蔽前滑台;
                    Controller.Instance.StationInfo.搬运轴流程选择 = 0;
                    break;

            }
        }

        //正常情况下   前工位取13位置    后工位取24位置   
        //屏蔽单工位时    前后滑台分别需要取13和24    

        private void 后滑台上下料穴位选择()
        {

            if (Controller.Instance.后滑台呼叫放料 == 1)
            {
                Controller.Instance.StationInfo.后滑台穴位选择 = 1;
                Controller.Instance.StationInfo.Get_后滑台流程 = 滑台_ProcessMode._13上料焊接13下料;
            }
            if (Controller.Instance.后滑台呼叫放料 == 2)
            {
                Controller.Instance.StationInfo.后滑台穴位选择 = 1;
                Controller.Instance.StationInfo.Get_后滑台流程 = 滑台_ProcessMode._24上料焊接24下料;
            }
        }



        private void Execute_前覆膜机流程_Process()
        {
            // 记录流程启动日志，包含当前模式（如左Epson/右Epson）
            LogProcessStart("前覆膜机流程跳转顺序", Controller.Instance.StationInfo.Get_前覆膜机.ToString());


            // 根据当前模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_前覆膜机)
            {
                case 覆膜机_ProcessMode.覆膜机工作:
                    ExecuteSubProcess(ProcessPath.前覆膜机_副流程path,
                        () => Controller.Instance.StationInfo.Get_前覆膜机 = 覆膜机_ProcessMode.覆膜机工作,
                        () => Controller.Instance.StationInfo.Get_前覆膜机 = 覆膜机_ProcessMode.未指定);
                    break;
                case 覆膜机_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_前覆膜机}，请检查流程配置表！！");
                    return;
            }
        }

        private void Execute_后覆膜机流程_Process()
        {
            // 记录流程启动日志，包含当后模式（如左Epson/右Epson）
            LogProcessStart("后覆膜机流程跳转顺序", Controller.Instance.StationInfo.Get_后覆膜机.ToString());


            // 根据当后模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_后覆膜机)
            {
                case 覆膜机_ProcessMode.覆膜机工作:
                    ExecuteSubProcess(ProcessPath.后覆膜机_副流程path,
                        () => Controller.Instance.StationInfo.Get_后覆膜机 = 覆膜机_ProcessMode.覆膜机工作,
                        () => Controller.Instance.StationInfo.Get_后覆膜机 = 覆膜机_ProcessMode.未指定);
                    break;
                case 覆膜机_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_后覆膜机}，请检查流程配置表！！");
                    return;
            }
        }

        private void Execute_换治具流程_Process()
        {
            // 记录流程启动日志，包含当后模式（如左Epson/右Epson）
            LogProcessStart("换治具流程跳转顺序", Controller.Instance.StationInfo.Get_换治具.ToString());


            // 根据当后模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_换治具)
            {
                case 搬运轴换治具_ProcessMode.治具选择:
                    ExecuteSubProcess(ProcessPath.治具选择_副流程path,
                        () => 治具选择(),
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定);
                    break;
                case 搬运轴换治具_ProcessMode.取治具1放治具2:
                    ExecuteSubProcess(ProcessPath.取治具1放治具2_副流程path,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定);

                    break;
                case 搬运轴换治具_ProcessMode.取治具1放治具3:
                    ExecuteSubProcess(ProcessPath.取治具1放治具3_副流程path,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定);

                    break;
                case 搬运轴换治具_ProcessMode.取治具2放治具1:
                    ExecuteSubProcess(ProcessPath.取治具2放治具1_副流程path,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定);

                    break;
                case 搬运轴换治具_ProcessMode.取治具2放治具3:
                    ExecuteSubProcess(ProcessPath.取治具2放治具3_副流程path,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定);

                    break;
                case 搬运轴换治具_ProcessMode.取治具3放治具1:
                    ExecuteSubProcess(ProcessPath.取治具3放治具1_副流程path,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定);

                    break;
                case 搬运轴换治具_ProcessMode.取治具3放治具2:
                    ExecuteSubProcess(ProcessPath.取治具3放治具2_副流程path,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定,
                        () => Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定);

                    break;
                case 搬运轴换治具_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_换治具}，请检查流程配置表！！");
                    return;
            }
        }

        private void Execute_手动测试流程_Process()
        {
            // 记录流程启动日志，包含当后模式（如左Epson/右Epson）
            LogProcessStart("换治具流程跳转顺序", Controller.Instance.StationInfo.Get_换治具.ToString());


            // 根据当后模式（枚举值）执行对应副流程
            switch (Controller.Instance.StationInfo.Get_手动测试)
            {
                case 手动测试_ProcessMode.流程选择:
                    ExecuteSubProcess(ProcessPath.手动测试_流程选择path,
                        () => 手动流程选择(),
                        () => Controller.Instance.StationInfo.Get_手动测试 = 手动测试_ProcessMode.未指定);
                    break;
                case 手动测试_ProcessMode.前滑台手动:
                    ExecuteSubProcess(ProcessPath.手动测试_前滑台path,
                        () => Controller.Instance.StationInfo.Get_手动测试 = 手动测试_ProcessMode.流程选择,
                        () => Controller.Instance.StationInfo.Get_手动测试 = 手动测试_ProcessMode.未指定);
                    Project.Instance.GlobalVariables.手动流程选择等待 = 0;
                    break;
                case 手动测试_ProcessMode.后滑台手动:
                    ExecuteSubProcess(ProcessPath.手动测试_后滑台path,
                        () => Controller.Instance.StationInfo.Get_手动测试 = 手动测试_ProcessMode.流程选择,
                        () => Controller.Instance.StationInfo.Get_手动测试 = 手动测试_ProcessMode.未指定);
                    Project.Instance.GlobalVariables.手动流程选择等待 = 0;
                    break;

                case 手动测试_ProcessMode.未指定:
                    // 模式未指定：设置错误状态
                    SetErrorStatus($"{_processType}--->>错误: {Controller.Instance.StationInfo.Get_换治具}，请检查流程配置表！！");
                    return;
            }
        }

        private void 治具选择()
        {
            switch (Controller.Instance.StationInfo.治具选择治具位空位)
            {

                case 0:
                    Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.未指定;
                    Controller.Instance.StationInfo.治具选择治具位空位 = 0;
                    break;
                case 1:
                    Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.取治具1放治具2;
                    Controller.Instance.StationInfo.治具选择治具位空位 = 0;
                    break;
                case 2:
                    Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.取治具1放治具3;
                    Controller.Instance.StationInfo.治具选择治具位空位 = 0;
                    break;
                case 3:
                    Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.取治具2放治具1;
                    Controller.Instance.StationInfo.治具选择治具位空位 = 0;
                    break;
                case 4:
                    Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.取治具2放治具3;
                    Controller.Instance.StationInfo.治具选择治具位空位 = 0;
                    break;
                case 5:
                    Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.取治具3放治具1;
                    Controller.Instance.StationInfo.治具选择治具位空位 = 0;
                    break;
                case 6:
                    Controller.Instance.StationInfo.Get_换治具 = 搬运轴换治具_ProcessMode.取治具3放治具2;
                    Controller.Instance.StationInfo.治具选择治具位空位 = 0;
                    break;


            }
        }


        private void 手动流程选择()
        {
            switch (Project.Instance.GlobalVariables.手动流程选择)
            {

                case 1:
                    
                    Controller.Instance.StationInfo.Get_手动测试 = 手动测试_ProcessMode.前滑台手动;
                    Project.Instance.GlobalVariables.手动流程选择 = 0;
                    Project.Instance.GlobalVariables.手动流程选择等待 = 0;
                    break;
                case 2:
                   
                    Controller.Instance.StationInfo.Get_手动测试 = 手动测试_ProcessMode.后滑台手动;
                    Project.Instance.GlobalVariables.手动流程选择 = 0;
                    Project.Instance.GlobalVariables.手动流程选择等待 = 0;
                    break;
                default:
                    Project.Instance.GlobalVariables.手动流程选择 = 0;
                    return;



            }
        }
        #endregion

        #region 通用辅助方法
        /// <summary>
        /// 执行副流程的通用方法（封装重复逻辑，包含超时控制）
        /// </summary>
        /// <param name="subProcessPath">副流程的路径</param>
        /// <param name="onComplete">副流程完成后执行的操作</param>
        /// <param name="onBeforeRun">副流程运行前执行的操作</param>
        private void ExecuteSubProcess(string subProcessPath, Action onComplete, Action onBeforeRun)
        {
            try
            {
                // 执行流程前的准备操作
                onBeforeRun?.Invoke();

                // 创建并运行副流程实例
                _subProcess = new AutoCore_副流程(subProcessPath, _processType);
                _subProcess.Run();
                WaitForSubProcessComplete();// 等待副流程完成

                // 如果当前状态是错误，直接返回
                if (CurrentStatus == GeneralStatus.Error)
                    return;

                // 流程完成后执行收尾操作
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                SetErrorStatus($"{_processType} 执行失败: {ex.Message}");
                return; // 异常处理后立即返回
            }
        }

        /// <summary>
        /// 等待副流程完成（带完整状态处理）
        /// </summary>
        /// <returns>true表示正常完成，false表示超时</returns>
        private bool WaitForSubProcessComplete()
        {
            _isWaiting = true;
            var lastStatus = GeneralStatus.Wait;
            int sameStatusCounter = 0;

            while (_isWaiting)
            {
                // 检查急停状态
                if (MenuProxy.Instance.MachineRunStatus == MachineRunStatus.EmergencyStopped)
                {
                    SetErrorStatus($"{_processType}--->>急停");
                    return true; // 急停属于正常终止等待
                }

                // 短延时，降低CPU占用
                Thread.Sleep(10);

                // 检查副流程实例是否初始化
                if (_subProcess == null)
                {
                    SetErrorStatus($"{_processType}--->>副流程实例未初始化");
                    return true;
                }

                // 处理副流程状态
                var currentStatus = _subProcess.AutoRunStatus;
                switch (currentStatus)
                {
                    case GeneralStatus.NoError:
                        // 副流程成功完成
                        _isWaiting = false;
                        Message = $"{_processType}--->>运行完成";
                        return true;
                    case GeneralStatus.Running:
                        // 运行中：更新状态消息
                        Message = $"{_processType}--->>运行中";
                        ResetStatusCounter(ref lastStatus, ref sameStatusCounter, currentStatus);
                        break;
                    case GeneralStatus.Error:
                        // 副流程出错
                        SetErrorStatus($"{_processType}--->>报警");
                        return true;
                }
            }

            return true;
        }

        /// <summary>
        /// 重置状态计数器（状态变化时调用）
        /// </summary>
        private void ResetStatusCounter(ref GeneralStatus lastStatus, ref int counter, GeneralStatus newStatus)
        {
            if (lastStatus != newStatus)
            {
                lastStatus = newStatus;
                counter = 0;
            }
        }

        /// <summary>
        /// 设置错误状态（统一更新运行状态、消息，并终止等待循环）
        /// </summary>
        private void SetErrorStatus(string errorMessage)
        {
            // 将当前状态更新为错误状态
            CurrentStatus = GeneralStatus.Error;

            // 保存错误消息
            Message = errorMessage;

            // 设置等待标识为false，终止等待循环
            _isWaiting = false;

            // 更新机器的运行状态为紧急停止
            Controller.Instance.UpdateMachineRunStatus(MachineRunStatus.EmergencyStopped);

            // 取消当前正在执行的子进程
            _subProcess?.Cancel();
        }

        /// <summary>
        /// 记录副流程启动日志（包含流程类型和当前模式）
        /// </summary>
        /// <param name="logTitle">日志标题</param>
        /// <param name="processMode">当前流程模式（如枚举值）</param>
        private void LogProcessStart(string logTitle, string processMode)
        {
            ProcessLog.Instance.SaveLog(
                logTitle,
                Color.Black,
                "副流程 ",
                processMode,
                "--开始执行！！"
            );
        }
        #endregion
    }
}
