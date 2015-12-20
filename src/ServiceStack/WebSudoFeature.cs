using System;
using System.Collections.Generic;
using ServiceStack.Auth;
using ServiceStack.Web;

namespace ServiceStack
{
    public class WebSudoFeature : IPlugin, IAuthEvents
    {
        public const string SessionCopyRequestItemKey = "__copy-of-request-session";

        public WebSudoFeature()
        {
            this.WebSudoDuration = TimeSpan.FromMinutes(20);
        }

        public TimeSpan WebSudoDuration { get; set; }

        public void Register(IAppHost appHost)
        {
            var s = AuthenticateService.CurrentSessionFactory() as IWebSudoAuthSession;
            if (s == null)
            {
                throw new NotSupportedException("The IUserAuth session must also implement IWebSudoAuthSession");
            }

            appHost.GlobalRequestFilters.Add(OnRequestStart);
            appHost.GlobalResponseFilters.Add(OnRequestEnd);

            var authFeature = appHost.GetPlugin<AuthFeature>();
            authFeature.AuthEvents.Add(this);
        }

        private void OnRequestStart(IRequest request, IResponse response, object dto)
        {
            if (dto == null) return;

            var session = request.GetSession();
            if (!session.IsAuthenticated) return;

            var authenticateDto = dto as Authenticate;
            if (authenticateDto != null && !AuthenticateService.LogoutAction.EqualsIgnoreCase(authenticateDto.provider))
            {
                var copy = AuthenticateService.CurrentSessionFactory().PopulateWith(session);

                request.Items[SessionCopyRequestItemKey] = copy;

                // clear details to allow credentials to be rechecked, 
                // otherwise IsAuthorized will just return, bypassing the auth provider's Authenticate method
                // fields cleared LoginMatchesSession
                session.UserAuthName = null;
                session.Email = null;
            }
        }

        private void OnRequestEnd(IRequest request, IResponse response, object dto)
        {
            if (!request.Items.ContainsKey(SessionCopyRequestItemKey)) return;
            var copy = request.Items[SessionCopyRequestItemKey] as IWebSudoAuthSession;
            if (copy == null) return;

            var session = request.GetSession();
            if (!session.IsAuthenticated)
            {
                // if the credential check failed, restore the session to it's prior, valid state.
                // this enures that a logged in user, remains logged in, but not elevated if the check failed.
                session.PopulateWith(copy);
            }

            request.SaveSession(session);
        }

        public void OnCreated(IRequest httpReq, IAuthSession session)
        {
            if (!httpReq.Items.ContainsKey(SessionCopyRequestItemKey)) return;
            var copy = httpReq.Items[SessionCopyRequestItemKey] as IWebSudoAuthSession;
            if (copy == null) return;

            var id = session.Id;
            var created = session.CreatedAt;

            session.PopulateWith(copy);
            session.Id = id;
            session.CreatedAt = created;
        }

        public void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, IAuthTokens tokens,
            Dictionary<string, string> authInfo)
        {
            var webSudoSession = session as IWebSudoAuthSession;
            if (webSudoSession == null) return;

            webSudoSession.AuthenticatedAt = DateTime.UtcNow;
            webSudoSession.AuthenticatedCount++;

            if (webSudoSession.AuthenticatedCount > 1)
            {
                webSudoSession.AuthenticatedWebSudoUntil = webSudoSession.AuthenticatedAt.Add(this.WebSudoDuration);
            }
        }

        public void OnLogout(IRequest httpReq, IAuthSession session, IServiceBase authService)
        {

        }

        public void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService)
        {

        }
    }
}