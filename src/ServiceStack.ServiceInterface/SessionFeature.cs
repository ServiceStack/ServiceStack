using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

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
		public const string SessionId = "ss-id";
		public const string PermanentSessionId = "ss-pid";
		public const string SessionOptionsKey = "ss-opt";

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

		public static string CreateSessionId(this IHttpResponse res, IHttpRequest req)
		{
			var sessionOptions = GetSessionOptions(req);
			return sessionOptions.Contains(SessionOptions.Permanent)
				? res.CreatePermanentSessionId(req)
				: res.CreateTemporarySessionId(req);
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

		public static HashSet<string> AddSessionOptions(this IHttpResponse res, IHttpRequest req, params string[] options)
		{
			if (res == null || req == null || options.Length == 0) return new HashSet<string>();

			var existingOptions = req.GetSessionOptions();
			foreach (var option in options)
			{
				if (option.IsNullOrEmpty()) continue;
				existingOptions.Add(option);
			}
			
			var strOptions = string.Join(",", existingOptions.ToArray());
			res.Cookies.AddPermanentCookie(SessionOptionsKey, strOptions);
			req.Items[SessionOptionsKey] = strOptions;
			
			return existingOptions;
		}

	}
}