using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 数学计算公式类
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// CRC校验表
        /// </summary>
        private static byte[] aucCRCHi = new byte[] {
            0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41, 1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40,
            1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40, 0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41,
            1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40, 0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41,
            0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41, 1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40,
            1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40, 0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41,
            0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41, 1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40,
            0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41, 1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40,
            1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40, 0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41,
            1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40, 0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41,
            0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41, 1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40,
            0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41, 1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40,
            1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40, 0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41,
            0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41, 1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40,
            1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40, 0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41,
            1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40, 0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41,
            0, 0xc1, 0x81, 0x40, 1, 0xc0, 0x80, 0x41, 1, 0xc0, 0x80, 0x41, 0, 0xc1, 0x81, 0x40
         };
        /// <summary>
        /// CRC校验表
        /// </summary>
        private static byte[] aucCRCLo = new byte[] {
            0, 0xc0, 0xc1, 1, 0xc3, 3, 2, 0xc2, 0xc6, 6, 7, 0xc7, 5, 0xc5, 0xc4, 4,
            0xcc, 12, 13, 0xcd, 15, 0xcf, 0xce, 14, 10, 0xca, 0xcb, 11, 0xc9, 9, 8, 200,
            0xd8, 0x18, 0x19, 0xd9, 0x1b, 0xdb, 0xda, 0x1a, 30, 0xde, 0xdf, 0x1f, 0xdd, 0x1d, 0x1c, 220,
            20, 0xd4, 0xd5, 0x15, 0xd7, 0x17, 0x16, 0xd6, 210, 0x12, 0x13, 0xd3, 0x11, 0xd1, 0xd0, 0x10,
            240, 0x30, 0x31, 0xf1, 0x33, 0xf3, 0xf2, 50, 0x36, 0xf6, 0xf7, 0x37, 0xf5, 0x35, 0x34, 0xf4,
            60, 0xfc, 0xfd, 0x3d, 0xff, 0x3f, 0x3e, 0xfe, 250, 0x3a, 0x3b, 0xfb, 0x39, 0xf9, 0xf8, 0x38,
            40, 0xe8, 0xe9, 0x29, 0xeb, 0x2b, 0x2a, 0xea, 0xee, 0x2e, 0x2f, 0xef, 0x2d, 0xed, 0xec, 0x2c,
            0xe4, 0x24, 0x25, 0xe5, 0x27, 0xe7, 230, 0x26, 0x22, 0xe2, 0xe3, 0x23, 0xe1, 0x21, 0x20, 0xe0,
            160, 0x60, 0x61, 0xa1, 0x63, 0xa3, 0xa2, 0x62, 0x66, 0xa6, 0xa7, 0x67, 0xa5, 0x65, 100, 0xa4,
            0x6c, 0xac, 0xad, 0x6d, 0xaf, 0x6f, 110, 0xae, 170, 0x6a, 0x6b, 0xab, 0x69, 0xa9, 0xa8, 0x68,
            120, 0xb8, 0xb9, 0x79, 0xbb, 0x7b, 0x7a, 0xba, 190, 0x7e, 0x7f, 0xbf, 0x7d, 0xbd, 0xbc, 0x7c,
            180, 0x74, 0x75, 0xb5, 0x77, 0xb7, 0xb6, 0x76, 0x72, 0xb2, 0xb3, 0x73, 0xb1, 0x71, 0x70, 0xb0,
            80, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92, 150, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54,
            0x9c, 0x5c, 0x5d, 0x9d, 0x5f, 0x9f, 0x9e, 0x5e, 90, 0x9a, 0x9b, 0x5b, 0x99, 0x59, 0x58, 0x98,
            0x88, 0x48, 0x49, 0x89, 0x4b, 0x8b, 0x8a, 0x4a, 0x4e, 0x8e, 0x8f, 0x4f, 0x8d, 0x4d, 0x4c, 140,
            0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 70, 0x86, 130, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80, 0x40
         };

        /// <summary>
        /// 获取Modbus CRC16
        /// </summary>
        /// <param name="data">计算校验的数据</param>
        /// <param name="startIndex">起始索引</param> 
        /// 
        /// <param name="length">长度</param>
        /// <returns></returns>
        public static ushort CRC16_Modbus(byte[] data, int startIndex = 0, int length = -1)
        {
            byte num = 0xff;
            byte num2 = 0xff;
            int index = 0;
            if (length == -1)
            {
                length = data.Length - startIndex;
            }
            for (int i = 0; i < length; i++)
            {
                index = num2 ^ data[startIndex + i];
                num2 = (byte)(num ^ aucCRCHi[index]);
                num = aucCRCLo[index];
            }
            return (ushort)((num << 8) | num2);
        }

        /// <summary>
        /// 平面度--平面一侧各取实测点z数值到拟合平面z的理论数值的最大距离
        /// </summary>
        /// <param name="coefficient">拟合平面系数</param>
        /// <param name="x">被测点x数组</param>
        /// <param name="y">被测点y数组</param>
        /// <param name="z">被测点z数组</param>
        /// <returns>平面度</returns>
        public static float Flatness(float[] coefficient, float[] x, float[] y, float[] z)
        {
            float[] numArray = new float[z.Length];
            List<float> list = new List<float>();
            for (int i = 0; i < z.Length; i++)
            {
                numArray[i] = (((coefficient[0] * x[i]) + (coefficient[1] * y[i])) - z[i]) + coefficient[2];
                list.Add(Math.Abs(numArray[i]) / Convert.ToSingle(Math.Sqrt((double)(((coefficient[0] * coefficient[0]) + (coefficient[1] * coefficient[1])) + 1f))));
            }
            list.Sort();
            return (list[list.Count - 1] - list[0]);
        }

        /// <summary>
        /// 点到面的高度
        /// </summary>
        /// <param name="coefficient"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static float HeightZ(float[] coefficient, float x, float y, float z)
        {
            float num = (((coefficient[0] * x) + (coefficient[1] * y)) - z) + coefficient[2];
            return (num / Convert.ToSingle(Math.Sqrt((double)(((coefficient[0] * coefficient[0]) + (coefficient[1] * coefficient[1])) + 1f))));
        }

        /// <summary>
        /// 求拟合平面
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        /// <param name="z">z坐标</param>
        /// <param name="docCount">计算点数</param>
        /// <return name="a">x系数、y系数、常数</return>
        public static float[] Plane(float[] x, float[] y, float[] z, int docCount)
        {
            float[] numArray = new float[3];
            if (docCount >= 3)
            {
                float num = 0f;
                float num2 = 0f;
                float num3 = 0f;
                float num4 = 0f;
                float num5 = 0f;
                float num6 = 0f;
                float num7 = 0f;
                float num8 = 0f;
                float num9 = 0f;
                for (int i = 0; i < docCount; i++)
                {
                    num += x[i];
                    num2 += y[i];
                    num3 += z[i];
                    num4 += x[i] * x[i];
                    num5 += y[i] * y[i];
                    num6 += z[i] * z[i];
                    num7 += x[i] * y[i];
                    num8 += x[i] * z[i];
                    num9 += y[i] * z[i];
                }
                float num10 = ((((num4 * num5) * docCount) + ((num7 * num2) * num)) + ((num7 * num2) * num)) - ((((num * num5) * num) + ((num7 * num7) * docCount)) + ((num2 * num2) * num4));
                float num11 = ((((num8 * num5) * docCount) + ((num7 * num2) * num3)) + ((num9 * num2) * num)) - ((((num * num5) * num3) + ((num7 * num9) * docCount)) + ((num2 * num2) * num8));
                float num12 = ((((num4 * num9) * docCount) + ((num8 * num2) * num)) + ((num7 * num3) * num)) - ((((num * num9) * num) + ((num8 * num7) * docCount)) + ((num2 * num3) * num4));
                float num13 = ((((num4 * num5) * num3) + ((num7 * num9) * num)) + ((num7 * num2) * num8)) - ((((num8 * num5) * num) + ((num7 * num7) * num3)) + ((num9 * num2) * num4));
                numArray[0] = num11 / num10;
                numArray[1] = num12 / num10;
                numArray[2] = num13 / num10;
            }
            return numArray;
        }

        /// <summary>
        /// 2个面的夹角角度
        /// </summary>
        /// <param name="coefficientA"></param>
        /// <param name="coefficientB"></param>
        /// <returns>角度</returns>
        public static float PlaneAngle(float[] coefficientA, float[] coefficientB)
        {
            double num = Math.Sqrt((double)(((coefficientA[0] * coefficientA[0]) + (coefficientA[1] * coefficientA[1])) + 1f));
            double num2 = Math.Sqrt((double)(((coefficientB[0] * coefficientB[0]) + (coefficientB[1] * coefficientB[1])) + 1f));
            double d = ((double)Math.Abs((float)(((coefficientA[0] * coefficientB[0]) + (coefficientA[1] * coefficientB[1])) + 1f))) / (num * num2);
            return Convert.ToSingle((double)((Math.Acos(d) * 180.0) / 3.1415926535897931));
        }

        /// <summary>
        /// 厚度--z坐标相加
        /// </summary>
        /// <param name="z1"></param>
        /// <param name="z2"></param>
        /// <returns></returns>
        public static float Thickness(float z1, float z2)
        {
            return (z1 + z2);
        }


        #region 标定计算
        /// <summary>
        /// 坐标转换关系计算(3行2列的矩阵)
        /// </summary>
        /// <param name="imageXLst">A坐标X</param>
        /// <param name="imagey">A坐标Y</param>
        /// <param name="worldx">B坐标X</param>
        /// <param name="worldYLst">B坐标Y</param>
        /// <param name="tranformMatrix">转换矩阵</param>
        /// <returns></returns>
        public static bool coorTransformMatrixCal(List<double> imageXLst, List<double> imagey, List<double> worldx, List<double> worldYLst, out double[,] tranformMatrix)
        {
            tranformMatrix = new double[3, 2];

            // 判断是否为空
            if (imageXLst == null || imagey == null || worldx == null || worldYLst == null)
                return false;

            // 判断个数是否一致
            if (imageXLst.Count != imagey.Count || imagey.Count != worldx.Count || worldx.Count != worldYLst.Count)
                return false;

            // 最少要2个点
            if (imageXLst.Count < 2)
                return false;

            // 转换成矩阵
            double[,] xy_array = new double[3, imageXLst.Count];
            double[,] XY_array = new double[2, imageXLst.Count];
            for (int i = 0; i < imageXLst.Count; i++)
            {
                xy_array[0, i] = imageXLst[i];
                xy_array[1, i] = imagey[i];
                xy_array[2, i] = 1;

                XY_array[0, i] = worldx[i];
                XY_array[1, i] = worldYLst[i];
            }
            var xy_matrix = MathNet.Numerics.LinearAlgebra.CreateMatrix.DenseOfArray(xy_array);
            var XY_matrix = MathNet.Numerics.LinearAlgebra.CreateMatrix.DenseOfArray(XY_array);

            // 只有两对点时
            if (xy_array.Length == 6)
            {
                double dx = xy_array[0, 1] - xy_array[0, 0];
                double dy = xy_array[1, 1] - xy_array[1, 0];
                double dX = XY_array[0, 1] - XY_array[0, 0];
                double dY = XY_array[1, 1] - XY_array[1, 0];
                double a = Math.Sqrt(dX * dX + dY * dY) / (Math.Sqrt(dx * dx + dy * dy) + 0.000000001);
                double b = Math.Atan2(dy, dx) - Math.Atan2(dY, dX);
                double a11 = a * Math.Cos(b);
                double a12 = a * Math.Sin(b);
                double a21 = -a12;
                double a22 = a11;
                double tx = (XY_array[0, 1] + XY_array[0, 0]) / 2 - a11 * (xy_array[0, 1] + xy_array[0, 0]) / 2 - a12 * (xy_array[1, 1] + xy_array[1, 0]) / 2;
                double ty = (XY_array[1, 1] + XY_array[1, 0]) / 2 - a21 * (xy_array[0, 1] + xy_array[0, 0]) / 2 - a22 * (xy_array[1, 1] + xy_array[1, 0]) / 2;
                //double[,] result_array = { { a11, a12, tx }, { a21, a22, ty } };
                //return result_array;

                tranformMatrix[0, 0] = a11;
                tranformMatrix[1, 0] = a12;
                tranformMatrix[2, 0] = tx;
                tranformMatrix[0, 1] = a21;
                tranformMatrix[1, 1] = a22;
                tranformMatrix[2, 1] = ty;

                return true;
            }
            // 多对点
            else
            {
                var result_matrix = xy_matrix.Transpose().QR().Solve(XY_matrix.Transpose());
                double[,] result_array = result_matrix.Transpose().ToArray();
                //return result_array;

                tranformMatrix[0, 0] = result_array[0, 0];
                tranformMatrix[1, 0] = result_array[0, 1];
                tranformMatrix[2, 0] = result_array[0, 2];
                tranformMatrix[0, 1] = result_array[1, 0];
                tranformMatrix[1, 1] = result_array[1, 1];
                tranformMatrix[2, 1] = result_array[1, 2];

                return true;
            }
        }

        /// <summary>
        /// 坐标转换映射(由像素坐标计算物理坐标)
        /// </summary>
        /// <param name="imageXLst">A坐标X</param>
        /// <param name="imagey">A坐标Y</param>
        /// <param name="tranformMatrix">A坐标系到B坐标系的转换矩阵</param>
        /// <param name="worldx">B坐标系X</param>
        /// <param name="worldYLst">B坐标系Y</param>
        public static void coorTransformMapCal(double imageXLst, double imagey, double[,] tranformMatrix, out double worldx, out double worldYLst)
        {
            worldx = new double();
            worldYLst = new double();

            double[,] imageMatrix = { { imageXLst, imagey, 1 } };
            var inputMatrix = MathNet.Numerics.LinearAlgebra.CreateMatrix.DenseOfArray(imageMatrix);
            var multiplierMatrix = MathNet.Numerics.LinearAlgebra.CreateMatrix.DenseOfArray(tranformMatrix);
            double[,] result_array = (inputMatrix * multiplierMatrix).ToArray();
            worldx = result_array[0, 0];
            worldYLst = result_array[0, 1];

            //worldYLst = imageXLst * tranformMatrix[0, 0] + imagey * tranformMatrix[1, 0] + tranformMatrix[2, 0];
            //worldYLst = imageXLst * tranformMatrix[0, 1] + imagey * tranformMatrix[1, 1] + tranformMatrix[2, 1];
        }

        /// <summary>
        /// 坐标转换反映射(由物理坐标计算像素坐标)
        /// </summary>
        /// <param name="imageXLst">B坐标X</param>
        /// <param name="imagey">B坐标Y</param>
        /// <param name="tranformMatrix">A坐标系到B坐标系的转换矩阵</param>
        /// <param name="worldx">A坐标系X</param>
        /// <param name="worldYLst">A坐标系Y</param>
        public static void coorTransformUnMapCal(double worldx, double worldYLst, double[,] tranformMatrix, out double imageXLst, out double imagey)
        {
            imageXLst = new double();
            imagey = new double();

            double a = tranformMatrix[0, 0];
            double b = tranformMatrix[1, 0];
            double x0 = tranformMatrix[2, 0];
            double c = tranformMatrix[0, 1];
            double d = tranformMatrix[1, 1];
            double y0 = tranformMatrix[2, 1];

            imageXLst = (d * worldx - b * worldYLst - d * x0 + b * y0) / (a * d - b * c);
            imagey = (c * worldx - a * worldYLst - c * x0 + a * y0) / (b * c - a * d);
        }

        /// <summary>
        /// 坐标转换偏差计算(只计算偏差值不考虑平移值)
        /// </summary>
        /// <param name="imageXLst">A坐标X</param>
        /// <param name="imagey">A坐标Y</param>
        /// <param name="tranformMatrix">A坐标系到B坐标系的转换矩阵</param>
        /// <param name="offWorldx">B坐标X偏差</param>
        /// <param name="offworldYLst">B坐标Y偏差</param>
        public static void coorTransformOffCal(double imageXLst, double imagey, double[,] tranformMatrix, out double offWorldx, out double offworldYLst)
        {
            offWorldx = new double();
            offworldYLst = new double();

            offWorldx = imageXLst * tranformMatrix[0, 0] + imagey * tranformMatrix[1, 0];
            offworldYLst = imageXLst * tranformMatrix[0, 1] + imagey * tranformMatrix[1, 1];
        }

        /// <summary>
        /// 两个坐标系对齐(联合标定对齐)
        /// </summary>
        /// <param name="srcDestAlignInput">对齐输入数据</param>
        /// <param name="offValX">补偿量X</param>
        /// <param name="offValY">补偿量Y</param>
        /// <param name="offValAngle">补偿量角度</param>
        public static void coorAlign(unionAlignInput srcDestAlignInput, out double offValX, out double offValY, out double offValAngle)
        {
            offValX = new double();
            offValY = new double();
            offValAngle = new double();

            // 1. 计算旋转角度(应该再减去两者角度差值)
            double predict_angle = srcDestAlignInput.destImageAngle - srcDestAlignInput.srcImageAngle - srcDestAlignInput.srcDestAngle;

            // 2. 预判旋转后的位置(在源上自行旋转)
            double srcCenterOffX = srcDestAlignInput.srcimageXLst - srcDestAlignInput.srcRotCenterX;
            double srcCenterOffY = srcDestAlignInput.srcImageY - srcDestAlignInput.srcRotCenterY;
            double srcRotAngle = MathNet.Numerics.Trig.DegreeToRadian(-predict_angle);
            double srcCenterOffX2 = srcCenterOffX * Math.Cos(srcRotAngle) - srcCenterOffY * Math.Sin(srcRotAngle);
            double srcCenterOffY2 = srcCenterOffX * Math.Sin(srcRotAngle) + srcCenterOffY * Math.Cos(srcRotAngle);
            double srcRotAfterX = srcCenterOffX2 + srcDestAlignInput.srcRotCenterX;
            double srcRotAfterY = srcCenterOffY2 + srcDestAlignInput.srcRotCenterY;

            // 3. 将源映射到目标的位置
            double srcRotMapX, srcRotMapY;
            coorTransformMapCal(srcRotAfterX, srcRotAfterY, srcDestAlignInput.srcToDestMatrix, out srcRotMapX, out srcRotMapY);

            // 4. 在目标像素坐标系中位置偏差
            double destSrcOffX = srcDestAlignInput.destimageXLst - srcRotMapX;
            double destSrcOffY = srcDestAlignInput.destImageY - srcRotMapY;

            // 5. 将像素偏差转换为物理偏差
            double destWorldOffX, destWorldOffY;
            coorTransformMapCal(destSrcOffX, destSrcOffY, srcDestAlignInput.destToWorldMatrix, out destWorldOffX, out destWorldOffY);

            // 6. 传递参数
            offValX = destWorldOffX;
            offValY = destWorldOffY;
            offValAngle = predict_angle;
        }

        /// <summary>
        /// 两个坐标系对齐(分别标定对齐)
        /// </summary>
        /// <param name="srcDestAlignInput">对齐输入数据</param>
        /// <param name="offValX">补偿量X</param>
        /// <param name="offValY">补偿量Y</param>
        /// <param name="offValAngle">补偿量角度</param>
        public static void coorAlign(aloneAlignInput srcDestAlignInput, out double offValX, out double offValY, out double offValAngle)
        {
            offValX = new double();
            offValY = new double();
            offValAngle = new double();

            // 1. 计算旋转角度(应该再减去两者角度差值)
            double predict_angle = srcDestAlignInput.destImageAngle - srcDestAlignInput.srcImageAngle + (srcDestAlignInput.destGoodAngle - srcDestAlignInput.srcGoodAngle);

            // 2. 预判旋转后的位置(在源上自行旋转)
            double srcCenterOffX = srcDestAlignInput.srcimageXLst - srcDestAlignInput.srcRotCenterX;
            double srcCenterOffY = srcDestAlignInput.srcImageY - srcDestAlignInput.srcRotCenterY;
            double srcRotAngle = MathNet.Numerics.Trig.DegreeToRadian(-predict_angle);
            double srcCenterOffX2 = srcCenterOffX * Math.Cos(srcRotAngle) - srcCenterOffY * Math.Sin(srcRotAngle);
            double srcCenterOffY2 = srcCenterOffX * Math.Sin(srcRotAngle) + srcCenterOffY * Math.Cos(srcRotAngle);
            double srcRotAfterX = srcCenterOffX2 + srcDestAlignInput.srcRotCenterX;
            double srcRotAfterY = srcCenterOffY2 + srcDestAlignInput.srcRotCenterY;

            // 3. 源端的位置偏差
            double srcWorldOffX, srcWorldOffY;
            coorTransformOffCal(srcDestAlignInput.srcGoodX - srcRotAfterX, srcDestAlignInput.srcGoodY - srcRotAfterY,
                srcDestAlignInput.srcToWorldMatrix,
                out srcWorldOffX, out srcWorldOffY);

            // 4. 目标端的位置偏差
            double destWorldOffX, destWorldOffY;
            coorTransformOffCal(srcDestAlignInput.destGoodX - srcDestAlignInput.destimageXLst, srcDestAlignInput.destGoodY - srcDestAlignInput.destImageY,
                srcDestAlignInput.destToWorldMatrix,
                out destWorldOffX, out destWorldOffY);

            // 5. 传递参数
            offValX = destWorldOffX - srcWorldOffX;
            offValY = destWorldOffY - srcWorldOffY;
            offValAngle = predict_angle;
        }
        #endregion

        #region 点运算
        /// <summary>
        /// 根据两点计算刚性仿射变换, 再计算输入点变换后点位
        /// </summary>
        /// <param name="initx">基准点x</param>
        /// <param name="inity">基准点y</param>
        /// <param name="inita">基准点角度</param>
        /// <param name="runx">运行点x</param>
        /// <param name="runy">运行点y</param>
        /// <param name="runa">运行点角度</param>
        /// <param name="pointx">输入计算点x</param>
        /// <param name="pointy">输入计算点y</param>
        /// <param name="pointa">输入计算点角度</param>
        /// <param name="calPointx">输出变换点x</param>
        /// <param name="calPointy">输出变换点y</param>
        /// <param name="calPointa">输出变换点角度</param>
        public static void Vector_Angle_Affine(double initx, double inity, double inita, double runx, double runy, double runa, double pointx, double pointy, double pointa, out double calPointx, out double calPointy, out double calPointa)
        {
            // 1. 初始化返回值
            calPointx = 0.0;
            calPointy = 0.0;
            calPointa = 0.0;

            // 2. 根据两点计算仿射变换矩阵
            double[] matrix = new double[6];
            double angle = -(runa - inita);
            matrix[0] = Math.Cos(angle);
            matrix[1] = -Math.Sin(angle);
            matrix[2] = -initx * Math.Cos(angle) + inity * Math.Sin(angle) + runx;
            matrix[3] = Math.Sin(angle);
            matrix[4] = Math.Cos(angle);
            matrix[5] = -initx * Math.Sin(angle) - inity * Math.Cos(angle) + runy;

            // 3. 根据变换矩阵计算输出点
            calPointx = pointx * matrix[0] + pointy * matrix[1] + matrix[2];
            calPointy = pointx * matrix[3] + pointy * matrix[4] + matrix[5];
            calPointa = MathNet.Numerics.Trig.RadianToDegree(pointa + angle);
        }

        /// <summary>
        /// 根据两点计算刚性仿射变换, 再计算输入点变换后点位
        /// </summary>
        /// <param name="initx">基准点x</param>
        /// <param name="inity">基准点y</param>
        /// <param name="inita">基准点角度</param>
        /// <param name="runx">运行点x</param>
        /// <param name="runy">运行点y</param>
        /// <param name="runa">运行点角度</param>
        /// <param name="pointx">输入计算点x</param>
        /// <param name="pointy">输入计算点y</param>
        /// <param name="pointa">输入计算点角度</param>
        /// <param name="calPointx">输出变换点x</param>
        /// <param name="calPointy">输出变换点y</param>
        /// <param name="calPointa">输出变换点角度</param>
        public static void Vector_Affine(double initx, double inity, double inita, double runx, double runy, double runa, double pointx, double pointy, double pointa, out double calPointx, out double calPointy, out double calPointa)
        {
            calPointx = pointx + runx - initx;
            calPointy = pointy + runy - inity;
            calPointa = pointa + runa - inita;
        }

        /// <summary>
        /// 向量相乘
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static double Points_Vector_Mul(vectorPoint a1, vectorPoint a2, vectorPoint b1, vectorPoint b2)
        {
            vectorPoint v1 = new vectorPoint(a2.x - a1.x, a2.y - a1.y);
            vectorPoint v2 = new vectorPoint(b2.x - b1.x, b2.y - b1.y);
            return v1.x * v2.y - v1.y * v2.x;
        }

        /// <summary>
        /// 两个点--计算中点
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static vectorPoint Points_Two_Middle_Point(vectorPoint p1, vectorPoint p2)
        {
            vectorPoint mp = new vectorPoint((p2.x + p1.x) / 2.0, (p1.y + p2.y) / 2.0);
            return mp;
        }

        /// <summary>
        /// 两个点--计算间距
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double Points_Two_Dis(vectorPoint p1, vectorPoint p2)
        {
            return Math.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
        }

        /// <summary>
        /// 两个点--计算间距平方
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double Points_Two_Dis_Pow2(vectorPoint p1, vectorPoint p2)
        {
            return (p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y);
        }

        /// <summary>
        /// 两个点--计算与X轴正方向的角度
        /// </summary>
        /// <param name="t"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double Points_Two_Angle(vectorPoint t, vectorPoint p)
        {
            return ((t.x - p.x) * (t.x - p.x) + 1 - (t.x - p.x - 1) * (t.x - p.x - 1)) / (2 * Math.Sqrt((t.x - p.x) * (t.x - p.x) + (t.y - p.y) * (t.y - p.y)));
        }

        /// <summary>
        /// 三个点--计算 ∠b2.a.b1 的大小
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static double Points_Triangle_Angle(vectorPoint a, vectorPoint b1, vectorPoint b2)
        {
            double l1 = Points_Two_Dis_Pow2(b1, a);
            double l2 = Points_Two_Dis_Pow2(b2, a);
            double l3 = Points_Two_Dis_Pow2(b1, b2);

            return Math.Acos((l1 + l2 - l3) / (2 * Math.Sqrt(l1 * l2)));
        }

        /// <summary>
        /// 三个点--计算圆心
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static vectorPoint Points_Triangle_Center_Point(vectorPoint a, vectorPoint b, vectorPoint c)
        {
            vectorPoint mp1 = new vectorPoint((b.x + a.x) / 2.0, (b.y + a.y) / 2.0);
            vectorPoint mp2 = new vectorPoint((b.x + c.x) / 2.0, (b.y + c.y) / 2.0);
            vectorPoint v1 = new vectorPoint(a.y - b.y, b.x - a.x);
            vectorPoint v2 = new vectorPoint(c.y - b.y, b.x - c.x);
            vectorPoint p1 = new vectorPoint(mp1.x + v1.x, mp1.y + v1.y);
            vectorPoint p2 = new vectorPoint(mp2.x + v2.x, mp2.y + v2.y);

            return Points_Segment_Cross_Point(mp1, p1, mp2, p2);
        }

        /// <summary>
        /// 四个点--计算交叉点
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static vectorPoint Points_Segment_Cross_Point(vectorPoint a1, vectorPoint a2, vectorPoint b1, vectorPoint b2)
        {
            double t = Points_Vector_Mul(b1, b2, b1, a1) / Points_Vector_Mul(a1, a2, b1, b2);
            vectorPoint cp = new vectorPoint(a1.x + (a2.x - a1.x) * t, a1.y + (a2.y - a1.y) * t);
            return cp;
        }

        /// <summary>
        /// 比较角度
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static bool Points_Cmp_angle(vectorPoint p1, vectorPoint p2)
        {
            return (p1.a < p2.a || (Math.Abs(p1.a - p2.a) < Math.Pow(10, -10) && p1.y < p2.y) || (Math.Abs(p1.a - p2.a) < Math.Pow(10, -10) && Math.Abs(p1.y - p2.y) < Math.Pow(10, -10) && p1.x < p2.x));
        }

        /// <summary>
        /// 创建凸包点集 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int Points_Creat_Convex_Hull(List<vectorPoint> p, int n)
        {
            int i = 0, top = 0;
            //re 为建立极角排序时的参考点  下左点  
            vectorPoint re;
            vectorPoint[] cp = new vectorPoint[1100];

            for (re = p[0], i = 1; i < n; ++i)
            {
                if (re.y > p[i].y || (Math.Abs(re.y - p[i].y) < Math.Pow(10, -10) && re.x > p[i].x))
                    re = p[i];
            }

            for (i = 0; i < n; ++i)
            {
                if (p[i].x == re.x && p[i].y == re.y)
                {
                    p[i].a = -2;
                }
                else
                {
                    p[i].a = Points_Two_Angle(re, p[i]);
                }
            }

            //sort(p,p+n,cmp_angle);  

            top = 0;
            cp[top++] = p[0];
            cp[top++] = p[1];

            for (i = 2; i < n;)
            {
                if (top < 2 || Points_Vector_Mul(cp[top - 2], cp[top - 1], cp[top - 2], p[i]) > Math.Pow(10, -10))
                {
                    cp[top++] = p[i++];
                }
                else
                {
                    --top;
                }
            }

            return top;
        }

        /// <summary>
        /// 判断点是否在圆内
        /// </summary>
        /// <param name="center"></param>
        /// <param name="rad"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool Points_Is_In_Circle(vectorPoint center, double rad, vectorPoint p)
        {
            double dis = Points_Two_Dis(center, p);
            return (dis < rad || Math.Abs(dis - rad) < Math.Pow(10, -10));
        }

        /// <summary>
        /// 判断点是否在线上
        /// </summary>
        /// <param name="m"></param>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns></returns>
        public static bool Points_Is_In_Seg(vectorPoint m, vectorPoint a1, vectorPoint a2)
        {
            double l1 = Points_Two_Dis(m, a1);
            double l2 = Points_Two_Dis(m, a2);
            double l3 = Points_Two_Dis(a1, a2);

            return (Math.Abs(l1 + l2 - l3) < Math.Pow(10, -10));
        }

        /// <summary>
        /// 判断两条直线是否平行
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static bool Points_Is_In_Parallel(vectorPoint a1, vectorPoint a2, vectorPoint b1, vectorPoint b2)
        {
            double xm1 = Points_Vector_Mul(a1, a2, a1, b1);
            double xm2 = Points_Vector_Mul(a1, a2, a1, b2);

            return (Math.Abs(xm1) < Math.Pow(10, -10) && Math.Abs(xm2) < Math.Pow(10, -10));
        }

        /// <summary>
        /// 判断两条直线是否无交点 
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static bool Points_Is_Cross_Without_Point(vectorPoint a1, vectorPoint a2, vectorPoint b1, vectorPoint b2)
        {
            double xm1 = Points_Vector_Mul(a1, a2, a1, b1);
            double xm2 = Points_Vector_Mul(a1, a2, a1, b2);
            double xm3 = Points_Vector_Mul(b1, b2, b1, a1);
            double xm4 = Points_Vector_Mul(b1, b2, b1, a2);

            return (xm1 * xm2 < (-Math.Pow(10, -10)) && xm3 * xm4 < (-Math.Pow(10, -10)));
        }

        /// <summary>
        /// 判断三点是否是可形成三角形
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <returns></returns>
        public static int Points_Is_Acute_Triangle(vectorPoint p1, vectorPoint p2, vectorPoint p3)
        {
            //三点共线  
            if (Math.Abs(Points_Vector_Mul(p1, p2, p1, p3)) < Math.Pow(10, -10))
            {
                return 0;
            }

            //计算三点的夹角
            double a = Points_Triangle_Angle(p1, p2, p3);
            if (a > Math.Acos(-1.0) || Math.Abs(a - Math.Acos(-1.0)) < Math.Pow(10, -10))
            {
                return 0;
            }
            a = Points_Triangle_Angle(p2, p1, p3);
            if (a > Math.Acos(-1.0) || Math.Abs(a - Math.Acos(-1.0)) < Math.Pow(10, -10))
            {
                return 0;
            }
            a = Points_Triangle_Angle(p3, p1, p2);
            if (a > Math.Acos(-1.0) || Math.Abs(a - Math.Acos(-1.0)) < Math.Pow(10, -10))
            {
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// 多点拟合--圆拟合
        /// </summary>
        /// <param name="ptNum"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public static int Points_MultiFit_Circle(int ptNum, List<double> x, List<double> y, out double centerX, out double centerY, out double r)
        {
            double x1 = 0;
            double x2 = 0;
            double x3 = 0;
            double y1 = 0;
            double y2 = 0;
            double y3 = 0;
            double x1y1 = 0;
            double x1y2 = 0;
            double x2y1 = 0;
            double C = 0;
            double D = 0;
            double E = 0;
            double G = 0;
            double H = 0;
            double a = 0;
            double b = 0;
            double c = 0;

            centerX = new double();
            centerY = new double();
            r = new double();

            if ((x == null) || (y == null) || (ptNum < 3))
            {
                return -1;
            }

            for (int i = 0; i < ptNum; i++)
            {
                x1 = x1 + x[i];
                x2 = x2 + x[i] * x[i];
                x3 = x3 + x[i] * x[i] * x[i];
                y1 = y1 + y[i];
                y2 = y2 + y[i] * y[i];
                y3 = y3 + y[i] * y[i] * y[i];
                x1y1 = x1y1 + x[i] * y[i];
                x1y2 = x1y2 + x[i] * y[i] * y[i];
                x2y1 = x2y1 + x[i] * x[i] * y[i];
            }

            C = ptNum * x2 - x1 * x1;
            D = ptNum * x1y1 - x1 * y1;
            E = ptNum * x3 + ptNum * x1y2 - (x2 + y2) * x1;
            G = ptNum * y2 - y1 * y1;
            H = ptNum * x2y1 + ptNum * y3 - (x2 + y2) * y1;

            a = C * G - D * D;
            if ((a <= 0.00000001) && (a >= 0.00000001))
            {
                return -2;
            }
            a = (H * D - E * G) / (C * G - D * D);
            b = (H * C - E * D) / (D * D - C * G);
            c = -(a * x1 + b * y1 + x2 + y2) / ptNum;

            centerX = a / (-2.0);
            centerY = b / (-2.0);
            r = (Math.Sqrt(a * a + b * b - 4 * c)) / 2.0;

            return 0;
        }

        /// <summary>
        /// 多点拟合--最小外接圆拟合
        /// </summary>
        /// <param name="p"></param>
        /// <param name="n"></param>
        public static void Points_MultiFit_MinCircle(List<vectorPoint> p, int n)
        {
            vectorPoint center = new vectorPoint();
            double rad = 0.0;

            if (n == 0)
            {
                return;
            }
            //只有一个点
            else if (n == 1)
            {
                return;
            }
            //只有两个点
            else if (n == 2)
            {
                center.x = (p[1].x + p[0].x) / 2;
                center.y = (p[0].y + p[1].y) / 2;
                rad = Points_Two_Dis(p[0], p[1]) / 2;
                return;
            }

            //超过两个点,先将两点中心设置为圆心
            center = Points_Two_Middle_Point(p[0], p[1]);
            vectorPoint tc1;

            //计算半径
            rad = Points_Two_Dis(p[0], center);
            double dis = -1, temp = 0.0, tra1 = 0.0;
            int a = 0, b = 1, c = 2;
            int i = 0, site = 0, d = 0, s1 = 0, s2 = 0, tp = 0;

            //计算最大半径及标识
            for (dis = -1, site = 0, i = 0; i < n; ++i)
            {
                temp = Points_Two_Dis(center, p[i]);
                if (temp > dis)
                {
                    dis = temp;
                    site = i;
                }
            }

            //若某点不在圆上(不断的调整圆心和最大半径来满足要求)
            while (Points_Is_In_Circle(center, rad, p[site]) == false)
            {
                d = site;

                //计算点距
                double l1 = Points_Two_Dis(p[a], p[d]);
                double l2 = Points_Two_Dis(p[b], p[d]);
                double l3 = Points_Two_Dis(p[c], p[d]);

                //a点距离最远
                if ((l1 > l2 || Math.Abs(l1 - l2) < Math.Pow(10, -10)) && (l1 > l3 || Math.Abs(l1 - l3) < Math.Pow(10, -10)))
                {
                    s1 = a;
                    s2 = d;
                    tp = b;
                }
                //b点距离最远
                else if ((l2 > l1 || Math.Abs(l1 - l2) < Math.Pow(10, -10)) && (l2 > l3 || Math.Abs(l2 - l3) < Math.Pow(10, -10)))
                {
                    s1 = b;
                    s2 = d;
                    tp = c;
                }
                //c点距离最远
                else
                {
                    s1 = c;
                    s2 = d;
                    tp = a;
                }

                //计算中点
                center = Points_Two_Middle_Point(p[s1], p[s2]);

                //计算间距
                rad = Points_Two_Dis(center, p[s1]);

                //其余三个点任一个点不在圆上或者圆内
                if (Points_Is_In_Circle(center, rad, p[a]) == false || Points_Is_In_Circle(center, rad, p[b]) == false || Points_Is_In_Circle(center, rad, p[c]) == false)
                {
                    //计算三角形外界圆形
                    center = Points_Triangle_Center_Point(p[a], p[c], p[d]);
                    rad = Points_Two_Dis(p[d], center);
                    s1 = a;
                    s2 = c;
                    tp = d;

                    //判断点是否在圆内  不在圆内
                    if (Points_Is_In_Circle(center, rad, p[b]) == false)
                    {
                        center = Points_Triangle_Center_Point(p[a], p[b], p[d]);
                        rad = Points_Two_Dis(p[d], center);
                        s1 = a;
                        s2 = b;
                        tp = d;
                    }
                    else
                    {
                        tc1 = Points_Triangle_Center_Point(p[a], p[b], p[d]);
                        tra1 = Points_Two_Dis(p[d], tc1);

                        if (tra1 < rad && Points_Is_In_Circle(tc1, tra1, p[c]))
                        {
                            rad = tra1;
                            center = tc1;
                            s1 = a;
                            s2 = b;
                            tp = d;
                        }
                    }

                    if (Points_Is_In_Circle(center, rad, p[c]) == false)
                    {
                        center = Points_Triangle_Center_Point(p[c], p[b], p[d]);
                        rad = Points_Two_Dis(center, p[d]);
                        s1 = c;
                        s2 = b;
                        tp = d;
                    }
                    else
                    {
                        tc1 = Points_Triangle_Center_Point(p[c], p[b], p[d]);
                        tra1 = Points_Two_Dis(p[d], tc1);
                        if (tra1 < rad && Points_Is_In_Circle(tc1, tra1, p[a]))
                        {
                            rad = tra1;
                            center = tc1;
                            s1 = b;
                            s2 = c;
                            tp = d;
                        }
                    }
                }

                a = s1;
                b = s2;
                c = tp;

                //计算最大半径及标识 
                for (dis = -1, site = 0, i = 0; i < n; ++i)
                {
                    temp = Points_Two_Dis(center, p[i]);
                    if (temp > dis)
                    {
                        dis = temp;
                        site = i;
                    }
                }
            }
        }

        /// <summary>
        /// 多点拟合--线性拟合
        /// </summary>
        /// <param name="array"></param>
        /// <param name="size"></param>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public static int Points_MultiFit_Linear(vectorPoint[] array, int size, out double slope, out double intercept)
        {
            double[] xInputArray = new double[size];
            double[] yInputArray = new double[size];

            //拷贝数据
            for (int i = 0; i < size; i++)
            {
                xInputArray[i] = array[i].x;
                yInputArray[i] = array[i].y;
            }

            //拟合直线
            Tuple<double, double> tuple = MathNet.Numerics.Fit.Line(xInputArray, yInputArray);

            //传递参数
            intercept = tuple.Item1;
            slope = tuple.Item2;

            return 0;
        }

        /// <summary>
        /// 根据输入点位计算中心
        /// </summary>
        /// <param name="array"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static vectorPoint Points_Multi_Center_Point(vectorPoint[] array, int size)
        {
            if (size == 0)
            {
                vectorPoint cp = new vectorPoint(0.0, 0.0);
                return cp;
            }
            else if (size == 1)
            {
                vectorPoint cp = new vectorPoint(array[0].x, array[0].y);
                return cp;
            }
            else if (size == 2)
            {
                return Points_Two_Middle_Point(array[0], array[1]);
            }
            /*
	        else if(size == 3)
	        {
		        return Points_Triangle_Center_Point(array[0], array[1], array[2]);
	        }
	        */
            else if (size == 4)
            {
                return Points_Segment_Cross_Point(array[0], array[2], array[1], array[3]);
            }
            else
            {
                vectorPoint cp = new vectorPoint(array[0].x, array[0].y);
                return cp;
            }
        }

        /// <summary> 
        /// 求均值
        /// </summary>
        /// <param name="input_array"></param>
        /// <param name="total_num"></param>
        /// <param name="delete_max_num"></param>
        /// <param name="delete_min_num"></param>
        /// <param name="mean_val"></param>
        /// <returns></returns>
        public static bool Array_1DCalMeanVal(double[] input_array, int total_num, int delete_max_num, int delete_min_num, out double mean_val)
        {
            mean_val = 0.0;

            //对数据进行保护
            int num = total_num - delete_max_num - delete_min_num;
            if (num <= 0)
            {
                return false;
            }

            //排序
            Array.Sort(input_array);

            //切片
            double[] new_array = new double[num];
            Array.Copy(input_array, delete_min_num, new_array, 0, num);

            //均值
            mean_val = MathNet.Numerics.Statistics.ArrayStatistics.Mean(new_array);

            return true;
        }
        #endregion

        #region 直线计算方法   已验证无误
        /// <summary>
        /// 两点计算一条直线
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static int CalOneLineBy2Pt(double x1, double y1, double x2, double y2, out double a, out double b, out double c)
        {
            a = y1 - y2;
            b = x2 - x1;
            c = x1 * y2 - x2 * y1;

            return 0;
        }

        /// <summary>
        /// 计算直线的距离
        /// </summary>
        /// <param name="x">点X坐标</param>
        /// <param name="y">点Y坐标</param>
        /// <param name="a">直线的A</param>
        /// <param name="b">直线的B</param>
        /// <param name="c">直线的C</param>
        /// <param name="dis"></param>
        /// <returns></returns>
        public static int CalDis2lLine(double x, double y, double a, double b, double c, out double dis)
        {
            dis = Math.Abs((a * x + b * y + c) / Math.Sqrt(a * a + b * b));
            return 0;
        }

        public static int calInjection(double a1, double b1, double c1, double a2, double b2, double c2, out double x, out double y)
        {
            x = (b1 * c2 - b2 * c1) / (a1 * b2 - a2 * b1);
            y = (a2 * c1 - a1 * c2) / (a1 * b2 - a2 * b1);
            return 0;
        }

        /// <summary>
        /// 通过距离计算平行线
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="dis"></param>
        /// <param name="c1"></param>
        /// <param name="bHor"></param>
        /// <returns></returns>
        public static int CalParalleByDis(double a, double b, double c, double dis, out double c1, bool bHor = true)
        {
            //if (dis < 0)
            //    c1 = c - dis * Math.Sqrt(a * a + b * b);
            //else
            //    c1 = c + dis * Math.Sqrt(a * a + b * b);

            c1 = 0;

            // 水平线
            if (a == 0)
            {
                if (b < 0)
                {
                    c1 = c + dis * Math.Sqrt(a * a + b * b);        // -dis/b  负
                }
                else
                {
                    c1 = c - dis * Math.Sqrt(a * a + b * b);        // dis/b   负
                }
            }
            // 垂直线
            else if (b == 0)
            {
                if (a < 0)
                {
                    c1 = c + dis * Math.Sqrt(a * a + b * b);        // -dis/a  负
                }
                else
                {
                    c1 = c - dis * Math.Sqrt(a * a + b * b);        // dis/a   负
                }
            }
            // 有倾角的水平线
            else if (bHor == true)
            {
                if (b < 0)
                {
                    c1 = c + dis * Math.Sqrt(a * a + b * b);        // -dis/b  负  若dis小于0，则截距应更小；若dis>0，则截距应更大
                }
                else
                {
                    c1 = c - dis * Math.Sqrt(a * a + b * b);        // dis/b   负 若dis小于0，则截距
                }
            }
            else
            {
                if (a < 0)
                {
                    c1 = c + dis * Math.Sqrt(a * a + b * b);        // -dis/a  负
                }
                else
                {
                    c1 = c - dis * Math.Sqrt(a * a + b * b);        // dis/a   负
                }
            }

            return 0;
        }
        #endregion
    }

    /// <summary>
    /// 联合标定对齐输入
    /// </summary>
    public class unionAlignInput
    {
        // 图像处理得到的像素坐标
        public double srcimageXLst;
        public double srcImageY;
        public double srcImageAngle;
        public double destimageXLst;
        public double destImageY;
        public double destImageAngle;

        // 源于目标两者之间的固定角度差值
        public double srcDestAngle;

        // 源的旋转中心
        public double srcRotCenterX;
        public double srcRotCenterY;

        // 源到目标端的映射矩阵(3X2的矩阵)
        public double[,] srcToDestMatrix;

        // 目标端到物理坐标系的映射矩阵(3X2的矩阵)
        public double[,] destToWorldMatrix;
    }

    /// <summary>
    /// 分别标定对齐输入
    /// </summary>
    public class aloneAlignInput
    {
        // 图像处理得到的像素坐标
        public double srcimageXLst;
        public double srcImageY;
        public double srcImageAngle;
        public double destimageXLst;
        public double destImageY;
        public double destImageAngle;

        // 正常工作时的像素坐标
        public double srcGoodX;
        public double srcGoodY;
        public double srcGoodAngle;
        public double destGoodX;
        public double destGoodY;
        public double destGoodAngle;

        // 源的旋转中心
        public double srcRotCenterX;
        public double srcRotCenterY;

        // 源端到物理坐标系的映射矩阵(3X2的矩阵)
        public double[,] srcToWorldMatrix;

        // 目标端到物理坐标系的映射矩阵(3X2的矩阵)
        public double[,] destToWorldMatrix;

    }

    /// <summary>
    /// 向量点
    /// </summary>
    public class vectorPoint
    {
        public double x;
        public double y;
        public double a;

        public vectorPoint(double inputx, double inputy)
        {
            x = inputx;
            y = inputy;
            a = 0.0;
        }

        public vectorPoint()
        {
            x = 0.0;
            y = 0.0;
            a = 0.0;
        }
    }

    public class vectorLine
    {
        public vectorPoint p1;
        public vectorPoint p2;

        /// <summary>
        /// 斜率
        /// </summary>
        public double slope;

        /// <summary>
        /// 截距
        /// </summary>
        public double intercept;
    }
}
