using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public interface IAuthProvider
    {
        string AuthRealm { get; set; }
        string Provider { get; set; }
        string CallbackUrl { get; set; }

        /// <summary>
        /// Remove the Users Session
        /// </summary>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        object Logout(IServiceBase service, Authenticate request);

        /// <summary>
        /// The entry point for all AuthProvider providers. Runs inside the AuthService so exceptions are treated normally.
        /// Overridable so you can provide your own Auth implementation.
        /// </summary>
        object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request);

        /// <summary>
        /// Determine if the current session is already authenticated with this AuthProvider
        /// </summary>
        bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null);
    }

    public interface IOAuthProvider : IAuthProvider
    {
        IAuthHttpGateway AuthHttpGateway { get; set; }
        string ConsumerKey { get; set; }
        string ConsumerSecret { get; set; }
        string RequestTokenUrl { get; set; }
        string AuthorizeUrl { get; set; }
        string AccessTokenUrl { get; set; }
    }

    public interface IAuthWithRequest
    {
        void PreAuthenticate(IRequest req, IResponse res);
    }

    public interface IAuthResponseFilter
    {
        void Execute(AuthFilterContext authContext);
    }

    public interface IUserSessionSource
    {
        IAuthSession GetUserSession(string userAuthId);
    }

    public class AuthFilterContext
    {
        public AuthenticateService AuthService { get; internal set; }
        public IAuthProvider AuthProvider { get; internal set; }
        public IAuthSession Session { get; internal set; }
        public Authenticate AuthRequest { get; internal set; }
        public AuthenticateResponse AuthResponse { get; internal set; }
        public bool AlreadyAuthenticated { get; internal set; }
        public bool DidAuthenticate { get; internal set; }
    }
}