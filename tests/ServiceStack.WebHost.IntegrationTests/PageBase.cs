using System.Web.UI;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.IntegrationTests.Tests;

namespace ServiceStack.WebHost.IntegrationTests
{
	public class CustomUserSession : AuthUserSession
	{
		public string CustomPropety { get; set; }

		public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, System.Collections.Generic.Dictionary<string, string> authInfo)
		{
			base.OnAuthenticated(authService, session, tokens, authInfo);

			if (session.Email == AuthTestsBase.AdminEmail)
				session.Roles.Add(RoleNames.Admin);
		}
	}

	public class PageBase : Page
	{
		private Container container;
		public Container Container
		{
			get { return container ?? (container = Endpoints.AppHostBase.Instance.Container); }
		}

		protected string SessionKey
		{
			get
			{
				return SessionFeature.GetSessionKey();
			}
		}

		private CustomUserSession userSession;
		protected CustomUserSession UserSession
		{
			get
			{
				return userSession ?? (userSession = SessionFeature.GetOrCreateSession<CustomUserSession>(Cache));
			}
		}

		public void ClearSession()
		{
			userSession = null;
			this.Cache.Remove(SessionKey);
		}

		public new ICacheClient Cache
		{
			get { return Container.Resolve<ICacheClient>(); }
		}

		public ISessionFactory SessionFactory
		{
			get { return Container.Resolve<ISessionFactory>(); }
		}

		private ISession session;
		public new ISession Session
		{
			get
			{
				return session ?? (session = SessionFactory.GetOrCreateSession());
			}
		}
	}
}