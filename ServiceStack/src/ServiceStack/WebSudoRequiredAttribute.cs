using System;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack;

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

    public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
    {
        if (HostContext.AppHost.HasValidAuthSecret(req))
            return;

        await base.ExecuteAsync(req, res, requestDto).ConfigAwait();
        if (res.IsClosed)
            return;

        var session = await req.GetSessionAsync();

        var authRepo = HostContext.AppHost.GetAuthRepository(req);
        using (authRepo as IDisposable)
        {
            if (session != null && session.HasRole(RoleNames.Admin, authRepo)
                || await this.HasWebSudoAsync(req, session as IWebSudoAuthSession))
                return;
        }

        await HandleShortCircuitedErrors(req, res, requestDto,
            HttpStatusCode.PaymentRequired, ErrorMessages.WebSudoRequired.Localize(req)).ConfigAwait();
    }

    public async Task<bool> HasWebSudoAsync(IRequest req, IWebSudoAuthSession session)
    {
        if (session?.AuthenticatedWebSudoUntil == null)
            return false;

        var now = DateTime.UtcNow;
        if (now < session.AuthenticatedWebSudoUntil.Value.ToUniversalTime())
            return true;

        session.AuthenticatedWebSudoUntil = null;
        await req.SaveSessionAsync(session);
        return false;
    }
}