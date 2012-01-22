using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface
{
	public class SessionOptions
	{
		public const string Temporary = "temp";
		public const string Permanent = "perm";
	}

	/// <summary>
	/// Configure ServiceStack to have ISession support
	/// </summary>
	public static class SessionFeature
	{
		public const string OnlyAspNet = "Only ASP.NET Requests accessible via Singletons are supported";
		public const string SessionId = "ss-id";
		public const string PermanentSessionId = "ss-pid";
		public const string SessionOptionsKey = "ss-opt";
		public const string XUserAuthId = HttpHeaders.XUserAuthId;

		private static bool alreadyConfigured;

		public static void Init(IAppHost appHost)
		{
			if (alreadyConfigured) return;
			alreadyConfigured = true;

			//Add permanent and session cookies if not already set.
			appHost.RequestFilters.Add((req, res, dto) => {
				if (req.GetCookieValue(SessionId) == null)
				{
					res.CreateTemporarySessionId(req);
				}
				if (req.GetCookieValue(PermanentSessionId) == null)
				{
					res.CreatePermanentSessionId(req);
				}
			});
		}

		public static string GetSessionId(this IHttpRequest httpReq)
		{
			var sessionOptions = GetSessionOptions(httpReq);

			return sessionOptions.Contains(SessionOptions.Permanent)
				? httpReq.GetItemOrCookie(PermanentSessionId)
				: httpReq.GetItemOrCookie(SessionId);
		}

		public static string GetPermanentSessionId(this IHttpRequest httpReq)
		{
			return httpReq.GetItemOrCookie(PermanentSessionId);
		}

		public static string GetTemporarySessionId(this IHttpRequest httpReq)
		{
			return httpReq.GetItemOrCookie(SessionId);
		}

		/// <summary>
		/// Create the active Session or Permanent Session Id cookie.
		/// </summary>
		/// <returns></returns>
		public static string CreateSessionId(this IHttpResponse res, IHttpRequest req)
		{
			var sessionOptions = GetSessionOptions(req);
			return sessionOptions.Contains(SessionOptions.Permanent)
				? res.CreatePermanentSessionId(req)
				: res.CreateTemporarySessionId(req);
		}

		/// <summary>
		/// Create both Permanent and Session Id cookies and return the active sessionId
		/// </summary>
		/// <returns></returns>
		public static string CreateSessionIds(this IHttpResponse res, IHttpRequest req)
		{
			var sessionOptions = GetSessionOptions(req);
			var permId = res.CreatePermanentSessionId(req);
			var tempId = res.CreateTemporarySessionId(req);
			return sessionOptions.Contains(SessionOptions.Permanent)
				? permId
				: tempId;
		}

		public static string CreatePermanentSessionId(this IHttpResponse res, IHttpRequest req)
		{
			var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			res.Cookies.AddPermanentCookie(PermanentSessionId, sessionId);
			req.Items[PermanentSessionId] = sessionId;
			return sessionId;
		}

		public static string CreateTemporarySessionId(this IHttpResponse res, IHttpRequest req)
		{
			var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			res.Cookies.AddSessionCookie(SessionId, sessionId);
			req.Items[SessionId] = sessionId;
			return sessionId;
		}

		public static HashSet<string> GetSessionOptions(this IHttpRequest httpReq)
		{
			var sessionOptions = httpReq.GetItemOrCookie(SessionOptionsKey);
			return sessionOptions.IsNullOrEmpty()
				? new HashSet<string>()
				: sessionOptions.Split(',').ToHashSet();
		}

		public static void UpdateSession(this IAuthSession session, UserAuth userAuth)
		{
			if (userAuth == null) return;
			session.Roles = userAuth.Roles;
			session.Permissions = userAuth.Permissions;
		}

		public static HashSet<string> AddSessionOptions(this IHttpResponse res, IHttpRequest req, params string[] options)
		{
			if (res == null || req == null || options.Length == 0) return new HashSet<string>();

			var existingOptions = req.GetSessionOptions();
			foreach (var option in options)
			{
				if (option.IsNullOrEmpty()) continue;

				if (option == SessionOptions.Permanent)
					existingOptions.Remove(SessionOptions.Temporary);
				else if (option == SessionOptions.Temporary)
					existingOptions.Remove(SessionOptions.Permanent);

				existingOptions.Add(option);
			}

			var strOptions = string.Join(",", existingOptions.ToArray());
			res.Cookies.AddPermanentCookie(SessionOptionsKey, strOptions);
			req.Items[SessionOptionsKey] = strOptions;
			
			return existingOptions;
		}

		public static string GetSessionId()
		{
			if (HttpContext.Current == null)
				throw new NotImplementedException(OnlyAspNet);

			return HttpContext.Current.Request.ToRequest().GetSessionId();
		}

		public static IHttpRequest ToRequest(this HttpRequest aspnetHttpReq)
		{
			return new HttpRequestWrapper(aspnetHttpReq) {
				Container = AppHostBase.Instance.Container
			};
		}

		public static IHttpRequest ToRequest(this HttpListenerRequest listenerHttpReq)
		{
			return new HttpListenerRequestWrapper(listenerHttpReq) {
				Container = AppHostBase.Instance.Container
			};
		}

		public static IHttpResponse ToResponse(this HttpResponse aspnetHttpRes)
		{
			return new HttpResponseWrapper(aspnetHttpRes);
		}

		public static IHttpResponse ToResponse(this HttpListenerResponse listenerHttpRes)
		{
			return new HttpListenerResponseWrapper(listenerHttpRes);
		}

		public static void CreateSessionIds()
		{
			if (HttpContext.Current != null)
			{
				HttpContext.Current.Response.ToResponse()
					.CreateSessionIds(HttpContext.Current.Request.ToRequest());
				return;
			}

			throw new NotImplementedException(OnlyAspNet);
		}

		public static string GetSessionKey(string sessionId)
		{
			return IdUtils.CreateUrn<IAuthSession>(sessionId);
		}
	}
}