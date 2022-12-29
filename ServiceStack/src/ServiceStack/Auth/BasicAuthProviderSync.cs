#pragma warning disable CS0618

using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class BasicAuthProviderSync : CredentialsAuthProviderSync, IAuthWithRequestSync
    {
        public new static string Name = AuthenticateService.BasicProvider;
        public new static string Realm = "/auth/" + AuthenticateService.BasicProvider;

        public override string Type => "Basic";

        public BasicAuthProviderSync()
        {
            this.Provider = Name;
            this.AuthRealm = Realm;
        }

        public BasicAuthProviderSync(IAppSettings appSettings)
            : base(appSettings, Realm, Name) {}

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var httpReq = authService.Request;
            var basicAuth = httpReq.GetBasicAuthUserAndPassword();
            if (basicAuth == null)
                throw HttpError.Unauthorized(ErrorMessages.InvalidBasicAuthCredentials.Localize(authService.Request));

            var userName = basicAuth.Value.Key;
            var password = basicAuth.Value.Value;

            return Authenticate(authService, session, userName, password, authService.Request.GetReturnUrl());
        }

        public virtual void PreAuthenticate(IRequest req, IResponse res)
        {
            //API Keys are sent in Basic Auth Username and Password is Empty
            var userPass = req.GetBasicAuthUserAndPassword();
            if (!string.IsNullOrEmpty(userPass?.Value))
            {
                //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
                SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

                using var authService = HostContext.ResolveService<AuthenticateService>(req);
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