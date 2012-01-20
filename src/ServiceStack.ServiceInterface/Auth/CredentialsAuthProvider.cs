using System.Collections.Generic;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;

namespace ServiceStack.ServiceInterface.Auth
{
	public class CredentialsAuthProvider : AuthProvider
	{
		class CredentialsAuthValidator : AbstractValidator<Auth>
		{
			public CredentialsAuthValidator()
			{
				RuleFor(x => x.UserName).NotEmpty();
				RuleFor(x => x.Password).NotEmpty();
			}
		}

		public static string Name = AuthService.CredentialsProvider;
		public static string Realm = "/auth/" + AuthService.CredentialsProvider;

		public CredentialsAuthProvider()
		{
			this.Provider = Name;
			this.AuthRealm = Realm;
		}

		public CredentialsAuthProvider(IResourceManager appSettings, string authRealm, string oAuthProvider)
			: base(appSettings, authRealm, oAuthProvider) { }

		public CredentialsAuthProvider(IResourceManager appSettings)
			: base(appSettings, Realm, Name) { }

		public virtual bool TryAuthenticate(IServiceBase authService, string userName, string password)
		{
			var authRepo = authService.TryResolve<IUserAuthRepository>();
			if (authRepo == null)
			{
				Log.WarnFormat("Tried to authenticate without a registered IUserAuthRepository");
				return false;
			}

			var session = authService.GetSession();
			string useUserName = null;
			if (authRepo.TryAuthenticate(userName, password, out useUserName))
			{
				session.IsAuthenticated = true;
				session.UserAuthName = userName;

				return true;
			}
			return false;
		}

		public override bool IsAuthorized(IAuthSession session, IOAuthTokens tokens, Auth request=null)
		{
			if (request != null)
			{
				if (!LoginMatchesSession(session, request.UserName)) return false;
			}

			return !session.UserAuthName.IsNullOrEmpty();
		}

		public override object Authenticate(IServiceBase authService, IAuthSession session, Auth request)
		{
			new CredentialsAuthValidator().ValidateAndThrow(request);
			return Authenticate(authService, session, request.UserName, request.Password);
		}

		protected object Authenticate(IServiceBase authService, IAuthSession session, string userName, string password)
		{
			if (!LoginMatchesSession(session, userName))
			{
				authService.RemoveSession();
				session = authService.GetSession();
			}

			if (TryAuthenticate(authService, userName, password))
			{
                if (session.UserAuthName == null)
                    session.UserAuthName = userName;
                
                OnAuthenticated(authService, session, null, null);

				return new AuthResponse {
					UserName = userName,
					SessionId = session.Id,
				};
			}

			throw HttpError.Unauthorized("Invalid UserName or Password");
		}

	}
}