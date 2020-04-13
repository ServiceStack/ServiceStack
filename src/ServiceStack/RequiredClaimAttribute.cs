﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequiredClaimAttribute : AuthenticateAttribute
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public RequiredClaimAttribute(ApplyTo applyTo, string type, string value)
        {
            this.Type = type;
            this.Value = value;
            this.ApplyTo = applyTo;
            this.Priority = (int)RequestFilterPriority.RequiredRole;
        }

        public RequiredClaimAttribute(string type, string value)
            : this(ApplyTo.All, type, value) { }

        public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.AppHost.HasValidAuthSecret(req))
                return;

            await base.ExecuteAsync(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            if (HasClaim(req, Type, Value))
                return;

            if (DoHtmlRedirectAccessDeniedIfConfigured(req, res))
                return;

            res.StatusCode = (int)HttpStatusCode.Forbidden;
            res.StatusDescription = ErrorMessages.ClaimDoesNotExistFmt.Fmt(Type, Value).Localize(req);
            await HostContext.AppHost.HandleShortCircuitedErrors(req, res, requestDto);
        }

        public static bool HasClaim(IRequest req, string type, string value)
        {
            var claims = req.GetClaims().ToList();

            if (claims.HasRole(RoleNames.Admin))
                return true;

            if (claims.HasClaim(type, value))
                return true;

            return false;
        }

        protected bool Equals(RequiredClaimAttribute other)
        {
            return base.Equals(other) && string.Equals(Type, other.Type) && string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RequiredClaimAttribute)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}