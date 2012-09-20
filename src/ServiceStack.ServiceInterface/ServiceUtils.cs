using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Validation;

namespace ServiceStack.ServiceInterface
{
    public static class ServiceUtils
    {
        public static object CreateErrorResponse<TRequest>(TRequest request, ValidationErrorResult validationError)
        {
            var responseStatus = ResponseStatusTranslator.Instance.Parse(validationError);
            
            var errorResponse = DtoUtils.CreateErrorResponse(
                request,
                new ValidationError(validationError),
                responseStatus);
            
            return errorResponse;
        }
    }
}