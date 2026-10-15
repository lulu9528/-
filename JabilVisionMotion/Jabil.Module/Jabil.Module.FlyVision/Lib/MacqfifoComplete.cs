using Cognex.VisionPro;
using Jabil.Lib;
using Jabil.Module.Log.Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Jabil.Module.FlyVision.Lib
{
    public class MacqfifoComplete
    {
        #region 前工位_下相机
        private int numacqs_前工位_下相机 = 0;
        public void Macqfifo_complete_前工位_下相机(object sender, Cognex.VisionPro.CogCompleteEventArgs e)
        {
            ICogAcqFifo fifo = (ICogAcqFifo)sender;
            ICogImage image = null;
            int numReadyVal, numPendingVal;
            bool busyVal;
            CogAcqInfo info = new CogAcqInfo();
            try
            {
                fifo.GetFifoState(out numPendingVal, out numReadyVal, out busyVal);
                if (numReadyVal > 0)
                {
                    image = fifo.CompleteAcquireEx(info);
                    if (numacqs_前工位_下相机 > 8)
                    {
                        GC.Collect();
                        numacqs_前工位_下相机 = 0;
                    }
                    numacqs_前工位_下相机 += 1;
                    FlyVisionSystem.Instance.indexImage_前工位_下相机.Image = image;
                    FlyVisionSystem.Instance.indexImage_前工位_下相机.Index = numacqs_前工位_下相机;
                    ThreadPool.QueueUserWorkItem(ImageRun_前工位_下相机, image);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Operation, LogLevel.Info, Color.Red, "前工位_下相机-->" + ex);
            }
        }
        #endregion
        #region 后工位_下相机
        private int numacqs_后工位_下相机 = 0;
        public void Macqfifo_complete_后工位_下相机(object sender, Cognex.VisionPro.CogCompleteEventArgs e)
        {
            ICogAcqFifo fifo = (ICogAcqFifo)sender;
            ICogImage image = null;
            int numReadyVal, numPendingVal;
            bool busyVal;
            CogAcqInfo info = new CogAcqInfo();
            try
            {
                fifo.GetFifoState(out numPendingVal, out numReadyVal, out busyVal);
                if (numReadyVal > 0)
                {
                    image = fifo.CompleteAcquireEx(info);
                    if (numacqs_后工位_下相机 > 8)
                    {
                        GC.Collect();
                        numacqs_后工位_下相机 = 0;
                    }
                    numacqs_后工位_下相机 += 1;
                    FlyVisionSystem.Instance.indexImage_后工位_下相机.Image = image;
                    FlyVisionSystem.Instance.indexImage_后工位_下相机.Index = numacqs_后工位_下相机;
                    ThreadPool.QueueUserWorkItem(ImageRun_后工位_下相机, image);
                }
            }
            catch (Exception ex)
            {
                RLog.SaveLog(LogType.Operation,LogLevel.Info, Color.Red, "后工位_下相机-->" + ex);
            }
        }
        #endregion

        private void ImageRun_前工位_下相机(object obj)
        {
            try
            {
                //RLog.SaveLog(LogType.Operation, Color.Black, "前工位_下相机-->第" + FlyVisionSystem.Instance.indexImage_前工位_下相机.Index.ToString() + "拍照完成！");
                FlyVisionSystem.Instance.List_前工位_下相机.Add((ICogImage)obj);

            }
            catch
            {

            }
        }
        private void ImageRun_后工位_下相机(object obj)
        {
            try
            {
              
                //RLog.SaveLog(LogType.Operation, Color.Black, "后工位_下相机-->第"+ FlyVisionSystem.Instance.indexImage_后工位_下相机.Index.ToString()+"拍照完成！");
                FlyVisionSystem.Instance.List_后工位_下相机.Add((ICogImage)obj);

            }
            catch(Exception ex)
            {

                RLog.SaveError(ex);
            }
        }

    }
}
