#nullable enable

using System;
using System.Linq;
using System.Security.Claims;

namespace ServiceStack;

public static class ClaimUtils
{
    public static string Admin { get; set; } = nameof(Admin);
    public static string PermissionType { get; set; } = "perm";

    public static bool IsAuthenticated(this ClaimsPrincipal? principal) => principal?.Identity?.IsAuthenticated == true;
    public static ClaimsPrincipal? AuthenticatedUser(this ClaimsPrincipal? principal) =>
        principal?.Identity?.IsAuthenticated == true ? principal : null;
    public static bool IsAdmin(this ClaimsPrincipal? principal) => principal?.IsInRole(Admin) == true;

    public static string? GetUserId(this ClaimsPrincipal? principal) => principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public static string? GetDisplayName(this ClaimsPrincipal? principal) => 
        principal?.FindFirst(principal.Identities.FirstOrDefault()?.NameClaimType ?? ClaimTypes.Name)?.Value;
    public static string? GetEmail(this ClaimsPrincipal principal) => principal.FindFirst(ClaimTypes.Email)?.Value;
    public static string[] GetRoles(this ClaimsPrincipal? principal) => principal?.Claims.Where(x => x.Type == ClaimTypes.Role)
        .Select(x => x.Value).ToArray() ?? Array.Empty<string>();
    public static string[] GetPermissions(this ClaimsPrincipal? principal) => principal?.Claims.Where(x => x.Type == PermissionType)
        .Select(x => x.Value).ToArray() ?? Array.Empty<string>();

    public static bool HasRole(this ClaimsPrincipal? principal, string roleName) => principal?.IsInRole(roleName) == true;
    public static bool HasAllRoles(this ClaimsPrincipal? principal, params string[] roleNames) => principal?.GetRoles()
        .All(roleNames.Contains) == true;
    
    public static bool HasScope(this ClaimsPrincipal? principal, string scope) => principal?.HasClaim(JwtClaimTypes.Scope, scope) == true;

    public static bool HasClaim(this ClaimsPrincipal? principal, string type, string value)
    {
        foreach (var claim in principal?.Claims ?? Array.Empty<Claim>())
        {
            if (claim.Type == type && claim.Value == value)
                return true;
        }
        return false;
    }    
}