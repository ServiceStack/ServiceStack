using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.OAuth
{
	public interface IOAuthSession
	{
		string ReferrerUrl { get; set; }
		string Id { get; set; }
		string OAuthToken { get; set; }
		string AccessToken { get; set; }
		string RequestToken { get; set; }
		string RequestTokenSecret { get; set; }
		DateTime CreatedAt { get; set; }
		DateTime LastModified { get; set; }
		bool IsAuthorized();
		Dictionary<string, string> Items { get; }

		void OnAuthenticated(OAuthService oAuthService, Dictionary<string, string> authInfo);
	}
}