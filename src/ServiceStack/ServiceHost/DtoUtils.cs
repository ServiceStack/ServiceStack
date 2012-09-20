using System;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost
{
    public static class DtoUtils
    {
        private static ILog Log = LogManager.GetLogger(typeof (DtoUtils));

        /// <summary>
        /// Naming convention for the ResponseStatus property name on the response DTO
        /// </summary>
        public const string ResponseStatusPropertyName = "ResponseStatus";

        /// <summary>
        /// Naming convention for the request's Response DTO
        /// </summary>
        public const string ResponseDtoSuffix = "Response";
        
        public static ResponseStatus ToResponseStatus(this Exception exception)
        {
            var validationError = exception as ValidationError;
            if (validationError != null)
            {
                return validationError.ToResponseStatus();
            }

            var httpError = exception as IHttpError;
            return httpError != null
                ? CreateErrorResponse(httpError.ErrorCode, httpError.Message)
                : CreateErrorResponse(exception.GetType().Name, exception.Message);
        }

        public static ResponseStatus ToResponseStatus(this ValidationError validationException)
        {
            return CreateErrorResponse(validationException.ErrorCode, validationException.Message, validationException.Violations);
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

        public static ResponseStatus CreateSuccessResponse(string message)
        {
            return new ResponseStatus { Message = message };
        }

        public static ResponseStatus CreateErrorResponse(string errorCode)
        {
            var errorMessage = errorCode.SplitCamelCase();
            return CreateErrorResponse(errorCode, errorMessage, null);
        }

        public static ResponseStatus CreateErrorResponse(string errorCode, string errorMessage)
        {
            return CreateErrorResponse(errorCode, errorMessage, null);
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

        public static string GetResponseDtoName<TRequest>(TRequest request)
        {
            return typeof(TRequest) != typeof(object)
                   ? typeof(TRequest).FullName + ResponseDtoSuffix
                   : request.GetType().FullName + ResponseDtoSuffix;
        }

        /// <summary>
        /// Creates the error response from the values provided.
        /// 
        /// If the errorCode is empty it will use the first validation error code, 
        /// if there is none it will throw an error.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="validationErrors">The validation errors.</param>
        /// <returns></returns>
        public static ResponseStatus CreateErrorResponse(string errorCode, string errorMessage, IEnumerable<ValidationErrorField> validationErrors)
        {
            var to = new ResponseStatus {
                ErrorCode = errorCode,
                Message = errorMessage,
                Errors = new List<ResponseError>(),
            };
            if (validationErrors != null)
            {
                foreach (var validationError in validationErrors)
                {
                    var error = new ResponseError {
                        ErrorCode = validationError.ErrorCode,
                        FieldName = validationError.FieldName,
                        Message = validationError.ErrorMessage,
                    };
                    to.Errors.Add(error);

                    if (String.IsNullOrEmpty(to.ErrorCode))
                    {
                        to.ErrorCode = validationError.ErrorCode;
                    }
                    if (String.IsNullOrEmpty(to.Message))
                    {
                        to.Message = validationError.ErrorMessage;
                    }
                }
            }
            if (String.IsNullOrEmpty(errorCode))
            {
                if (String.IsNullOrEmpty(to.ErrorCode))
                {
                    throw new ArgumentException("Cannot create a valid error response with a en empty errorCode and an empty validationError list");
                }
            }
            return to;
        }
        
        public static object HandleException<TRequest>(IAppHost appHost, TRequest request, Exception ex)
        {
            if (ex.InnerException != null && !(ex is IHttpError))
                ex = ex.InnerException;

            var responseStatus = ex.ToResponseStatus();

            if (EndpointHost.UserConfig.DebugMode)
            {
                // View stack trace in tests and on the client
                responseStatus.StackTrace = GetRequestErrorBody(request) + ex;
            }

            Log.Error("ServiceBase<TRequest>::Service Exception", ex);

            var errorResponse = CreateErrorResponse(request, ex, responseStatus);

            return errorResponse;
        }

        /// <summary>
        /// Override to provide additional/less context about the Service Exception. 
        /// By default the request is serialized and appended to the ResponseStatus StackTrace.
        /// </summary>
        public static string GetRequestErrorBody(object request)
        {
            var requestString = "";
            try
            {
                requestString = TypeSerializer.SerializeToString(request);
            }
            catch /*(Exception ignoreSerializationException)*/
            {
                //Serializing request successfully is not critical and only provides added error info
            }

            return String.Format("[{0}: {1}]:\n[REQUEST: {2}]", (request?? new object()).GetType().Name, DateTime.UtcNow, requestString);
        }
    }
}