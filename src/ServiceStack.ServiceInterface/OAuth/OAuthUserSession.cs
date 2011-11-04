using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;

namespace ServiceStack.ServiceInterface.OAuth
{
	public class OAuthUserSession : IOAuthSession
	{
		public OAuthUserSession()
		{
			this.ProviderOAuthAccess = new Dictionary<string, IOAuthTokens>();
		}

		public string ReferrerUrl { get; set; }

		public string Id { get; set; }

		public string TwitterUserId { get; set; }

		public string TwitterScreenName { get; set; }

		public string RequestTokenSecret { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime LastModified { get; set; }

		public Dictionary<string, IOAuthTokens> ProviderOAuthAccess { get; set; }

		public virtual bool IsAuthorized()
		{
			return ProviderOAuthAccess.Values
				.Any(x => !string.IsNullOrEmpty(x.OAuthToken) 
					&& !string.IsNullOrEmpty(x.AccessToken));
		}

		public virtual void OnAuthenticated(OAuthService oAuthService, string provider, Dictionary<string, string> authInfo)
		{
			if (provider == TwitterOAuthConfig.Name)
			{
				if (authInfo.ContainsKey("user_id"))
					this.TwitterUserId = authInfo.GetValueOrDefault("user_id");

				if (authInfo.ContainsKey("screen_name"))
					this.TwitterScreenName = authInfo.GetValueOrDefault("screen_name");
			}
			else if (provider == FacebookOAuthConfig.Name) {}

			IOAuthTokens providerTokens;
			if (!ProviderOAuthAccess.TryGetValue(provider, out providerTokens))
				ProviderOAuthAccess[provider] = providerTokens = new OAuthTokens();

			authInfo.ForEach((x, y) => providerTokens.Items[x] = y);
		}
	}

}