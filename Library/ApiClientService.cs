
using Library;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Library
{
    public class ApiClientService
    {
        readonly string API_URL = "http://localhost:5001";
        public ApiClientService(string apiUrl)
        {
            API_URL = apiUrl;
        }

        /// <summary>
        /// Create HttpClient based request
        /// </summary>
        /// <param name="action"></param>
        /// <param name="queryAccessToken"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public async Task<Stream> StreamRequest(string action, string queryAccessToken = "", string query = "")
        {
            try
            {
                HttpClient client = new HttpClient();

                string requestUrl = string.Format("{0}/{1}", API_URL, action);
                string reqUri = queryAccessToken.IsNullOrEmpty() ? requestUrl : requestUrl + "?accessToken=" + queryAccessToken;
                reqUri += query.IsNullOrEmpty() ? "" : "&" + query;

                client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                client.DefaultRequestHeaders.Add("ContentType", "text/event-stream");
                return await client.GetStreamAsync(reqUri);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public async Task<Type> ClientRequest<Type>(object data, string action, string queryAccessToken = "", string bearerToken = "", string method = "POST", string query = "", string contentType = "application/json")
        {
            try
            {

                ServicePointManager.Expect100Continue = true;
                //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                string requestUrl = string.Format("{0}/{1}", API_URL, action);
                string reqUri = queryAccessToken.IsNullOrEmpty() ? requestUrl : requestUrl + "?accessToken=" + queryAccessToken;
                reqUri += query.IsNullOrEmpty() ? "" : "&" + query;
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(reqUri);
                request.Method = method;

                request.ContentType = contentType;
                request.Timeout = 10 * 1000;
                request.Headers.Add("User-Agent", "PostmanRuntime/7.29.2");
                request.Headers.Add("Accept", "*/*");
                //    request.Headers.Add("Accept-Encoding", "gzip, deflate, br");

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    request.Headers.Add("Authorization", "Bearer " + bearerToken);
                }
                //request.KeepAlive = false;
                request.UseDefaultCredentials = true;
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
                if (data != null)
                {
                    string jsonString = JsonConvert.SerializeObject(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    Regex removeEmpty = new Regex("\\s*\"[^\"]+\":\\s*\\[\\]\\,?");
                    Regex removeComma = new Regex(",(?=\\s*})");
                    Regex removeEmptyObject = new Regex("\\s*\"[^\"]+\":\\s*\\{\\}\\,?");
                    string result = removeEmpty.Replace(jsonString, "");
                    result = removeComma.Replace(result, "");
                    result = removeEmptyObject.Replace(result, "");
                    result = removeEmptyObject.Replace(result, "");


                    byte[] byteArray = Encoding.UTF8.GetBytes(result);
                    request.ContentLength = byteArray.Length;
                    using (StreamWriter writer = new StreamWriter(await request.GetRequestStreamAsync()))
                    {
                        await writer.WriteAsync(result);
                    }
                }

                WebResponse response = await request.GetResponseAsync();
                var reader = new StreamReader(response.GetResponseStream());

                using (var ms = new MemoryStream())
                {
                    if (response.Headers.AllKeys.Contains("Content-Type") && response.Headers["Content-Type"].Contains("gzip"))
                        using (var stream = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
                        {
                            await stream.CopyToAsync(ms);
                        }
                    else
                        await reader.BaseStream.CopyToAsync(ms);
                    if (typeof(Type) == typeof(string))
                    {
                        object obj = System.Text.Encoding.Default.GetString(ms.ToArray());
                        return (Type)obj;
                    }
                    else if (typeof(byte[]) == typeof(Type))
                    {
                        object obj = ms.ToByteArray();
                        return (Type)obj;
                    }
                }

                return default(Type);
            }
            catch (WebException ex)
            {
                //todo : loglama yapabiliriz 
                string message = "";

                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var resp = ex.Response;
                    message = new System.IO.StreamReader(resp.GetResponseStream()).ReadToEnd().Trim();
                }
                return default(Type); ;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<T> ClientRequestJson<T>(object data, string action, string queryAccessToken = "", string bearerToken = "", string method = "POST", string query = "")
        {
            try
            {
                string json = await ClientRequest<string>(data, action, queryAccessToken, bearerToken, method, query);
                if (!json.IsNullOrEmpty())

                {
                    T obj = JsonConvert.DeserializeObject<T>(json);
                    return obj;
                }
                else return default(T);
            }
            catch (WebException ex)
            {
                string message = "";

                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var resp = ex.Response;
                    message = new System.IO.StreamReader(resp.GetResponseStream()).ReadToEnd().Trim();
                }
                return default(T);
            }
        }
    }
}
