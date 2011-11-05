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
			this.ProviderOAuthAccess = new List<IOAuthTokens>();
		}

		public string ReferrerUrl { get; set; }

		public string Id { get; set; }

		public string TwitterUserId { get; set; }

		public string TwitterScreenName { get; set; }

		public string RequestTokenSecret { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime LastModified { get; set; }

		public List<IOAuthTokens> ProviderOAuthAccess { get; set; }

		public virtual bool IsAuthorized()
		{
			return ProviderOAuthAccess
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

			var tokens = ProviderOAuthAccess.FirstOrDefault(x => x.Provider == provider);
			if (tokens == null)
				ProviderOAuthAccess.Add(tokens = new OAuthTokens());

			authInfo.ForEach((x, y) => tokens.Items[x] = y);
		}
	}

}