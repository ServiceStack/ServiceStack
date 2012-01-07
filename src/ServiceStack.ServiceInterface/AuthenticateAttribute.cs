using System;
using System.Linq;
using System.Net;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface
{
	/// <summary>
	/// Indicates that the request dto, which is associated with this attribute,
	/// requires authentication.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class AuthenticateAttribute : RequestFilterAttribute
	{
		public string Provider { get; set; }
		public ApplyTo ApplyTo { get; set; }

		public AuthenticateAttribute()
			: base(ApplyTo.All)
		{
		}

		public AuthenticateAttribute(string provider)
			: base(ApplyTo.All)
		{
			this.Provider = provider;
		}

		public AuthenticateAttribute(ApplyTo applyTo)
			: base(applyTo)
		{
		}

		public AuthenticateAttribute(ApplyTo applyTo, string provider)
			: base(applyTo)
		{
			this.Provider = provider;
		}

		public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
		{
			if (AuthService.AuthConfigs == null) throw new InvalidOperationException("The AuthService must be initialized by calling "
				 + "AuthService.Init to use an authenticate attribute");

			var matchingOAuthConfigs = AuthService.AuthConfigs.Where(x =>
							this.Provider.IsNullOrEmpty()
							|| x.Provider == this.Provider).ToList();

			if (matchingOAuthConfigs.Count == 0)
			{
				res.WriteError(req, requestDto, "No OAuth Configs found matching {0} provider"
					.Fmt(this.Provider ?? "any"));
				res.Close();
				return;
			}

			var userPass = req.GetBasicAuthUserAndPassword();
			if (userPass != null)
			{
				var authService = req.TryResolve<AuthService>();
				authService.RequestContext = new HttpRequestContext(req, res, requestDto);
				var response = authService.Post(new Auth.Auth {
					provider = BasicAuthConfig.Name,
					UserName = userPass.Value.Key,
					Password = userPass.Value.Value
				});
			}

			using (var cache = req.GetCacheClient())
			{
				var sessionId = req.GetPermanentSessionId();
				var session = sessionId != null ? cache.GetSession(sessionId) : null;

				if (session == null || !matchingOAuthConfigs.Any(x => session.IsAuthorized(x.Provider)))
				{
					res.StatusCode = (int)HttpStatusCode.Unauthorized;
					res.AddHeader(HttpHeaders.WwwAuthenticate, "{0} realm=\"{1}\""
						.Fmt(matchingOAuthConfigs[0].Provider, matchingOAuthConfigs[0].AuthRealm));

					res.Close();
				}
			}
		}
	}
}