namespace ServiceStack.AI;

/// <summary>A provider has sent a request whose outcome must not be repeated by orchestration.</summary>
public sealed class ChatProviderRequestException(Exception error) : HttpError(
    error is HttpError http ? (int)http.StatusCode : 502,
    error is HttpError detail ? detail.ErrorCode : "ProviderRequestUncertain",
    error is HttpError known ? known.Message : "The provider request could not be completed. It has not been retried.");
