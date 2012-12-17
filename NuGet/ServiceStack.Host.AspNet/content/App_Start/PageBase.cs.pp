using System.Web.UI;
using ServiceStack.CacheAccess;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;


/**
 * Base ASP.NET WebForms page using ServiceStack's Compontents, see: http://www.servicestack.net/mvc-powerpack/
 */

namespace $rootnamespace$.App_Start
{
	//A customizeable typed UserSession that can be extended with your own properties
	public class CustomUserSession : AuthUserSession
	{
		public string CustomProperty { get; set; }
	}

	public class PageBase : Page
	{
        /// <summary>
        /// Typed UserSession
        /// </summary>
        private object userSession;
        protected virtual TUserSession SessionAs<TUserSession>()
        {
            return (TUserSession)(userSession ?? (userSession = Cache.SessionAs<TUserSession>()));
        }

        protected CustomUserSession UserSession
        {
            get
            {
                return SessionAs<CustomUserSession>();
            }
        }

        public new ICacheClient Cache
        {
            get { return AppHostBase.Resolve<ICacheClient>(); }
        }

        private ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory
        {
            get { return sessionFactory ?? (sessionFactory = AppHostBase.Resolve<ISessionFactory>()) ?? new SessionFactory(Cache); }
        }

        /// <summary>
        /// Dynamic Session Bag
        /// </summary>
        private ISession session;
        public new ISession Session
        {
            get
            {
                return session ?? (session = SessionFactory.GetOrCreateSession());
            }
        }

        public void ClearSession()
        {
            userSession = null;
            this.Cache.Remove(SessionFeature.GetSessionKey());
        }
	}
}