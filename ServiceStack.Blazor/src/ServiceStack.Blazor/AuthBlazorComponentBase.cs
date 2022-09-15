using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Data;
using System.Security.Claims;

namespace ServiceStack.Blazor;

/// <summary>
/// For Pages and Components requiring Authentication
/// </summary>
public abstract class AuthBlazorComponentBase : BlazorComponentBase
{
    [CascadingParameter]
    protected Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected bool HasInit { get; set; }

    protected bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    protected ClaimsPrincipal? User { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var state = await AuthenticationStateTask!;
        User = state.User;
        HasInit = true;
    }

    protected virtual bool CanAccess(MetadataOperationType op)
    {
        if (op == null) throw new ArgumentNullException(nameof(op));
        if (op.RequiresAuth == false)
            return true;

        if (!IsAuthenticated)
            return false;

        if (User.IsAdmin())
            return true;

        var roles = User.GetRoles();
        var permissions = User.GetPermissions();

        if (op.RequiredRoles != null && op.RequiredRoles.All(role => roles.Contains(role)))
            return false;
        if (op.RequiresAnyRole?.Count > 0 && !op.RequiresAnyRole.Any(role => roles.Contains(role)))
            return false;
        if (op.RequiredPermissions != null && op.RequiredPermissions.All(permission => permissions.Contains(permission)))
            return false;
        if (op.RequiresAnyPermission?.Count > 0 && !op.RequiresAnyPermission.Any(permission => permissions.Contains(permission)))
            return false;

        return true;
    }

    public virtual string? InvalidAccessMessage(MetadataOperationType op)
    {
        if (op == null) throw new ArgumentNullException(nameof(op));
        if (op.RequiresAuth == false)
            return null;

        var roles = User.GetRoles();
        var permissions = User.GetPermissions();

        var missingRoles = op.RequiredRoles?.Where(x => !roles.Contains(x)).ToArray() ?? Array.Empty<string>();
        if (missingRoles.Length > 0)
            return $"Requires {missingRoles.Map(x => $"<b>{x}</b>").Join(", ")} Role" + (missingRoles.Length > 1 ? "s" : "");
        var missingPerms = op.RequiredPermissions?.Where(x => !roles.Contains(x)).ToArray() ?? Array.Empty<string>();
        if (missingPerms.Length > 0)
            return $"Requires {missingPerms.Map(x => $"<b>{x}</b>").Join(", ")} Perm" + (missingPerms.Length > 1 ? "s" : "");


        if (op.RequiresAnyRole?.Count > 0 && !op.RequiresAnyRole.Any(role => roles.Contains(role)))
            return $"Requires any ${op.RequiresAnyRole.Where(x => !roles.Contains(x)).Map(x => $"<b>{x}</b>").Join(", ")} Role"
                + (missingRoles.Length > 1 ? "s" : "");
        if (op.RequiresAnyPermission?.Count > 0 && !op.RequiresAnyPermission.Any(perm => permissions.Contains(perm)))
            return $"Requires any ${op.RequiresAnyPermission.Where(x => !permissions.Contains(x)).Map(x => $"<b>{x}</b>").Join(", ")} Permission"
                + (missingRoles.Length > 1 ? "s" : "");

        return null;
    }
}

