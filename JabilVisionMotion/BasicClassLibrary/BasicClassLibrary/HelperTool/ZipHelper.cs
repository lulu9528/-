using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary.HelperTool
{
    /// <summary>
    /// 文件压缩解压缩类
    /// </summary>
    public static class ZipHelper
    {
        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="files"></param>
        /// <param name="strZipFile"></param>
        /// <returns></returns>
        public static bool Compress(string strZipFile, string[] files)
        {
            using (ZipFile file = ZipFile.Create(strZipFile))
            {
                file.BeginUpdate();
                foreach (string str in files)
                {
                    FileInfo info = new FileInfo(str);
                    file.NameTransform = new ZipNameTransform(info.DirectoryName);
                    if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        file.AddDirectory(str);
                    }
                    else
                    {
                        file.Add(str);
                    }
                }
                file.CommitUpdate();
            }
            return true;
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="strZipFile"></param>
        /// <param name="strFolder"></param>
        /// <returns></returns>
        public static bool UnCompress(string strZipFile, string strFolder)
        {
            new FastZip().ExtractZip(strZipFile, strFolder, "");
            return true;
        }
    }
}
