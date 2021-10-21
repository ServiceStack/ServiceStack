using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Auth
{
    public class UserAuthRepositoryAsyncWrapper : IUserAuthRepositoryAsync, IRequiresSchema, ICustomUserAuth, IQueryUserAuthAsync
    {
        public IAuthRepository AuthRepo { get; }
        public UserAuthRepositoryAsyncWrapper(IAuthRepository authRepo) => this.AuthRepo = authRepo;

        public Task LoadUserAuthAsync(IAuthSession session, IAuthTokens tokens, CancellationToken token = default)
        {
            AuthRepo.LoadUserAuth(session, tokens);
            return TypeConstants.EmptyTask;
        }

        public Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token = default)
        {
            AuthRepo.SaveUserAuth(authSession);
            return TypeConstants.EmptyTask;
        }

        public Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token = default)
        {
            return AuthRepo.GetUserAuthDetails(userAuthId).InTask();
        }

        public Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
        {
            return AuthRepo.CreateOrMergeAuthSession(authSession, tokens).InTask();
        }

        public Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
        {
            return AuthRepo.GetUserAuth(authSession, tokens).InTask();
        }

        public Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token = default)
        {
            return AuthRepo.GetUserAuthByUserName(userNameOrEmail).InTask();
        }

        public Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token = default)
        {
            AuthRepo.SaveUserAuth(userAuth);
            return TypeConstants.EmptyTask;
        }

        public Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token = default)
        {
            return (AuthRepo.TryAuthenticate(userName, password, out var userAuth) ? userAuth : null).InTask();
        }

        public Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence,
            CancellationToken token = default)
        {
            return (AuthRepo.TryAuthenticate(digestHeaders, privateKey, nonceTimeOut, sequence, out var userAuth)
                ? userAuth
                : null).InTask();
        }

        public Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token = default)
        {
            return AuthRepo.CreateUserAuth(newUser, password).InTask();
        }

        public Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token = default)
        {
            return AuthRepo.UpdateUserAuth(existingUser, newUser).InTask();
        }

        public Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token = default)
        {
            return AuthRepo.UpdateUserAuth(existingUser, newUser, password).InTask();
        }

        public Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token = default)
        {
            return AuthRepo.GetUserAuth(userAuthId).InTask();
        }

        public Task DeleteUserAuthAsync(string userAuthId, CancellationToken token = default)
        {
            AuthRepo.DeleteUserAuth(userAuthId);
            return TypeConstants.EmptyTask;
        }

        public void InitSchema() => AuthRepo.InitSchema();
        
        public IUserAuth CreateUserAuth()
        {
            return AuthRepo is ICustomUserAuth customUserAuth
                ? customUserAuth.CreateUserAuth()
                : new UserAuth();
        }

        public IUserAuthDetails CreateUserAuthDetails()
        {
            return AuthRepo is ICustomUserAuth customUserAuth
                ? customUserAuth.CreateUserAuthDetails()
                : new UserAuthDetails();
        }

        public Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token = default)
        {
            return AuthRepo.GetUserAuths(orderBy, skip, take).InTask();
        }

        public Task<List<IUserAuth>> SearchUserAuthsAsync(string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token = default)
        {
            return AuthRepo.SearchUserAuths(query, orderBy, skip, take).InTask();
        }
    }

    public class UserAuthRepositoryAsyncManageRolesWrapper : UserAuthRepositoryAsyncWrapper, IManageRolesAsync
    {
        private readonly IManageRoles manageRoles;

        public UserAuthRepositoryAsyncManageRolesWrapper(IAuthRepository authRepo) : base(authRepo)
        {
            this.manageRoles = (IManageRoles) authRepo;
        }

        public Task<ICollection<string>> GetRolesAsync(string userAuthId, CancellationToken token = default)
        {
            return manageRoles.GetRoles(userAuthId).InTask();
        }

        public Task<ICollection<string>> GetPermissionsAsync(string userAuthId, CancellationToken token = default)
        {
            return manageRoles.GetPermissions(userAuthId).InTask();
        }

        public Task<Tuple<ICollection<string>, ICollection<string>>> GetRolesAndPermissionsAsync(string userAuthId, CancellationToken token = default)
        {
            manageRoles.GetRolesAndPermissions(userAuthId, out var roles, out var permissions);
            return new Tuple<ICollection<string>, ICollection<string>>(roles, permissions).InTask();
        }

        public Task<bool> HasRoleAsync(string userAuthId, string role, CancellationToken token = default)
        {
            return manageRoles.HasRole(userAuthId, role).InTask();
        }

        public Task<bool> HasPermissionAsync(string userAuthId, string permission, CancellationToken token = default)
        {
            return manageRoles.HasPermission(userAuthId, permission).InTask();
        }

        public Task AssignRolesAsync(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token = default)
        {
            manageRoles.AssignRoles(userAuthId, roles, permissions);
            return TypeConstants.EmptyTask;
        }

        public Task UnAssignRolesAsync(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token = default)
        {
            manageRoles.UnAssignRoles(userAuthId, roles, permissions);
            return TypeConstants.EmptyTask;
        }
    }

    public static class UserAuthRepositoryAsyncWrapperExtensions
    {
        /// <summary>
        /// Returns either the native IAuthRepositoryAsync provider or an IAuthRepositoryAsync sync-wrapped provider over IAuthRepository
        /// </summary>
        public static IAuthRepositoryAsync AsAsync(this IAuthRepository authRepo)
        {
            if (authRepo == null)
                return null;
            if (authRepo is IAuthRepositoryAsync authRepoAsync)
                return authRepoAsync;
            if (authRepo is IManageRoles)
                return new UserAuthRepositoryAsyncManageRolesWrapper(authRepo);
            return new UserAuthRepositoryAsyncWrapper(authRepo);
        }

        public static IAuthRepository UnwrapAuthRepository(this IAuthRepositoryAsync asyncRepo)
        {
            if (asyncRepo is UserAuthRepositoryAsyncWrapper asyncWrapper)
                return asyncWrapper.AuthRepo;
            return asyncRepo as IAuthRepository;
        }

        public static bool TryGetNativeQueryAuth(this IAuthRepository syncRepo, IAuthRepositoryAsync asyncRepo,
            out IQueryUserAuth queryUserAuth, out IQueryUserAuthAsync queryUserAuthAsync)
        {
            queryUserAuth = syncRepo as IQueryUserAuth;
            queryUserAuthAsync = !(asyncRepo is UserAuthRepositoryAsyncWrapper)
                ? asyncRepo as IQueryUserAuthAsync
                : null;

            return queryUserAuth != null || queryUserAuthAsync != null;
        }
    }
}