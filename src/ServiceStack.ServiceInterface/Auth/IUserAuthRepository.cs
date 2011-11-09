using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Auth
{
	public interface IUserAuthRepository
	{
		UserAuth CreateUserAuth(UserAuth newUser);
		UserAuth GetUserAuthByUserName(string userName);
		bool TryAuthenticate(string userName, string password, out string realUserName);
		void LoadUserAuth(IOAuthSession session, IOAuthTokens tokens);
		UserAuth GetUserAuth(string userAuthId);
		List<UserOAuthProvider> GetUserAuthProviders(string userAuthId);
		UserAuth GetUserAuth(IOAuthSession authSession, IOAuthTokens tokens);
		string CreateOrMergeAuthSession(IOAuthSession oAuthSession, IOAuthTokens tokens);
	}
}