using System;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Configure ServiceStack to have ISession support
	/// </summary>
	public static class SessionFeature
	{
		public const string SessionId = "ss-id";
		public const string PermanentSessionId = "ss-pid";

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

		public static string GetPermanentSessionId(this IHttpRequest httpReq)
		{
			return httpReq.GetItemOrCookie(PermanentSessionId);
		}

		public static string GetTemporarySessionId(this IHttpRequest httpReq)
		{
			return httpReq.GetItemOrCookie(SessionId);
		}

		public static string CreatePermanentSessionId(this IHttpResponse res, IHttpRequest req)
		{
			var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			res.SetPermanentCookie(PermanentSessionId, sessionId);
			req.Items[PermanentSessionId] = sessionId;
			return sessionId;
		}

		public static string CreateTemporarySessionId(this IHttpResponse res, IHttpRequest req)
		{
			var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			res.SetSessionCookie(SessionId, sessionId);
			req.Items[SessionId] = sessionId;
			return sessionId;
		}

	}
}