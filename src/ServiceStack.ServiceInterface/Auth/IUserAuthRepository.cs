using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Auth
{
	public interface IUserAuthRepository
	{
		UserAuth CreateUserAuth(UserAuth newUser, string password);
		UserAuth GetUserAuthByUserName(string userNameOrEmail);
		bool TryAuthenticate(string userName, string password, out string userId);
		void LoadUserAuth(IOAuthSession session, IOAuthTokens tokens);
		UserAuth GetUserAuth(string userAuthId);
		void SaveUserAuth(IOAuthSession oAuthSession);
		void SaveUserAuth(UserAuth userAuth);
		List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId);
		UserAuth GetUserAuth(IOAuthSession authSession, IOAuthTokens tokens);
		string CreateOrMergeAuthSession(IOAuthSession oAuthSession, IOAuthTokens tokens);
	}
}