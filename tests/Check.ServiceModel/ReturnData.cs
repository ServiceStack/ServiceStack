using System.IO;
using System.Net;
using ServiceStack;

namespace Check.ServiceModel
{
    [Route("/return/string")]
    public class ReturnString : IReturn<string>
    {
        public string Data { get; set; }
    }

    [Route("/return/bytes")]
    public class ReturnBytes : IReturn<byte[]>
    {
        public byte[] Data { get; set; }
    }

    [Route("/return/stream")]
    public class ReturnStream : IReturn<Stream>
    {
        public byte[] Data { get; set; }
    }

    [Route("/return/httpwebresponse")]
    public class ReturnHttpWebResponse : IReturn<HttpWebResponse>
    {
        public byte[] Data { get; set; }
    }
}