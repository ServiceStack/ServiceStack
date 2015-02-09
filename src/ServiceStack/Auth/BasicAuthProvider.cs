using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class BasicAuthProvider : CredentialsAuthProvider, IAuthWithRequest
    {
        public new static string Name = AuthenticateService.BasicProvider;
        public new static string Realm = "/auth/" + AuthenticateService.BasicProvider;

        public BasicAuthProvider()
        {
            this.Provider = Name;
            this.AuthRealm = Realm;
        }

        public BasicAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name) {}

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var httpReq = authService.Request;
            var basicAuth = httpReq.GetBasicAuthUserAndPassword();
            if (basicAuth == null)
                throw HttpError.Unauthorized(ErrorMessages.InvalidBasicAuthCredentials);

            var userName = basicAuth.Value.Key;
            var password = basicAuth.Value.Value;

            return Authenticate(authService, session, userName, password, request.Continue);
        }

        public void PreAuthenticate(IRequest req, IResponse res)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            var userPass = req.GetBasicAuthUserAndPassword();
            if (userPass != null)
            {
                var authService = req.TryResolve<AuthenticateService>();
                authService.Request = req;
                var response = authService.Post(new Authenticate
                {
                    provider = Name,
                    UserName = userPass.Value.Key,
                    Password = userPass.Value.Value
                });
            }
        }
    }
}