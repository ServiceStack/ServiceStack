using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.ServiceClient.Web
{
    public class HtmlServiceClient: ServiceClientBase
    {
        public HtmlServiceClient()
        {
        }

        public HtmlServiceClient(string baseUri)
            // Can't call SetBaseUri as that appends the format specific suffixes.
            :base(baseUri, baseUri)
        {
        }

        public override string Format
        {
            // Don't return a format as we are not using a ServiceStack format specific endpoint, but 
            // rather the general purpose endpoint (just like a html <form> POST would use).
            get { return null; }
        }

        public override string Accept
        {
            get { return Common.Web.ContentType.Html; }
        }

        public override string ContentType
        {
            // Only used by the base class when POST-ing.
            get { return Common.Web.ContentType.FormUrlEncoded; }
        }

        public override void SerializeToStream(IRequestContext requestContext, object request, Stream stream)
        {
            var queryString = QueryStringSerializer.SerializeToString(request);
            stream.Write(queryString);
        }

        public override T DeserializeFromStream<T>(Stream stream)
        {
            return (T) DeserializeDtoFromHtml(typeof (T), stream);
        }

        public override StreamDeserializerDelegate StreamDeserializer
        {
            get { return DeserializeDtoFromHtml; }
        }

        private object DeserializeDtoFromHtml(Type type, Stream fromStream)
        {
            // TODO: No tests currently use the response, but this could be something that will come in handy later.
            // It isn't trivial though, will have to parse the HTML content.
            return Activator.CreateInstance(type);
        }
    }
}
