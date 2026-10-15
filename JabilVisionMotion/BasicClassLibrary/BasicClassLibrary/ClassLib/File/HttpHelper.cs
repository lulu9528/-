using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BasicClassLibrary
{
    public static class HttpHelper
    {
        /// <summary>
        /// 超时时间设置
        /// </summary>
        public static Int32 HttpRequestTimeOut = 5000;

        /// <summary>
        /// GET
        /// </summary>
        /// <param name="url">接口地址</param>
        /// <returns></returns>
        public static HttpResponseData Get(String url)
        {
            return Invoke<Object>("GET", url, null, null);
        }
        /// <summary>
        /// GET
        /// </summary>
        /// <param name="url">接口地址</param>
        /// <param name="Id">URL参数</param>
        /// <returns></returns>
        public static HttpResponseData Get(String url, Object Id)
        {
            return Invoke<Object>("GET", url, Id, null);
        }

        /// <summary>
        /// POST
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">接口地址</param>
        /// <param name="Id">URL参数（可选）</param>
        /// <param name="Data">提交数据</param>
        /// <returns></returns>
        public static HttpResponseData Post<T>(String url, Object Id, T Data)
        {
            return Invoke("POST", url, Id, Data);
        }

        /// <summary>
        /// PUT
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">接口地址</param>
        /// <param name="Data">提交数据</param>
        /// <returns></returns>
        public static HttpResponseData Put<T>(String url, T Data)
        {
            return Invoke("PUT", url, null, Data);
        }
        /// <summary>
        /// PUT
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">接口地址</param>
        /// <param name="Id">URL参数（可选）</param>
        /// <param name="Data">提交数据（可选）</param>
        /// <returns></returns>
        public static HttpResponseData Put<T>(String url, Object Id, T Data)
        {
            return Invoke("PUT", url, Id, Data);
        }

        /// <summary>
        /// DELETE
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">接口地址</param>
        /// <param name="Id">url参数（可选）</param>
        /// <param name="Data">提交数据（可选）</param>
        /// <returns></returns>
        public static HttpResponseData Delete<T>(String url, Object Id, T Data)
        {
            return Invoke<Object>("DELETE", url, Id, Data);
        }

        /// <summary>
        /// 操作调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Method"></param>
        /// <param name="url"></param>
        /// <param name="Id"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        private static HttpResponseData Invoke<T>(String Method, String url, Object Id, T Data)
        {
            HttpResponseData Response = new HttpResponseData()
            {
                Code = HttpStatusCode.RequestTimeout,
                Data = String.Empty,
                Message = String.Empty,
            };
            try
            {
                String PostParam = String.Empty;
                if (Data != null)
                {
                    //PostParam = Newtonsoft.Json.JsonConvert.SerializeObject(Data);
                    PostParam = Data.ToString();
                }
                byte[] postData = Encoding.UTF8.GetBytes(PostParam);

                HttpWebRequest request =
                    (HttpWebRequest)WebRequest.Create(new Uri(url + (Id == null ? "" : '/' + Id.ToString())));
                request.Method = Method;
                request.ServicePoint.Expect100Continue = false;
                request.Timeout = HttpRequestTimeOut;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.ContentLength = postData.Length;

                if (postData.Length > 0)
                {
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        requestStream.Write(postData, 0, postData.Length);
                    }
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Response.Code = response.StatusCode;


                    using (StreamReader stream = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        Response.Data = stream.ReadToEnd();
                        //Response.Data = response.StatusCode + "," + stream.ReadToEnd();
                    }
                }

            }
            catch (Exception ex)
            {
                Response.Message = ex.Message;
            }
            return Response;
        }


        /// <summary>
        /// Http 响应数据
        /// </summary>
        public class HttpResponseData
        {
            /// <summary>
            /// Http StatusCode
            /// </summary>
            public HttpStatusCode Code { get; set; }

            /// <summary>
            /// Response Data
            /// </summary>
            public String Data { get; set; }

            /// <summary>
            /// Error Message
            /// </summary>
            public String Message { get; set; }

        }
    }
}
