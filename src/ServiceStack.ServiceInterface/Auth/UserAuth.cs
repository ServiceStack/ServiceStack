using System;
using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;

namespace ServiceStack.ServiceInterface.Auth
{
	public class UserAuth
	{
		public UserAuth()
		{
			this.Roles = new List<string>();
			this.Permissions = new List<string>();
		}

		[AutoIncrement]
		public int Id { get; set; }
		public string UserName { get; set; }
        public string Email { get; set; }
        public string PrimaryEmail { get; set; }
        public string FirstName { get; set; }
		public string LastName { get; set; }
		public string DisplayName { get; set; }
		public string Salt { get; set; }
		public string PasswordHash { get; set; }
		public List<string> Roles { get; set; }
		public List<string> Permissions { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime ModifiedDate { get; set; }

		public void PopulateMissing(UserOAuthProvider authProvider)
		{
			if (!authProvider.FirstName.IsNullOrEmpty())
				this.FirstName = authProvider.FirstName;
			if (!authProvider.LastName.IsNullOrEmpty())
				this.LastName = authProvider.LastName;
            if (!authProvider.DisplayName.IsNullOrEmpty())
                this.DisplayName = authProvider.DisplayName;
            if (!authProvider.Email.IsNullOrEmpty())
                this.PrimaryEmail = authProvider.Email;
		}
	}

	public class UserOAuthProvider : IOAuthTokens
	{
		public UserOAuthProvider()
		{
			this.Items = new Dictionary<string, string>();
		}

		[AutoIncrement]
		public int Id { get; set; }
		public int UserAuthId { get; set; }
		public string Provider { get; set; }
		public string UserId { get; set; }
		public string UserName { get; set; }
		public string DisplayName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string RequestToken { get; set; }
		public string RequestTokenSecret { get; set; }
		public Dictionary<string, string> Items { get; set; }
		public string AccessToken { get; set; }
		public string AccessTokenSecret { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime ModifiedDate { get; set; }

		public void PopulateMissing(IOAuthTokens withTokens)
		{
			if (!withTokens.UserId.IsNullOrEmpty())
				this.UserId = withTokens.UserId;
			if (!withTokens.UserName.IsNullOrEmpty())
				this.UserName = withTokens.UserName;
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