using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class WebSudoFeature : AuthEvents, IPlugin, Model.IHasStringId
    {
        public string Id { get; set; } = Plugins.WebSudo;
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

            appHost.GlobalRequestFiltersAsync.Add(OnRequestStartAsync);
            appHost.GlobalResponseFiltersAsync.Add(OnRequestEndAsync);

            var authFeature = appHost.GetPlugin<AuthFeature>();
            authFeature.AuthEvents.Add(this);
        }

        private async Task OnRequestStartAsync(IRequest request, IResponse response, object dto)
        {
            if (dto == null) return;

            var session = await request.GetSessionAsync().ConfigAwait();
            if (!session.IsAuthenticated) return;

            if (dto is Authenticate authenticateDto && !AuthenticateService.LogoutAction.EqualsIgnoreCase(authenticateDto.provider))
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

        private async Task OnRequestEndAsync(IRequest request, IResponse response, object dto)
        {
            if (!request.Items.ContainsKey(SessionCopyRequestItemKey)) return;
            if (!(request.Items[SessionCopyRequestItemKey] is IWebSudoAuthSession copy)) return;

            var session = await request.GetSessionAsync().ConfigAwait();
            if (!session.IsAuthenticated)
            {
                // if the credential check failed, restore the session to it's prior, valid state.
                // this ensures that a logged in user, remains logged in, but not elevated if the check failed.
                session.PopulateWith(copy);
            }

            await request.SaveSessionAsync(session).ConfigAwait();
        }

        public override void OnCreated(IRequest httpReq, IAuthSession session)
        {
            if (!httpReq.Items.ContainsKey(SessionCopyRequestItemKey)) return;
            if (!(httpReq.Items[SessionCopyRequestItemKey] is IWebSudoAuthSession copy)) return;

            var id = session.Id;
            var created = session.CreatedAt;

            session.PopulateWith(copy);
            session.Id = id;
            session.CreatedAt = created;
        }

        public override void OnAuthenticated(IRequest httpReq, IAuthSession session, IServiceBase authService, IAuthTokens tokens,
            Dictionary<string, string> authInfo)
        {
            if (!(session is IWebSudoAuthSession webSudoSession)) return;

            webSudoSession.AuthenticatedAt = DateTime.UtcNow;
            webSudoSession.AuthenticatedCount++;

            if (webSudoSession.AuthenticatedCount > 1)
            {
                webSudoSession.AuthenticatedWebSudoUntil = webSudoSession.AuthenticatedAt.Add(this.WebSudoDuration);
            }
        }
    }
}