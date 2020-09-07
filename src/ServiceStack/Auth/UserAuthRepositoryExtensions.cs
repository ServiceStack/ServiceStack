using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Auth
{
    public static class UserAuthRepositoryExtensions
    {
        /// <summary>
        /// Creates the required missing tables or DB schema 
        /// </summary>
        public static void AssignRoles(this IAuthRepository UserAuthRepo, IUserAuth userAuth,
            ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            if (UserAuthRepo is IManageRoles managesRoles)
            {
                managesRoles.AssignRoles(userAuth.Id.ToString(), roles, permissions);
            }
            else
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
                    foreach (var missingPermission in permissions.Where(x => userAuth.Permissions == null || !userAuth.Permissions.Contains(x)))
                    {
                        if (userAuth.Permissions == null)
                            userAuth.Permissions = new List<string>();

                        userAuth.Permissions.Add(missingPermission);
                    }
                }

                UserAuthRepo.SaveUserAuth(userAuth);
            }
        }

        public static void UnAssignRoles(this IAuthRepository UserAuthRepo, IUserAuth userAuth,
            ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            if (UserAuthRepo is IManageRoles managesRoles)
            {
                managesRoles.UnAssignRoles(userAuth.Id.ToString(), roles, permissions);
            }
            else
            {
                roles.Each(x => userAuth.Roles.Remove(x));
                permissions.Each(x => userAuth.Permissions.Remove(x));

                if (roles != null || permissions != null)
                {
                    UserAuthRepo.SaveUserAuth(userAuth);
                }
            }
        }

        public static ICollection<string> GetRoles(this IAuthRepository UserAuthRepo, IUserAuth userAuth)
        {
            return UserAuthRepo is IManageRoles managesRoles 
                ? managesRoles.GetRoles(userAuth.Id.ToString()) 
                : userAuth.Roles;
        }

        public static ICollection<string> GetPermissions(this IAuthRepository UserAuthRepo, IUserAuth userAuth)
        {
            return UserAuthRepo is IManageRoles managesRoles 
                ? managesRoles.GetPermissions(userAuth.Id.ToString()) 
                : userAuth.Permissions;
        }

        public static List<IAuthTokens> GetAuthTokens(this IAuthRepository repo, string userAuthId) =>
            repo != null && userAuthId != null
                ? repo.GetUserAuthDetails(userAuthId).ConvertAll(x => (IAuthTokens) x)
                : TypeConstants<IAuthTokens>.EmptyList; 

        public static async Task<List<IAuthTokens>> GetAuthTokensAsync(this IAuthRepositoryAsync repo, string userAuthId, CancellationToken token=default) =>
            repo != null && userAuthId != null
                ? (await repo.GetUserAuthDetailsAsync(userAuthId, token)).ConvertAll(x => (IAuthTokens) x)
                : TypeConstants<IAuthTokens>.EmptyList; 

        public static void PopulateSession(this IAuthSession session, IUserAuth userAuth, IAuthRepository authRepo = null)
        {
            if (userAuth == null)
                return;

            var holdSessionId = session.Id;
            session.PopulateWith(userAuth);  
            session.Id = holdSessionId;
            session.UserAuthId ??= (userAuth.Id != default ? userAuth.Id.ToString(CultureInfo.InvariantCulture) : null);
            session.IsAuthenticated = true;

            var hadUserAuthId = session.UserAuthId != null;
            if (hadUserAuthId && authRepo != null)
                session.ProviderOAuthAccess = authRepo.GetAuthTokens(session.UserAuthId);
            
            var existingPopulator = AutoMappingUtils.GetPopulator(typeof(IAuthSession), typeof(IUserAuth));
            existingPopulator?.Invoke(session, userAuth);
            
            //session.UserAuthId could be populated in populator
            if (!hadUserAuthId && authRepo != null) 
                session.ProviderOAuthAccess = authRepo.GetAuthTokens(session.UserAuthId);
        }

        public static async Task PopulateSessionAsync(this IAuthSession session, IUserAuth userAuth, IAuthRepositoryAsync authRepo = null, CancellationToken token=default)
        {
            if (userAuth == null)
                return;

            var holdSessionId = session.Id;
            session.PopulateWith(userAuth);  
            session.Id = holdSessionId;
            session.UserAuthId ??= (userAuth.Id != default ? userAuth.Id.ToString(CultureInfo.InvariantCulture) : null);
            session.IsAuthenticated = true;

            var hadUserAuthId = session.UserAuthId != null;
            if (hadUserAuthId && authRepo != null)
                session.ProviderOAuthAccess = await authRepo.GetAuthTokensAsync(session.UserAuthId, token);
            
            var existingPopulator = AutoMappingUtils.GetPopulator(typeof(IAuthSession), typeof(IUserAuth));
            existingPopulator?.Invoke(session, userAuth);
            
            //session.UserAuthId could be populated in populator
            if (!hadUserAuthId && authRepo != null) 
                session.ProviderOAuthAccess = await authRepo.GetAuthTokensAsync(session.UserAuthId, token);
        }

        public static List<IUserAuthDetails> GetUserAuthDetails(this IAuthRepository authRepo, int userAuthId)
        {
            return authRepo.GetUserAuthDetails(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static IUserAuth GetUserAuth(this IUserAuthRepository authRepo, int userAuthId)
        {
            return authRepo.GetUserAuth(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static void DeleteUserAuth(this IUserAuthRepository authRepo, int userAuthId)
        {
            authRepo.DeleteUserAuth(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static ICollection<string> GetRoles(this IManageRoles manageRoles, int userAuthId)
        {
            return manageRoles.GetRoles(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static ICollection<string> GetPermissions(this IManageRoles manageRoles, int userAuthId)
        {
            return manageRoles.GetPermissions(userAuthId.ToString(CultureInfo.InvariantCulture));
        }

        public static bool HasRole(this IManageRoles manageRoles, int userAuthId, string role)
        {
            return manageRoles.HasRole(userAuthId.ToString(CultureInfo.InvariantCulture), role);
        }

        public static bool HasPermission(this IManageRoles manageRoles, int userAuthId, string permission)
        {
            return manageRoles.HasPermission(userAuthId.ToString(CultureInfo.InvariantCulture), permission);
        }

        public static void AssignRoles(this IManageRoles manageRoles, int userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            manageRoles.AssignRoles(userAuthId.ToString(CultureInfo.InvariantCulture), roles, permissions);
        }

        public static void UnAssignRoles(this IManageRoles manageRoles, int userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            manageRoles.UnAssignRoles(userAuthId.ToString(CultureInfo.InvariantCulture), roles, permissions);
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

        public static Task<IUserAuth> UpdateUserAuthAsync(this IUserAuthRepositoryAsync authRepo, IUserAuth existingUser, IUserAuth newUser, CancellationToken token=default) => 
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

        public static void ValidateNewUser(this IUserAuth newUser)
        {
            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentNullException(ErrorMessages.UsernameOrEmailRequired);

            if (!newUser.UserName.IsNullOrEmpty() && !HostContext.GetPlugin<AuthFeature>().IsValidUsername(newUser.UserName))
                throw new ArgumentException(ErrorMessages.IllegalUsername, "UserName");
        }

        public static void ValidateNewUser(this IUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentNullException(ErrorMessages.UsernameOrEmailRequired);

            if (!newUser.UserName.IsNullOrEmpty())
            {
                if (!HostContext.GetPlugin<AuthFeature>().IsValidUsername(newUser.UserName))
                    throw new ArgumentException(ErrorMessages.IllegalUsername, "UserName");
            }
        }
    }
}
