#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack;

public static class ApiResult
{
    public const string FieldErrorCode = "ValidationException";

    public static ApiResult<TResponse> Create<TResponse>(TResponse response) => new(response);

    public static ApiResult<TResponse> CreateError<TResponse>(ResponseStatus errorStatus) => new(errorStatus);
    public static ApiResult<EmptyResponse> CreateError(ResponseStatus errorStatus) => new(errorStatus);
    
    public static ApiResult<TResponse> CreateError<TResponse>(string message, string? errorCode = nameof(Exception)) =>
        new(ErrorUtils.CreateError(message, errorCode));
    public static ApiResult<EmptyResponse> CreateError(string message, string? errorCode = nameof(Exception)) =>
        new(ErrorUtils.CreateError(message, errorCode));

    public static ApiResult<TResponse> CreateError<TResponse>(Exception ex) => new(ErrorUtils.CreateError(ex));
    public static ApiResult<EmptyResponse> CreateError(Exception ex) => new(ErrorUtils.CreateError(ex));

    public static ApiResult<TResponse> CreateFieldError<TResponse>(string fieldName, string message, string? errorCode = FieldErrorCode)
    {
        var apiResult = new ApiResult<TResponse>();
        apiResult.AddFieldError(fieldName, message, errorCode);
        return apiResult;
    }
    public static ApiResult<EmptyResponse> CreateFieldError(string fieldName, string message,
        string? errorCode = FieldErrorCode)
    {
        var apiResult = new ApiResult<EmptyResponse>();
        apiResult.AddFieldError(fieldName, message, errorCode);
        return apiResult;
    }
}

public class ApiResult<TResponse> : IHasErrorStatus
{
    public TResponse? Response { get; }

    public ResponseStatus? Error { get; private set; }

    public bool Failed => Error.IsError();
    public bool Succeeded => !Failed && Response != null;
    public bool Completed => Response != null || Error != null;

    public string? ErrorMessage => Error?.Message;

    public ResponseError[] Errors => Error?.Errors.ToArray() ?? TypeConstants<ResponseError>.EmptyArray;

    public string? ErrorSummary => Error != null && Errors.Length == 0
        ? Error.Message
        : null;

    public ApiResult() { }
    public ApiResult(TResponse response) => Response = response;
    public ApiResult(ResponseStatus errorStatus) => Error = errorStatus;
    
    public string? FieldErrorMessage(string fieldName) => Error.FieldErrorMessage(fieldName);

    public ResponseError? FieldError(string fieldName) => Error.FieldError(fieldName);

    public bool HasFieldError(string fieldName) => Error.HasErrorField(fieldName);

    public void ClearErrors() => Error = null;

    public void AddFieldError(string fieldName, string message, string? errorCode = ApiResult.FieldErrorCode)
    {
        Error ??= new ResponseStatus {
            ErrorCode = ErrorUtils.FieldErrorCode,
            Message = message,
        };
        Error.AddFieldError(fieldName, message);
    }

    public void SetError(string errorMessage, string errorCode = nameof(Exception))
    {
        Error ??= new ResponseStatus();
        Error.Message = errorMessage;
        Error.ErrorCode = errorCode;
    }
}

public static class ErrorUtils
{
    public const string FieldErrorCode = "ValidationException";

    public static bool IsSuccess(this ResponseStatus? status) => !status.IsError();
    public static bool IsError(this ResponseStatus? status) => status?.ErrorCode != null || status?.Message != null;

    public static ResponseError? FieldError(this ResponseStatus? status, string fieldName) => status?.Errors?
        .FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

