using System.Net;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;

namespace ServiceStack.ServiceHost.Tests.Routes
{
    public class RequestDto
    {
        public string Name { get; set; }
    }

    public class RestServiceWithSomeVerbsImplemented : RestServiceBase<RequestDto>
    {
        public override object OnGet(RequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public override object OnPut(RequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }
    }

    public class RestServiceWithAllVerbsImplemented : RestServiceBase<RequestDto>
    {
        public override object OnGet(RequestDto request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

        public override object OnPut(RequestDto request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

        public override object OnPost(RequestDto request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

        public override object OnDelete(RequestDto request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

        public override object OnPatch(RequestDto request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }
    }
}