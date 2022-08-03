using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public static class UserAuthRepositoryExtensions
    {
        /// <summary>
        /// Creates the required missing tables or DB schema 
        /// </summary>
        public static void AssignRoles(this IAuthRepository userAuthRepo, IUserAuth userAuth,
            ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            if (userAuthRepo is IManageRoles managesRoles)
            {
                managesRoles.AssignRoles(userAuth.Id.ToString(), roles, permissions);
            }
            else
            {
                AssignRolesInternal(userAuth, roles, permissions);

                userAuthRepo.SaveUserAuth(userAuth);
            }
        }
        /// <summary>
        /// Creates the required missing tables or DB schema 
        /// </summary>
        public static async Task AssignRolesAsync(this IAuthRepositoryAsync userAuthRepo, IUserAuth userAuth,
            ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default)
        {
            if (userAuthRepo is IManageRolesAsync managesRoles)
            {
                await managesRoles.AssignRolesAsync(userAuth.Id.ToString(), roles, permissions, token).ConfigAwait();
            }
            else
            {
                AssignRolesInternal(userAuth, roles, permissions);

                await userAuthRepo.SaveUserAuthAsync(userAuth, token).ConfigAwait();
            }
        }

        private static void AssignRolesInternal(IUserAuth userAuth, ICollection<string> roles, ICollection<string> permissions)
        {
            if (!roles.IsEmpty())
            {
                foreach (var missingRole in roles.Where(x => userAuth.Roles == null || !userAuth.Roles.Contains(x)))
                {
                    if (userAuth.Roles == null)
                        userAuth.Roles = new List<string>();

                    userAuth.Roles.Add(missingRole);
                }
            }

            if (!permissions.IsEmpty())
            {
                foreach (var missingPermission in permissions.Where(x =>
                    userAuth.Permissions == null || !userAuth.Permissions.Contains(x)))
                {
                    if (userAuth.Permissions == null)
                        userAuth.Permissions = new List<string>();

                    userAuth.Permissions.Add(missingPermission);
                }
            }
        }

        public static void UnAssignRoles(this IAuthRepository userAuthRepo, IUserAuth userAuth,
            ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            if (userAuthRepo is IManageRoles managesRoles)
            {
                managesRoles.UnAssignRoles(userAuth.Id.ToString(), roles, permissions);
            }
            else
            {
                roles.Each(x => userAuth.Roles.Remove(x));
                permissions.Each(x => userAuth.Permissions.Remove(x));

                if (roles != null || permissions != null)
                {
                    userAuthRepo.SaveUserAuth(userAuth);
                }
            }
        }

        public static async Task UnAssignRolesAsync(this IAuthRepositoryAsync userAuthRepo, IUserAuth userAuth,
            ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default)
        {
            if (userAuthRepo is IManageRolesAsync managesRoles)
            {
                await managesRoles.UnAssignRolesAsync(userAuth.Id.ToString(), roles, permissions, token).ConfigAwait();
            }
            else
            {
                roles.Each(x => userAuth.Roles.Remove(x));
                permissions.Each(x => userAuth.Permissions.Remove(x));

                if (roles != null || permissions != null)
                {
                    await userAuthRepo.SaveUserAuthAsync(userAuth, token).ConfigAwait();
                }
            }
        }

        public static ICollection<string> GetRoles(this IAuthRepository userAuthRepo, IUserAuth userAuth)
        {
            return userAuthRepo is IManageRoles managesRoles 
                ? managesRoles.GetRoles(userAuth.Id.ToString()) 
                : userAuth.Roles;
        }

        public static async Task<ICollection<string>> GetRolesAsync(this IAuthRepositoryAsync userAuthRepo, IUserAuth userAuth, CancellationToken token=default)
        {
            return userAuthRepo is IManageRolesAsync managesRoles 
                ? await managesRoles.GetRolesAsync(userAuth.Id.ToString(), token).ConfigAwait()
                : userAuth.Roles;
        }

        public static ICollection<string> GetPermissions(this IAuthRepository userAuthRepo, IUserAuth userAuth)
        {
            return userAuthRepo is IManageRoles managesRoles 
                ? managesRoles.GetPermissions(userAuth.Id.ToString()) 
                : userAuth.Permissions;
        }

        public static async Task<ICollection<string>> GetPermissionsAsync(this IAuthRepositoryAsync userAuthRepo, IUserAuth userAuth, CancellationToken token=default)
        {
            return userAuthRepo is IManageRolesAsync managesRoles 
                ? await managesRoles.GetPermissionsAsync(userAuth.Id.ToString(), token).ConfigAwait()
                : userAuth.Permissions;
        }

        public static List<IAuthTokens> GetAuthTokens(this IAuthRepository repo, string userAuthId) =>
            repo != null && userAuthId != null
                ? repo.GetUserAuthDetails(userAuthId).ConvertAll(x => (IAuthTokens) x)
                : TypeConstants<IAuthTokens>.EmptyList; 

        public static async Task<List<IAuthTokens>> GetAuthTokensAsync(this IAuthRepositoryAsync repo, string userAuthId, CancellationToken token=default) =>
            repo != null && userAuthId != null
                ? (await repo.GetUserAuthDetailsAsync(userAuthId, token).ConfigAwait()).ConvertAll(x => (IAuthTokens) x)
                : TypeConstants<IAuthTokens>.EmptyList; 

        public static void PopulateSession(this IAuthSession session, IUserAuth userAuth, IAuthRepository authRepo = null)
        {
            if (userAuth == null)
                return;

            PopulateSessionInternal(session, userAuth);

            var hadUserAuthId = session.UserAuthId != null;
            if (hadUserAuthId && authRepo != null)
                session.ProviderOAuthAccess = authRepo.GetAuthTokens(session.UserAuthId);
            
            //session.UserAuthId could be populated in populator
            if (!hadUserAuthId && authRepo != null) 
                session.ProviderOAuthAccess = authRepo.GetAuthTokens(session.UserAuthId);
            
            var existingPopulator = AutoMappingUtils.GetPopulator(typeof(IAuthSession), typeof(IUserAuth));
            existingPopulator?.Invoke(session, userAuth);
        }

        public static async Task PopulateSessionAsync(this IAuthSession session, IUserAuth userAuth, IAuthRepositoryAsync authRepo = null, CancellationToken token=default)
        {
            if (userAuth == null)
                return;

            PopulateSessionInternal(session, userAuth);

            var hadUserAuthId = session.UserAuthId != null;
            if (hadUserAuthId && authRepo != null)
                session.ProviderOAuthAccess = await authRepo.GetAuthTokensAsync(session.UserAuthId, token).ConfigAwait();
            
            //session.UserAuthId could be populated in populator
            if (!hadUserAuthId && authRepo != null) 
                session.ProviderOAuthAccess = await authRepo.GetAuthTokensAsync(session.UserAuthId, token).ConfigAwait();
            
            var existingPopulator = AutoMappingUtils.GetPopulator(typeof(IAuthSession), typeof(IUserAuth));
            existingPopulator?.Invoke(session, userAuth);
        }

        private static void PopulateSessionInternal(IAuthSession session, IUserAuth userAuth)
        {
            var holdSessionId = session.Id;
            session.PopulateWith(userAuth);
            session.Id = holdSessionId;
            session.UserAuthId ??= (userAuth.Id != default ? userAuth.Id.ToString(CultureInfo.InvariantCulture) : null);
            if (userAuth.Meta != null)
                session.PopulateFromMap(userAuth.Meta);
            session.IsAuthenticated = true;

            if (string.IsNullOrEmpty(session.DisplayName))
            {
                session.DisplayName = session.UserName;
                if (string.IsNullOrEmpty(session.DisplayName)
                    && (!string.IsNullOrEmpty(session.FirstName) || !string.IsNullOrEmpty(session.LastName)))
                {
                    session.DisplayName = !string.IsNullOrEmpty(session.FirstName)
                        ? session.FirstName + (!string.IsNullOrEmpty(session.LastName) ? " " + session.LastName : "")
                        : session.LastName;
                }
            }
        }

        public static List<IUserAuthDetails> GetUserAuthDetails(this IAuthRepository authRepo, int userAuthId)
        {
            return authRepo.GetUserAuthDetails(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(this IAuthRepositoryAsync authRepo, int userAuthId, CancellationToken token=default)
        {
            return authRepo.GetUserAuthDetailsAsync(userAuthId.ToString(CultureInfo.InvariantCulture), token);
        }

        public static IUserAuth GetUserAuth(this IUserAuthRepository authRepo, int userAuthId)
        {
            return authRepo.GetUserAuth(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static Task<IUserAuth> GetUserAuthAsync(this IUserAuthRepositoryAsync authRepo, int userAuthId, CancellationToken token=default)
        {
            return authRepo.GetUserAuthAsync(userAuthId.ToString(CultureInfo.InvariantCulture), token);
        }

        public static void DeleteUserAuth(this IUserAuthRepository authRepo, int userAuthId)
        {
            authRepo.DeleteUserAuth(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static Task DeleteUserAuthAsync(this IUserAuthRepositoryAsync authRepo, int userAuthId, CancellationToken token=default)
        {
            return authRepo.DeleteUserAuthAsync(userAuthId.ToString(CultureInfo.InvariantCulture), token);
        }

        public static ICollection<string> GetRoles(this IManageRoles manageRoles, int userAuthId)
        {
            return manageRoles.GetRoles(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static Task<ICollection<string>> GetRolesAsync(this IManageRolesAsync manageRoles, int userAuthId, CancellationToken token=default)
        {
            return manageRoles.GetRolesAsync(userAuthId.ToString(CultureInfo.InvariantCulture), token);
        }

        public static ICollection<string> GetPermissions(this IManageRoles manageRoles, int userAuthId)
        {
            return manageRoles.GetPermissions(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static Task<ICollection<string>> GetPermissionsAsync(this IManageRolesAsync manageRoles, int userAuthId, CancellationToken token=default)
        {
            return manageRoles.GetPermissionsAsync(userAuthId.ToString(CultureInfo.InvariantCulture), token);
        }

        public static bool HasRole(this IManageRoles manageRoles, int userAuthId, string role)
        {
            return manageRoles.HasRole(userAuthId.ToString(CultureInfo.InvariantCulture), role);
        }

        public static Task<bool> HasRoleAsync(this IManageRolesAsync manageRoles, int userAuthId, string role, CancellationToken token=default)
        {
            return manageRoles.HasRoleAsync(userAuthId.ToString(CultureInfo.InvariantCulture), role, token);
        }

        public static bool HasPermission(this IManageRoles manageRoles, int userAuthId, string permission)
        {
            return manageRoles.HasPermission(userAuthId.ToString(CultureInfo.InvariantCulture), permission);
        }

        public static Task<bool> HasPermissionAsync(this IManageRolesAsync manageRoles, int userAuthId, string permission, CancellationToken token=default)
        {
            return manageRoles.HasPermissionAsync(userAuthId.ToString(CultureInfo.InvariantCulture), permission, token);
        }

        public static void AssignRoles(this IManageRoles manageRoles, int userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            manageRoles.AssignRoles(userAuthId.ToString(CultureInfo.InvariantCulture), roles, permissions);
        }

        public static Task AssignRolesAsync(this IManageRolesAsync manageRoles, int userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default)
        {
            return manageRoles.AssignRolesAsync(userAuthId.ToString(CultureInfo.InvariantCulture), roles, permissions, token);
        }

        public static void UnAssignRoles(this IManageRoles manageRoles, int userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            manageRoles.UnAssignRoles(userAuthId.ToString(CultureInfo.InvariantCulture), roles, permissions);
        }

        public static Task UnAssignRolesAsync(this IManageRolesAsync manageRoles, int userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default)
        {
            return manageRoles.UnAssignRolesAsync(userAuthId.ToString(CultureInfo.InvariantCulture), roles, permissions, token);
        }

        static IUserAuthRepository AssertUserAuthRepository(this IAuthRepository repo)
        {
            if (!(repo is IUserAuthRepository userRepo))
                throw new NotSupportedException("This operation requires a IUserAuthRepository");

            return userRepo;
        }

        static IUserAuthRepositoryAsync AssertUserAuthRepositoryAsync(this IAuthRepositoryAsync repo)
        
        {
            if (!(repo is IUserAuthRepositoryAsync userRepo))
                throw new NotSupportedException("This operation requires a IUserAuthRepositoryAsync");

            return userRepo;
        }

        public static IUserAuth CreateUserAuth(this IAuthRepository authRepo, IUserAuth newUser, string password) => 
            authRepo.AssertUserAuthRepository().CreateUserAuth(newUser, password);

        public static Task<IUserAuth> CreateUserAuthAsync(this IAuthRepositoryAsync authRepo, IUserAuth newUser, string password, CancellationToken token=default) => 
            authRepo.AssertUserAuthRepositoryAsync().CreateUserAuthAsync(newUser, password, token);

        public static IUserAuth UpdateUserAuth(this IAuthRepository authRepo, IUserAuth existingUser, IUserAuth newUser) => 
            authRepo.AssertUserAuthRepository().UpdateUserAuth(existingUser, newUser);

        public static Task<IUserAuth> UpdateUserAuthAsync(this IAuthRepositoryAsync authRepo, IUserAuth existingUser, IUserAuth newUser, CancellationToken token=default) => 
            authRepo.AssertUserAuthRepositoryAsync().UpdateUserAuthAsync(existingUser, newUser, token);

        public static IUserAuth UpdateUserAuth(this IAuthRepository authRepo, IUserAuth existingUser, IUserAuth newUser, string password) => 
            authRepo.AssertUserAuthRepository().UpdateUserAuth(existingUser, newUser, password);

        public static Task<IUserAuth> UpdateUserAuthAsync(this IAuthRepositoryAsync authRepo, IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token=default) => 
            authRepo.AssertUserAuthRepositoryAsync().UpdateUserAuthAsync(existingUser, newUser, password, token);

        public static IUserAuth GetUserAuth(this IAuthRepository authRepo, string userAuthId) => 
            authRepo.AssertUserAuthRepository().GetUserAuth(userAuthId);

        public static Task<IUserAuth> GetUserAuthAsync(this IAuthRepositoryAsync authRepo, string userAuthId, CancellationToken token=default) => 
            authRepo.AssertUserAuthRepositoryAsync().GetUserAuthAsync(userAuthId, token);

        public static void DeleteUserAuth(this IAuthRepository authRepo, string userAuthId) => 
            authRepo.AssertUserAuthRepository().DeleteUserAuth(userAuthId);

        public static Task DeleteUserAuthAsync(this IAuthRepositoryAsync authRepo, string userAuthId, CancellationToken token=default) => 
            authRepo.AssertUserAuthRepositoryAsync().DeleteUserAuthAsync(userAuthId, token);

        static IQueryUserAuth AssertQueryUserAuth(this IAuthRepository repo)
        {
            if (!(repo is IQueryUserAuth queryUserAuth))
                throw new NotSupportedException("This operation requires an Auth Repository that implements IQueryUserAuth");

            return queryUserAuth;
        }

        static IQueryUserAuthAsync AssertQueryUserAuthAsync(this IAuthRepositoryAsync repo)
        {
            if (!(repo is IQueryUserAuthAsync queryUserAuth))
                throw new NotSupportedException("This operation requires an Auth Repository that implements IQueryUserAuthAsync");

            return queryUserAuth;
        }

        public static List<IUserAuth> GetUserAuths(this IAuthRepository authRepo, string orderBy = null, int? skip = null, int? take = null) => 
            authRepo.AssertQueryUserAuth().GetUserAuths(orderBy: orderBy, skip: skip, take: take);

        public static Task<List<IUserAuth>> GetUserAuthsAsync(this IAuthRepositoryAsync authRepo, string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default) => 
            authRepo.AssertQueryUserAuthAsync().GetUserAuthsAsync(orderBy: orderBy, skip: skip, take: take, token: token);

        public static List<IUserAuth> SearchUserAuths(this IAuthRepository authRepo, string query, string orderBy = null, int? skip = null, int? take = null) => 
            authRepo.AssertQueryUserAuth().SearchUserAuths(query:query, orderBy: orderBy, skip: skip, take: take);

        public static Task<List<IUserAuth>> SearchUserAuthsAsync(this IAuthRepositoryAsync authRepo, string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default) => 
            authRepo.AssertQueryUserAuthAsync().SearchUserAuthsAsync(query:query, orderBy: orderBy, skip: skip, take: take, token: token);
        
        public static async Task MergeRolesAsync(this IAuthRepositoryAsync authRepo, string userAuthId, string source, ICollection<string> roles, CancellationToken token=default)
        {
            if (string.IsNullOrEmpty(userAuthId))
                throw new ArgumentNullException(nameof(userAuthId));
            if (roles.IsEmpty())
                return;
            
            if (authRepo is IManageSourceRolesAsync manageSourceRoles)
            {
                await manageSourceRoles.MergeRolesAsync(userAuthId, source, roles, token).ConfigAwait();
                return;
            }

            var log = LogManager.GetLogger(authRepo.GetType());
            var rolesStr = roles.Join(", ");
            log.Warn($"As '{authRepo.GetType()}' doesn't support IManageSourceRolesAsync, the '{rolesStr}' roles from '{source}' will only be added to user '{userAuthId}' and will need to be manually removed if removed from '{source}'");
            
            if (authRepo is IManageRolesAsync manageRoles)
            {
                await manageRoles.AssignRolesAsync(userAuthId, roles:roles, token: token).ConfigAwait();
            }
            else
            {
                var userAuth = await authRepo.GetUserAuthAsync(userAuthId, token: token).ConfigAwait();
                await authRepo.AssignRolesAsync(userAuth, roles: roles, token: token).ConfigAwait();
            }
        }
        
        public static Task<Tuple<ICollection<string>,ICollection<string>>> GetLocalRolesAndPermissionsAsync(this IManageRolesAsync manageRoles, string userAuthId, CancellationToken token=default)
        {
            if (manageRoles is IManageSourceRolesAsync manageSourceRoles)
            {
                return manageSourceRoles.GetLocalRolesAndPermissionsAsync(userAuthId, token);
            }
            return manageRoles.GetRolesAndPermissionsAsync(userAuthId, token);
        }

        public static void ValidateNewUser(this IUserAuth newUser)
        {
            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentException(ErrorMessages.UsernameOrEmailRequired, nameof(IUserAuth.UserName));

            if (!newUser.UserName.IsNullOrEmpty() && !HostContext.GetPlugin<AuthFeature>().IsValidUsername(newUser.UserName))
                throw new ArgumentException(ErrorMessages.IllegalUsername, "UserName");
        }

        public static void ValidateNewUser(this IUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentException(ErrorMessages.UsernameOrEmailRequired, nameof(IUserAuth.UserName));

            if (!newUser.UserName.IsNullOrEmpty())
            {
                if (!HostContext.GetPlugin<AuthFeature>().IsValidUsername(newUser.UserName))
                    throw new ArgumentException(ErrorMessages.IllegalUsername, "UserName");
            }
        }
    }
}
