using System;
using System.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class CsvServiceClient
        : ServiceClientBase
    {
        public override string Format => "csv";

        public CsvServiceClient() {}

        public CsvServiceClient(string baseUri) 
        {
            SetBaseUri(baseUri);
        }

        public CsvServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri) 
            : base(syncReplyBaseUri, asyncOneWayBaseUri)
        {
        }

        public override string ContentType => $"text/{Format}";

        public override void SerializeToStream(IRequest requestContext, object request, Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                CsvSerializer.SerializeToWriter(request, writer);
            }
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return CsvSerializer.DeserializeFromStream<T>(stream);
        }

        public override StreamDeserializerDelegate StreamDeserializer => CsvSerializer.DeserializeFromStream;
    }
}