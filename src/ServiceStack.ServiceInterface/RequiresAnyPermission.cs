using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface
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
            : this(ApplyTo.All, permissions) { }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            base.Execute(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed) return; //AuthenticateAttribute already closed the request (ie auth failed)

            var session = req.GetSession();
            if (HasAnyPermissions(req, session)) return;

            if (DoHtmlRedirectIfConfigured(req, res)) return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = "Invalid Permission";
            res.EndRequest();
        }

        public bool HasAnyPermissions(IHttpRequest req, IAuthSession session, IUserAuthRepository userAuthRepo = null)
        {
            if (HasAnyPermissions(session)) return true;

            if (userAuthRepo == null)
                userAuthRepo = req.TryResolve<IUserAuthRepository>();

            if (userAuthRepo == null) return false;

            var userAuth = userAuthRepo.GetUserAuth(session, null);
            session.UpdateSession(userAuth);

            if (HasAnyPermissions(session))
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public bool HasAnyPermissions(IAuthSession session)
        {
            return this.RequiredPermissions
                .Any(requiredPermission => session != null
                    && session.HasPermission(requiredPermission));
        }
    }

}
