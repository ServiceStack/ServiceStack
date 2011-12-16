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

	public class RequestDto2
	{
		public string Name { get; set; }
	}

	public class RestServiceWithAllVerbsImplemented : RestServiceBase<RequestDto2>
    {
		public override object OnGet(RequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

		public override object OnPut(RequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

		public override object OnPost(RequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

		public override object OnDelete(RequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

		public override object OnPatch(RequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }
    }
}