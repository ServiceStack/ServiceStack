using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ServiceStack.Blazor;

public static class BlazorClaimUtils
{
    public static string AuthenticationType { get; set; } = "Server Authentication";
    public static string Picture { get; set; } = "picture";

    public static bool IsAuthenticated(this AuthenticationState? state) => state?.User?.AuthenticatedUser() != null;
    public static ClaimsPrincipal? AuthenticatedUser(this AuthenticationState? state) => state?.User?.AuthenticatedUser();
    public static string? GetProfileUrl(this ClaimsPrincipal? principal) => 
        X.Map(principal?.FindFirst(Picture)?.Value, x => string.IsNullOrWhiteSpace(x) ? null : x)
        ?? BlazorConfig.Instance.DefaultProfileUrl;
}
