using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    /// <summary>
    /// 几何计算基础类，提供点、线、圆、弧、样条曲线的核心运算方法
    /// 包含坐标转换、斜率计算、投影、圆弧拟合、向量运算等几何功能
    /// </summary>
    public class CalculateBase
    {
        #region 点运算（二维点的旋转、距离、投影等计算）

        /// <summary>
        /// 二维坐标旋转转换（绕中心点旋转指定弧度）
        /// </summary>
        /// <param name="center">旋转中心点（旋转轴）</param>
        /// <param name="orgPoint">待旋转的原始点</param>
        /// <param name="radian">旋转角度（弧度制，正为逆时针，负为顺时针）</param>
        /// <returns>旋转后得到的新坐标点</returns>
        public PointD RevolveTwoDimensionChange(PointD center, PointD orgPoint, double radian)
        {
            // 旋转公式推导：
            // 设旋转中心为( Xr, Yr )，原始点为( X, Y )，旋转弧度为α
            // 旋转后坐标( X', Y' )的计算公式为：
            // X' = Xr + (X - Xr)·cosα - (Y - Yr)·sinα
            // Y' = Yr + (X - Xr)·sinα + (Y - Yr)·cosα
            // 注：代码中简化了(X - Xr)和(Y - Yr)的计算（可能原逻辑默认旋转中心为原点，需根据实际场景确认）

            // 结果点初始化
            PointD resultPoint = new PointD();

            // 计算余弦和正弦值（保留7位小数，减少精度误差）
            double cos = Math.Round(Math.Cos(radian), 7);
            double sin = Math.Round(Math.Sin(radian), 7);

            // 计算旋转后的X坐标
            resultPoint.X = center.X + (orgPoint.X) * cos - (orgPoint.Y) * sin;
            // 计算旋转后的Y坐标
            resultPoint.Y = center.Y + (orgPoint.Y) * cos + (orgPoint.X) * sin;

            return resultPoint;
        }

        /// <summary>
        /// 计算两点坐标差的平方和（即两点间距离的平方）
        /// </summary>
        /// <param name="A">点A</param>
        /// <param name="B">点B</param>
        /// <returns>（B.X - A.X）² +（B.Y - A.Y）² 的结果</returns>
        public double TwoPointSquare(PointD A, PointD B)
        {
            // 计算Y方向差的平方
            double ySquare = Math.Pow(B.Y - A.Y, 2);
            // 计算X方向差的平方
            double xSquare = Math.Pow(B.X - A.X, 2);
            // 返回平方和
            return ySquare + xSquare;
        }

        /// <summary>
        /// 计算点到直线的投影点（垂足）
        /// </summary>
        /// <param name="A">待投影的点</param>
        /// <param name="line">目标直线</param>
        /// <returns>点A在直线上的投影点</returns>
        public PointD ProjectionPointToLine(PointD A, FigueLine line)
        {
            double Px = 0, Py = 0;  // 投影点坐标

            // 投影公式：
            // 若直线斜率为k，方程为y = kx + b，则点(x0,y0)的投影点(x,y)满足：
            // x = [k(y0 - b) + x0] / (k² + 1)
            // y = kx + b
            // 特殊情况：直线垂直于X轴（斜率不存在），投影点x坐标与直线相同，y坐标与原点点相同

            // 判断直线是否有斜率（非垂直直线）
            if (line.Slope.Count != 0)
            {
                double k = line.Slope[0];  // 直线斜率
                // 计算直线方程的截距b（由直线上一点line.A推导：b = y - kx）
                double b = -k * line.A.X + line.A.Y;
                // 计算投影点X坐标
                Px = (k * (A.Y - b) + A.X) / (Math.Pow(k, 2) + 1);
                // 计算投影点Y坐标（代入直线方程）
                Py = k * Px + b;
            }
            else
            {
                // 直线垂直于X轴（斜率不存在），投影点X等于直线的X坐标，Y等于原点点Y
                Px = line.A.X;
                Py = A.Y;
            }

            return new PointD(Px, Py);
        }

        #endregion

        #region 线运算（直线斜率计算）

        /// <summary>
        /// 计算两点确定的直线的斜率
        /// </summary>
        /// <param name="p1">直线上的点p1</param>
        /// <param name="p2">直线上的点p2</param>
        /// <returns>斜率列表：若直线不垂直于X轴，返回包含斜率k的列表；若垂直（X坐标相同），返回空列表</returns>
        public List<double> slopeK(PointD p1, PointD p2)
        {
            List<double> listk = new List<double>();

            // 斜率公式：k = (y1 - y2)/(x1 - x2)
            // 若x1 == x2，分母为0，斜率不存在（直线垂直于X轴）
            if (Math.Round(p1.X, 4) - Math.Round(p2.X, 4) != 0)
            {
                // 计算斜率（保留4位小数减少精度误差）
                double k = (Math.Round(p1.Y, 4) - Math.Round(p2.Y, 4))
                         / (Math.Round(p1.X, 4) - Math.Round(p2.X, 4));
                listk.Add(k);
            }

            return listk;
        }

        #endregion

        #region 圆运算（已知圆上一点和夹角求另一点）

        /// <summary>
        /// 已知圆上一点和夹角，求圆上另一点（两点与圆心的连线夹角为指定角度）
        /// </summary>
        /// <param name="p">圆上已知点</param>
        /// <param name="angel">两点与圆心连线的夹角（角度制）</param>
        /// <param name="cir">圆（包含圆心和半径）</param>
        /// <returns>圆上满足夹角条件的另一点</returns>
        public PointD OnePointToOtherPoint(PointD p, double angel, FigueCircule cir)
        {
            // 原理：利用向量夹角公式和圆方程联立求解
            // 设圆心为O，已知点为P，待求点为Q，则∠POQ = angel
            // 向量OP·向量OQ = |OP|·|OQ|·cos(angel) （点积公式）
            // |OP| = |OQ| = 半径r（圆的性质）
            // 联立圆方程x² + y² = r²，求解Q点坐标

            PointD result = new PointD();
            // 向量OP（从圆心到已知点p的向量）
            PointD a = TwoPointToVector(p, cir.Center);

            // 将角度转换为弧度
            double rad = AngleToRad(angel);
            // 联立方程求解（一元二次方程）
            double t1 = (cir.Radius * cir.Radius * Math.Cos(rad)) / p.Y;
            double A = 1 + (p.X * p.X) / (p.Y * p.Y);
            double B = -2 * t1 * p.X / p.Y;
            double C = t1 * t1 - cir.Radius * cir.Radius;

            // 求解一元二次方程，得到可能的X坐标
            List<double> resultPoint = RootEqution(A, B, C);

            // 根据方程解计算Y坐标，并判断哪个点符合夹角方向
            if (resultPoint.Count == 2)
            {
                // 第一个解对应的Y坐标
                double y = (cir.Radius * cir.Radius * Math.Cos(rad) - p.X * resultPoint[0]) / p.Y;
                PointD r1 = new PointD(resultPoint[0], y);
                // 第二个解对应的Y坐标
                y = (cir.Radius * cir.Radius * Math.Cos(rad) - p.X * resultPoint[1]) / p.Y;
                PointD r2 = new PointD(resultPoint[1], y);

                // 向量OQ1（从圆心到r1的向量）
                PointD r1Vector = TwoPointToVector(r1, cir.Center);
                // 利用叉积判断方向（sin值符号），确保夹角为正方向
                double sin1 = VectorCrossProduct(r1Vector, a) / (cir.Radius * cir.Radius);
                if (Math.Asin(sin1) / angel > 0)
                {
                    result = r1;
                }
                else
                {
                    result = r2;
                }
            }
            else if (resultPoint.Count == 1)
            {
                // 唯一解（相切情况）
                double y = (cir.Radius * cir.Radius * Math.Cos(rad) - p.X * resultPoint[0]) / p.Y;
                result = new PointD(resultPoint[0], y);
            }

            return result;
        }

        #endregion

        #region 弧运算（三点拟合圆弧）

        /// <summary>
        /// 通过圆弧上的三个点（起点A、中间点B、终点C）拟合圆弧
        /// </summary>
        /// <param name="A">圆弧上的起点</param>
        /// <param name="B">圆弧上的中间点</param>
        /// <param name="C">圆弧上的终点</param>
        /// <returns>包含圆心、半径、起始角度和结束角度的圆弧对象</returns>
        public FigureArc ThreePointArc(PointD A, PointD B, PointD C)
        {
            // 原理：三点确定一个圆，圆弧的圆心是AB和BC的垂直平分线的交点
            // 半径为圆心到任意一点的距离
            // 起始角度和结束角度为起点、终点与圆心连线和X轴的夹角

            // 计算AB和BC的斜率
            double slope_AB = (A.Y - B.Y) / (A.X - B.X);  // AB线段的斜率
            double slope_BC = (B.Y - C.Y) / (B.X - C.X);  // BC线段的斜率
            // 计算AB和BC的中点
            PointD centerPoint_AB = new PointD((A.X + B.X) / 2, (A.Y + B.Y) / 2);  // AB中点
            PointD centerPoint_BC = new PointD((B.X + C.X) / 2, (B.Y + C.Y) / 2);  // BC中点

            double centerX = 0;  // 圆心X坐标
            PointD cirCenter = new PointD();  // 圆心坐标

            // 分情况计算圆心（处理垂直或非垂直直线的情况）
            if (A.X != B.X && B.X != C.X)
            {
                // 非垂直直线：垂直平分线方程联立求解
                centerX = (slope_AB * centerPoint_BC.X - slope_BC * centerPoint_AB.X
                            + slope_AB * slope_BC * (centerPoint_BC.Y - centerPoint_AB.Y))
                          / (slope_AB - slope_BC);
                cirCenter = new PointD(centerX, (centerPoint_AB.X - centerX) / slope_AB + centerPoint_AB.Y);
            }
            else if (A.X != B.X && B.X == C.X)
            {
                // BC垂直于X轴的情况
                centerX = centerPoint_AB.X - slope_AB * (centerPoint_BC.Y - centerPoint_AB.Y);
                cirCenter = new PointD(centerX, (centerPoint_AB.X - centerX) / slope_AB + centerPoint_AB.Y);
            }
            else if (A.X == B.X && B.X != C.X)
            {
                // AB垂直于X轴的情况
                centerX = centerPoint_BC.X + slope_BC * (centerPoint_BC.Y - centerPoint_AB.Y);
                cirCenter = new PointD(centerX, (centerPoint_BC.X - centerX) / slope_BC + centerPoint_BC.Y);
            }

            // 计算半径（圆心到A点的距离）
            double radiusSquared = (cirCenter.X - A.X) * (cirCenter.X - A.X) + (cirCenter.Y - A.Y) * (cirCenter.Y - A.Y);
            double radius = Math.Sqrt(radiusSquared);

            // 初始化圆弧对象
            FigureArc _result = new FigureArc();
            _result.Center = cirCenter;
            _result.Radius = radius;

            // 计算圆心到三点的向量（从圆心指向点）
            PointD VecOA = TwoPointToVector(A, cirCenter);
            PointD VecOB = TwoPointToVector(B, cirCenter);
            PointD VecOC = TwoPointToVector(C, cirCenter);
            // 水平参考向量（沿X轴正方向）
            PointD VecOrg = new PointD(5, 0);

            // 计算各点与水平向量的夹角（即与X轴的夹角）
            double angleOA = VectorInculedAngle(VecOrg, VecOA);  // 起点A的角度
            double angleOC = VectorInculedAngle(VecOrg, VecOC);  // 终点C的角度

            // 设置圆弧的起始和结束角度
            _result.StartAngle = angleOA;
            _result.EndAngle = angleOC;

            return _result;
        }

        #endregion

        #region 样条曲线运算（计算样条曲线的最小半径）

        /// <summary>
        /// 计算样条曲线上的最小曲率半径（通过采样点拟合圆弧估算）
        /// </summary>
        /// <param name="SplinePoints">样条曲线上的点集合</param>
        /// <param name="accurayNum">采样精度间隔（每隔多少个点取一个样本）</param>
        /// <returns>曲线的最小半径</returns>
        public double SplineMinRadius(List<PointD> SplinePoints, int accurayNum)
        {
            double result = 0;
            List<double> listRadius = new List<double>();  // 存储各段圆弧的半径

            // 计算有效采样次数（确保有足够的点拟合圆弧）
            int num = SplinePoints.Count / accurayNum / 3;

            // 按精度间隔采样点，每3个点拟合一段圆弧，计算半径
            for (int i = 0; i < num * accurayNum * 3; i += accurayNum * 3)
            {
                PointD A = SplinePoints[i];                  // 第一个采样点
                PointD B = SplinePoints[i + accurayNum];     // 第二个采样点（间隔accurayNum）
                PointD C = SplinePoints[i + accurayNum * 2]; // 第三个采样点（间隔2*accurayNum）

                // 拟合圆弧并获取半径
                FigureArc arc = ThreePointArc(A, B, C);
                listRadius.Add(arc.Radius);
            }

            // 排序后取最小半径
            listRadius.Sort();
            result = listRadius[0];

            return result;
        }

        #endregion

        #region 公共方法（通用几何工具方法）

        /// <summary>
        /// 计算两个数的平方和
        /// </summary>
        /// <param name="a">第一个数</param>
        /// <param name="b">第二个数</param>
        /// <returns>a² + b² 的结果</returns>
        public double TwoSquare(double a, double b)
        {
            return Math.Pow(a, 2) + Math.Pow(b, 2);
        }

        #region 弧度与角度转换

        /// <summary>
        /// 弧度转换为角度
        /// </summary>
        /// <param name="rad">弧度值</param>
        /// <returns>对应的角度值（°）</returns>
        public double RadToAngle(double rad)
        {
            return rad * 180 / Math.PI;
        }

        /// <summary>
        /// 角度转换为弧度
        /// </summary>
        /// <param name="angle">角度值（°）</param>
        /// <returns>对应的弧度值</returns>
        public double AngleToRad(double angle)
        {
            return angle * Math.PI / 180;
        }

        #endregion

        #region 向量运算

        /// <summary>
        /// 计算两点构成的向量（从点b指向点a）
        /// </summary>
        /// <param name="a">向量终点</param>
        /// <param name="b">向量起点</param>
        /// <returns>向量（a.X - b.X, a.Y - b.Y）</returns>
        public PointD TwoPointToVector(PointD a, PointD b)
        {
            return new PointD(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        /// 计算两个二维向量的叉积（结果为标量，反映向量的旋转方向）
        /// </summary>
        /// <param name="A">向量A</param>
        /// <param name="B">向量B</param>
        /// <returns>叉积结果：A.X*B.Y - B.X*A.Y</returns>
        public double VectorCrossProduct(PointD A, PointD B)
        {
            // 二维向量叉积的标量计算，结果的正负表示A相对于B的旋转方向（正为逆时针，负为顺时针）
            return A.X * B.Y - B.X * A.Y;
        }

        /// <summary>
        /// 计算两个二维向量的点积（结果为标量，反映向量的夹角）
        /// </summary>
        /// <param name="A">向量A</param>
        /// <param name="B">向量B</param>
        /// <returns>点积结果：A.X*B.X + A.Y*B.Y</returns>
        public double VectorDotProduct(PointD A, PointD B)
        {
            // 点积 = |A|·|B|·cosθ，θ为两向量夹角
            return A.X * B.X + A.Y * B.Y;
        }

        /// <summary>
        /// 计算两个向量的夹角（考虑方向，返回带符号的角度）
        /// </summary>
        /// <param name="A">向量A</param>
        /// <param name="B">向量B</param>
        /// <returns>夹角角度（°）：正为逆时针，负为顺时针，范围(-180, 180]</returns>
        public double VectorInculedAngle(PointD A, PointD B)
        {
            double resultAngle = 0;

            // 处理零向量（避免除以零）
            if ((Math.Round(A.X, 3) == 0 && Math.Round(A.Y, 3) == 0)
                || (Math.Round(B.X, 3) == 0 && Math.Round(B.Y, 3) == 0))
            {
                return resultAngle;
            }

            // 计算点积
            double abPoint = VectorDotProduct(A, B);
            // 计算两向量的模长乘积
            double abLength = Math.Sqrt(TwoSquare(A.X, A.Y)) * Math.Sqrt(TwoSquare(B.X, B.Y));
            // 计算夹角的余弦值（cosθ = 点积/(|A|·|B|)）
            double cos = abPoint / abLength;

            // 修正余弦值范围（避免浮点误差导致超出[-1,1]）
            if (cos > 1) cos = 1;
            else if (cos < -1) cos = -1;

            // 计算角度（弧度转角度）
            resultAngle = RadToAngle(Math.Acos(cos));

            // 利用叉积判断方向：叉积为负时，夹角为顺时针（取负值）
            double abCross = VectorCrossProduct(A, B);
            if (abCross < 0)
            {
                resultAngle = -resultAngle;
            }

            return resultAngle;
        }

        /// <summary>
        /// 计算两个向量的夹角（可指定顺时针/逆时针方向，返回对应范围的角度）
        /// </summary>
        /// <param name="A">向量A</param>
        /// <param name="B">向量B</param>
        /// <param name="isCw">是否按顺时针计算：true返回[-360,0)，false返回[0,360)</param>
        /// <returns>指定方向的夹角角度（°）</returns>
        public double VectorInculedAngle(PointD A, PointD B, bool isCw)
        {
            double resultAngle = VectorInculedAngle(A, B);  // 先计算基础夹角

            // 调整角度范围以符合指定方向
            if (!isCw)
            {
                // 逆时针方向：角度范围[0, 360)
                if (resultAngle < 0)
                {
                    resultAngle = 360 + resultAngle;
                }
            }
            else
            {
                // 顺时针方向：角度范围[-360, 0)
                if (resultAngle > 0)
                {
                    resultAngle = resultAngle - 360;
                }
            }

            return resultAngle;
        }

        /// <summary>
        /// 勾股定理计算（已知两边求第三边）
        /// </summary>
        /// <param name="l1">边1</param>
        /// <param name="l2">边2</param>
        /// <param name="isL1Long">是否边1为斜边：true则求直角边，false则求斜边</param>
        /// <returns>计算得到的第三边长度</returns>
        public double PythagoraranTheorem(double l1, double l2, bool isL1Long)
        {
            double l3 = 0;
            if (isL1Long)
            {
                // 边1为斜边：l3 = √(l1² - l2²)
                l3 = Math.Sqrt(Math.Pow(l1, 2) - Math.Pow(l2, 2));
            }
            else
            {
                // 求斜边：l3 = √(l1² + l2²)
                l3 = Math.Sqrt(Math.Pow(l1, 2) + Math.Pow(l2, 2));
            }

            return l3;
        }

        #endregion

        #region 一元二次方程求解

        /// <summary>
        /// 求解一元二次方程ax² + bx + c = 0的实根
        /// </summary>
        /// <param name="a">二次项系数</param>
        /// <param name="b">一次项系数</param>
        /// <param name="c">常数项</param>
        /// <returns>实根列表：两个不同实根/一个重根/空列表（无实根）</returns>
        public List<double> RootEqution(double a, double b, double c)
        {
            List<double> resultList = new List<double>();
            if (a == 0)
            {
                // 不是二次方程，直接返回空
                return resultList;
            }

            // 计算判别式Δ = b² - 4ac
            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0)
            {
                // 无实根
                return resultList;
            }

            // 求根公式：x = [-b ± √Δ]/(2a)
            double sqrtD = Math.Sqrt(discriminant);
            double root1 = (-b + sqrtD) / (2 * a);
            resultList.Add(root1);

            // 若Δ≠0，添加第二个根
            if (discriminant != 0)
            {
                double root2 = (-b - sqrtD) / (2 * a);
                resultList.Add(root2);
            }

            return resultList;
        }

        #endregion

        #endregion

        #region 私有方法（辅助计算）

        /// <summary>
        /// 计算点相对于中心点的象限角度（与X轴正方向的顺时针夹角，范围[0,360)）
        /// </summary>
        /// <param name="p">目标点</param>
        /// <param name="center">中心点（原点）</param>
        /// <param name="slope">点与中心点连线的斜率</param>
        /// <returns>角度值（°），从X轴正方向顺时针计算</returns>
        private double AngelQuadrant(PointD p, PointD center, double slope)
        {
            double angle = 0;
            // 计算点相对于中心点的偏移量
            double scaleX = p.X - center.X;  // X方向偏移（正为右，负为左）
            double scaleY = p.Y - center.Y;  // Y方向偏移（正为上，负为下）

            // 根据象限计算角度
            if (scaleX > 0 && scaleY > 0)
            {
                // 第一象限：角度 = arctan(k)
                angle = Math.Abs(RadToAngle(Math.Atan(slope)));
            }
            else if (scaleX < 0 && scaleY > 0)
            {
                // 第二象限：角度 = 180° - arctan(k)
                angle = 180 - Math.Abs(RadToAngle(Math.Atan(slope)));
            }
            else if (scaleX < 0 && scaleY < 0)
            {
                // 第三象限：角度 = 180° + arctan(k)
                angle = 180 + Math.Abs(RadToAngle(Math.Atan(slope)));
            }
            else if (scaleX > 0 && scaleY < 0)
            {
                // 第四象限：角度 = 360° - arctan(k)
                angle = 360 - Math.Abs(RadToAngle(Math.Atan(slope)));
            }

            // 特殊位置（坐标轴上）
            if (scaleX == 0 && scaleY > 0) angle = 90;    // 正Y轴
            if (scaleY == 0 && scaleX > 0) angle = 0;     // 正X轴
            if (scaleX == 0 && scaleY < 0) angle = 270;   // 负Y轴
            if (scaleY == 0 && scaleX < 0) angle = 180;   // 负X轴

            return angle;
        }

        #endregion
    }

    /// <summary>
    /// 二维点结构体（双精度坐标）
    /// </summary>
    public struct PointD
    {
        /// <summary>
        /// X坐标
        /// </summary>
        public double X;
        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y;

        /// <summary>
        /// 初始化二维点
        /// </summary>
        /// <param name="X">X坐标</param>
        /// <param name="Y">Y坐标</param>
        public PointD(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        /// <summary>
        /// 重写ToString，返回点的字符串表示（如"(1.2,3.4)"）
        /// </summary>
        /// <returns>点的坐标字符串</returns>
        public override string ToString()
        {
            return $"({X},{Y})";
        }
    }

    /// <summary>
    /// 直线类，包含直线上的两点、距离和斜率
    /// </summary>
    public class FigueLine
    {
        #region 私有属性
        /// <summary>
        /// 两点之间的距离
        /// </summary>
        private double _Distance;
        /// <summary>
        /// 直线上的点A
        /// </summary>
        private PointD _A;
        /// <summary>
        /// 直线上的点B
        /// </summary>
        private PointD _B;
        /// <summary>
        /// 直线的斜率（空列表表示斜率不存在，即垂直于X轴）
        /// </summary>
        private List<double> _Slope;
        #endregion

        #region 公共访问器
        /// <summary>
        /// 直线上两点A和B之间的距离
        /// </summary>
        public double Distance
        {
            get { return _Distance; }
            set { _Distance = value; }
        }

        /// <summary>
        /// 直线上的点A（用于定义直线位置）
        /// </summary>
        public PointD A
        {
            get { return _A; }
            set { _A = value; }
        }

        /// <summary>
        /// 直线上的点B（用于定义直线位置）
        /// </summary>
        public PointD B
        {
            get { return _B; }
            set { _B = value; }
        }

        /// <summary>
        /// 直线的斜率：若包含一个元素则为斜率值，空列表表示斜率不存在（垂直直线）
        /// </summary>
        public List<double> Slope
        {
            get { return _Slope; }
            set { _Slope = value; }
        }
        #endregion
    }

    /// <summary>
    /// 圆类，包含圆心和半径
    /// </summary>
    public class FigueCircule
    {
        #region 私有属性
        /// <summary>
        /// 圆心坐标
        /// </summary>
        private PointD _Center;
        /// <summary>
        /// 圆的半径
        /// </summary>
        private double _Radius;
        #endregion

        #region 公共访问器
        /// <summary>
        /// 圆的中心点坐标
        /// </summary>
        public PointD Center
        {
            get { return _Center; }
            set { _Center = value; }
        }

        /// <summary>
        /// 圆的半径长度
        /// </summary>
        public double Radius
        {
            get { return _Radius; }
            set { _Radius = value; }
        }
        #endregion
    }

    /// <summary>
    /// 圆弧类，包含圆心、半径、起始角度和结束角度
    /// </summary>
    public class FigureArc
    {
        #region 私有属性
        /// <summary>
        /// 圆弧所在圆的圆心坐标
        /// </summary>
        private PointD _Center;
        /// <summary>
        /// 圆弧的半径
        /// </summary>
        private double _Radius;
        /// <summary>
        /// 圆弧起点与圆心连线和X轴的夹角（角度制）
        /// </summary>
        private double _StartAngle;
        /// <summary>
        /// 圆弧终点与圆心连线和X轴的夹角（角度制）
        /// </summary>
        private double _EndAngle;
        #endregion

        #region 公共访问器
        /// <summary>
        /// 圆弧所在圆的中心点坐标
        /// </summary>
        public PointD Center
        {
            get { return _Center; }
            set { _Center = value; }
        }

        /// <summary>
        /// 圆弧的半径长度
        /// </summary>
        public double Radius
        {
            get { return _Radius; }
            set { _Radius = value; }
        }

        /// <summary>
        /// 圆弧起点对应的角度（与X轴正方向的夹角，角度制）
        /// </summary>
        public double StartAngle
        {
            get { return _StartAngle; }
            set { _StartAngle = value; }
        }

        /// <summary>
        /// 圆弧终点对应的角度（与X轴正方向的夹角，角度制）
        /// </summary>
        public double EndAngle
        {
            get { return _EndAngle; }
            set { _EndAngle = value; }
        }
        #endregion
    }
}