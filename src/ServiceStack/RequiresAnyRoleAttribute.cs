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
    /// can only execute, if the user has any of the specified roles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequiresAnyRoleAttribute : AuthenticateAttribute
    {
        public List<string> RequiredRoles { get; set; }

        public RequiresAnyRoleAttribute(ApplyTo applyTo, params string[] roles)
        {
            this.RequiredRoles = roles.ToList();
            this.ApplyTo = applyTo;
            this.Priority = (int)RequestFilterPriority.RequiredRole;
        }

        public RequiresAnyRoleAttribute(params string[] roles)
            : this(ApplyTo.All, roles)
        { }

        public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.HasValidAuthSecret(req))
                return;

            await base.ExecuteAsync(req, res, requestDto).ConfigAwait(); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            var session = await req.GetSessionAsync().ConfigAwait();

            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
#if NET472 || NETSTANDARD2_0 || NETCOREAPP3_1 || NET5_0
            await using (authRepo as IAsyncDisposable)
#else
            using (authRepo as IDisposable)
#endif
            {
                if (session != null && await session.HasRoleAsync(RoleNames.Admin, authRepo).ConfigAwait())
                    return;

                if (await HasAnyRolesAsync(req, session, authRepo).ConfigAwait())
                    return;
            }

            if (DoHtmlRedirectAccessDeniedIfConfigured(req, res))
                return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = ErrorMessages.InvalidRole.Localize(req);
            await HostContext.AppHost.HandleShortCircuitedErrors(req, res, requestDto).ConfigAwait();
        }

        public virtual bool HasAnyRoles(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            if (HasAnyRoles(session, authRepo)) 
                return true;

            session.UpdateFromUserAuthRepo(req, authRepo);

            if (HasAnyRoles(session, authRepo))
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public virtual async Task<bool> HasAnyRolesAsync(IRequest req, IAuthSession session, IAuthRepositoryAsync authRepo)
        {
            if (await HasAnyRolesAsync(session, authRepo).ConfigAwait()) 
                return true;

            await session.UpdateFromUserAuthRepoAsync(req, authRepo).ConfigAwait();

            if (await HasAnyRolesAsync(session, authRepo).ConfigAwait())
            {
                await req.SaveSessionAsync(session).ConfigAwait();
                return true;
            }
            return false;
        }

        public virtual bool HasAnyRoles(IAuthSession session, IAuthRepository authRepo)
        {
            return session != null && this.RequiredRoles
                .Any(requiredRole => session.HasRole(requiredRole, authRepo));
        }

        public virtual async Task<bool> HasAnyRolesAsync(IAuthSession session, IAuthRepositoryAsync authRepo)
        {
            return session != null && await this.RequiredRoles
                .AnyAsync(requiredRole => session.HasRoleAsync(requiredRole, authRepo)).ConfigAwait();
        }

        /// <summary>
        /// Check all session is in any supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        /// <param name="request"></param>
        /// <param name="requiredRoles"></param>
        public static void AssertRequiredRoles(IRequest req, params string[] requiredRoles)
        {
            if (requiredRoles.IsEmpty()) return;

            if (HostContext.HasValidAuthSecret(req))
                return;

            var session = req.GetSession();

            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                if (session != null && session.HasRole(RoleNames.Admin, authRepo))
                    return;

                if (session?.UserAuthId != null && requiredRoles.Any(x => session.HasRole(x, authRepo)))
                    return;

                session.UpdateFromUserAuthRepo(req);

                if (session?.UserAuthId != null && requiredRoles.Any(x => session.HasRole(x, authRepo)))
                    return;
            }

            var statusCode = session != null && session.IsAuthenticated
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;

            throw new HttpError(statusCode, ErrorMessages.InvalidRole.Localize(req));
        }
    }
}
