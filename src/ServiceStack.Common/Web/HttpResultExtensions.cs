using System;
using ServiceStack.Text;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Common.Web
{
    public static class HttpResultExtensions
    {
        /// <summary>
        /// Shortcut to get the ResponseDTO whether it's bare or inside a IHttpResult
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static object ToDto(this object response)
        {
            if (response == null) return null;
            var httpResult = response as IHttpResult;
            return httpResult != null ? httpResult.Response : response;
        }

        /// <summary>
        /// Alias of ToDto
        /// </summary>
        public static object ToResponseDto(this object response)
        {
            return ToDto(response);
        }

        /// <summary>
        /// Shortcut to get the ResponseDTO whether it's bare or inside a IHttpResult
        /// </summary>
        /// <param name="response"></param>
        /// <returns>TResponse if found; otherwise null</returns>
        public static TResponse ToDto<TResponse>(this object response) where TResponse : class
        {
            if (response == null) return default(TResponse);
            var httpResult = response as IHttpResult;
            return (httpResult != null ? httpResult.Response : response) as TResponse;
        }

        /// <summary>
        /// Alias of ToDto
        /// </summary>
        public static TResponse ToResponseDto<TResponse>(this object response) where TResponse : class
        {
            return ToDto<TResponse>(response);
        }

        public static object ToErrorResponse(this IHttpError httpError)
        {
            if (httpError == null) return null;
            var errorDto = httpError.ToDto();
            if (errorDto != null) return errorDto;

            var error = httpError as HttpError;
            return new ErrorResponse {
                ResponseStatus = new ResponseStatus {
                    ErrorCode = httpError.ErrorCode,
                    Message = httpError.Message,
                    StackTrace = error != null ? error.StackTrace : null,
                }
            };
        }

        /// <summary>
        /// Shortcut to get the ResponseStatus whether it's bare or inside a IHttpResult
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static ResponseStatus ToResponseStatus(this object response)
        {
            if (response == null) return null;

            var hasResponseStatus = response as IHasResponseStatus;
            if (hasResponseStatus != null)
                return hasResponseStatus.ResponseStatus;

            var propertyInfo = response.GetType().GetPropertyInfo("ResponseStatus");
            if (propertyInfo == null)
                return null;

            return ReflectionUtils.GetProperty(response, propertyInfo) as ResponseStatus;
        }

        /// <summary>
        /// Whether the response is an IHttpError or Exception
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool IsErrorResponse(this object response)
        {
            return response != null && (response is IHttpError || response is Exception);
        }
    }
}