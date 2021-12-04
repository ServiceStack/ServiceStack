#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceStack;

public class ApiResult<TResponse>
{
    public TResponse? Response { get; }

    public ResponseStatus? ErrorStatus { get; private set; }

    public bool Completed => Response != null || ErrorStatus != null;
    public bool IsError => ErrorStatus.IsError();
    public bool IsSuccess => !IsError && Response != null;

    public string? ErrorMessage => ErrorStatus?.Message;

    public string? ErrorSummary => ErrorStatus != null && (ErrorStatus.Errors == null || ErrorStatus.Errors.Count == 0)
        ? ErrorStatus.Message
        : null;

    public string? FieldErrorMessage(string fieldName) => ErrorStatus.FieldErrorMessage(fieldName);

    public ResponseError? FieldError(string fieldName) => ErrorStatus.FieldError(fieldName);

    public bool HasFieldError(string fieldName) => ErrorStatus.HasFieldError(fieldName);

    public ApiResult(TResponse response)
    {
        Response = response;
    }

    public ApiResult(ResponseStatus errorStatus)
    {
        ErrorStatus = errorStatus;
    }

    public ApiResult() { }

    public void Reset() => ErrorStatus = null;

    public void AddFieldError(string fieldName, string message)
    {
        ErrorStatus ??= new ResponseStatus {
            ErrorCode = ApiResultUtils.FieldErrorCode,
            Message = message,
        };
        ErrorStatus.AddFieldError(fieldName, message);
    }

    public void SetError(string errorMessage, string errorCode = nameof(Exception))
    {
        ErrorStatus ??= new ResponseStatus();
        ErrorStatus.Message = errorMessage;
        ErrorStatus.ErrorCode = errorCode;
    }
}

public static class ApiResultUtils
{
    public const string FieldErrorCode = "ValidationException";

    public static bool IsSuccess(this ResponseStatus? status) => !status.IsError();
    public static bool IsError(this ResponseStatus? status) => status?.ErrorCode != null || status?.Message != null;

    public static ResponseError? FieldError(this ResponseStatus? status, string fieldName) => status?.Errors?
        .FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase));

    public static string? FieldErrorMessage(this ResponseStatus? status, string fieldName) => status?.Errors?
        .FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))?.Message;

    public static bool HasFieldError(this ResponseStatus? status, string fieldName) => status?.FieldError(fieldName) != null;

    public static bool ShowSummary(this ResponseStatus? status, params string[] exceptFields)
    {
        if (!status.IsError())
            return false;

        // Don't show summary message if an error for a visible field exists
        foreach (var fieldName in exceptFields)
        {
            if (status.HasFieldError(fieldName))
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

    public static ResponseStatus CreateError(string message, string? errorCode = nameof(Exception)) =>
        new() {
            ErrorCode = errorCode,
            Message = message,
        };

    public static ResponseStatus AddFieldError(this ResponseStatus status, string fieldName, string errorMessage,
        string errorCode = FieldErrorCode)
    {
        var fieldError = status.FieldError(fieldName);
        if (fieldError != null)
        {
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

    /// <summary>
    /// Annotate Request DTOs with IGet, IPost, etc HTTP Verb markers to specify which HTTP Method is used:
    /// https://docs.servicestack.net/csharp-client.html#http-verb-interface-markers
    /// </summary>
    public static async Task<ApiResult<TResponse>> ApiAsync<TResponse>(this IServiceClient client, IReturn<TResponse> request)
    {
        try
        {
            var result = await client.SendAsync(request);
            return new ApiResult<TResponse>(result);
        }
        catch (Exception ex)
        {
            if (ex is WebServiceException webEx)
                return new ApiResult<TResponse>(webEx.ResponseStatus);

            return new ApiResult<TResponse>(new ResponseStatus {
                ErrorCode = ex.GetType().Name,
                Message = ex.Message,
            });
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
            await client.SendAsync(request);
            return new ApiResult<EmptyResponse>(new EmptyResponse());
        }
        catch (Exception ex)
        {
            if (ex is WebServiceException webEx)
                return new ApiResult<EmptyResponse>(webEx.ResponseStatus);

            return new ApiResult<EmptyResponse>(new ResponseStatus
            {
                ErrorCode = ex.GetType().Name,
                Message = ex.Message,
            });
        }
    }
}