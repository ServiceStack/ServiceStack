using ServiceStack.Common;
using ServiceStack.DataAnnotations;

namespace ServiceStack.ServiceInterface.Auth
{
	public class UserAuth
	{
		[AutoIncrement]
		public long Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string DisplayName { get; set; }
		public string Email { get; set; }
		public string Salt { get; set; }
		public string PasswordHash { get; set; }

		public void PopulateMissing(UserOAuthProvider authProvider)
		{
			if (!authProvider.FirstName.IsNullOrEmpty())
				this.FirstName = authProvider.FirstName;
			if (!authProvider.LastName.IsNullOrEmpty())
				this.LastName = authProvider.LastName;
			if (!authProvider.DisplayName.IsNullOrEmpty())
				this.DisplayName = authProvider.DisplayName;
			if (!authProvider.Email.IsNullOrEmpty())
				this.Email = authProvider.Email;
		}
	}

	public class UserOAuthProvider
	{
		[AutoIncrement]
		public long Id { get; set; }
		public long UserAuthId { get; set; }
		public string Provider { get; set; }
		public string UserId { get; set; }
		public string DisplayName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string RequestToken { get; set; }
		public string RequestTokenSecret { get; set; }
		public string AccessToken { get; set; }
		public string AccessTokenSecret { get; set; }

		public void PopulateMissing(IOAuthTokens withTokens)
		{
			if (!withTokens.UserId.IsNullOrEmpty())
				this.UserId = withTokens.UserId;
			if (!withTokens.RequestToken.IsNullOrEmpty())
				this.RequestToken = withTokens.RequestToken;
			if (!withTokens.RequestTokenSecret.IsNullOrEmpty())
				this.RequestTokenSecret = withTokens.RequestTokenSecret;
			if (!withTokens.AccessToken.IsNullOrEmpty())
				this.AccessToken = withTokens.AccessToken;
			if (!withTokens.AccessTokenSecret.IsNullOrEmpty())
				this.AccessTokenSecret = withTokens.AccessTokenSecret;
			if (!withTokens.DisplayName.IsNullOrEmpty())
				this.DisplayName = withTokens.DisplayName;
			if (!withTokens.FirstName.IsNullOrEmpty())
				this.FirstName = withTokens.FirstName;
			if (!withTokens.LastName.IsNullOrEmpty())
				this.LastName = withTokens.LastName;
			if (!withTokens.Email.IsNullOrEmpty())
				this.Email = withTokens.Email;
		}
	}

}