using System;
using Check.ServiceModel;
using ServiceStack;

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