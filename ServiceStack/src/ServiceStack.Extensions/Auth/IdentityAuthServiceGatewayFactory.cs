using ServiceStack.Web;

namespace ServiceStack.Auth;

public class IdentityAuthServiceGatewayFactory : IServiceGatewayFactory
{
    public IServiceGateway GetServiceGateway(IRequest request)
    {
        var session = SessionFeature.CreateNewSession(request, request.GetSessionId());
        var user = request.GetClaimsPrincipal();
        if (user != null)
        {
            IdentityAuth.AuthApplication.PopulateSession(
                request,
                session,
                user);
        }

        request.Items[Keywords.Session] = session;
        var gateway = new InProcessServiceGateway(request);
        return gateway;
    }
}
