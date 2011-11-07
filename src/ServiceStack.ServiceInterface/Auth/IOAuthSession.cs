using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Auth
{
	public interface IOAuthSession
	{
		string ReferrerUrl { get; set; }
		string Id { get; set; }
		string UserAuthId { get; set; }
		string DisplayName { get; set; }
		string FirstName { get; set; }
		string LastName { get; set; }
		string Email { get; set; }
		List<IOAuthTokens> ProviderOAuthAccess { get; set; }
		DateTime CreatedAt { get; set; }
		DateTime LastModified { get; set; }
		bool IsAnyAuthorized();
		bool IsAuthorized(string provider);

		void OnAuthenticated(IServiceBase oAuthService, IOAuthTokens tokens, Dictionary<string, string> authInfo);
	}
}