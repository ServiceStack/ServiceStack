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

            await base.ExecuteAsync(req, res, requestDto).ConfigAwait(); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)

            if (await HasAllPermissionsAsync(req, await req.AssertAuthenticatedSessionAsync().ConfigAwait(), RequiredPermissions).ConfigAwait())
                return;

            await HostContext.AppHost.HandleShortCircuitedErrors(req, res, requestDto,
                HttpStatusCode.Forbidden, ErrorMessages.InvalidPermission.Localize(req)).ConfigAwait();
        }

        [Obsolete("Use HasAllPermissionsAsync")]
        public bool HasAllPermissions(IRequest req, IAuthSession session, IAuthRepository authRepo)
        {
            if (SessionValidForAllPermissions(req, session, RequiredPermissions))
                return true;

            return session.HasAllPermissions(RequiredPermissions, authRepo, req);
        }

        public async Task<bool> HasAllPermissionsAsync(IRequest req, IAuthSession session, IAuthRepositoryAsync authRepo)
        {
            if (await SessionValidForAllPermissionsAsync(req, session, RequiredPermissions).ConfigAwait())
                return true;

            return await session.HasAllPermissionsAsync(RequiredPermissions, authRepo, req).ConfigAwait();
        }

        [Obsolete("Use HasAllPermissionsAsync")]
        public static bool HasAllPermissions(IRequest req, IAuthSession session, ICollection<string> requiredPermissions)
        {
            if (SessionValidForAllPermissions(req, session, requiredPermissions))
                return true;
            
            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                return session.HasAllPermissions(requiredPermissions, authRepo, req);
            }
        }
        
        public static async Task<bool> HasAllPermissionsAsync(IRequest req, IAuthSession session, ICollection<string> requiredPermissions, CancellationToken token=default)
        {
            if (await SessionValidForAllPermissionsAsync(req, session, requiredPermissions).ConfigAwait())
                return true;
            
            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(req);
            await using (authRepo as IAsyncDisposable)
            {
                return await session.HasAllPermissionsAsync(requiredPermissions, authRepo, req, token).ConfigAwait();
            }
        }

        [Obsolete("AssertRequiredPermissionsAsync")]
        public static void AssertRequiredPermissions(IRequest req, params string[] requiredPermissions)
        {
            var session = req.AssertAuthenticatedSession();
            if (HasAllPermissions(req, session, requiredPermissions))
                return;

            ThrowInvalidPermission(req);
        }

        /// <summary>
        /// Check all session is in all supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        public static async Task AssertRequiredPermissionsAsync(IRequest req, string[] requiredPermissions, CancellationToken token=default)
        {
            var session = await req.AssertAuthenticatedSessionAsync(token: token).ConfigAwait();
            if (await HasAllPermissionsAsync(req, session, requiredPermissions, token).ConfigAwait())
                return;

            ThrowInvalidPermission(req);
        }
        
        public static Task<bool> HasRequiredPermissionsAsync(IRequest req, string[] requiredPermissions) => 
            HasAllPermissionsAsync(req, req.GetSession(), requiredPermissions);

        [Obsolete("Use SessionValidForAllPermissionsAsync")]
        private static bool SessionValidForAllPermissions(IRequest req, IAuthSession session, ICollection<string> requiredPermissions)
        {
            if (requiredPermissions.IsEmpty()) 
                return true;
            
            if (HostContext.HasValidAuthSecret(req))
                return true;

            AssertAuthenticated(req, requestDto:req.Dto, session:session);

            return false;
        }

        private static async Task<bool> SessionValidForAllPermissionsAsync(IRequest req, IAuthSession session, ICollection<string> requiredPermissions)
        {
            if (requiredPermissions.IsEmpty()) 
                return true;
            
            if (HostContext.HasValidAuthSecret(req))
                return true;

            await AssertAuthenticatedAsync(req, requestDto:req.Dto, session:session).ConfigAwait();

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
