﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Auth;
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
            : this(ApplyTo.All, roles) { }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.HasValidAuthSecret(req))
                return;

            base.Execute(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed) return; //AuthenticateAttribute already closed the request (ie auth failed)

            var session = req.GetSession();
            if (HasAnyRoles(req, session)) return;

            if (DoHtmlRedirectIfConfigured(req, res)) return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = "Invalid Role";
            res.EndRequest();
        }

        public virtual bool HasAnyRoles(IRequest req, IAuthSession session, IAuthRepository userAuthRepo = null)
        {
            if (HasAnyRoles(session)) return true;

            session.UpdateFromUserAuthRepo(req, userAuthRepo);

            if (HasAnyRoles(session))
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public virtual bool HasAnyRoles(IAuthSession session)
        {
            return this.RequiredRoles
                .Any(requiredRole => session != null
                    && session.HasRole(requiredRole));
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

            if (session != null && session.UserAuthId != null && requiredRoles.Any(session.HasRole))
                return;

            session.UpdateFromUserAuthRepo(req);

            if (session != null && session.UserAuthId != null && requiredRoles.Any(session.HasRole))
                return;

            var statusCode = session != null && session.IsAuthenticated
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;

            throw new HttpError(statusCode, "Invalid Role");
        }
    }
}
