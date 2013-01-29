using System.Net;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;

namespace ServiceStack.ServiceHost.Tests.Routes
{
    public class OldApiRequestDto
    {
        public string Name { get; set; }
    }

    public class OldApiRestServiceWithSomeVerbsImplemented : RestServiceBase<OldApiRequestDto>
    {
        public override object OnGet(OldApiRequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }

        public override object OnPut(OldApiRequestDto request)
        {
            return new HttpResult { StatusCode = HttpStatusCode.OK };
        }
    }

	public class OldApiRequestDto2
	{
		public string Name { get; set; }
	}

	public class OldApiRestServiceWithAllVerbsImplemented : RestServiceBase<OldApiRequestDto2>
    {
		public override object OnGet(OldApiRequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

		public override object OnPut(OldApiRequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

		public override object OnPost(OldApiRequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

		public override object OnDelete(OldApiRequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }

		public override object OnPatch(OldApiRequestDto2 request)
        {
            return new HttpResult {StatusCode = HttpStatusCode.OK};
        }
    }

    
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