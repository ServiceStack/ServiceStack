using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.FluentValidation;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.Text;

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
	}

	public class AuthResponse
	{
		public AuthResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		public string SessionId { get; set; }

		public string UserName { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	public class AuthService : RestServiceBase<Auth>
	{
		public const string BasicProvider = "basic";
		public const string CredentialsProvider = "credentials";
		public const string LogoutAction = "logout";

		public static string DefaultOAuthProvider { get; private set; }
		public static string DefaultOAuthRealm { get; private set; }
		public static AuthConfig[] AuthConfigs { get; private set; }
		public static Func<IAuthSession> SessionFactory { get; private set; }
		public static ValidateFn ValidateFn { get; set; }

		public static string GetSessionKey(string sessionId)
		{
			return IdUtils.CreateUrn<IAuthSession>(sessionId);
		}

		public static void Init(IAppHost appHost, Func<IAuthSession> sessionFactory, params AuthConfig[] authConfigs)
		{
			if (authConfigs.Length == 0)
				throw new ArgumentNullException("authConfigs");

			DefaultOAuthProvider = authConfigs[0].Provider;
			DefaultOAuthRealm = authConfigs[0].AuthRealm;

			AuthConfigs = authConfigs;
			SessionFactory = sessionFactory;
			appHost.RegisterService<AuthService>();

			SessionFeature.Init(appHost);
		}

		private void AssertAuthProviders()
		{
			if (AuthConfigs == null || AuthConfigs.Length == 0)
				throw new ConfigurationException("No OAuth providers have been registered in your AppHost.");
		}

		public override object OnGet(Auth request)
		{
			if (ValidateFn != null)
			{
				var response = ValidateFn(this, HttpMethods.Get, request);
				if (response != null) return response;
			}

			AssertAuthProviders();

			if (request.provider == LogoutAction)
			{
				this.RemoveSession();
				return new AuthResponse();
			}

			var provider = request.provider ?? AuthConfigs[0].Provider;
			if (provider == BasicProvider || provider == CredentialsProvider)
			{
				return CredentialsAuth(request);
			}

			var oAuthConfig = AuthConfigs.FirstOrDefault(x => x.Provider == provider);
			if (oAuthConfig == null)
				throw HttpError.NotFound("No configuration was added for OAuth provider '{0}'".Fmt(provider));

			var session = this.GetSession();

			if (oAuthConfig.CallbackUrl.IsNullOrEmpty())
				oAuthConfig.CallbackUrl = base.RequestContext.AbsoluteUri;

			if (session.ReferrerUrl.IsNullOrEmpty())
				session.ReferrerUrl = base.RequestContext.GetHeader("Referer") ?? oAuthConfig.CallbackUrl;

			var oAuth = new OAuthAuthorizer(oAuthConfig);

			if (!session.IsAuthorized(provider))
			{
				var tokens = session.ProviderOAuthAccess.FirstOrDefault(x => x.Provider == provider);
				if (tokens == null)
					session.ProviderOAuthAccess.Add(tokens = new OAuthTokens { Provider = provider });

				return oAuthConfig.Authenticate(this, request, session, tokens, oAuth);
			}

			//Already Authenticated
			return this.Redirect(session.ReferrerUrl.AddHashParam("s", "0"));
		}

        public override object OnPost(Auth request)
        {
            if (ValidateFn != null)
            {
                var response = ValidateFn(this, HttpMethods.Post, request);
                if (response != null) return response;
            }

            return CredentialsAuth(request);
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

		class CredentialsAuthValidator : AbstractValidator<Auth>
		{
			public CredentialsAuthValidator()
			{
				RuleFor(x => x.provider)
					.Must(x => x == BasicProvider || x == CredentialsProvider)
					.WithErrorCode("InvalidProvider")
					.WithMessage("Provider must be either 'basic' or 'credentials'");

				RuleFor(x => x.UserName).NotEmpty();
				RuleFor(x => x.Password).NotEmpty();
			}
		}

		private object CredentialsAuth(Auth request)
		{
			AssertAuthProviders();

			new CredentialsAuthValidator().ValidateAndThrow(request);

			var userName = request.UserName;
			var password = request.Password;

			var session = this.GetSession();

			if (request.provider == BasicProvider)
			{
				var httpReq = base.RequestContext.Get<IHttpRequest>();
				var basicAuth = httpReq.GetBasicAuthUserAndPassword();
				if (basicAuth == null)
					throw HttpError.Unauthorized("Invalid BasicAuth credentials");

				userName = basicAuth.Value.Key;
				password = basicAuth.Value.Value;
			}

			if (session.TryAuthenticate(this, userName, password))
			{
				if (session.UserName == null)
					session.UserName = userName;

				this.SaveSession(session);

				return new AuthResponse {
					UserName = userName,
					SessionId = session.Id,
				};
			}

			throw HttpError.Unauthorized("Invalid UserName or Password");
		}
	}
}

