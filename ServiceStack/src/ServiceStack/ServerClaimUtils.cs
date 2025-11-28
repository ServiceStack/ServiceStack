#nullable enable
using ServiceStack.Web;

namespace ServiceStack;

public static class ServerClaimUtils
{
    /// <summary>
    /// Retrieves the User Id associated with the request from either API Key or ClaimsPrincipal.
    /// </summary>
    public static string? GetUserId(this IRequest? req)
    {
        var apiKeyUserId = req.GetApiKey()?.UserAuthId;
        if (apiKeyUserId != null)
            return apiKeyUserId;
        var user = req.GetClaimsPrincipal();
        return user.IsAuthenticated()
            ? user.GetUserId()
            : null;
    }

    /// <summary>
    /// Retrieves the required User Id associated with the request from either API Key or ClaimsPrincipal.
    /// Throws an unauthorized exception if the User Id is not available.
    /// </summary>
    public static string GetRequiredUserId(this IRequest? req) => req.GetUserId()
        ?? throw HttpError.Unauthorized("User Authentication required");
}