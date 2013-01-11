#if !SILVERLIGHT 
using System;
using System.IO;
using System.Net;
using System.Text;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
    public static class WebRequestExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (WebRequestExtensions));

        public static string DownloadJsonFromUrl(this string url)
        {
            return url.DownloadUrl(acceptContentType: ContentType.Json);
        }

        public static string DownloadXmlFromUrl(this string url)
        {
            return url.DownloadUrl(acceptContentType: ContentType.Xml);
        }

        public static string DownloadCsvFromUrl(this string url)
        {
            return url.DownloadUrl(acceptContentType: ContentType.Csv);
        }

        public static string DownloadUrl(this string url, 
            string httpMethod = null,
            string postData = null,
            string contentType = null,
            string acceptContentType = null,
            Action<HttpWebRequest> requestFilter = null,
            Action<HttpWebResponse> responseFilter = null)
        {
            var bytesRequest = postData != null ? postData.ToUtf8Bytes() : null;
            
            var bytesResponse = DownloadBytesFromUrl(url, httpMethod,
                bytesRequest, contentType, acceptContentType, requestFilter, responseFilter);

            var text = bytesResponse.FromUtf8Bytes();
            return text;
        }

        public static string DownloadUrl(this string url, Encoding encoding,
            string httpMethod = null,
            string postData = null,
            string contentType = null,
            string acceptContentType = null,
            Action<HttpWebRequest> requestFilter = null,
            Action<HttpWebResponse> responseFilter = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            var bytesRequest = postData != null ? encoding.GetBytes(postData) : null;

            var bytesResponse = DownloadBytesFromUrl(url, httpMethod,
                bytesRequest, contentType, acceptContentType, requestFilter, responseFilter);

            var text = encoding.GetString(bytesResponse);
            return text;
        }

        public static byte[] DownloadBytesFromUrl(this string url, 
            string httpMethod = null,
            byte[] postData = null,
            string contentType = null,
            string acceptContentType = null,
            Action<HttpWebRequest> requestFilter = null,
            Action<HttpWebResponse> responseFilter = null)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            if (httpMethod != null)
                webReq.Method = httpMethod;
            if (contentType != null)
                webReq.ContentType = contentType;
            if (acceptContentType != null)
                webReq.Accept = acceptContentType;
            if (requestFilter != null)
                requestFilter(webReq);

            try
            {
                if (postData != null)
                {
                    using (var req = webReq.GetRequestStream())
                        req.Write(postData, 0, postData.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error sending Request: " + ex.Message, ex);
                throw;
            }

            using (var webRes = webReq.GetResponse())
            {
                if (responseFilter != null)
                    responseFilter((HttpWebResponse)webRes);

                using (var stream = webRes.GetResponseStream())
                {
                    return stream.ReadFully();
                }
            }
        }

        public static string PostJsonToUrl(this string url, string data)
        {
            return SendToUrl(url, HttpMethods.Post, Encoding.UTF8.GetBytes(data), ContentType.Json, ContentType.Json);
        }

        public static string PostJsonToUrl(this string url, object data)
        {
            return SendToUrl(url, HttpMethods.Post, Encoding.UTF8.GetBytes(data.ToJson()), ContentType.Json, ContentType.Json);
        }

        public static string PostToUrl(this string url, string data, string requestContentType = null, string acceptContentType = null)
        {
            return SendToUrl(url, HttpMethods.Post, Encoding.UTF8.GetBytes(data), requestContentType, acceptContentType);
        }

        public static string PutToUrl(this string url, string data, string requestContentType = null, string acceptContentType = null)
        {
            return SendToUrl(url, HttpMethods.Put, Encoding.UTF8.GetBytes(data), requestContentType, acceptContentType);
        }

        public static string PostToUrl(this string url, byte[] data, string requestContentType = null, string acceptContentType = null)
        {
            return SendToUrl(url, HttpMethods.Post, data, requestContentType, acceptContentType);
        }

        public static string PutToUrl(this string url, byte[] data, string requestContentType = null, string acceptContentType = null)
        {
            return SendToUrl(url, HttpMethods.Put, data, requestContentType, acceptContentType);
        }

        public static string SendToUrl(this string url, string httpMethod, byte[] data, string requestContentType = null, string acceptContentType = null)
        {
            var responseBytes = url.DownloadBytesFromUrl(
                httpMethod: httpMethod,
                postData: data,
                contentType: requestContentType,
                acceptContentType: acceptContentType);

            var text = responseBytes.FromUtf8Bytes();
            return text;
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

            var boundary = "----------------------------" + DateTime.UtcNow.Ticks.ToString("x");

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