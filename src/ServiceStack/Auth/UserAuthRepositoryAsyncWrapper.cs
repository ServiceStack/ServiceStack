using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Auth
{
    public class UserAuthRepositoryAsyncWrapper : IUserAuthRepositoryAsync, IRequiresSchema
    {
        private readonly IAuthRepository authRepo;
        public UserAuthRepositoryAsyncWrapper(IAuthRepository authRepo) => this.authRepo = authRepo;

        public Task LoadUserAuthAsync(IAuthSession session, IAuthTokens tokens, CancellationToken token = default)
        {
            authRepo.LoadUserAuth(session, tokens);
            return TypeConstants.EmptyTask;
        }

        public Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token = default)
        {
            authRepo.SaveUserAuth(authSession);
            return TypeConstants.EmptyTask;
        }

        public Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token = default)
        {
            return authRepo.GetUserAuthDetails(userAuthId).InTask();
        }

        public Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
        {
            return authRepo.CreateOrMergeAuthSession(authSession, tokens).InTask();
        }

        public Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token = default)
        {
            return authRepo.GetUserAuth(authSession, tokens).InTask();
        }

        public Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token = default)
        {
            return authRepo.GetUserAuthByUserName(userNameOrEmail).InTask();
        }

        public Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token = default)
        {
            authRepo.SaveUserAuth(userAuth);
            return TypeConstants.EmptyTask;
        }

        public Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token = default)
        {
            return (authRepo.TryAuthenticate(userName, password, out var userAuth) ? userAuth : null).InTask();
        }

        public Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence,
            CancellationToken token = default)
        {
            return (authRepo.TryAuthenticate(digestHeaders, privateKey, nonceTimeOut, sequence, out var userAuth)
                ? userAuth
                : null).InTask();
        }

        public Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token = default)
        {
            return authRepo.CreateUserAuth(newUser, password).InTask();
        }

        public Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token = default)
        {
            return authRepo.UpdateUserAuth(existingUser, newUser).InTask();
        }

        public Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token = default)
        {
            return authRepo.UpdateUserAuth(existingUser, newUser, password).InTask();
        }

        public Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token = default)
        {
            return authRepo.GetUserAuth(userAuthId).InTask();
        }

        public Task DeleteUserAuthAsync(string userAuthId, CancellationToken token = default)
        {
            authRepo.DeleteUserAuth(userAuthId);
            return TypeConstants.EmptyTask;
        }

        public void InitSchema() => authRepo.InitSchema();
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
    }
}