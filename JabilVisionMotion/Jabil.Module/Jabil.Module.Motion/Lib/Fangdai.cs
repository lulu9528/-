//using Jabil.Lib;
//using Jabil.Module.Log.Lib;
//using Jabil.Module.Motion.Common.Dtos;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Jabil.Module.Motion.Lib
//{
//    /// <summary>
//    /// 防呆全局读取轴位置
//    /// </summary>
//    public static class AllAxisPos
//    {
//        /// <summary>
//        /// 所有轴的编码器位置
//        /// </summary>
//        public static List<double> AllAxisEncPos { get; set; } = new List<double>(16);
//        /// <summary>
//        /// 所有轴的运动状态
//        /// </summary>
//        public static List<bool> AllAxisRuning { get; set; } = new List<bool>(16);
//        static AllAxisPos()
//        {
//            for (int i = 0; i < 16; i++)
//            {
//                AllAxisEncPos.Add(0.00);
//                AllAxisRuning.Add(false);
//            }
//        }
//        // 急停/STO相关（安全扭矩关闭）
//        public const int 上料Y轴 = 0;
//        public const int 上料Z轴 = 1;
//        public const int 检测X轴 = 2;

//        public const int 检测旋转1 = 3;
//        public const int 检测旋转2 = 4;
//        public const int 检测旋转3 = 5;
//        public const int 检测旋转4 = 6;

//        public const int 左检测相机 = 7;
//        public const int 右检测相机 = 8;

//        public const int 补料BufferX轴 = 9;
//        public const int 分料Y轴 = 10;
//        public const int 分料Z轴 = 11;
//        public const int 组装X轴 = 12;
//        public const int 组装Y轴 = 13;
//        public const int 组装Z轴 = 14;
//        public const int 组装U轴 = 15;
//    }

//    /// <summary>
//    /// 防呆全局读取IO
//    /// </summary>
//    public static class IoIndices
//    {
//        public static List<bool> AllDiStatus { get; set; } = new List<bool>(128);
//        static IoIndices()
//        {
//            for (int i = 0; i < 128; i++)
//            {
//                AllDiStatus.Add(false);
//            }
//        }
//        // 急停/STO相关（安全扭矩关闭）
//        public const int EmergencyStop1 = 9; // 急停信号索引
//        public const int EmergencyStop2 = 10; // 急停信号索引
//        public const int MachineInternalSto1 = 16; // 机台内部STO索引
//        public const int MachineInternalSto2 = 17; // 机台内部STO索引

//        public const int 安全门正左 = 11; // 安全门正面索引
//        public const int 安全门正右 = 12; // 安全门正面索引
//        public const int 安全门后左 = 13; // 安全门右索引
//        public const int 安全门后右 = 14; // 安全门右索引

//        public const int AutoManualSwitch = 15; // 手自动切换索引

//        public const int MachineStart = 4; // 机台启动索引
//        public const int MachineReset = 5; // 机台复位索引
//        public const int MachinePause = 6; // 机台暂停索引

//        public const int 上料解锁气缸原点 = 26;
//        public const int 下料解锁气缸原点 = 28;

//        public const int 流线_出口有料 = 87;
//        public const int 清料按钮 = 7;
//    }

