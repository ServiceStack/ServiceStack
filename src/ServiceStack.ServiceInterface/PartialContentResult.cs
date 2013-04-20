using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface
{
    public class PartialContentResult : IPartialContentResult, IDisposable
    {
        private readonly FileInfo file;
        private readonly Stream stream;
        private PartialContentResult(string contentType)
        {
            if (contentType == null) throw new ArgumentNullException("contentType");

            ContentType = contentType;

            Headers = new Dictionary<string, string>
                {
                    {"Accept-Ranges", "bytes"}
                };
        }

        public PartialContentResult(FileInfo file, string contentType = null) : this(contentType ?? MimeTypes.GetMimeType(file.Name))
        {
            this.file = file;
        }

        public PartialContentResult(Stream stream, string contentType)
            : this(contentType)
        {
            if (contentType == null) throw new ArgumentNullException("contentType");
            this.stream = stream;
        }

        public IDictionary<string, string> Options
        {
            get { return Headers; }
        }

        public int Status { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public object Response { get; set; }
        public IContentTypeWriter ResponseFilter { get; set; }
        public IRequestContext RequestContext { get; set; }

        public long Start { get; set; }
        public long End { get; set; }

        public long GetContentLength()
        {
            if (file != null)
                return file.Length;
            if (stream != null)
                return stream.Length;

            throw new InvalidOperationException("Neither file nor stream were set when attempting to establish Content-Length.");
        }

        public void WriteTo(Stream responseStream)
        {
            if (file != null)
            {
                using (var fs = file.OpenRead())
                {
                    if (End != file.Length - 1)
                        WriteTo(fs, responseStream);
                    else
                    {
                        fs.WriteTo(responseStream);
                        responseStream.Flush();
                    }
                        
                }
                return;
            }

            if (stream != null)
            {
                WriteTo(stream, responseStream);
                return;
            }
                

            throw new InvalidOperationException("Neither file nor stream were set when attempting to write to the Response Stream.");
        }

        private void WriteTo(Stream inputStream, Stream responseStream)
        {
            if (!inputStream.CanSeek)
                throw new InvalidOperationException(
                    "Sending Range Responses requires a seekable stream eg. FileStream or MemoryStream");

            
            long totalBytesToSend = End - Start + 1;
            var buffer = new byte[0x1000]; //new byte[BufferSize];
            long bytesRemaining = totalBytesToSend;

            inputStream.Seek(Start, SeekOrigin.Begin);

            while (bytesRemaining > 0)
            {
           
                int count;
                if (bytesRemaining <= buffer.Length)
                    count = inputStream.Read(buffer, 0,
                                             (bytesRemaining <= int.MaxValue) ? (int)bytesRemaining : int.MaxValue);
                else
                    count = inputStream.Read(buffer, 0, buffer.Length);

             
                try
                {
                    //Log.DebugFormat("Writing {0} to response",System.Text.Encoding.UTF8.GetString(buffer));
                    responseStream.Write(buffer, 0, count);
                    responseStream.Flush();
                    bytesRemaining -= count;
                }
                catch (HttpException httpException)
                {
                    /* in Asp.Net we can call HttpResponseBase.IsClientConnected
                        * to see if the client broke off the connection
                        * and avoid trying to flush the response stream.
                        * I'm not quite I can do the same here without some invasive changes,
                        * so instead I'll swallow the exception that IIS throws in this situation.*/

                    if (httpException.Message ==
                        "An error occurred while communicating with the remote host. The error code is 0x80070057.")
                        break;
                }
            }
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
            }
        }
    }
}
