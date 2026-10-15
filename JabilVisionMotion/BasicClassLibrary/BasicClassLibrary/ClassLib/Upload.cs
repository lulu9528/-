using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public class Upload
    {
        private Encoding encoding = Encoding.GetEncoding("utf-8");
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受   
        }


        /// <summary>
        /// 向服务器发送数据并且返回响应结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Dictionary<string, string> PostAndGet(string url, Dictionary<string, string> parameters)
        {
            HttpWebResponse response = CreatePostHttpResponse(url, parameters);
            string result = GetResponse(response);
            Dictionary<string, string> resultDictionary = DecodeJson(result);
            return resultDictionary;
        }

        /// <summary>
        /// 向服务器发送数据并且返回响应结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Dictionary<string, string> PostAndGetTestId(string url, Dictionary<string, string> parameters)
        {
            HttpWebResponse response = CreatePostHttpResponseTestId(url, parameters);
            string result = GetResponse(response);
            Dictionary<string, string> resultDictionary = DecodeJson(result);
            return resultDictionary;
        }

        /// <summary>
        /// 向服务器发送数据并且返回响应结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Dictionary<string, string> PostAndGet(string url)
        {
            HttpWebResponse response = CreatePostHttpResponse(url);
            string result = GetResponse(response);
            Dictionary<string, string> resultDictionary = DecodeJson(result);
            return resultDictionary;
        }

        private Dictionary<string, string> DecodeJson(string json)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (json != null && json.Length != 0)
            {
                string[] jsonSplit = json.Split('\"');
                if (jsonSplit != null && jsonSplit.Length == 9)
                {
                    result.Add(jsonSplit[1], jsonSplit[3]);
                    result.Add(jsonSplit[5], jsonSplit[7]);
                }
            }
            return result;
        }

        private HttpWebResponse CreatePostHttpResponse(string url)
        {
            try
            {
                HttpWebRequest request = null;
                //HTTPSQ请求
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.UserAgent = DefaultUserAgent;
                //不需要POST数据   
                return request.GetResponse() as HttpWebResponse;

            }
            catch (WebException ex)
            {
                //m_Logger.Error("连接Mes失败..." + ex.ToString());
            }
            return null;
        }

        private HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters)
        {
            try
            {
                HttpWebRequest request = null;
                //HTTPSQ请求
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.UserAgent = DefaultUserAgent;
                //如果需要POST数据   
                if (!(parameters == null || parameters.Count == 0))
                {
                    StringBuilder buffer = new StringBuilder();
                    int i = 0;
                    foreach (string key in parameters.Keys)
                    {
                        if (i > 0)
                        {
                            buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                        }
                        else
                        {
                            buffer.AppendFormat("{0}={1}", key, parameters[key]);
                        }
                        i++;
                    }

                    byte[] data = encoding.GetBytes(buffer.ToString());
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }
                return request.GetResponse() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                //m_Logger.Error("连接Mes失败..." + ex.ToString());
            }
            return null;
        }


        private HttpWebResponse CreatePostHttpResponseTestId(string url, IDictionary<string, string> parameters)
        {
            try
            {
                HttpWebRequest request = null;
                //HTTPSQ请求
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.UserAgent = DefaultUserAgent;
                //如果需要POST数据   
                if (!(parameters == null || parameters.Count == 0))
                {
                    string strNew = "";
                    int i = 0;
                    foreach (string key in parameters.Keys)
                    {
                        strNew = "dataJson={\"" + key + "\":\"" + parameters[key] + "\"}";
                    }
                    byte[] data = encoding.GetBytes(strNew);
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }
                return request.GetResponse() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                //m_Logger.Error("连接Mes失败..." + ex.ToString());
            }
            return null;
        }

        private string GetResponse(HttpWebResponse response)
        {
            if (response != null)
            {
                Stream stream = response.GetResponseStream();   //获取响应的字符串流
                StreamReader sr = new StreamReader(stream); //创建一个stream读取流
                StreamReader streamReader = new StreamReader(stream, encoding);
                string result = streamReader.ReadToEnd();
                streamReader.Close();
                stream.Close();
                return result;
            }
            return null;
        }
    }
}
