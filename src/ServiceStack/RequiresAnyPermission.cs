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

            await base.ExecuteAsync(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            var session = req.GetSession();

            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                if (session != null && session.HasRole(RoleNames.Admin, authRepo))
                    return;

                if (HasAnyPermissions(req, session, authRepo))
                    return;
            }

            if (DoHtmlRedirectIfConfigured(req, res))
                return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = ErrorMessages.InvalidPermission.Localize(req);
            res.EndRequest();
        }

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

        public virtual bool HasAnyPermissions(IAuthSession session, IAuthRepository authRepo)
        {
            return this.RequiredPermissions
                .Any(requiredPermission => session != null
                    && session.HasPermission(requiredPermission, authRepo));
        }
    }

}
