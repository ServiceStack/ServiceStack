using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Auth
{
    public interface IAuthRepository
    {
        void LoadUserAuth(IAuthSession session, IAuthTokens tokens);
        void SaveUserAuth(IAuthSession authSession);
        List<IUserAuthDetails> GetUserAuthDetails(string userAuthId);
        IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens);

        IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens);
        IUserAuth GetUserAuthByUserName(string userNameOrEmail);
        void SaveUserAuth(IUserAuth userAuth);
        bool TryAuthenticate(string userName, string password, out IUserAuth userAuth);
        bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth);
    }

    public interface IAuthRepositoryAsync
    {
        Task LoadUserAuthAsync(IAuthSession session, IAuthTokens tokens, CancellationToken token=default);
        Task SaveUserAuthAsync(IAuthSession authSession, CancellationToken token=default);
        Task<List<IUserAuthDetails>> GetUserAuthDetailsAsync(string userAuthId, CancellationToken token=default);
        Task<IUserAuthDetails> CreateOrMergeAuthSessionAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default);

        Task<IUserAuth> GetUserAuthAsync(IAuthSession authSession, IAuthTokens tokens, CancellationToken token=default);
        Task<IUserAuth> GetUserAuthByUserNameAsync(string userNameOrEmail, CancellationToken token=default);
        Task SaveUserAuthAsync(IUserAuth userAuth, CancellationToken token=default);
        Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token=default);
        Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, CancellationToken token=default);
    }

    public interface IUserAuthRepository : IAuthRepository
    {
        IUserAuth CreateUserAuth(IUserAuth newUser, string password);
        IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser);
        IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password);
        IUserAuth GetUserAuth(string userAuthId);
        void DeleteUserAuth(string userAuthId);
    }

    public interface IUserAuthRepositoryAsync : IAuthRepositoryAsync
    {
        Task<IUserAuth> CreateUserAuthAsync(IUserAuth newUser, string password, CancellationToken token=default);
        Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, CancellationToken token=default);
        Task<IUserAuth> UpdateUserAuthAsync(IUserAuth existingUser, IUserAuth newUser, string password, CancellationToken token=default);
        Task<IUserAuth> GetUserAuthAsync(string userAuthId, CancellationToken token=default);
        Task DeleteUserAuthAsync(string userAuthId, CancellationToken token=default);
    }

    public interface IQueryUserAuth
    {
        List<IUserAuth> GetUserAuths(string orderBy = null, int? skip = null, int? take = null);
        List<IUserAuth> SearchUserAuths(string query, string orderBy = null, int? skip = null, int? take = null);
    }

    public interface IQueryUserAuthAsync
    {
        Task<List<IUserAuth>> GetUserAuthsAsync(string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default);
        Task<List<IUserAuth>> SearchUserAuthsAsync(string query, string orderBy = null, int? skip = null, int? take = null, CancellationToken token=default);
    }

    public interface ICustomUserAuth
    {
        IUserAuth CreateUserAuth();
        IUserAuthDetails CreateUserAuthDetails();
    }

    public interface IManageRoles
    {
        ICollection<string> GetRoles(string userAuthId);
        ICollection<string> GetPermissions(string userAuthId);
        void GetRolesAndPermissions(string userAuthId, out ICollection<string> roles, out ICollection<string> permissions);

        bool HasRole(string userAuthId, string role);
        bool HasPermission(string userAuthId, string permission);

        void AssignRoles(string userAuthId,
            ICollection<string> roles = null, ICollection<string> permissions = null);

        void UnAssignRoles(string userAuthId,
            ICollection<string> roles = null, ICollection<string> permissions = null);
    }
    
    public interface IManageRolesAsync
    {
        Task<ICollection<string>> GetRolesAsync(string userAuthId, CancellationToken token=default);
        Task<ICollection<string>> GetPermissionsAsync(string userAuthId, CancellationToken token=default);
        Task<Tuple<ICollection<string>,ICollection<string>>> GetRolesAndPermissionsAsync(string userAuthId, CancellationToken token=default);

        Task<bool> HasRoleAsync(string userAuthId, string role, CancellationToken token=default);
        Task<bool> HasPermissionAsync(string userAuthId, string permission, CancellationToken token=default);

        Task AssignRolesAsync(string userAuthId,
            ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default);

        Task UnAssignRolesAsync(string userAuthId,
            ICollection<string> roles = null, ICollection<string> permissions = null, CancellationToken token=default);
    }
    
    public class UserAuthRepositoryAsyncWrapper : IUserAuthRepositoryAsync
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

        public async Task<IUserAuth> TryAuthenticateAsync(string userName, string password, CancellationToken token = default)
        {
            return authRepo.TryAuthenticate(userName, password, out var userAuth) ? userAuth : null;
        }

        public async Task<IUserAuth> TryAuthenticateAsync(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence,
            CancellationToken token = default)
        {
            return authRepo.TryAuthenticate(digestHeaders, privateKey, nonceTimeOut, sequence, out var userAuth) ? userAuth : null;
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
    }
}