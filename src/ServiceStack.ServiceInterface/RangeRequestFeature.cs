using System.Collections.Generic;
using System.IO;
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


    public class RangeResult : IStreamWriter, IHasOptions, IRequiresHttpRequest
    {
        private readonly int? end;
        private readonly FileInfo file;
        private readonly int start;

        public RangeResult(FileInfo file, string contentType)
        {
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

            Options = new Dictionary<string, string>
                {
                    {HttpHeaders.ContentType, contentType},
                    {HttpHeaders.ContentLength, file.Length.ToString()},
                    {"Accept-Ranges", "bytes"}
                };
            if (end.HasValue)
                end = (int) file.Length - 1;

            Options.Add("Content-Range",
                        "bytes {0}-{1}/{2}".Fmt(start, end, file.Length));
        }

        public IDictionary<string, string> Options { get; set; }

        public IHttpRequest HttpRequest { get; set; }

        public void WriteTo(Stream responseStream)
        {
            using (FileStream fs = file.OpenRead())
            {
                fs.WriteTo(responseStream);
                responseStream.Flush();
            }
        }
    }
}