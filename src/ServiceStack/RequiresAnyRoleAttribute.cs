using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Text;
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
            : this(ApplyTo.All, roles)
        { }

        public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.HasValidAuthSecret(req))
                return;

            await base.ExecuteAsync(req, res, requestDto).ConfigAwait(); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            var session = await req.AssertAuthenticatedSessionAsync().ConfigAwait();

            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
            await using (authRepo as IAsyncDisposable)
            {
                if (await session.HasAnyRolesAsync(RequiredRoles, authRepo, req).ConfigAwait())
                    return;
            }

            await HandleShortCircuitedErrors(req, res, requestDto,
                HttpStatusCode.Forbidden, ErrorMessages.InvalidRole.Localize(req)).ConfigAwait();
        }

        [Obsolete("Use HasAnyRolesAsync")]
        public virtual bool HasAnyRoles(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            return session.HasAnyRoles(RequiredRoles, authRepo, req);
        }
        
        /// <summary>
        /// Check all session is in any supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        [Obsolete("Use AssertRequiredRolesAsync")]
        public static void AssertRequiredRoles(IRequest req, params string[] requiredRoles)
        {
            if (requiredRoles.IsEmpty()) return;

            if (HostContext.HasValidAuthSecret(req))
                return;

            var session = req.AssertAuthenticatedSession();

            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                if (session.HasAnyRoles(requiredRoles, authRepo, req))
                    return;
            }

            throw new HttpError(HttpStatusCode.Forbidden, ErrorMessages.InvalidRole.Localize(req));
        }
    }
}
