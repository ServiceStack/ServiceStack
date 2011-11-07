using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;

namespace ServiceStack.ServiceInterface.Auth
{
	public interface IUserAuthRepository
	{
		void LoadUserAuth(IOAuthSession session, IOAuthTokens tokens);
		UserAuth GetUserAuth(string userAuthId);
		List<UserOAuthProvider> GetUserAuthProviders(string userAuthId);
		UserAuth GetUserAuth(IOAuthSession authSession, IOAuthTokens tokens);
		string CreateOrMergeAuthSession(IOAuthSession oAuthSession, IOAuthTokens tokens);
	}
}