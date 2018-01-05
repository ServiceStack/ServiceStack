namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
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

    public class ReturnsHttpResult { }

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

        public HttpResult Any(ReturnsHttpResult request)
        {
            return new HttpResult();
        }
    }


}