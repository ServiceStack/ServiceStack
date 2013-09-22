using ServiceStack.Configuration;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Web;

namespace ServiceStack.ServiceInterface.Auth
{
    public class BasicAuthProvider : CredentialsAuthProvider
    {
        public new static string Name = AuthService.BasicProvider;
        public new static string Realm = "/auth/" + AuthService.BasicProvider;

        public BasicAuthProvider()
        {
            this.Provider = Name;
            this.AuthRealm = Realm;
        }

        public BasicAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Auth request)
        {
            var httpReq = authService.RequestContext.Get<IHttpRequest>();
            var basicAuth = httpReq.GetBasicAuthUserAndPassword();
            if (basicAuth == null)
                throw HttpError.Unauthorized("Invalid BasicAuth credentials");

            var userName = basicAuth.Value.Key;
            var password = basicAuth.Value.Value;

            return Authenticate(authService, session, userName, password, request.Continue);
        }

        
    }
}