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

        public static WebResponse PostFileToUrl(this string url,
            FileInfo uploadFileInfo, string uploadFileMimeType,
            string acceptContentType = null,
            Action<HttpWebRequest> requestFilter = null)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            using (var fileStream = uploadFileInfo.OpenRead())
            {
                var fileName = uploadFileInfo.Name;

                webReq.UploadFile(fileStream, fileName, uploadFileMimeType, acceptContentType:acceptContentType, requestFilter:requestFilter);
            }

            return webReq.GetResponse();
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

        public static void UploadFile(this WebRequest webRequest, Stream fileStream, string fileName, string mimeType,
            string acceptContentType = null, Action<HttpWebRequest> requestFilter = null)
        {
            var httpReq = (HttpWebRequest)webRequest;
            httpReq.UserAgent = Env.ServerUserAgent;
            httpReq.Method = "POST";
            httpReq.AllowAutoRedirect = false;
            httpReq.KeepAlive = false;

            if (acceptContentType != null)
                httpReq.Accept = acceptContentType;

            if (requestFilter != null)
                requestFilter(httpReq);

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