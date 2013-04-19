using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
    public class RangeRequestFeature : IPlugin
    {
        public const string RangeStartItemsKey = "__rangeStart";
        public const string RangeEndItemsKey = "__rangeEnd";

        private bool alreadyConfigured;

        public void Register(IAppHost appHost)
        {
            if (alreadyConfigured) return;
            alreadyConfigured = true;

            appHost.RequestFilters.Add(AddRangeRequestParserToRequestFilter);
        }

        public static void AddRangeRequestParserToRequestFilter(IHttpRequest request, IHttpResponse response,
                                                                object requestDto)
        {
            /* look for a Range header, parse the values and stash in request.Items */
            if (request.Headers["Range"] == null)
                return;

            string rangeHeader = request.Headers["Range"];
            //rangeHeader should be of the format "bytes=0-" or "bytes=0-12345"
            string[] range = rangeHeader.SplitOnFirst("=")[1].SplitOnFirst("-");
            int start = int.Parse(range[0]);
            int? end = null;

            if (range.Length == 2 && !string.IsNullOrEmpty(range[1]))
                end = int.Parse(range[1]); //the client requested a certain range of bytes

            request.Items[RangeStartItemsKey] = start;
            request.Items[RangeEndItemsKey] = end;
        }
    }


    public class RangeResult : IStreamWriter, IRequiresHttpRequest, IHttpResult
    {
        private readonly int? end;
        private readonly FileInfo file;
        private readonly int start;

        public RangeResult(FileInfo file, string contentType = null)
        {
            Status = 206;
            StatusCode = HttpStatusCode.PartialContent;
            StatusDescription = "Partial Content";
            ContentType = contentType ?? MimeTypes.GetMimeType(file.Name);

            this.file = file;
            start =
                (int)
                (HttpRequest.Items.ContainsKey(RangeRequestFeature.RangeStartItemsKey)
                     ? HttpRequest.Items[RangeRequestFeature.RangeStartItemsKey]
                     : 0);
            end = (int?)
                  (HttpRequest.Items.ContainsKey(RangeRequestFeature.RangeEndItemsKey)
                       ? HttpRequest.Items[RangeRequestFeature.RangeStartItemsKey]
                       : null);

            Headers = new Dictionary<string, string>
                {
                    {HttpHeaders.ContentType, contentType},
                    {HttpHeaders.ContentLength, file.Length.ToString()},
                    {"Accept-Ranges", "bytes"}
                };

            if (!end.HasValue)
                end = (int) file.Length - 1;

            Headers.Add("Content-Range",
                        "bytes {0}-{1}/{2}".Fmt(start, end, file.Length));
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
        public IHttpRequest HttpRequest { get; set; }

        public void WriteTo(Stream responseStream)
        {
            using (FileStream fs = file.OpenRead())
            {
                var buffer = new byte[0x1000]; //new byte[BufferSize];
                long totalToSend = end.Value - start;
                long bytesRemaining = totalToSend;
                int count;

                fs.Seek(start, SeekOrigin.Begin);

                while (bytesRemaining > 0)
                {
                    if (bytesRemaining <= buffer.Length)
                        count = fs.Read(buffer, 0,
                                        (bytesRemaining <= int.MaxValue) ? (int) bytesRemaining : int.MaxValue);
                    else
                        count = fs.Read(buffer, 0, buffer.Length);

                    /* Would be nice if we could do this */
                    //if (!response.IsClientConnected)
                    //{
                    //    
                    //    break;
                    //}

                    try
                    {
                        responseStream.Write(buffer, 0, count);
                        responseStream.Flush();
                        bytesRemaining -= count;
                    }
                    catch (HttpException httpException)
                    {
                       /* in Asp.Net we can call HttpResponseBase.IsClientConnected
                        * to see if the client broke off the connection
                        * and stop avoid trying to flush the response stream.
                        * I'm not quite I can do the same here without some invasive changes,
                        * so instead I'll swallow the exception that IIS throws.*/

                        if (httpException.Message ==
                            "An error occurred while communicating with the remote host. The error code is 0x80070057.")
                            break;
                    }
                }
            }
        }
    }
}