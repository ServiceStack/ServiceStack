using System.Web.UI;
using ServiceStack;
using ServiceStack.Caching;


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
            get { return HostContext.Resolve<ICacheClient>(); }
        }

        private ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory
        {
            get { return sessionFactory ?? (sessionFactory = HostContext.Resolve<ISessionFactory>()) ?? new SessionFactory(Cache); }
        }

        /// <summary>
        /// Dynamic SessionBag Bag
        /// </summary>
        private ISession sessionBag;
        public new ISession SessionBag
        {
            get
            {
                return sessionBag ?? (sessionBag = SessionFactory.GetOrCreateSession());
            }
        }

        public void ClearSession()
        {
            userSession = null;
            this.Cache.Remove(SessionFeature.GetSessionKey());
        }
    }
}
