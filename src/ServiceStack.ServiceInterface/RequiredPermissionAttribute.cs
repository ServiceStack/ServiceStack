﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// can only execute, if the user has specific permissions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method /*MVC Actions*/, Inherited = false, AllowMultiple = true)]
    public class RequiredPermissionAttribute : RequestFilterAttribute
    {
        public List<string> RequiredPermissions { get; set; }

        public RequiredPermissionAttribute(ApplyTo applyTo, params string[] permissions)
        {
            this.RequiredPermissions = permissions.ToList();
            this.ApplyTo = applyTo;
            this.Priority = (int) RequestFilterPriority.RequiredPermission;
        }

        public RequiredPermissionAttribute(params string[] permissions)
            : this(ApplyTo.All, permissions) {}

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            AuthenticateAttribute.AuthenticateIfBasicAuth(req, res);

            var session = req.GetSession();
            if (HasAllPermissions(req, session)) return;

            res.StatusCode = (int)HttpStatusCode.Unauthorized;
            res.StatusDescription = "Invalid Permissions";
			ServiceStack.WebHost.Endpoints.EndpointHost.AddGlobalResponseHeaders(res);
			res.Close();
        }

        public bool HasAllPermissions(IHttpRequest req, IAuthSession session, IUserAuthRepository userAuthRepo=null)
        {
            if (HasAllPermissions(session)) return true;

            if (userAuthRepo == null) 
                userAuthRepo = req.TryResolve<IUserAuthRepository>();

            if (userAuthRepo == null) return false;

            var userAuth = userAuthRepo.GetUserAuth(session, null);
            session.UpdateSession(userAuth);

            if (HasAllPermissions(session))
            {				
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public bool HasAllPermissions(IAuthSession session)
        {
            return this.RequiredPermissions
                .All(requiredPermission => session != null 
                    && session.HasPermission(requiredPermission));
        }
    }

}
