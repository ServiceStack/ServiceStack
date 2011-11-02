using System;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	public class SessionFeature
	{
		private static bool alreadyConfigured;

		public static void Register(IAppHost appHost)
		{
			if (alreadyConfigured) return;
			alreadyConfigured = true;

			//Add permanent and session cookies if not already set.
			appHost.RequestFilters.Add((req, res, dto) => {
				if (req.GetCookieValue("ss-session") == null)
				{
					var sessionId = Guid.NewGuid().ToString("N");
					res.SetSessionCookie("ss-session", sessionId);
					req.Items["ss-session"] = sessionId;
				}
				if (req.GetCookieValue("ss-psession") == null)
				{
					var permanentId = Guid.NewGuid().ToString("N");
					res.SetPermanentCookie("ss-psession", permanentId);
					req.Items["ss-psession"] = permanentId;
				}
			});
		}
	}
}