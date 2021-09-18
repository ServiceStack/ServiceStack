using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
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
#if NET472 || NETSTANDARD2_0
            await using (authRepo as IAsyncDisposable)
#else
            using (authRepo as IDisposable)
#endif
            {
                if (await HasAnyPermissionsAsync(req, session, authRepo).ConfigAwait())
                    return;

                if (await session.HasRoleAsync(RoleNames.Admin, authRepo).ConfigAwait())
                    return;
            }

            await HostContext.AppHost.HandleShortCircuitedErrors(req, res, requestDto,
                HttpStatusCode.Forbidden, ErrorMessages.InvalidPermission.Localize(req)).ConfigAwait();
        }

        [Obsolete("Use HasAnyPermissionsAsync")]
        public bool HasAnyPermissions(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            if (HasAnyPermissions(session, authRepo)) return true;

            if (authRepo == null)
                authRepo = HostContext.AppHost.GetAuthRepository(req);

            if (authRepo == null)
                return false;

            using (authRepo as IDisposable)
            {
                var userAuth = authRepo.GetUserAuth(session, null);
                session.UpdateSession(userAuth);

                if (HasAnyPermissions(session, authRepo))
                {
                    req.SaveSession(session);
                    return true;
                }
                return false;
            }
        }

        public async Task<bool> HasAnyPermissionsAsync(IRequest req, IAuthSession session, IAuthRepositoryAsync authRepo)
        {
            if (await HasAnyPermissionsAsync(session, authRepo).ConfigAwait()) 
                return true;

            await session.UpdateFromUserAuthRepoAsync(req, authRepo).ConfigAwait();

            if (await HasAnyPermissionsAsync(session, authRepo).ConfigAwait())
            {
                await req.SaveSessionAsync(session).ConfigAwait();
                return true;
            }
            return false;
        }

        public virtual bool HasAnyPermissions(IAuthSession session, IAuthRepository authRepo)
        {
            var allPerms = session.GetPermissions(authRepo);
            return this.RequiredPermissions.Any(allPerms.Contains);
        }

        public virtual async Task<bool> HasAnyPermissionsAsync(IAuthSession session, IAuthRepositoryAsync authRepo)
        {
            var allPerms = await session.GetPermissionsAsync(authRepo).ConfigAwait();
            return this.RequiredPermissions.Any(allPerms.Contains);
        }
    }

}
