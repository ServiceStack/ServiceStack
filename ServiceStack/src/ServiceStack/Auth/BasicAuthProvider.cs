#pragma warning disable CS0618

using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class BasicAuthProvider : CredentialsAuthProvider, IAuthWithRequest
    {
        public new static string Name = AuthenticateService.BasicProvider;
        public new static string Realm = "/auth/" + AuthenticateService.BasicProvider;

        public override string Type => "Basic";

        public BasicAuthProvider()
        {
            this.Provider = Name;
            this.AuthRealm = Realm;
        }

        public BasicAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name) {}

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
        {
            var httpReq = authService.Request;
            var basicAuth = httpReq.GetBasicAuthUserAndPassword();
            if (basicAuth == null)
                throw HttpError.Unauthorized(ErrorMessages.InvalidBasicAuthCredentials.Localize(authService.Request));

            var userName = basicAuth.Value.Key;
            var password = basicAuth.Value.Value;

            var ret = await AuthenticateAsync(authService, session, userName, password, authService.Request.GetReturnUrl(), token).ConfigAwait();
            return ret;
        }

        public virtual async Task PreAuthenticateAsync(IRequest req, IResponse res)
        {
            //API Keys are sent in Basic Auth Username and Password is Empty
            var userPass = req.GetBasicAuthUserAndPassword();
            if (!string.IsNullOrEmpty(userPass?.Value))
            {
                //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
                SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

                using var authService = HostContext.ResolveService<AuthenticateService>(req);
                var response = await authService.PostAsync(new Authenticate
                {
                    provider = Name,
                    UserName = userPass.Value.Key,
                    Password = userPass.Value.Value
                }).ConfigAwait();
            }
        }
    }
}