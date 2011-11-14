using System;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
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
					var sessionId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
					res.SetSessionCookie(SessionId, sessionId);
					req.Items[SessionId] = sessionId;
				}
				if (req.GetCookieValue(PermanentSessionId) == null)
				{
					var permanentId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
					res.SetPermanentCookie(PermanentSessionId, permanentId);
					req.Items[PermanentSessionId] = permanentId;
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
	}
}