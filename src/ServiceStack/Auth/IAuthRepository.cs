using System.Collections.Generic;

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

    public interface IUserAuthRepository : IAuthRepository
    {
        IUserAuth CreateUserAuth(IUserAuth newUser, string password);
        IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser);
        IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password);
        IUserAuth GetUserAuth(string userAuthId);
        void DeleteUserAuth(string userAuthId);
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

        bool HasRole(string userAuthId, string role);
        bool HasPermission(string userAuthId, string permission);

        void AssignRoles(string userAuthId,
            ICollection<string> roles = null, ICollection<string> permissions = null);

        void UnAssignRoles(string userAuthId,
            ICollection<string> roles = null, ICollection<string> permissions = null);
    }
}