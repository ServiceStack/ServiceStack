﻿using System.Net;
using System.Web;
using System.Web.Security;
using ServiceStack.ServiceClient.Web;

namespace ServiceStack.WebHost.Endpoints
{
	public static class HttpWebRequestConfig
	{
		public static void Configure()
		{
			ServiceClientBase.HttpWebRequestFilter = TransferAuthenticationTokens;
		}

		public static void TransferAuthenticationTokens(HttpWebRequest httpWebRequest)
		{
			var cookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
			if (cookie == null) return;
			
			var authenticationCookie = new Cookie(
				FormsAuthentication.FormsCookieName,
				cookie.Value,
				cookie.Path,
				HttpContext.Current.Request.Url.Authority);

			httpWebRequest.CookieContainer = new CookieContainer();
			httpWebRequest.CookieContainer.Add(authenticationCookie);
		}
	}
}
