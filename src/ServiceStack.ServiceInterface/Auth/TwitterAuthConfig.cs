using System;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface.Auth
{
	public class TwitterAuthConfig : AuthConfig
	{
		public const string Name = "twitter";
		public static string Realm = "https://api.twitter.com/";

		public TwitterAuthConfig(IResourceManager appSettings)
			: base(appSettings, Realm, Name) {}

		protected override void LoadUserAuthInfo(AuthUserSession userSession, IOAuthTokens tokens, Dictionary<string, string> authInfo)
		{
			if (authInfo.ContainsKey("user_id"))
				tokens.UserId = userSession.TwitterUserId = authInfo.GetValueOrDefault("user_id");

			if (authInfo.ContainsKey("screen_name"))
				tokens.UserName = userSession.TwitterScreenName = authInfo.GetValueOrDefault("screen_name");

			try
			{
				var json = AuthHttpGateway.DownloadTwitterUserInfo(userSession.TwitterUserId);
				var obj = JsonObject.Parse(json);
				tokens.DisplayName = obj.Get("name");
				userSession.DisplayName = tokens.DisplayName ?? userSession.DisplayName;
			}
			catch (Exception ex)
			{
				Log.Error("Could not retrieve twitter user info for '{0}'".Fmt(userSession.TwitterUserId), ex);
			}
		}
	}
}