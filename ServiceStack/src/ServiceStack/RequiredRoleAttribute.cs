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
    /// Protect access to this API to only Authenticated Users assigned with all specified Roles
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

            await base.ExecuteAsync(req, res, requestDto).ConfigAwait(); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            if (await HasAllRolesAsync(req, await req.GetSessionAsync().ConfigAwait(), RequiredRoles).ConfigAwait())
                return;

            await HandleShortCircuitedErrors(req, res, requestDto,
                HttpStatusCode.Forbidden, ErrorMessages.InvalidRole.Localize(req)).ConfigAwait();
        }

        [Obsolete("Use HasAllRolesAsync")]
        public bool HasAllRoles(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            if (SessionValidForAllRoles(req, session, RequiredRoles))
                return true;

            return session.HasAllRoles(RequiredRoles, authRepo, req);
        }

        public async Task<bool> HasAllRolesAsync(IRequest req, IAuthSession session, IAuthRepositoryAsync authRepo)
        {
            if (await SessionValidForAllRolesAsync(req, session, RequiredRoles).ConfigAwait())
                return true;

            return await session.HasAllRolesAsync(RequiredRoles, authRepo, req).ConfigAwait();
        }
        
        [Obsolete("Use HasAllRolesAsync")]
        public static bool HasAllRoles(IRequest req, IAuthSession session, ICollection<string> requiredRoles)
        {
            if (SessionValidForAllRoles(req, session, requiredRoles))
                return true;
            
            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                return session.HasAllRoles(requiredRoles, authRepo, req);
            }
        }

        public static async Task<bool> HasAllRolesAsync(IRequest req, ICollection<string> requiredRoles, CancellationToken token = default)
        {
            if (PreAuthenticatedValidForAllRoles(req, requiredRoles))
                return true;

            var session = await req.AssertAuthenticatedSessionAsync(token:token).ConfigAwait();
            return await HasAllRolesAsync(req, session, requiredRoles, token);
        }

        public static async Task<bool> HasAllRolesAsync(IRequest req, IAuthSession session, ICollection<string> requiredRoles, CancellationToken token=default)
        {
            if (await SessionValidForAllRolesAsync(req, session, requiredRoles))
                return true;
            
            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
            await using (authRepo as IAsyncDisposable)
            {
                return await session.HasAllRolesAsync(requiredRoles, authRepo, req, token).ConfigAwait();
            }
        }

        [Obsolete("Use AssertRequiredRolesAsync")]
        public static void AssertRequiredRoles(IRequest req, params string[] requiredRoles)
        {
            var session = req.AssertAuthenticatedSession();
            if (HasAllRoles(req, session, requiredRoles))
                return;

            ThrowInvalidRole(req);
        }

        /// <summary>
        /// Check all session is in all supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        public static Task AssertRequiredRoleAsync(IRequest req, string requiredRole, CancellationToken token = default) =>
            AssertRequiredRolesAsync(req, new[] { requiredRole }, token);

        /// <summary>
        /// Check all session is in all supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        public static async Task AssertRequiredRolesAsync(IRequest req, string[] requiredRoles, CancellationToken token=default)
        {
            if (await HasAllRolesAsync(req, requiredRoles, token).ConfigAwait())
                return;

            ThrowInvalidRole(req);
        }

        [Obsolete("Use HasRequiredRolesAsync")]
        public static bool HasRequiredRoles(IRequest req, string[] requiredRoles) => HasAllRoles(req, req.GetSession(), requiredRoles);

        public static Task<bool> HasRequiredRolesAsync(IRequest req, string[] requiredRoles) => HasAllRolesAsync(req, req.GetSession(), requiredRoles);

        public static bool PreAuthenticatedValidForAllRoles(IRequest req, ICollection<string> requiredRoles)
        {
            var singleRequiredRole = requiredRoles.Count == 1 ? requiredRoles.First() : null; 
            if (singleRequiredRole == RoleNames.AllowAnon)
                return true;

            if (requiredRoles.IsEmpty()) 
                return true;
            
            if (HostContext.HasValidAuthSecret(req))
                return true;

            return false;
        }

        [Obsolete("Use SessionValidForAllRolesAsync")]
        private static bool SessionValidForAllRoles(IRequest req, IAuthSession session, ICollection<string> requiredRoles)
        {
            if (PreAuthenticatedValidForAllRoles(req, requiredRoles))
                return true;

            AssertAuthenticated(req, requestDto:req.Dto, session:session);

            var singleRequiredRole = requiredRoles.Count == 1 ? requiredRoles.First() : null;
            if (singleRequiredRole == RoleNames.AllowAnyUser && session.IsAuthenticated)
                return true;

            return false;
        }
        
        private static async Task<bool> SessionValidForAllRolesAsync(IRequest req, IAuthSession session, ICollection<string> requiredRoles)
        {
            if (PreAuthenticatedValidForAllRoles(req, requiredRoles))
                return true;

            await AssertAuthenticatedAsync(req, requestDto:req.Dto, session:session).ConfigAwait();

            var singleRequiredRole = requiredRoles.Count == 1 ? requiredRoles.First() : null;
            if (singleRequiredRole == RoleNames.AllowAnyUser && session.IsAuthenticated)
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
