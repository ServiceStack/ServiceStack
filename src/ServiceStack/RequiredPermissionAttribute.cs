﻿using System;
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
            this.Priority = (int) RequestFilterPriority.RequiredPermission;
        }

        public RequiredPermissionAttribute(params string[] permissions)
            : this(ApplyTo.All, permissions) {}

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.AppHost.HasValidAuthSecret(req))
                return;

            base.Execute(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed) return; //AuthenticateAttribute already closed the request (ie auth failed)

            var session = req.GetSession();

            if (session != null && session.HasRole(RoleNames.Admin))
                return;

            if (HasAllPermissions(req, session)) return;

            if (DoHtmlRedirectIfConfigured(req, res)) return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = ErrorMessages.InvalidPermission.Localize(req);
            res.EndRequest();
        }

        public bool HasAllPermissions(IRequest req, IAuthSession session, IAuthRepository userAuthRepo=null)
        {
            if (HasAllPermissions(session)) return true;

            session.UpdateFromUserAuthRepo(req, userAuthRepo);

            if (HasAllPermissions(session))
            {
                req.SaveSession(session);
                return true;
            }
            return false;
        }

        public bool HasAllPermissions(IAuthSession session)
        {
            if (session == null)
                return false;

            return this.RequiredPermissions.All(session.HasPermission);
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
            return Equals((RequiredPermissionAttribute) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ (RequiredPermissions != null ? RequiredPermissions.GetHashCode() : 0);
            }
        }
    }

}
