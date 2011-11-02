using System;
using System.Collections.Generic;
using ServiceStack.Common;

namespace ServiceStack.ServiceInterface.OAuth
{
	public class OAuthUserSession : IOAuthSession
	{
		public OAuthUserSession()
		{
			this.Items = new Dictionary<string, string>();
		}

		public string ReferrerUrl { get; set; }

		public string Id { get; set; }

		public string TwitterUserId { get; set; }

		public string TwitterScreenName { get; set; }

		public string OAuthToken { get; set; }

		public string AccessToken { get; set; }

		public string RequestToken { get; set; }

		public string RequestTokenSecret { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime LastModified { get; set; }

		public Dictionary<string, string> Items { get; set; }

		public virtual bool IsAuthorized()
		{
			return !string.IsNullOrEmpty(OAuthToken)
				&& !string.IsNullOrEmpty(AccessToken);
		}

		public virtual void OnAuthenticated(OAuthService oAuthService, Dictionary<string, string> authInfo)
		{
			if (authInfo.ContainsKey("user_id"))
			{
				this.TwitterUserId = authInfo.GetValueOrDefault("user_id");
				authInfo.Remove("user_info");
			}
			if (authInfo.ContainsKey("screen_name"))
			{
				this.TwitterScreenName = authInfo.GetValueOrDefault("screen_name");
				authInfo.Remove("screen_name");
			}
			authInfo.ForEach((x, y) => this.Items[x] = y);
		}
	}

}