//    public static class Fangdai
//    {
//        /// <summary>
//        /// 为true 表示 上料允许移动，或者当前移动安全
//        /// </summary>
//        /// <returns></returns>
//        public static bool 上料YZ移动允许(double tagYPos)
//        {
//            //Y轴需要移动超过1 并且Z轴不在上面，就不允许移动
//            if (Math.Abs(AllAxisPos.AllAxisEncPos[AllAxisPos.上料Y轴] - tagYPos) > 1 && AllAxisPos.AllAxisEncPos[AllAxisPos.上料Z轴] > 0)
//            {
//                //检测Z轴在运行或者 Z 轴高度不够高
//                return false;
//            }
//            if (AllAxisPos.AllAxisEncPos[AllAxisPos.上料Y轴] > 100 && AllAxisPos.AllAxisEncPos[AllAxisPos.上料Z轴] > 20)
//            {
//                //放料侧，放料Z不许再往下
//                return false;
//            }
//            if (AllAxisPos.AllAxisEncPos[AllAxisPos.上料Y轴] < 100 && AllAxisPos.AllAxisEncPos[AllAxisPos.上料Z轴] > 130)
//            {
//                //取料侧，放料Z不许再往下
//                return false;
//            }
//            return true;
//        }
//        public static bool 检测轴移动允许()
//        {
//            //其他轴的防呆
//            if (!IoIndices.AllDiStatus[IoIndices.上料解锁气缸原点] || !IoIndices.AllDiStatus[IoIndices.下料解锁气缸原点])
//            {
//                //检测Z轴在运行
//                //防抖
//                System.Threading.Thread.Sleep(100);
//                if (!IoIndices.AllDiStatus[IoIndices.上料解锁气缸原点] || !IoIndices.AllDiStatus[IoIndices.下料解锁气缸原点])
//                {
//                    return false;
//                }
//            }
//            if (AllAxisPos.AllAxisEncPos[AllAxisPos.上料Y轴] > 100 && AllAxisPos.AllAxisEncPos[AllAxisPos.上料Z轴] > 0.1)
//            {
//                //检测Z轴在运行或者 Z 轴高度不够高
//                return false;
//            }
//            if (AllAxisPos.AllAxisEncPos[AllAxisPos.分料Y轴] < 200 && AllAxisPos.AllAxisEncPos[AllAxisPos.分料Y轴] > 100 && AllAxisPos.AllAxisEncPos[AllAxisPos.分料Z轴] > 0.1)
//            {
//                //分料轴在取料位
//                return false;
//            }
//            return true;
//        }

//        public static bool 分料移动允许(double tagYPos)
//        {
//            //Y轴需要移动超过1 并且Z轴不在上面，就不允许移动
//            if (Math.Abs(AllAxisPos.AllAxisEncPos[AllAxisPos.分料Y轴] - tagYPos) > 1 && AllAxisPos.AllAxisEncPos[AllAxisPos.分料Z轴] > 0)
//            {
//                //检测Z轴在运行或者 Z 轴高度不够高
//                return false;
//            }
//            if (tagYPos > 200 && AllAxisPos.AllAxisEncPos[AllAxisPos.组装Y轴] < -20)
//            {
//                //分料Y轴 和 组装Y轴 干涉
//                return false;
//            }
//            return true;
//        }

//        public static bool 缓冲X轴移动允许()
//        {
//            //分料Y轴在 缓冲位 且Z轴在下面
//            if (AllAxisPos.AllAxisEncPos[AllAxisPos.分料Y轴] > 50 && AllAxisPos.AllAxisEncPos[AllAxisPos.分料Y轴] < 150 && AllAxisPos.AllAxisEncPos[AllAxisPos.分料Z轴] > 0)
//            {
//                return false;
//            }
//            return true;
//        }



//        /// <summary>
//        /// 检查运动点位的防呆安全条件
//        /// </summary>
//        /// <param name="currentMotionPoint">当前运动点位信息</param>
//        /// <returns>是否允许运动（true=允许，false=禁止）</returns>
//        public static bool CheckMotionSafety(MotionPointDto currentMotionPoint)
//        {
//            // 防呆逻辑！！！重要
//            switch (currentMotionPoint.点位定义)
//            {
//                case "上料":
//                    if (!Fangdai.上料YZ移动允许(currentMotionPoint.Y轴目标位置))
//                    {
//                        RLog.SaveLog(LogType.Operation, Color.Yellow, "上料模组-安全互锁不满足，无法移动！");
//                        return false;
//                    }
//                    break;

//                case "检测X/旋转轴":
//                case "检测X轴":
//                    if (!Fangdai.检测轴移动允许())
//                    {
//                        RLog.SaveLog(LogType.Operation, Color.Yellow, "检测轴-安全互锁不满足，无法移动！");
//                        return false;
//                    }
//                    break;

//                case "分料":
//                    if (!Fangdai.分料移动允许(currentMotionPoint.Y轴目标位置))
//                    {
//                        RLog.SaveLog(LogType.Operation, Color.Yellow, "分料模组-安全互锁不满足，无法移动！");
//                        return false;
//                    }
//                    break;

//                case "缓冲取放":
//                case "缓冲轴":
//                    if (!Fangdai.缓冲X轴移动允许())
//                    {
//                        RLog.SaveLog(LogType.Operation, Color.Yellow, "缓冲轴-安全互锁不满足，无法移动！");
//                        return false;
//                    }
//                    break;

//                default:
//                    // 未定义防呆检查的点位，默认允许移动
//                    // 可根据实际需求修改默认行为
//                    break;
//            }

//            // 所有防呆检查通过
//            return true;
//        }

//    }

//}
