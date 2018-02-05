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
    /// can only execute, if the user has specific roles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequiredRoleAttribute : AuthenticateAttribute
    {
        public List<string> RequiredRoles { get; set; }

        public RequiredRoleAttribute(ApplyTo applyTo, params string[] roles)
        {
            this.RequiredRoles = roles.ToList();
            this.ApplyTo = applyTo;
            this.Priority = (int)RequestFilterPriority.RequiredRole;
        }

        public RequiredRoleAttribute(params string[] roles)
            : this(ApplyTo.All, roles)
        { }

        public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.AppHost.HasValidAuthSecret(req))
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

                if (HasAllRoles(req, session, authRepo))
                    return;
            }

            if (DoHtmlRedirectIfConfigured(req, res))
                return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = ErrorMessages.InvalidRole.Localize(req);
            res.EndRequest();
        }

        public bool HasAllRoles(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            if (HasAllRoles(session, authRepo)) return true;

            session.UpdateFromUserAuthRepo(req, authRepo);

            if (HasAllRoles(session, authRepo))
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public bool HasAllRoles(IAuthSession session, IAuthRepository authRepo)
        {
            if (session == null)
                return false;

            return this.RequiredRoles.All(x => session.HasRole(x, authRepo));
        }

        /// <summary>
        /// Check all session is in all supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        /// <param name="req"></param>
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
                if (session != null)
                {
                    if (session.HasRole(RoleNames.Admin, authRepo))
                        return;
                    if (requiredRoles.All(x => session.HasRole(x, authRepo)))
                        return;

                    session.UpdateFromUserAuthRepo(req, authRepo);
                }
            }

            if (session != null && requiredRoles.All(x => session.HasRole(x, authRepo)))
                return;

            var statusCode = session != null && session.IsAuthenticated
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;

            throw new HttpError(statusCode, ErrorMessages.InvalidRole.Localize(req));
        }

        public static bool HasRequiredRoles(IRequest req, string[] requiredRoles)
        {
            if (requiredRoles.IsEmpty())
                return true;

            if (HostContext.HasValidAuthSecret(req))
                return true;

            var session = req.GetSession();

            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                if (session != null)
                {
                    if (session.HasRole(RoleNames.Admin, authRepo))
                        return true;
                    if (requiredRoles.All(x => session.HasRole(x, authRepo)))
                        return true;

                    session.UpdateFromUserAuthRepo(req);
                }

                if (session != null && requiredRoles.All(x => session.HasRole(x, authRepo)))
                    return true;
            }

            return false;
        }

        protected bool Equals(RequiredRoleAttribute other)
        {
            return base.Equals(other)
                && Equals(RequiredRoles, other.RequiredRoles);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RequiredRoleAttribute)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (RequiredRoles?.GetHashCode() ?? 0);
            }
        }
    }

}
