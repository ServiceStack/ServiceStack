using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Indicates that the request dto, which is associated with this attribute,
/// can only execute, if the user has specific permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class RequiresAnyPermissionAttribute : AuthenticateAttribute
{
    public List<string> RequiredPermissions { get; set; }

    public RequiresAnyPermissionAttribute(ApplyTo applyTo, params string[] permissions)
    {
        this.RequiredPermissions = permissions.ToList();
        this.ApplyTo = applyTo;
        this.Priority = (int)RequestFilterPriority.RequiredPermission;
    }

    public RequiresAnyPermissionAttribute(params string[] permissions)
        : this(ApplyTo.All, permissions) {}

    public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
    {
        if (HostContext.HasValidAuthSecret(req))
            return;

        await base.ExecuteAsync(req, res, requestDto).ConfigAwait(); //first check if session is authenticated
        if (res.IsClosed)
            return; //AuthenticateAttribute already closed the request (ie auth failed)

        var session = await req.AssertAuthenticatedSessionAsync().ConfigAwait();

        var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
        await using (authRepo as IAsyncDisposable)
        {
            if (await session.HasAnyPermissionsAsync(RequiredPermissions, authRepo, req).ConfigAwait())
                return;

            if (await session.HasRoleAsync(RoleNames.Admin, authRepo).ConfigAwait())
                return;
        }

        await HostContext.AppHost.HandleShortCircuitedErrors(req, res, requestDto,
            HttpStatusCode.Forbidden, ErrorMessages.InvalidPermission.Localize(req)).ConfigAwait();
    }

    [Obsolete("Use HasAnyPermissionsAsync")]
    public virtual bool HasAnyPermissions(IRequest req, IAuthSession session, IAuthRepository authRepo)
    {
        return session.HasAnyPermissions(RequiredPermissions, authRepo, req);
    }

}