    public static string? FieldErrorMessage(this ResponseStatus? status, string fieldName) => status?.Errors?
        .FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))?.Message;

    public static bool HasErrorField(this ResponseStatus? status, string fieldName) =>
        status?.FieldError(fieldName) != null;

    public static bool ShowSummary(this ResponseStatus? status, params string[] exceptFields)
    {
        if (!status.IsError())
            return false;

        // Don't show summary message if an error for a visible field exists
        foreach (var fieldName in exceptFields)
        {
            if (status.HasErrorField(fieldName))
                return false;
        }

        return true;
    }

    public static string? SummaryMessage(this ResponseStatus? status, params string[] exceptFields)
    {
        if (status.ShowSummary(exceptFields))
        {
            // Return first field error that's not visible
            if (status?.Errors != null)
            {
                var fieldSet = new HashSet<string>(exceptFields, StringComparer.OrdinalIgnoreCase);
                foreach (var errorField in status.Errors)
                {
                    if (fieldSet.Contains(errorField.FieldName))
                        continue;
                    return errorField.Message;
                }
            }

            // otherwise return summary message
            return status?.Message;
        }

        return null;
    }

    public static ResponseStatus AsResponseStatus(this Exception ex) => CreateError(ex);

    public static ResponseStatus CreateError(Exception ex) => ex switch {
        IResponseStatusConvertible rsc => rsc.ToResponseStatus(),
        IHasResponseStatus hasStatus => hasStatus.ResponseStatus,
        ArgumentException { ParamName: { } } ae => CreateFieldError(ae.ParamName, ex.Message, ex.GetType().Name),
        _ => CreateError(ex.Message, ex.GetType().Name)
    };

    public static ResponseStatus CreateError(string message, string? errorCode = nameof(Exception)) => new() {
        ErrorCode = errorCode,
        Message = message,
    };

    public static ResponseStatus CreateFieldError(string fieldName, string errorMessage,
        string errorCode = FieldErrorCode) =>
        new ResponseStatus().AddFieldError(fieldName, errorMessage, errorCode);

    public static ResponseStatus AddFieldError(this ResponseStatus status, string fieldName, string errorMessage,
        string errorCode = FieldErrorCode)
    {
        var fieldError = status.FieldError(fieldName);
        if (fieldError != null)
        {
            fieldError.ErrorCode = errorCode;
            fieldError.Message = errorMessage;
        }
        else
        {
            status.Errors ??= new List<ResponseError>();
            status.Errors.Add(new ResponseError {
                FieldName = fieldName,
                Message = errorMessage,
                ErrorCode = errorCode,
            });
        }
        return status;
    }
}

public static class ApiResultUtils
{
    public static ApiResult<TResponse> ToApiResult<TResponse>(this Exception ex) => ApiResult.CreateError<TResponse>(ex);
    public static ApiResult<EmptyResponse> ToApiResult(this Exception ex) => ApiResult.CreateError(ex);

    /// <summary>
    /// Annotate Request DTOs with IGet, IPost, etc HTTP Verb markers to specify which HTTP Method is used:
    /// https://docs.servicestack.net/csharp-client.html#http-verb-interface-markers
    /// </summary>
    public static ApiResult<TResponse> Api<TResponse>(this IServiceClient client, IReturn<TResponse> request)
    {
        try
        {
            var result = client.Send(request);
            return ApiResult.Create(result);
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }

    /// <summary>
    /// Annotate Request DTOs with IGet, IPost, etc HTTP Verb markers to specify which HTTP Method is used:
    /// https://docs.servicestack.net/csharp-client.html#http-verb-interface-markers
    /// </summary>
    public static async Task<ApiResult<TResponse>> ApiAsync<TResponse>(this IServiceClient client,
        IReturn<TResponse> request)
    {
        try
        {
            var result = await client.SendAsync(request).ConfigAwait();
            return ApiResult.Create(result);
        }
        catch (Exception ex)
        {
            return ex.ToApiResult<TResponse>();
        }
    }

    /// <summary>
    /// Annotate Request DTOs with IGet, IPost, etc HTTP Verb markers to specify which HTTP Method is used:
    /// https://docs.servicestack.net/csharp-client.html#http-verb-interface-markers
    /// </summary>
    public static ApiResult<EmptyResponse> Api(this IServiceClient client, IReturnVoid request)
    {
        try
        {
            client.Send(request);
            return ApiResult.Create(new EmptyResponse());
        }
        catch (Exception ex)
        {
            return ex.ToApiResult();
        }
    }

    /// <summary>
    /// Annotate Request DTOs with IGet, IPost, etc HTTP Verb markers to specify which HTTP Method is used:
    /// https://docs.servicestack.net/csharp-client.html#http-verb-interface-markers
    /// </summary>
    public static async Task<ApiResult<EmptyResponse>> ApiAsync(this IServiceClient client, IReturnVoid request)
    {
        try
        {
            await client.SendAsync(request).ConfigAwait();
            return ApiResult.Create(new EmptyResponse());
        }
        catch (Exception ex)
        {
            return ex.ToApiResult();
        }
    }

    /// <summary>
    /// Convert AutoLogin RegisterResponse into AuthenticateResponse to avoid additional trip
    /// </summary>
    public static AuthenticateResponse ToAuthenticateResponse(this RegisterResponse from) => new() {
        // don't use automapping/reflection as needs to work in AOT/Blazor 
        UserId = from.UserId,
        SessionId = from.SessionId,
        UserName = from.UserName,
        ReferrerUrl = from.ReferrerUrl,
        BearerToken = from.BearerToken,
        RefreshToken = from.RefreshToken,
        Roles = from.Roles,
        Permissions = from.Permissions,
        Meta = from.Meta,
    };
}