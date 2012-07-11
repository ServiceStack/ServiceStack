using System;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.ServiceInterface
{
    public static class ServiceUtils
    {
        /// <summary>
        /// Naming convention for the ResponseStatus property name on the response DTO
        /// </summary>
        public const string ResponseStatusPropertyName = "ResponseStatus";

        /// <summary>
        /// Naming convention for the request's Response DTO
        /// </summary>
        public const string ResponseDtoSuffix = "Response";

        public static object CreateErrorResponse<TRequest>(TRequest request, ValidationErrorResult validationError)
        {
            var responseStatus = ResponseStatusTranslator.Instance.Parse(validationError);
            
            var errorResponse = CreateErrorResponse(
                request,
                new ValidationError(validationError),
                responseStatus);
            
            return errorResponse;
        }

        public static object CreateErrorResponse<TRequest>(TRequest request, Exception ex, ResponseStatus responseStatus)
        {
            var responseDto = CreateResponseDto(request, responseStatus);

            var httpError = ex as IHttpError;
            if (httpError != null)
            {
                if (responseDto != null)
                    httpError.Response = responseDto;

                return httpError;
            }

            var errorCode = ex.GetType().Name;
            var errorMsg = ex.Message;
            if (responseStatus != null)
            {
                errorCode = responseStatus.ErrorCode ?? errorCode;
                errorMsg = responseStatus.Message ?? errorMsg;
            }

            return new HttpError(responseDto, ex.ToStatusCode(), errorCode, errorMsg);
        }

        /// <summary>
        /// Create an instance of the service response dto type and inject it with the supplied responseStatus
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStatus"></param>
        /// <returns></returns>
        public static object CreateResponseDto<TRequest>(TRequest request, ResponseStatus responseStatus)
        {
            // Predict the Response message type name
            // Get the type
            var responseDtoType = AssemblyUtils.FindType(GetResponseDtoName(request));
            var responseDto = CreateResponseDto(request);

            if (responseDto == null)
                return null;

            // For faster serialization of exceptions, services should implement IHasResponseStatus
            var hasResponseStatus = responseDto as IHasResponseStatus;
            if (hasResponseStatus != null)
            {
                hasResponseStatus.ResponseStatus = responseStatus;
            }
            else
            {
                // Get the ResponseStatus property
                var responseStatusProperty = responseDtoType.GetProperty(ResponseStatusPropertyName);

                if (responseStatusProperty != null)
                {
                    // Set the ResponseStatus
                    ReflectionUtils.SetProperty(responseDto, responseStatusProperty, responseStatus);
                }
            }

            // Return an Error DTO with the exception populated
            return responseDto;
        }

        /// <summary>
        /// Create an instance of the response dto based on the requestDto type and default naming convention
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static object CreateResponseDto<TRequest>(TRequest request)
        {
            // Get the type
            var responseDtoType = AssemblyUtils.FindType(GetResponseDtoName(request));

            if (responseDtoType == null)
            {
                // We don't support creation of response messages without a predictable type name
                return null;
            }

            // Create an instance of the response message for this request
            var responseDto = ReflectionUtils.CreateInstance(responseDtoType);
            return responseDto;
        }

        public static string GetResponseDtoName<TRequest>(TRequest request)
        {
            return typeof(TRequest) != typeof(object)
                ? typeof(TRequest).FullName + ResponseDtoSuffix
                : request.GetType().FullName + ResponseDtoSuffix;
        }
    }
}