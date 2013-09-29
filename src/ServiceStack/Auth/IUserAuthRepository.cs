using System.Collections.Generic;

namespace ServiceStack.Auth
{
    public interface IUserAuthRepository
    {
        UserAuth CreateUserAuth(UserAuth newUser, string password);
        UserAuth UpdateUserAuth(UserAuth existingUser, UserAuth newUser, string password);
        UserAuth GetUserAuthByUserName(string userNameOrEmail);
        bool TryAuthenticate(string userName, string password, out UserAuth userAuth);
        bool TryAuthenticate(Dictionary<string, string> digestHeaders, string PrivateKey, int NonceTimeOut, string sequence, out UserAuth userAuth);
        void LoadUserAuth(IAuthSession session, IAuthTokens tokens);
        UserAuth GetUserAuth(string userAuthId);
        void SaveUserAuth(IAuthSession authSession);
        void SaveUserAuth(UserAuth userAuth);
        List<UserAuthProvider> GetUserOAuthProviders(string userAuthId);
        UserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens);
        string CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens);
    }
}
