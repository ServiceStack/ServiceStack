using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Auth
{
	/// <summary>
	/// Inject logic into existing services by introspecting the request and injecting your own
	/// validation logic. Exceptions thrown will have the same behaviour as if the service threw it.
	/// 
	/// If a non-null object is returned the request will short-circuit and return that response.
	/// </summary>
	/// <param name="service">The instance of the service</param>
	/// <param name="httpMethod">GET,POST,PUT,DELETE</param>
	/// <param name="requestDto"></param>
	/// <returns>Response DTO; non-null will short-circuit execution and return that response</returns>
	public delegate object ValidateFn(IServiceBase service, string httpMethod, object requestDto);

	public class Auth
	{
		public string provider { get; set; }
		public string State { get; set; }
		public string oauth_token { get; set; }
		public string oauth_verifier { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public bool? RememberMe { get; set; }
        // Thise are used for digest auth
        public string nonce { get; set; }
        public string uri { get; set; }
        public string response { get; set; }
        public string qop { get; set; }
        public string nc { get; set; }
        public string cnonce { get; set; }
	}

	public class AuthResponse
	{
		public AuthResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		public string SessionId { get; set; }

		public string UserName { get; set; }

		public string ReferrerUrl { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	public class AuthService : RestServiceBase<Auth>
	{
		public const string BasicProvider = "basic";
		public const string CredentialsProvider = "credentials";
		public const string LogoutAction = "logout";
        public const string DigestProvider = "digest";

		public static Func<IAuthSession> CurrentSessionFactory { get; set; }
		public static ValidateFn ValidateFn { get; set; }

		public static string DefaultOAuthProvider { get; private set; }
		public static string DefaultOAuthRealm { get; private set; }
		public static IAuthProvider[] AuthProviders { get; private set; }


		static AuthService()
		{
			CurrentSessionFactory = () => new AuthUserSession();
		}

		public static IAuthProvider GetAuthProvider(string provider)
		{
			if (AuthProviders == null || AuthProviders.Length == 0) return null;
			if (provider == LogoutAction) return AuthProviders[0];

			foreach (var authConfig in AuthProviders)
			{
				if (string.Compare(authConfig.Provider, provider,
					StringComparison.InvariantCultureIgnoreCase) == 0)
					return authConfig;
			}

			return null;
		}

		public static void Init(Func<IAuthSession> sessionFactory, params IAuthProvider[] authProviders)
		{
			if (authProviders.Length == 0)
				throw new ArgumentNullException("authProviders");

			DefaultOAuthProvider = authProviders[0].Provider;
			DefaultOAuthRealm = authProviders[0].AuthRealm;

			AuthProviders = authProviders;
			if (sessionFactory != null)
				CurrentSessionFactory = sessionFactory;
		}

		private void AssertAuthProviders()
		{
			if (AuthProviders == null || AuthProviders.Length == 0)
				throw new ConfigurationException("No OAuth providers have been registered in your AppHost.");
		}

		public override object OnGet(Auth request)
		{
			return OnPost(request);
		}

		public override object OnPost(Auth request)
		{
			AssertAuthProviders();

			if (ValidateFn != null)
			{
				var response = ValidateFn(this, HttpMethods.Get, request);
				if (response != null) return response;
			}

			if (request.RememberMe.HasValue)
			{
				var opt = request.RememberMe.GetValueOrDefault(false)
					? SessionOptions.Permanent
					: SessionOptions.Temporary;

				base.RequestContext.Get<IHttpResponse>()
					.AddSessionOptions(base.RequestContext.Get<IHttpRequest>(), opt);
			}

			var provider = request.provider ?? AuthProviders[0].Provider;
			var oAuthConfig = GetAuthProvider(provider);
			if (oAuthConfig == null)
				throw HttpError.NotFound("No configuration was added for OAuth provider '{0}'".Fmt(provider));

			if (request.provider == LogoutAction)
				return oAuthConfig.Logout(this, request);

			var session = this.GetSession();
			if (!oAuthConfig.IsAuthorized(session, session.GetOAuthTokens(provider), request))
			{
				return oAuthConfig.Authenticate(this, session, request);
			}

			var referrerUrl = session.ReferrerUrl
				?? this.RequestContext.GetHeader("Referer")
				?? oAuthConfig.CallbackUrl;

			//Already Authenticated
			if (base.RequestContext.ResponseContentType == ContentType.Html)
				return this.Redirect(referrerUrl.AddHashParam("s", "0"));

			return new AuthResponse {
				UserName = session.UserName,
				SessionId = session.Id,
				ReferrerUrl = referrerUrl,
			};
		}

		public override object OnDelete(Auth request)
		{
			if (ValidateFn != null)
			{
				var response = ValidateFn(this, HttpMethods.Delete, request);
				if (response != null) return response;
			}

			this.RemoveSession();

			return new AuthResponse();
		}
	}

}

