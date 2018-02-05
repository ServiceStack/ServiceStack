using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
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

            await base.ExecuteAsync(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            var session = req.GetSession();

            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                if (session != null && session.HasRole(RoleNames.Admin, authRepo))
                    return;

                if (HasAnyRoles(req, session, authRepo))
                    return;
            }

            if (DoHtmlRedirectIfConfigured(req, res))
                return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = ErrorMessages.InvalidRole.Localize(req);
            res.EndRequest();
        }

        public virtual bool HasAnyRoles(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            if (HasAnyRoles(session, authRepo)) return true;

            session.UpdateFromUserAuthRepo(req, authRepo);

            if (HasAnyRoles(session, authRepo))
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public virtual bool HasAnyRoles(IAuthSession session, IAuthRepository authRepo)
        {
            return this.RequiredRoles
                .Any(requiredRole => session != null
                    && session.HasRole(requiredRole, authRepo));
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
