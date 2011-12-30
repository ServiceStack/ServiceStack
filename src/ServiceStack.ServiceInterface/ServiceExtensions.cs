using System;
using System.Net;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Web;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
	public static class ServiceExtensions
	{
		public static string AddQueryParam(this string url, string key, string val)
		{
			var prefix = url.IndexOf('?') == -1 ? "?" : "&";
			return url + prefix + key + "=" + val.UrlEncode();
		}

		public static IHttpResult Redirect(this IServiceBase service, string url)
		{
			return service.Redirect(url, "Moved Temporarily");
		}

		public static IHttpResult Redirect(this IServiceBase service, string url, string message)
		{
			return new HttpResult(HttpStatusCode.Redirect, message) {
				ContentType = service.RequestContext.ResponseContentType,
				Headers = {
					{ HttpHeaders.Location, url }
				},
			};
		}

		public static IHttpResult AuthenticationRequired(this IServiceBase service)
		{
			return new HttpResult {
				StatusCode = HttpStatusCode.Unauthorized,
				ContentType = service.RequestContext.ResponseContentType,
				Headers = {
					{ HttpHeaders.WwwAuthenticate, "OAuth realm=\"{0}\"".Fmt(AuthService.DefaultOAuthRealm) }
				},
			};
		}

		public static string GetSessionId(this IServiceBase service)
		{
			var req = service.RequestContext.Get<IHttpRequest>();
			var id = req.GetPermanentSessionId();
			if (id == null)
				throw new ArgumentNullException("Session not set. Is Session being set in RequestFilters?");

			return id;
		}

		/// <summary>
		/// If they don't have an ICacheClient configured use an In Memory one.
		/// </summary>
		private static readonly MemoryCacheClient DefaultCache = new MemoryCacheClient { FlushOnDispose = true };

		public static ICacheClient GetCacheClient(this IServiceBase service)
		{
			return service.TryResolve<ICacheClient>()
				?? (ICacheClient)service.TryResolve<IRedisClientsManager>()
				?? DefaultCache;
		}

		public static ICacheClient GetCacheClient(this IAppHost appHost)
		{
			return appHost.TryResolve<ICacheClient>()
				?? (ICacheClient)appHost.TryResolve<IRedisClientsManager>()
				?? DefaultCache;
		}

		public static void SaveSession(this IServiceBase service, IAuthSession session)
		{
			using (var cache = service.GetCacheClient())
			{
				var sessionKey = AuthService.GetSessionKey(service.GetSessionId());
				cache.Set(sessionKey, session);
			}
		}

		public static IAuthSession GetSession(this IServiceBase service)
		{
			using (var cache = service.GetCacheClient())
			{
				return GetSession(cache, service.GetSessionId());
			}
		}

		public static IAuthSession GetSession(this ICacheClient cache, string sessionId)
		{
			var session = cache.Get<IAuthSession>(AuthService.GetSessionKey(sessionId));
			if (session == null)
			{
				session = AuthService.SessionFactory();
				session.Id = sessionId;
				session.CreatedAt = session.LastModified = DateTime.UtcNow;
			}
			return session;
		}
	}
}