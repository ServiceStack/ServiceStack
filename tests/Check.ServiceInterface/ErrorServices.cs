using System;
using Check.ServiceModel;
using ServiceStack;
using ServiceStack.Web;

namespace Check.ServiceInterface
{

    public class CustomHttpErrorService : Service
    {
        public object Any(CustomHttpError request)
        {
            throw new HttpError(request.StatusCode, request.StatusDescription);
        }

        public object Any(AlwaysThrows request)
        {
            throw new Exception(request.GetType().Name);
        }
    }

    public class AlwaysThrowsService : Service
    {
        [AlwaysThrows]
        public object Any(AlwaysThrowsFilterAttribute request) => request;

        public object Any(AlwaysThrowsGlobalFilter request) => request;
    }

    public class AlwaysThrowsAttribute : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            throw new Exception(requestDto.GetType().Name);
        }
    }

    public class CustomFieldHttpErrorService : Service
    {
        public object Any(CustomFieldHttpError request)
        {
            throw new HttpError(new CustomFieldHttpErrorResponse
            {
                Custom = "Ignored",
                ResponseStatus = new ResponseStatus("StatusErrorCode", "StatusErrorMessage")
            },
            500,
            "HeaderErrorCode");
        }
    }
}