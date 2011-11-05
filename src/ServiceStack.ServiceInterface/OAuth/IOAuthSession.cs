using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.OAuth
{
	public interface IOAuthSession
	{
		string ReferrerUrl { get; set; }
		string Id { get; set; }
		List<IOAuthTokens> ProviderOAuthAccess { get; set; }
		DateTime CreatedAt { get; set; }
		DateTime LastModified { get; set; }
		bool IsAuthorized();

		void OnAuthenticated(OAuthService oAuthService, string provider, Dictionary<string, string> authInfo);
	}
}