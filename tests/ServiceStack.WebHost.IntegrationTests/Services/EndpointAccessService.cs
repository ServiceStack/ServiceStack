using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    public class GetsOnly { }
    public class PostsOnly { }
    public class PutsOnly { }
    public class DeletesOnly { }
    public class AnyRequest { }
    public class Response { }

    [Restrict(VisibleLocalhostOnly = true)]
    public class VisibleLocalhost { }
    [Restrict(VisibleInternalOnly = true)]
    public class VisibleInternal { }

    [Restrict(LocalhostOnly = true)]
    public class LocalhostOnly { }
    [Restrict(InternalOnly = true)]
    public class InternalOnly { }

    [Restrict(RequestAttributes.Xml)]
    public class XmlOnly { }
    [Restrict(RequestAttributes.Json)]
    public class JsonOnly { }
    [Restrict(RequestAttributes.Jsv)]
    public class JsvOnly { }
    [Restrict(RequestAttributes.Csv)]
    public class CsvOnly { }
    [Restrict(RequestAttributes.ProtoBuf)]
    public class ProtoBufOnly { }
    [Restrict(RequestAttributes.Soap11)]
    public class Soap11Only { }
    [Restrict(RequestAttributes.Soap12)]
    public class Soap12Only { }
    [Restrict(RequestAttributes.FormatOther)]
    public class OtherFormatOnly { }

    [Restrict(
        RequestAttributes.InternalNetworkAccess | RequestAttributes.Json,
        RequestAttributes.External | RequestAttributes.Xml)]
    public class JsonInternalXmlExternal { }

    [Restrict(RequestAttributes.Secure)]
    public class SslOnly { }

    [Restrict(RequestAttributes.Secure   | RequestAttributes.External,
              RequestAttributes.InSecure | RequestAttributes.InternalNetworkAccess)]
    public class SslExternalAndInsecureInternal { }


    public class ReturnsHttpResult
    {
        public int Id { get; set; }
    }

    public class ReturnsHttpResultWithMarkerResult{}
    public class ReturnsHttpResultWithMarker : IReturn<ReturnsHttpResultWithMarkerResult>
    {
        public int Id { get; set; }
    }
    public class ReturnsHttpResultWithResponseResponse { }
    public class ReturnsHttpResultWithResponse 
    {
        public int Id { get; set; }
    }

    public class EndpointAccessService : Service
    {
        public Response Get(GetsOnly request)
        {
            return new Response();
        }
        
        public Response Post(PostsOnly request)
        {
            return new Response();
        }
        
        public Response Put(PutsOnly request)
        {
            return new Response();
        }
        
        public Response Delete(DeletesOnly request)
        {
            return new Response();
        }

        public Response Any(AnyRequest request)
        {
            return new Response();
        }

        public Response Any(VisibleLocalhost request)
        {
            return new Response();
        }

        public Response Any(VisibleInternal request)
        {
            return new Response();
        }

        public Response Any(LocalhostOnly request)
        {
            return new Response();
        }

        public Response Any(InternalOnly request)
        {
            return new Response();
        }

        public Response Any(XmlOnly request)
        {
            return new Response();
        }

        public Response Any(JsonOnly request)
        {
            return new Response();
        }

        public Response Any(JsvOnly request)
        {
            return new Response();
        }

        public Response Any(CsvOnly request)
        {
            return new Response();
        }

        public Response Any(ProtoBufOnly request)
        {
            return new Response();
        }

        public Response Any(Soap11Only request)
        {
            return new Response();
        }

        public Response Any(Soap12Only request)
        {
            return new Response();
        }

        public Response Any(OtherFormatOnly request)
        {
            return new Response();
        }

        public Response Any(JsonInternalXmlExternal request)
        {
            return new Response();
        }

        public HttpResult Any(ReturnsHttpResult request)
        {
            return new HttpResult();
        }

        public HttpResult Any(ReturnsHttpResultWithMarker request)
        {
            return new HttpResult();
        }

        public HttpResult Any(ReturnsHttpResultWithResponse request)
        {
            return new HttpResult();
        }
    }
}