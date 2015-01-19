using Check.ServiceModel;
using ServiceStack;

namespace Check.ServiceInterface
{
    public class ErrorsService : Service
    {
        public object Any(ThrowHttpError request)
        {
            throw new HttpError(request.Status, request.Message);
        }

        public object Any(Throw404 request)
        {
            throw HttpError.NotFound(request.Message ?? "Custom Status Description");
        }
    }
}