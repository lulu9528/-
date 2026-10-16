using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jabil.Module.FlyVision.Lib;
using Jabil.Module.Log.Lib;


namespace Jabil.Module.FlyVision
{
    public class FlyVisionSystem
    {
        public FlyVisionSystem()
        {
            try
            {
            }
            catch (Exception ex)
            {
                RLog.SaveError(ex);
            }
        }
        private static FlyVisionSystem _instance;
        public static FlyVisionSystem Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FlyVisionSystem();
                return _instance;
            }
        }
        public IndexImage indexImage_前工位_下相机 = new IndexImage();
        public IndexImage indexImage_后工位_下相机 = new IndexImage();
        //public IndexImage[] List_前工位_下相机 = new IndexImage[8];// 前工位_下相机缓存
        //public IndexImage[] List_后工位_下相机 = new IndexImage[8];// 后工位_下相机缓存

        public List<object> List_前工位_下相机 = new List<object>();
        public List<object> List_后工位_下相机 = new List<object>();

        public class IndexImage
        {
            public int Index = 0;
            public object Image;
            public bool Result;
        }

    }
}
