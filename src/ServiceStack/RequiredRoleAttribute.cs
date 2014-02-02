using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            this.Priority = (int) RequestFilterPriority.RequiredRole;
        }

        public RequiredRoleAttribute(params string[] roles)
            : this(ApplyTo.All, roles) {}

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.AppHost.HasValidAuthSecret(req))
                return;

            base.Execute(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed) return; //AuthenticateAttribute already closed the request (ie auth failed)

            var session = req.GetSession();

            if (session != null && session.HasRole(RoleNames.Admin))
                return;

            if (HasAllRoles(req, session)) return;

            if (DoHtmlRedirectIfConfigured(req, res)) return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = "Invalid Role";
            res.EndRequest();
        }

        public bool HasAllRoles(IRequest req, IAuthSession session, IAuthRepository userAuthRepo=null)
        {
            if (HasAllRoles(session)) return true;

            session.UpdateFromUserAuthRepo(req, userAuthRepo);

            if (HasAllRoles(session))
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public bool HasAllRoles(IAuthSession session)
        {
            if (session == null)
                return false;

            return this.RequiredRoles.All(session.HasRole);
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

            if (session != null)
            {
                if (session.HasRole(RoleNames.Admin))
                    return;
                if (requiredRoles.All(session.HasRole))
                    return;
            }

            session.UpdateFromUserAuthRepo(req);

            if (session != null && requiredRoles.All(session.HasRole))
                return;

            var statusCode = session != null && session.IsAuthenticated
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;

            throw new HttpError(statusCode, "Invalid Role");
        }
    }

}
