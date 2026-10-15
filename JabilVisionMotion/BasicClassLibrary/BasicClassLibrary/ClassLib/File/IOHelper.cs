using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class IOHelper
    {
        // 读取文件到 byte[]
        /// <summary>
        /// 读取文件到 byte[]
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static byte[] ReadBytes(string FullName)
        {
            if (!File.Exists(FullName))
                return null;

            try
            {
                FileStream fs = new FileStream(FullName, FileMode.Open, FileAccess.Read, FileShare.Read);

                byte[] buffer = new byte[fs.Length];

                fs.Read(buffer, 0, buffer.Length);

                fs.Close();

                return buffer;
            }
            catch
            {
                return null;
            }
        }

        public static bool WriteBytes(byte[] bytes, string FullName)
        {
            FileStream fs = null;

            try
            {
                fs = new FileStream(FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);

                fs.Write(bytes, 0, bytes.Length);

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                fs.Close();
            }
        }

        /// <summary>
        /// 连接共享目录
        /// </summary>
        /// <param name="path">共享目录名称</param>
        /// <param name="userName">登录名称</param>
        /// <param name="passWord">登录密码</param>
        /// <returns></returns>
        public static bool ConnectSharePath(string path, string userName, string passWord)
        {
            bool Flag = false;
            Process proc = new Process();
            try
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                string dosLine = @"net use * /del /yes";
                proc.StandardInput.WriteLine(dosLine);
                dosLine = @"net use " + path + " /User:" + userName + " " + passWord + " /PERSISTENT:YES";
                proc.StandardInput.WriteLine(dosLine);
                proc.StandardInput.WriteLine("exit");
                while (!proc.HasExited)
                {
                    proc.WaitForExit(1000);
                }
                string errormsg = proc.StandardError.ReadToEnd();
                proc.StandardError.Close();
                if (string.IsNullOrEmpty(errormsg))
                {
                    Flag = true;
                }
                else
                {
                    throw new Exception(errormsg);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                proc.Close();
                proc.Dispose();
            }
            return Flag;
        }
        /// <summary>
        /// 上传文件到共享目录
        /// </summary>
        /// <param name="localFileFullName"></param>
        /// <param name="sharePath"></param>
        /// <returns></returns>
        public static bool UpLoadFileToSharePath(string localFileFullName, string sharePath)
        {
            string newFileName = localFileFullName.Substring(localFileFullName.LastIndexOf(@"\") + 1);//取文件名称

            if (sharePath.EndsWith(@"\") == false)
                sharePath = sharePath + @"\";

            sharePath = sharePath + newFileName;

            WebClient myWebClient = new WebClient();

            NetworkCredential cread = new NetworkCredential();

            myWebClient.Credentials = cread;
            FileStream fs = new FileStream(localFileFullName, FileMode.Open, FileAccess.Read);
            BinaryReader r = new BinaryReader(fs);

            try
            {
                byte[] postArray = r.ReadBytes((int)fs.Length);
                Stream postStream = myWebClient.OpenWrite(sharePath);
                // postStream.m
                if (postStream.CanWrite)
                {
                    postStream.Write(postArray, 0, postArray.Length);
                }
                else
                {
                    return false;
                }

                postStream.Close();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// 向远程文件夹保存本地内容，或者从远程文件夹下载文件到本地
        /// </summary>
        /// <param name="localFileFullName">要保存的文件的路径，如果保存文件到共享文件夹，这个路径就是本地文件路径如：@"D:\1.avi"</param>
        /// <param name="sharePath">保存文件的路径，不含名称及扩展名</param>
        /// <param name="fileName">保存文件的名称以及扩展名</param>
        public static bool Transport(string localFileFullName, string sharePath, string fileName)
        {
            try
            {
                FileStream inFileStream = new FileStream(localFileFullName, FileMode.Open);
                if (!Directory.Exists(sharePath))
                    Directory.CreateDirectory(sharePath);

                sharePath = sharePath + fileName;
                FileStream outFileStream = new FileStream(sharePath, FileMode.OpenOrCreate);

                byte[] buf = new byte[inFileStream.Length];

                int byteCount;

                while ((byteCount = inFileStream.Read(buf, 0, buf.Length)) > 0)
                {
                    outFileStream.Write(buf, 0, byteCount);
                }

                inFileStream.Flush();
                inFileStream.Close();
                outFileStream.Flush();
                outFileStream.Close();

                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }

        }


    }
}
