using Jabil.Module.Motion.Common.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Module.Motion
{
    /// <summary>
    /// 运动控制器接口
    /// 
    /// 定义运动控制卡所需实现的所有核心功能契约，是运动控制系统的抽象层核心。
    /// 设计理念采用"面向接口编程"原则，使上层应用与具体硬件实现解耦，
    /// 支持不同品牌/型号运动控制卡的无缝替换（如雷赛、研华、倍福等）。
    /// 
    /// 接口方法覆盖运动控制全生命周期：初始化→操作控制→状态查询→资源释放，
    /// 所有方法均围绕轴卡(MotionCardDto)和轴参数(MotionAxisDto)展开，这两个数据传输对象
    /// 封装了硬件相关的标识信息和配置参数。
    /// </summary>
    public interface IMotionController
    {
        /// <summary>
        /// 初始化轴卡
        /// 
        /// 建立与物理轴卡的通信连接，加载配置参数，分配系统资源，完成硬件初始化。
        /// 这是使用运动控制功能的第一步，必须在其他操作前调用且确保成功。
        /// 多次调用可能导致不可预知的结果，建议在系统启动时执行一次。
        /// </summary>
        /// <param name="currentCardParam">轴卡参数对象，包含轴卡硬件ID、通信端口、配置文件路径等关键信息</param>
        /// <returns>初始化结果：true表示轴卡成功启动并可用，false表示初始化失败（如硬件连接错误、配置无效）</returns>
        bool Open(MotionCardDto currentCardParam);

        /// <summary>
        /// 释放轴卡资源
        /// 
        /// 关闭与轴卡的通信连接，释放分配的内存资源和硬件锁，将轴卡置于安全状态。
        /// 通常在系统关闭、轴卡更换或长时间不使用时调用，确保资源正确回收。
        /// </summary>
        /// <param name="motionCardDto">轴卡参数对象，指定需要关闭的目标轴卡</param>
        /// <returns>释放结果：true表示资源释放成功，false表示释放失败（可能仍有资源被占用）</returns>
        bool Close(MotionCardDto motionCardDto);

        /// <summary>
        /// 伺服使能控制
        /// 
        /// 控制伺服电机的使能状态。使能后电机绕组通电，可接收运动指令并产生扭矩；
        /// 禁用后电机处于自由状态，无法保持位置。常用于维护模式或安全保护场景。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，指定目标轴的标识信息</param>
        /// <param name="servoOn">使能指令：1=使能伺服（电机锁定），0=禁用伺服（电机释放）</param>
        /// <returns>操作结果："NoError"表示指令执行成功；其他字符串为具体错误信息（如"轴未初始化"、"通信超时"）</returns>
        string ServoSet(MotionAxisDto currentAxisParam, ushort servoOn);

        /// <summary>
        /// 报警复位操作
        /// 
        /// 清除伺服驱动器的报警状态。当伺服发生异常（如过流、过载、位置偏差超限）时，
        /// 需先排除物理故障原因，再调用此方法复位报警，否则可能导致报警再次触发。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，指定需要复位报警的轴</param>
        /// <param name="ERCOn">复位指令：1=执行报警复位，0=无操作（保留参数兼容性）</param>
        /// <returns>操作结果："NoError"表示复位成功；其他字符串为错误信息（如"报警原因未排除"）</returns>
        string ERCSet(MotionAxisDto currentAxisParam, ushort ERCOn);

        /// <summary>
        /// 读取伺服使能状态
        /// 
        /// 查询当前轴的伺服使能状态，用于确认电机是否处于可操作状态。
        /// 通常在发送运动指令前调用，避免因未使能导致指令失效。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，指定目标轴</param>
        /// <returns>状态值：1=已使能，0=未使能，-1=查询失败（如通信错误）</returns>
        short ServoRead(MotionAxisDto currentAxisParam);

        /// <summary>
        /// 点动控制
        /// 
        /// 控制轴以预设速度进行连续运动（正方向/负方向），常用于手动调试或位置微调。
        /// 点动状态会持续到调用停止方法(SmtStop/EmgStop)为止，需注意操作安全性。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，包含点动速度等配置信息</param>
        /// <param name="isNegative">运动方向：1=负方向，0=正方向（方向定义与机械坐标系相关）</param>
        /// <returns>操作结果："NoError"表示点动指令已发出；其他为错误信息（如"未使能"、"速度参数无效"）</returns>
        string JogMove(MotionAxisDto currentAxisParam, ushort isNegative);

        /// <summary>
        /// 相对运动
        /// 
        /// 控制轴从当前位置移动指定的相对距离，适用于需要增量移动的场景（如装配步骤）。
        /// 运动参数（速度、加速度）由轴参数对象中的配置决定。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，包含运动参数配置</param>
        /// <param name="dist">相对移动距离，单位通常为毫米(mm)，正负值表示方向</param>
        /// <returns>操作结果："NoError"表示运动指令已接受；其他为错误信息（如"距离超限"）</returns>
        string RelMove(MotionAxisDto currentAxisParam, double dist);

        /// <summary>
        /// 绝对运动
        /// 
        /// 控制轴移动到指定的绝对坐标位置，基于系统原点的坐标系。
        /// 适用于需要精确定位到固定位置的场景（如工位切换）。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，包含基础配置</param>
        /// <param name="pose">目标绝对位置，单位通常为毫米(mm)</param>
        /// <param name="speed">运动速度，单位通常为毫米/秒(mm/s)</param>
        /// <returns>操作结果："NoError"表示指令已接受；其他为错误信息（如"位置超出软限位"）</returns>
        string AbsMove(MotionAxisDto currentAxisParam, double pose, double speed);

        /// <summary>
        /// 回原点操作
        /// 
        /// 控制轴执行回原点流程，建立机械坐标系的基准点。
        /// 回原方式由轴参数配置决定（如：接近开关+编码器Z相、机械限位触发等），
        /// 回原成功后位置计数器会被重置为原点坐标值。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，包含回原速度、传感器类型等配置</param>
        /// <returns>操作结果："NoError"表示回原流程已启动；其他为错误信息（如"原点传感器故障"）</returns>
        string GoHome(MotionAxisDto currentAxisParam);

        /// <summary>
        /// 减速停止
        /// 
        /// 控制轴按设定的减速曲线平稳停止，避免急停带来的机械冲击。
        /// 适用于正常操作中的运动终止，如到达目标位置或手动停止。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，指定目标轴</param>
        /// <param name="stop_mode">停止模式：0=平滑减速（较长停止距离），1=快速减速（较短停止距离）</param>
        /// <returns>操作结果："NoError"表示停止指令已执行；其他为错误信息</returns>
        string SmtStop(MotionAxisDto currentAxisParam, UInt16 stop_mode);

        /// <summary>
        /// 紧急停止
        /// 
        /// 立即切断轴的运动输出，使轴以最快速度停止（通常通过硬件回路实现）。
        /// 仅用于紧急情况（如设备故障、人员危险），可能导致机械冲击和位置偏差。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，指定目标轴</param>
        /// <returns>操作结果："NoError"表示急停指令已执行；其他为错误信息</returns>
        string EmgStop(MotionAxisDto currentAxisParam);

        /// <summary>
        /// 获取轴综合状态
        /// 
        /// 一次性读取轴的多种关键状态，用于监控和诊断。状态数组的索引与含义固定，
        /// 上层应用可通过索引快速判断轴的当前状态组合（如"报警且正限位触发"）。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，指定目标轴</param>
        /// <returns>布尔状态数组，各索引含义：
        /// [0]：伺服报警状态（true=报警触发）
        /// [1]：伺服使能状态（true=已使能）
        /// [2]：正极限触发状态（true=触发正限位）
        /// [3]：原点信号状态（true=检测到原点信号）
        /// [4]：负极限触发状态（true=触发负限位）
        /// [5]：运动中状态（true=正在运动）
        /// [6]：到位状态（true=已到达目标位置）
        /// [7]：掉线状态（true=与驱动器通信中断）
        /// [8]：缓停触发状态（true=已触发减速停止）
        /// 注：后续可扩展更多状态位，需保持向前兼容
        /// </returns>
        bool[] GetMotionSts(MotionAxisDto currentAxisParam);

        /// <summary>
        /// 获取指令位置
        /// 
        /// 读取轴当前的规划位置（控制器计算出的理论位置），非实际反馈位置。
        /// 用于监控运动规划是否正常，与实际位置对比可判断跟随误差。
        /// </summary>
        /// <param name="motionAxis">轴参数对象，指定目标轴</param>
        /// <returns>指令位置值（单位：mm）；-999表示查询失败</returns>
        double GetCmd(MotionAxisDto motionAxis);

        /// <summary>
        /// 获取指令速度
        /// 
        /// 读取轴当前的规划速度（控制器输出的目标速度），非实际运行速度。
        /// 用于验证速度参数是否正确应用，排查速度异常问题。
        /// </summary>
        /// <param name="motionAxis">轴参数对象，指定目标轴</param>
        /// <returns>指令速度值（单位：mm/s）；-999表示查询失败</returns>
        double GetCmdVel(MotionAxisDto motionAxis);

        /// <summary>
        /// 设置当前位置值
        /// 
        /// 手动校准轴的当前位置（将指定值写入位置计数器），不产生实际运动。
        /// 常用于原点偏移调整、机械误差补偿或更换工件后的位置重置。
        /// </summary>
        /// <param name="Pos">要设置的位置值（单位：mm）</param>
        /// <param name="currentAxisParam">轴参数对象，指定目标轴</param>
        /// <returns>操作结果："NoError"表示设置成功；其他为错误信息（如"运动中无法设置"）</returns>
        string SetPos(double Pos, MotionAxisDto currentAxisParam);

        /// <summary>
        /// 获取编码器位置
        /// 
        /// 读取编码器反馈的当前位置，反映电机轴的实际转动位置（可能包含传动误差）。
        /// 是闭环控制中的关键反馈信号，用于计算位置偏差。
        /// </summary>
        /// <param name="motionAxis">轴参数对象，指定目标轴</param>
        /// <returns>编码器位置值（单位：mm）；-999表示查询失败</returns>
        double GetEncPos(MotionAxisDto motionAxis);

        /// <summary>
        /// 获取编码器速度
        /// 
        /// 读取编码器反馈的实时速度，反映电机的实际转动速度。
        /// 用于监控速度稳定性和动态响应性能。
        /// </summary>
        /// <param name="motionAxis">轴参数对象，指定目标轴</param>
        /// <returns>编码器速度值（单位：mm/s）；-999表示查询失败</returns>
        double GetEncVel(MotionAxisDto motionAxis);

        /// <summary>
        /// 读取驱动器实际位置
        /// 
        /// 从伺服驱动器直接读取经过处理的实际位置，通常经过滤波和补偿，
        /// 比原始编码器位置更接近机械系统的真实位置。
        /// </summary>
        /// <param name="motionAxis">轴参数对象，指定目标轴</param>
        /// <returns>实际位置值（单位：mm）；-999表示查询失败</returns>
        double ReadActualPosition(MotionAxisDto motionAxis);

        /// <summary>
        /// 检查运动是否完成
        /// 
        /// 判断轴是否已到达目标位置并处于静止状态（位置偏差在允许范围内）。
        /// 常用于运动序列控制，确保前一步运动完成后再执行下一步操作。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，指定目标轴</param>
        /// <returns>运动状态：true=已完成（静止且到位），false=运动中或未到位</returns>
        bool CheckMoveDone(MotionAxisDto currentAxisParam);

        /// <summary>
        /// 检查回零动作是否完成
        /// 
        /// 专门用于判断回原点操作是否成功完成，比通用运动检查更精确，
        /// 可能包含回零流程特有的验证（如原点信号是否稳定触发）。
        /// </summary>
        /// <param name="currentAxisParam">轴参数对象，指定目标轴</param>
        /// <returns>回零状态：true=回零完成且成功，false=回零中或失败</returns>
        bool CheckMoveHomeDone(MotionAxisDto currentAxisParam);

        /// <summary>
        /// 获取目标位置
        /// 
        /// 读取轴当前运动指令的最终目标位置（绝对坐标），适用于监控运动任务的目标点。
        /// 与当前位置对比可判断剩余运动距离。
        /// </summary>
        /// <param name="motionAxis">轴参数对象，指定目标轴</param>
        /// <returns>目标位置值（单位：mm）；-999表示查询失败</returns>
        double GetTargetPos(MotionAxisDto motionAxis);

        /// <summary>
        /// 检查伺服报警状态
        /// 
        /// 专门用于查询轴是否处于报警状态，是GetMotionSts[0]的便捷封装，
        /// 简化上层应用的报警判断逻辑。
        /// </summary>
        /// <param name="axisParam">轴参数对象，指定目标轴</param>
        /// <returns>报警状态：true=存在报警，false=正常无报警</returns>
        bool CheckServoAlarm(MotionAxisDto axisParam);

        #region 多轴插补运动方法

        /// <summary>
        /// 两轴直线插补运动
        /// 
        /// 控制两个轴同时运动，沿直线轨迹从当前位置移动到目标位置，保持合成速度恒定。
        /// 适用于平面内的直线运动，如XY工作台的精确移动。
        /// </summary>
        /// <param name="axis1">第一轴参数对象（通常为X轴）</param>
        /// <param name="axis2">第二轴参数对象（通常为Y轴）</param>
        /// <param name="targetPos1">第一轴目标位置（单位：mm）</param>
        /// <param name="targetPos2">第二轴目标位置（单位：mm）</param>
        /// <param name="speed">合成运动速度（单位：mm/s）</param>
        /// <param name="accel">加速度（单位：mm/s²）</param>
        /// <param name="decel">减速度（单位：mm/s²）</param>
        /// <returns>操作结果："NoError"表示指令已接受；其他为错误信息</returns>
        string LinearInterpolation2D(MotionAxisDto axis1, MotionAxisDto axis2,
                                    double targetPos1, double targetPos2,
                                    double speed, double accel, double decel);

        /// <summary>
        /// 两轴圆弧插补运动
        /// 
        /// 控制两个轴协调运动，沿圆弧轨迹移动，保持切向速度恒定。
        /// 适用于需要圆弧轨迹的场景，如圆角加工、弧形路径规划。
        /// </summary>
        /// <param name="xAxis">X轴参数对象</param>
        /// <param name="yAxis">Y轴参数对象</param>
        /// <param name="centerX">圆弧中心点X坐标（单位：mm）</param>
        /// <param name="centerY">圆弧中心点Y坐标（单位：mm）</param>
        /// <param name="endX">终点X坐标（单位：mm）</param>
        /// <param name="endY">终点Y坐标（单位：mm）</param>
        /// <param name="isClockwise">是否顺时针方向：true=顺时针，false=逆时针</param>
        /// <param name="speed">圆弧切向速度（单位：mm/s）</param>
        /// <param name="accel">加速度（单位：mm/s²）</param>
        /// <param name="decel">减速度（单位：mm/s²）</param>
        /// <returns>操作结果："NoError"表示指令已接受；其他为错误信息</returns>
        string CircularInterpolation2D(MotionAxisDto xAxis, MotionAxisDto yAxis,
                                      double centerX, double centerY,
                                      double endX, double endY,
                                      bool isClockwise, double speed,
                                      double accel, double decel);

        /// <summary>
        /// 三轴直线插补运动
        /// 
        /// 控制三个轴同时运动，沿三维空间直线轨迹移动，保持合成速度恒定。
        /// 适用于三维空间内的直线运动，如XYZ机器人的空间定位。
        /// </summary>
        /// <param name="axis1">第一轴参数对象（通常为X轴）</param>
        /// <param name="axis2">第二轴参数对象（通常为Y轴）</param>
        /// <param name="axis3">第三轴参数对象（通常为Z轴）</param>
        /// <param name="targetPos1">第一轴目标位置（单位：mm）</param>
        /// <param name="targetPos2">第二轴目标位置（单位：mm）</param>
        /// <param name="targetPos3">第三轴目标位置（单位：mm）</param>
        /// <param name="speed">合成运动速度（单位：mm/s）</param>
        /// <param name="accel">加速度（单位：mm/s²）</param>
        /// <param name="decel">减速度（单位：mm/s²）</param>
        /// <returns>操作结果："NoError"表示指令已接受；其他为错误信息</returns>
        string LinearInterpolation3D(MotionAxisDto axis1, MotionAxisDto axis2, MotionAxisDto axis3,
                                    double targetPos1, double targetPos2, double targetPos3,
                                    double speed, double accel, double decel);

        /// <summary>
        /// 四轴插补运动
        /// 
        /// 控制四个轴协调运动，通常为XYZ三个直线轴加一个旋转轴，实现复杂空间运动。
        /// 适用于需要姿态调整的三维运动，如带有旋转工作台的加工设备。
        /// </summary>
        /// <param name="axes">四个轴的参数对象数组（通常为X,Y,Z和旋转轴A）</param>
        /// <param name="targetPositions">对应四个轴的目标位置数组（单位：mm或度）</param>
        /// <param name="speed">合成运动速度（单位：mm/s）</param>
        /// <param name="accel">加速度（单位：mm/s²）</param>
        /// <param name="decel">减速度（单位：mm/s²）</param>
        /// <returns>操作结果："NoError"表示指令已接受；其他为错误信息</returns>
        string Interpolation4D(MotionAxisDto[] axes, double[] targetPositions,
                              double speed, double accel, double decel);

        /// <summary>
        /// 五轴插补运动
        /// 
        /// 控制五个轴协调运动，通常为XYZ三个直线轴加两个旋转轴，实现复杂空间姿态运动。
        /// 适用于高端加工设备，如五轴加工中心，可实现复杂曲面的连续加工。
        /// </summary>
        /// <param name="axes">五个轴的参数对象数组（通常为X,Y,Z,A和B旋转轴）</param>
        /// <param name="targetPositions">对应五个轴的目标位置数组（单位：mm或度）</param>
        /// <param name="speed">合成运动速度（单位：mm/s）</param>
        /// <param name="accel">加速度（单位：mm/s²）</param>
        /// <param name="decel">减速度（单位：mm/s²）</param>
        /// <param name="toolLength">刀具长度补偿值（单位：mm），用于复杂加工场景</param>
        /// <returns>操作结果："NoError"表示指令已接受；其他为错误信息</returns>
        string Interpolation5D(MotionAxisDto[] axes, double[] targetPositions,
                              double speed, double accel, double decel,
                              double toolLength = 0);

        /// <summary>
        /// 检查插补运动是否完成
        /// 
        /// 判断一组插补运动的所有轴是否已到达目标位置并处于静止状态。
        /// 用于多轴运动序列控制，确保插补运动完全完成后再执行下一步操作。
        /// </summary>
        /// <param name="axes">参与插补运动的轴参数对象数组</param>
        /// <returns>运动状态：true=所有轴均已完成运动，false=存在未完成运动的轴</returns>
        bool CheckInterpolationDone(MotionAxisDto[] axes);

        /// <summary>
        /// 停止插补运动
        /// 
        /// 使参与当前插补运动的所有轴按设定的减速曲线平稳停止。
        /// 适用于正常操作中需要终止插补运动的场景。
        /// </summary>
        /// <param name="axes">参与插补运动的轴参数对象数组</param>
        /// <param name="stopMode">停止模式：0=平滑减速，1=快速减速</param>
        /// <returns>操作结果："NoError"表示停止指令已执行；其他为错误信息</returns>
        string StopInterpolation(MotionAxisDto[] axes, ushort stopMode);

        #endregion
    }
}
