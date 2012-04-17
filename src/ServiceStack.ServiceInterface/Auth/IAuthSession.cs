﻿using System;
using System.Collections.Generic;

namespace ServiceStack.ServiceInterface.Auth
{
	public interface IAuthSession
	{
		string ReferrerUrl { get; set; }
		string Id { get; set; }
		string UserAuthId { get; set; }
		string UserAuthName { get; set; }
		string UserName { get; set; }
		string DisplayName { get; set; }
		string FirstName { get; set; }
		string LastName { get; set; }
		string Email { get; set; }
		List<IOAuthTokens> ProviderOAuthAccess { get; set; }
		DateTime CreatedAt { get; set; }
		DateTime LastModified { get; set; }
		List<string> Roles { get; set; }
        List<string> Permissions { get; set; }
		bool IsAuthenticated { get; set; }
        //Used for digest authentication replay protection
        string Sequence { get; set; }

		bool HasRole(string role);
		bool HasPermission(string permission);
		bool IsAuthorized(string provider);
		void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo);
	}
}