using System.IO;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class HttpFile : IHttpFile
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public Stream InputStream { get; set; }
    }
}