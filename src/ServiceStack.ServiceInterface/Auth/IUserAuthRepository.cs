using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Auth
{
    public interface IUserAuthRepository
    {
        UserAuth CreateUserAuth(UserAuth newUser, string password);
        UserAuth UpdateUserAuth(UserAuth existingUser, UserAuth newUser, string password);
        UserAuth GetUserAuthByUserName(string userNameOrEmail);
        bool TryAuthenticate(string userName, string password, out UserAuth userAuth);
        bool TryAuthenticate(Dictionary<string, string> digestHeaders, string PrivateKey, int NonceTimeOut, string sequence, out UserAuth userAuth);
        void LoadUserAuth(IAuthSession session, IOAuthTokens tokens);
        UserAuth GetUserAuth(string userAuthId);
        void SaveUserAuth(IAuthSession authSession);
        void SaveUserAuth(UserAuth userAuth);
        List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId);
        UserAuth GetUserAuth(IAuthSession authSession, IOAuthTokens tokens);
        string CreateOrMergeAuthSession(IAuthSession authSession, IOAuthTokens tokens);
    }
}
