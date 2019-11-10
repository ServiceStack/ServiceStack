using System.Web.UI;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.WebHost.IntegrationTests.Tests;

namespace ServiceStack.WebHost.IntegrationTests
{
    public class CustomUserSession : AuthUserSession
    {
        public string CustomPropety { get; set; }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, System.Collections.Generic.Dictionary<string, string> authInfo)
        {
            base.OnAuthenticated(authService, session, tokens, authInfo);

            if (session.Email == AuthTestsBase.AdminEmail)
            {
                var authRepo = authService.TryResolve<IAuthRepository>();
                var userAuth = authRepo.GetUserAuth(session, tokens);
                authRepo.AssignRoles(userAuth, roles: new[] { RoleNames.Admin });
            }
        }
    }

    public class PageBase : Page
    {
        /// <summary>
        /// Typed UserSession
        /// </summary>
        private object userSession;
        protected virtual TUserSession SessionAs<TUserSession>() => (TUserSession)(userSession ?? (userSession = Cache.SessionAs<TUserSession>()));

        protected CustomUserSession UserSession => SessionAs<CustomUserSession>();

        public new ICacheClient Cache => HostContext.AppHost.GetCacheClient(null);

        private ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory => sessionFactory ?? (sessionFactory = HostContext.Resolve<ISessionFactory>()) ?? new SessionFactory(Cache);

        /// <summary>
        /// Dynamic Session Bag
        /// </summary>
        private ISession session;
        public new ISession SessionBag => session ?? (session = SessionFactory.GetOrCreateSession());

        public void ClearSession()
        {
            userSession = null;
            this.Cache.Remove(SessionFeature.GetSessionKey());
        }
    }
}