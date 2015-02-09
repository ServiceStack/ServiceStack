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
    }
}