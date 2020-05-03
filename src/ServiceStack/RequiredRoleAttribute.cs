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
            : this(ApplyTo.All, roles) {}

        public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.AppHost.HasValidAuthSecret(req))
                return;

            await base.ExecuteAsync(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            if (HasAllRoles(req, req.GetSession(), RequiredRoles))
                return;

            if (DoHtmlRedirectAccessDeniedIfConfigured(req, res))
                return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = ErrorMessages.InvalidRole.Localize(req);
            await HostContext.AppHost.HandleShortCircuitedErrors(req, res, requestDto);
        }

        public bool HasAllRoles(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            if (SessionValidForAllRoles(req, session, RequiredRoles))
                return true;

            return SessionHasAllRoles(req, session, authRepo, RequiredRoles);
        }
        
        public static bool HasAllRoles(IRequest req, IAuthSession session, ICollection<string> requiredRoles)
        {
            if (SessionValidForAllRoles(req, session, requiredRoles))
                return true;
            
            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                return SessionHasAllRoles(req, session, authRepo, requiredRoles);
            }
        }

        /// <summary>
        /// Check all session is in all supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        /// <param name="req"></param>
        /// <param name="requiredRoles"></param>
        public static void AssertRequiredRoles(IRequest req, params string[] requiredRoles)
        {
            var session = req.GetSession();
            if (HasAllRoles(req, session, requiredRoles))
                return;

            var isAuthenticated = session != null && session.IsAuthenticated;
            if (!isAuthenticated)
                ThrowNotAuthenticated(req);
            else
                ThrowInvalidRole(req);
        }

        public static bool HasRequiredRoles(IRequest req, string[] requiredRoles) => HasAllRoles(req, req.GetSession(), requiredRoles);

        private static bool SessionHasAllRoles(IRequest req, IAuthSession session, IAuthRepository authRepo, ICollection<string> requiredRoles)
        {
            if (session.HasRole(RoleNames.Admin, authRepo))
                return true;

            if (requiredRoles.All(x => session.HasRole(x, authRepo)))
                return true;

            session.UpdateFromUserAuthRepo(req, authRepo);

            if (requiredRoles.All(x => session.HasRole(x, authRepo)))
            {
                req.SaveSession(session);
                return true;
            }

            return false;
        }
        
        private static bool SessionValidForAllRoles(IRequest req, IAuthSession session, ICollection<string> requiredRoles)
        {
            var singleRequiredRole = requiredRoles.Count == 1 ? requiredRoles.First() : null; 
            if (singleRequiredRole == RoleNames.AllowAnon)
                return true;

            if (requiredRoles.IsEmpty()) 
                return true;
            
            if (HostContext.HasValidAuthSecret(req))
                return true;

            AssertAuthenticated(req, requestDto:req.Dto, session:session);

            if (session != null && singleRequiredRole == RoleNames.AllowAnyUser && session.IsAuthenticated)
                return true;

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
