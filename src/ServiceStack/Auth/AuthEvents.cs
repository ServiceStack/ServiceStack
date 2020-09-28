using System.Collections.Generic;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public interface IAuthEvents
    {
        /// <summary>
        /// Fired when a new Session is created
        /// </summary>
        void OnCreated(IRequest httpReq, IAuthSession session);

        /// <summary>
        /// Called when the user is registered or on the first OAuth login 
        /// </summary>
        void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService);

        /// <summary>
        /// Override with Custom Validation logic to Assert if User is allowed to Authenticate. 
        /// Returning a non-null response invalidates Authentication with IHttpResult response returned to client.
        /// </summary>
        IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
            Dictionary<string, string> authInfo);
        
        /// <summary>
        /// Called after the user has successfully authenticated 
        /// </summary>
        void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, 
            IAuthTokens tokens, Dictionary<string, string> authInfo);
        
        /// <summary>
        /// Fired before the session is removed after the /auth/logout Service is called
        /// </summary>
        void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService);
    }

    /// <summary>
    /// Convenient base class with empty virtual methods so subclasses only need to override the hooks they need.
    /// </summary>
    public class AuthEvents : IAuthEvents
    {
        public virtual IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
            Dictionary<string, string> authInfo) => null;

        public virtual void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService) {}
        public virtual void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, 
                                            IAuthTokens tokens, Dictionary<string, string> authInfo) {}
        public virtual void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService) {}
        public virtual void OnCreated(IRequest httpReq, IAuthSession session) {}
    }

    public class MultiAuthEvents : IAuthEvents
    {
        public MultiAuthEvents(IEnumerable<IAuthEvents> authEvents=null)
        {
            ChildEvents = new List<IAuthEvents>(authEvents ?? TypeConstants<IAuthEvents>.EmptyArray);
        }

        public List<IAuthEvents> ChildEvents { get; private set; }

        public IHttpResult Validate(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            foreach (var authEvent in ChildEvents)
            {
                var ret = authEvent.Validate(authService, session, tokens, authInfo);
                if (ret != null)
                    return ret;
            }
            return null;
        }

        public void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService)
        {
            ChildEvents.Each(x => x.OnRegistered(httpReq, session, registrationService));
        }

        public void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, 
            IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            ChildEvents.Each(x => x.OnAuthenticated(httpReq, session, authService, tokens, authInfo));
        }

        public void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService)
        {
            ChildEvents.Each(x => x.OnLogout(httpReq, session, authService));
        }

        public void OnCreated(IRequest httpReq, IAuthSession session)
        {
            ChildEvents.Each(x => x.OnCreated(httpReq, session));
        }
    }
}