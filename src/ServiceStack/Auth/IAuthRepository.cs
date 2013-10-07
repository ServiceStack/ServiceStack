using System.Collections.Generic;

namespace ServiceStack.Auth
{
    public interface IAuthRepository
    {
        void LoadUserAuth(IAuthSession session, IAuthTokens tokens);
        void SaveUserAuth(IAuthSession authSession);
        List<IUserAuthProvider> GetUserOAuthProviders(string userAuthId);
        string CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens);

        IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens);
        IUserAuth GetUserAuthByUserName(string userNameOrEmail);
        void SaveUserAuth(IUserAuth userAuth);
        bool TryAuthenticate(string userName, string password, out IUserAuth userAuth);
        bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth);
    }


    public interface IUserAuthRepository : IUserAuthRepository<UserAuth>
    {}


    public interface IUserAuthRepository<TUserAuth> : IAuthRepository
        where TUserAuth : class, IUserAuth, new()
    {
        TUserAuth CreateUserAuth(TUserAuth newUser, string password);
        TUserAuth UpdateUserAuth(TUserAuth existingUser, TUserAuth newUser, string password);
        bool TryAuthenticate(string userName, string password, out TUserAuth userAuth);
        bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out TUserAuth userAuth);
        TUserAuth GetUserAuth(string userAuthId);
        new TUserAuth GetUserAuthByUserName(string userNameOrEmail);
        new TUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens);
        void SaveUserAuth(TUserAuth userAuth);
    }
}