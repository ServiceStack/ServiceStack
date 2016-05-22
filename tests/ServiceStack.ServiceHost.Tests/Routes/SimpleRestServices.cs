using System.Net;
using ServiceStack.Web;

namespace ServiceStack.ServiceHost.Tests.Routes
{
    public class NewApiRequestDto
    {
        public string Name { get; set; }
    }

    public class NewApiRequestDto2
    {
        public string Name { get; set; }
    }

    public class NewApiRestServiceWithAllVerbsImplemented : IService
    {
        public object Get(NewApiRequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Put(NewApiRequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Post(NewApiRequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Delete(NewApiRequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Patch(NewApiRequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Get(NewApiRequestDto2 request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Put(NewApiRequestDto2 request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Post(NewApiRequestDto2 request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Delete(NewApiRequestDto2 request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Patch(NewApiRequestDto2 request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }
    }

    public class NewApiRequestDtoWithId
    {
        public int Id { get; set; }
    }

    public class NewApiRequestDtoWithIdService : IService
    {
        public object Get(NewApiRequestDtoWithId request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Any(NewApiRequestDtoWithId request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }
    }

    public class NewApiRequestDtoWithFieldId
    {
        public int Id { get; set; }
    }

    public class NewApiRequestDtoWithFieldIdService : IService
    {
        public object Get(NewApiRequestDtoWithFieldId request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public object Any(NewApiRequestDtoWithFieldId request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }
    }

}