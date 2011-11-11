using System;
using System.IO;
using System.Net;
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

            using (var ms = new MemoryStream())
            {
                var boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

                var headerTemplate = "\r\n--" + boundary +
                                     "\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\nContent-Type: {1}\r\n\r\n";

                var header = string.Format(headerTemplate, fileName, mimeType);

                var headerbytes = System.Text.Encoding.ASCII.GetBytes(header);

                ms.Write(headerbytes, 0, headerbytes.Length);
                fileStream.WriteTo(ms);

                ms.Write(boundarybytes, 0, boundarybytes.Length);

                httpReq.ContentLength = ms.Length;

                var requestStream = httpReq.GetRequestStream();

                ms.Position = 0;

                ms.WriteTo(requestStream);

                requestStream.Close();
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