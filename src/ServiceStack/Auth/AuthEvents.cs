using System.Collections.Generic;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public interface IAuthEvents
    {
        void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService);
        void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, 
            IAuthTokens tokens, Dictionary<string, string> authInfo);
        void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService);
        void OnCreated(IRequest httpReq, IAuthSession session);
    }

    /// <summary>
    /// Conveneint base class with empty virtual methods so subclasses only need to override the hooks they need.
    /// </summary>
    public class AuthEvents : IAuthEvents
    {
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