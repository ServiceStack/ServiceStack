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
    /// can only execute, if the user has specific permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequiredPermissionAttribute : AuthenticateAttribute
    {
        public List<string> RequiredPermissions { get; set; }

        public RequiredPermissionAttribute(ApplyTo applyTo, params string[] permissions)
        {
            this.RequiredPermissions = permissions.ToList();
            this.ApplyTo = applyTo;
            this.Priority = (int)RequestFilterPriority.RequiredPermission;
        }

        public RequiredPermissionAttribute(params string[] permissions)
            : this(ApplyTo.All, permissions)
        { }

        public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.AppHost.HasValidAuthSecret(req))
                return;

            await base.ExecuteAsync(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            if (HasAllPermissions(req, req.GetSession(), RequiredPermissions))
                return;

            if (DoHtmlRedirectAccessDeniedIfConfigured(req, res))
                return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = ErrorMessages.InvalidPermission.Localize(req);
            await HostContext.AppHost.HandleShortCircuitedErrors(req, res, requestDto);
        }

        public bool HasAllPermissions(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            if (SessionValidForAllPermissions(req, session, RequiredPermissions))
                return true;

            return SessionHasAllPermissions(req, session, authRepo, RequiredPermissions);
        }
        
        public static bool HasAllPermissions(IRequest req, IAuthSession session, ICollection<string> requiredPermissions)
        {
            if (SessionValidForAllPermissions(req, session, requiredPermissions))
                return true;
            
            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                return SessionHasAllPermissions(req, session, authRepo, requiredPermissions);
            }
        }

        /// <summary>
        /// Check all session is in all supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        /// <param name="req"></param>
        /// <param name="requiredPermissions"></param>
        public static void AssertRequiredPermissions(IRequest req, params string[] requiredPermissions)
        {
            var session = req.GetSession();
            if (HasAllPermissions(req, session, requiredPermissions))
                return;

            var statusCode = session != null && session.IsAuthenticated
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;

            throw new HttpError(statusCode, ErrorMessages.InvalidPermission.Localize(req));
        }

        public static bool HasRequiredPermissions(IRequest req, string[] requiredPermissions) =>  HasAllPermissions(req, req.GetSession(), requiredPermissions);

        private static bool SessionHasAllPermissions(IRequest req, IAuthSession session, IAuthRepository authRepo, ICollection<string> requiredPermissions)
        {
            if (session.HasRole(RoleNames.Admin, authRepo))
                return true;

            if (requiredPermissions.All(x => session.HasPermission(x, authRepo)))
                return true;

            session.UpdateFromUserAuthRepo(req, authRepo);

            if (requiredPermissions.All(x => session.HasPermission(x, authRepo)))
            {
                req.SaveSession(session);
                return true;
            }

            return false;
        }

        private static bool SessionValidForAllPermissions(IRequest req, IAuthSession session, ICollection<string> requiredPermissions)
        {
            if (requiredPermissions.IsEmpty()) 
                return true;
            
            if (HostContext.HasValidAuthSecret(req))
                return true;

            if (session != null && session.IsAuthenticated)
                return true;

            return false;
        }

        protected bool Equals(RequiredPermissionAttribute other)
        {
            return base.Equals(other)
                && Equals(RequiredPermissions, other.RequiredPermissions);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RequiredPermissionAttribute)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (RequiredPermissions?.GetHashCode() ?? 0);
            }
        }
    }

}
