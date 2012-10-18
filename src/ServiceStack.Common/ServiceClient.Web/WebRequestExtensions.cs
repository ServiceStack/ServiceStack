#if !SILVERLIGHT 
using System;
using System.IO;
using System.Net;
using System.Text;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
    public static class WebRequestExtensions
    {
        public static string DownloadJsonFromUrl(this string url)
        {
            return url.DownloadUrl(ContentType.Json);
        }

        public static string DownloadXmlFromUrl(this string url)
        {
            return url.DownloadUrl(ContentType.Xml);
        }

        public static string DownloadCsvFromUrl(this string url)
        {
            return url.DownloadUrl(ContentType.Csv);
        }

        public static string DownloadUrl(this string url, string acceptContentType)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.Accept = acceptContentType;
            using (var webRes = webReq.GetResponse())
                return DownloadText(webRes);
        }

        public static string DownloadUrl(this string url)
        {
            var webReq = WebRequest.Create(url);
            using (var webRes = webReq.GetResponse())
                return DownloadText(webRes);
        }

        public static byte[] DownloadBinaryFromUrl(this string url)
        {
            var webReq = WebRequest.Create(url);
            using (var webRes = webReq.GetResponse())
                return DownloadBinary(webRes);
        }

        public static string PostJsonToUrl(this string url, string data)
        {
            return SendToUrl(url, HttpMethod.Post, Encoding.UTF8.GetBytes(data), ContentType.Json, ContentType.Json);
        }

        public static string PostJsonToUrl(this string url, object data)
        {
            return SendToUrl(url, HttpMethod.Post, Encoding.UTF8.GetBytes(data.ToJson()), ContentType.Json, ContentType.Json);
        }

        public static string PostToUrl(this string url, string data, string requestContentType = null, string acceptContentType = null)
        {
            return SendToUrl(url, HttpMethod.Post, Encoding.UTF8.GetBytes(data), requestContentType, acceptContentType);
        }

        public static string PutToUrl(this string url, string data, string requestContentType = null, string acceptContentType = null)
        {
            return SendToUrl(url, HttpMethod.Put, Encoding.UTF8.GetBytes(data), requestContentType, acceptContentType);
        }

        public static string PostToUrl(this string url, byte[] data, string requestContentType = null, string acceptContentType = null)
        {
            return SendToUrl(url, HttpMethod.Post, data, requestContentType, acceptContentType);
        }

        public static string PutToUrl(this string url, byte[] data, string requestContentType = null, string acceptContentType = null)
        {
            return SendToUrl(url, HttpMethod.Put, data, requestContentType, acceptContentType);
        }

        private static string SendToUrl(string url, string httpMethod, byte[] data, string requestContentType = null, string acceptContentType = null)
        {
            var webReq = (HttpWebRequest) WebRequest.Create(url);
            webReq.Method = httpMethod;

            if (requestContentType != null)
                webReq.ContentType = requestContentType;

            if (acceptContentType != null)
                webReq.Accept = acceptContentType;

            try
            {
                using (var req = webReq.GetRequestStream())
                    req.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending Request: " + ex);
                throw;
            }

            try
            {
                using (var webRes = webReq.GetResponse())
                    return DownloadText(webRes);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading Response: " + ex);
                throw;
            }
        }

        public static string DownloadAsString(this string url)
        {
            var webReq = WebRequest.Create(url);
            using (var webRes = webReq.GetResponse())
                return DownloadText(webRes);
        }

        public static string DownloadText(this WebResponse webRes)
        {
            using (var stream = webRes.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] DownloadBinary(this WebResponse webRes)
        {
            using (var stream = webRes.GetResponseStream())
            {
                return stream.ReadFully();
            }
        }

        public static HttpWebResponse GetErrorResponse(this string url)
        {
            try
            {
                var webReq = WebRequest.Create(url);
                var webRes = webReq.GetResponse();
                var strRes = webRes.DownloadText();
                Console.WriteLine("Expected error, got: " + strRes);
                return null;
            }
            catch (WebException webEx)
            {
                return (HttpWebResponse)webEx.Response;
            }
        }

        public static WebResponse UploadFile(this WebRequest webRequest,
            FileInfo uploadFileInfo, string uploadFileMimeType)
        {
            using (var fileStream = uploadFileInfo.OpenRead())
            {
                var fileName = uploadFileInfo.Name;

                webRequest.UploadFile(fileStream, fileName, uploadFileMimeType);
            }

            return webRequest.GetResponse();
        }

        public static void UploadFile(this WebRequest webRequest, Stream fileStream, string fileName, string mimeType)
        {
            var httpReq = (HttpWebRequest)webRequest;
            httpReq.UserAgent = Env.ServerUserAgent;
            httpReq.Method = "POST";
            httpReq.AllowAutoRedirect = false;
            httpReq.KeepAlive = false;

            var boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");

            httpReq.ContentType = "multipart/form-data; boundary=" + boundary;

            var boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            var headerTemplate = "\r\n--" + boundary +
                                 "\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\nContent-Type: {1}\r\n\r\n";

            var header = string.Format(headerTemplate, fileName, mimeType);

            var headerbytes = System.Text.Encoding.ASCII.GetBytes(header);

            httpReq.ContentLength = fileStream.Length + headerbytes.Length + boundarybytes.Length;

            using (Stream outputStream = httpReq.GetRequestStream())
            {
                outputStream.Write(headerbytes, 0, headerbytes.Length);

                byte[] buffer = new byte[4096];
                int byteCount;

                while ((byteCount = fileStream.Read(buffer, 0, 4096)) > 0)
                {
                    outputStream.Write(buffer, 0, byteCount);
                }

                outputStream.Write(boundarybytes, 0, boundarybytes.Length);

                outputStream.Close();
            }
        }


        public static void UploadFile(this WebRequest webRequest, Stream fileStream, string fileName)
        {
            fileName.ThrowIfNull("fileName");
            var mimeType = MimeTypes.GetMimeType(fileName);
            if (mimeType == null)
                throw new ArgumentException("Mime-type not found for file: " + fileName);

            UploadFile(webRequest, fileStream, fileName, mimeType);
        }
    }

}
#endif