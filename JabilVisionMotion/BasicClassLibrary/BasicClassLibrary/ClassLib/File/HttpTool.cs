using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace KENDLL.Common
{
    /// <summary>
    ///Http 协议操作
    ///备忘
    //Encoding编码
    //Encoding.UTF8.GetBytes()
    //Encoding.UTF8.GetString(bt)
    //ContentType类型
    //request.ContentType = "application/x-www-form-urlencoded";//提交表单，数据提交格式："tel=手机号&pswd=密码"。类似url传参
    //context.Response.ContentType = "text/xml;charset=UTF-8";//XML
    //context.Response.ContentType = "application/json;charset=UTF-8";//JSON
    //context.Response.ContentType = "text/plain;charset=UTF-8";//普通文本
    //context.Response.ContentType = "text/html;charset=GBK";//HTML
    /// </summary>
    public class HttpTool
    {
        public static int Timeout = 1000 * 60 * 20;
        public enum EHttpMethod
        {
            GET, POST, PUT, DELETE,
        }

        /// <summary>
        /// 执行HTTP请求，获取响应字符串。
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="method"></param>
        /// <param name="paramData"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static string httpUrl(string url, EHttpMethod method, string reqdata = null, Dictionary<string, string> headers = null, string contentType = null)
        {
            try
            {
                //Log.LogTest("url:" + url);
                //Log.LogTest("req:" + reqdata);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method.ToString();
                request.Timeout = Timeout;
                request.ContentType = string.IsNullOrEmpty(contentType) ? "application/json" : contentType;
                //请求头 Headers
                if (headers != null)
                {
                    WebHeaderCollection wc = request.Headers;
                    foreach (var item in headers)
                    {
                        wc.Add(item.Key, item.Value);
                    }
                }
                //请求流
                if (request.Method.ToString() != "GET" && !string.IsNullOrEmpty(reqdata))
                {

                    byte[] byteArray = Encoding.UTF8.GetBytes(reqdata);
                    request.ContentLength = byteArray.Length;
                    request.GetRequestStream().Write(byteArray, 0, byteArray.Length);

                }
                //响应流
                using (WebResponse resp = request.GetResponse())
                {
                    using (StreamReader stream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string result = stream.ReadToEnd();
                        //Log.writeLine("res:"+result);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                //Log.LogError(ex.Message + " url:" + url);
                //Log.LogError(ex.StackTrace);
                //return ex.ToString();
                throw ex;
            }

        }

        /// <summary>
        /// 下载网络文件 ，第一种方式
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="savefileurl"></param>
        /// <returns></returns>
        public static bool httpByte(string url, Dictionary<string, string> headers, string savefileurl)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = Timeout;
                request.ContentType = "application/octet-stream";
                //请求头 Headers
                if (headers != null)
                {
                    WebHeaderCollection wc = request.Headers;
                    foreach (var item in headers)
                    {
                        wc.Add(item.Key, item.Value);
                    }
                }
                //响应流
                using (WebResponse resp = request.GetResponse())
                {

                    Stream st = resp.GetResponseStream();
                    using (FileStream fs = new FileStream(savefileurl, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        byte[] bt = new byte[512];
                        int c = 0;
                        while ((c = st.Read(bt, 0, bt.Length)) > 0)
                        {
                            fs.Write(bt, 0, c);
                        }

                    }
                    st.Close();
                    st.Dispose();
                }
                return true;
            }
            catch (Exception ex)
            {

                //Log.LogError(ex.ToString());
                return false;
            }

        }

        /// <summary>
        /// 下载网络文件，第二种方式
        /// </summary>
        /// <param name="url"></param>
        /// <param name="savefileurl"></param>
        /// <returns></returns>
        public static bool xiazaiFile(string url, string savefileurl)
        {
            try
            {
                WebClient myWebClient = new WebClient();
                myWebClient.DownloadFile(url, savefileurl);
                return true;
            }
            catch (Exception ex)
            {
                //Log.LogError(ex.ToString());
                return false;
            }


        }

        /// <summary>
        /// 通过url获取文件内容  如：txt
        /// </summary>
        /// <param name="fileurl"></param>
        /// <returns></returns>
        public static string getFileFromWebUrl(string wwwfileurl)
        {
            return httpUrl(wwwfileurl, EHttpMethod.GET, "", null, null);
        }
    }

}

