using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ServiceStack.Blazor;

public static class ClaimUtils
{
    public const string AuthenticationType = "Server Authentication";
    public const string PermissionType = "perm";
    public const string Picture = "picture";
    public const string Admin = nameof(Admin);

    public static bool IsAuthenticated(this AuthenticationState? state) => state?.User?.AuthenticatedUser() != null;
    public static ClaimsPrincipal? AuthenticatedUser(this AuthenticationState? state) => state?.User?.AuthenticatedUser();

    public static bool IsAuthenticated(this ClaimsPrincipal? principal) => principal?.Identity?.IsAuthenticated == true;
    public static ClaimsPrincipal? AuthenticatedUser(this ClaimsPrincipal? principal) =>
        principal?.Identity?.IsAuthenticated == true ? principal : null;
    public static bool IsAdmin(this ClaimsPrincipal? principal) => principal?.GetRoles().Contains(Admin) != null;

    public static string? GetUserId(this ClaimsPrincipal? principal) => principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public static string? GetDisplayName(this ClaimsPrincipal? principal) => principal?.FindFirst(ClaimTypes.Name)?.Value;
    public static string? GetEmail(this ClaimsPrincipal principal) => principal.FindFirst(ClaimTypes.Email)?.Value;
    public static string[] GetRoles(this ClaimsPrincipal? principal) => principal?.Claims.Where(x => x.Type == ClaimTypes.Role)
        .Select(x => x.Value).ToArray() ?? Array.Empty<string>();
    public static string[] GetPermissions(this ClaimsPrincipal? principal) => principal?.Claims.Where(x => x.Type == PermissionType)
        .Select(x => x.Value).ToArray() ?? Array.Empty<string>();

    public static bool HasRole(this ClaimsPrincipal? principal, string roleName) => principal?.GetRoles()
        .Contains(roleName) == true;
    public static bool HasAllRoles(this ClaimsPrincipal? principal, params string[] roleNames) => principal?.GetRoles()
        .All(roleNames.Contains) == true;

    public static string? GetProfileUrl(this ClaimsPrincipal? principal) => 
        X.Map(principal?.FindFirst(Picture)?.Value, x => string.IsNullOrWhiteSpace(x) ? null : x)
        ?? BlazorConfig.Instance.DefaultProfileUrl;
}
