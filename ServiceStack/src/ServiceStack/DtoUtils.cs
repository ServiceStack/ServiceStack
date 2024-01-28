using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Model;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack;

public static class DtoUtils
{
    /// <summary>
    /// Naming convention for the ResponseStatus property name on the response DTO
    /// </summary>
    public const string ResponseStatusPropertyName = "ResponseStatus";

    public static ResponseStatus CreateResponseStatus(Exception ex, object request = null, bool debugMode = false)
    {
        var e = ex.UnwrapIfSingleException();
        bool ranFilter = false;
        var responseStatus = e is IResponseStatusConvertible customStatus ? customStatus.ToResponseStatus() : null;

        var appHost = ServiceStackHost.Instance;
        if (responseStatus == null && appHost != null)
        {
            responseStatus = appHost.CreateResponseStatus(e, request);
            ranFilter = true;
        }
        responseStatus ??= ResponseStatusUtils.CreateResponseStatus(e.GetType().Name, e.Message);

        if (!ranFilter && appHost != null)
        {
            responseStatus = PopulateResponseStatus(responseStatus, request, e, debugMode);
            appHost.OnExceptionTypeFilter(ex, responseStatus);
        }

        return responseStatus;
    }

    public static ResponseStatus PopulateResponseStatus(ResponseStatus responseStatus, object request, Exception e, bool debugMode = false)
    {
        if (debugMode)
        {
#if !NETCORE
                if (e is System.Web.HttpCompileException compileEx && compileEx.Results.Errors.HasErrors)
                {
                    responseStatus.Errors ??= [];
                    foreach (var err in compileEx.Results.Errors)
                    {
                        responseStatus.Errors.Add(new ResponseError { Message = err.ToString() });
                    }
                }
#endif
            // View stack trace in tests and on the client
            var sb = StringBuilderCache.Allocate();
                
            if (request != null)
            {
                try
                {
                    var str = $"[{request.GetType().GetOperationName()}: {DateTime.UtcNow}]:\n[REQUEST: {TypeSerializer.SerializeToString(request)}]";
                    sb.AppendLine(str);
                }
                catch (Exception requestEx)
                {
                    sb.AppendLine($"[{request.GetType().GetOperationName()}: {DateTime.UtcNow}]:\n[REQUEST: {requestEx.Message}]");
                }
            }
                
            if (e is HttpError httpError)
                sb.AppendLine(httpError.StackTrace);
            else
                sb.AppendLine(e.ToString());
                
            var innerMessages = new List<string>();
            var innerEx = e.InnerException;
            while (innerEx != null)
            {
                sb.AppendLine("");
                sb.AppendLine(innerEx.ToString());
                innerMessages.Add(innerEx.Message);
                innerEx = innerEx.InnerException;
            }
            var stackTrace = StringBuilderCache.ReturnAndFree(sb);
                
            responseStatus.StackTrace = stackTrace;
            if (innerMessages.Count > 0)
            {
                responseStatus.Meta ??= new Dictionary<string, string>();
                responseStatus.Meta["InnerMessages"] = innerMessages.Join("\n");
            }
        }

        return responseStatus;
    }

    public static ResponseStatus ToResponseStatus(this Exception exception, object requestDto = null)
    {
        var appHost = HostContext.AppHost;
        return appHost != null 
            ? appHost.CreateResponseStatus(exception, requestDto)
            : CreateResponseStatus(exception, requestDto);
    }

    public static ResponseStatus ToResponseStatus(this ValidationError validationException) => 
        ResponseStatusUtils.CreateResponseStatus(validationException.ErrorCode, validationException.Message, validationException.Violations);

    public static ResponseStatus ToResponseStatus(this ValidationErrorResult validationResult)
    {
        return validationResult.IsValid
            ? CreateSuccessResponse(validationResult.SuccessMessage)
            : ResponseStatusUtils.CreateResponseStatus(validationResult.ErrorCode, validationResult.ErrorMessage, validationResult.Errors);
    }

    public static ResponseStatus CreateSuccessResponse(string message)
    {
        return new ResponseStatus { Message = message };
    }

    public static ResponseStatus CreateResponseStatus(string errorCode)
    {
        var errorMessage = errorCode.SplitCamelCase();
        return ResponseStatusUtils.CreateResponseStatus(errorCode, errorMessage, null);
    }

    public static ResponseStatus CreateResponseStatus(string errorCode, string errorMessage)
    {
        return ResponseStatusUtils.CreateResponseStatus(errorCode, errorMessage, null);
    }

    public static object CreateErrorResponse(string errorCode, string errorMessage, IEnumerable<ValidationErrorField> validationErrors)
    {
        var responseStatus = ResponseStatusUtils.CreateResponseStatus(errorCode, errorMessage, validationErrors);
        var responseDto = CreateResponseDto(null, responseStatus);
        return new HttpError(responseDto, HttpStatusCode.BadRequest, errorCode, errorMessage);
    }

    public static object CreateErrorResponse(object request, ValidationErrorResult validationError)
    {
        var responseStatus = validationError.ToResponseStatus();

        var errorResponse = CreateErrorResponse(
            request,
            new ValidationError(validationError),
            responseStatus);

        return errorResponse;
    }

    public static object CreateErrorResponse(object request, Exception ex, ResponseStatus responseStatus)
    {
        var responseDto = CreateResponseDto(request, responseStatus);

        if (ex is IHttpError httpError)
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

        return new HttpError(responseDto, ex.ToStatusCode(), errorCode, errorMsg, ex);
    }

    /// <summary>
    /// Create an instance of the service response dto type and inject it with the supplied responseStatus
    /// </summary>
    /// <param name="request"></param>
    /// <param name="responseStatus"></param>
    /// <returns></returns>
    public static object CreateResponseDto(object request, ResponseStatus responseStatus)
    {
        // Predict the Response message type name
        var responseDtoType = WebRequestUtils.GetErrorResponseDtoType(request);
        var responseDto = responseDtoType.CreateInstance();
        if (responseDto == null)
            return null;

        // For faster serialization of exceptions, services should implement IHasResponseStatus
        if (responseDto is IHasResponseStatus hasResponseStatus)
        {
            hasResponseStatus.ResponseStatus = responseStatus;
        }
        else
        {
            var responseStatusProperty = responseDtoType.GetProperty(ResponseStatusPropertyName);
            // Set the ResponseStatus
            responseStatusProperty?.SetProperty(responseDto, responseStatus);
        }

        // Return an Error DTO with the exception populated
        return responseDto;
    }

    /// <summary>
    /// Create an Error Response DTO for the specified Request DTO from the Exception 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static object CreateErrorResponse(object request, Exception ex)
    {
        var appHost = HostContext.AppHost;
        if (appHost != null)
        {
            var useEx = appHost.UseException(ex);
            var responseStatus = CreateResponseStatus(useEx, request, appHost.Config.DebugMode);

            if (appHost.Config.DebugMode || appHost.IsDebugLogEnabled)
                appHost.OnLogError(appHost.GetType(), responseStatus.Message, useEx);
            return CreateErrorResponse(request, useEx, responseStatus);
        }
        else
        {
            var responseStatus = CreateResponseStatus(ex, request);
            return CreateErrorResponse(request, ex, responseStatus);
        }
    }
}