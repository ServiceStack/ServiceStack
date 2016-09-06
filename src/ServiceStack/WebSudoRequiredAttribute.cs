using System;
using ServiceStack.Auth;
using ServiceStack.Web;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class WebSudoRequiredAttribute : AuthenticateAttribute
    {
        public WebSudoRequiredAttribute(ApplyTo applyTo)
        {
            this.ApplyTo = applyTo;
            this.Priority = -79;
        }

        public WebSudoRequiredAttribute()
            : this(ApplyTo.All) {}

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.AppHost.HasValidAuthSecret(req))
                return;

            base.Execute(req, res, requestDto);
            if (res.IsClosed)
                return;

            var session = req.GetSession();

            var authRepo = HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                if (session != null && session.HasRole("Admin", authRepo)
                    || (this.HasWebSudo(req, session as IWebSudoAuthSession)
                    || this.DoHtmlRedirectIfConfigured(req, res)))
                    return;
            }

            res.StatusCode = 402;
            res.StatusDescription = "Web Sudo Required";
            res.EndRequest();
        }

        public bool HasWebSudo(IRequest req, IWebSudoAuthSession session)
        {
            if (session?.AuthenticatedWebSudoUntil == null)
                return false;

            var now = DateTime.UtcNow;
            if (now < session.AuthenticatedWebSudoUntil.Value.ToUniversalTime())
                return true;

            session.AuthenticatedWebSudoUntil = null;
            req.SaveSession(session);
            return false;
        }
    }
}