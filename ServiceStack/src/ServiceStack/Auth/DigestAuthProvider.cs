using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth;

//DigestAuth Info: http://www.ntu.edu.sg/home/ehchua/programming/webprogramming/HTTP_Authentication.html
public class DigestAuthProvider : AuthProvider, IAuthWithRequest
{
    public override string Type => "Digest";
        
    public static string Name = AuthenticateService.DigestProvider;
    public static string Realm = "/auth/" + AuthenticateService.DigestProvider;
    public static int NonceTimeOut = 600;
    public string PrivateKey;
    public IAppSettings AppSettings { get; set; }

    public DigestAuthProvider()
    {
        Provider = Name;
        PrivateKey = Guid.NewGuid().ToString();
        AuthRealm = Realm;
    }

    public DigestAuthProvider(IAppSettings appSettings, string authRealm, string authProvider)
        : base(appSettings, authRealm, authProvider) { }

    public DigestAuthProvider(IAppSettings appSettings)
        : base(appSettings, Realm, Name) { }

    public virtual bool TryAuthenticate(IServiceBase authService, string userName, string password)
    {
        var authRepo = HostContext.AppHost.GetAuthRepository(authService.Request);
        using (authRepo as IDisposable)
        {
            var session = authService.GetSession();
            var digestInfo = authService.Request.GetDigestAuth();
            if (authRepo.TryAuthenticate(digestInfo, PrivateKey, NonceTimeOut, session.Sequence, out var userAuth))
            {
                session.Sequence = digestInfo["nc"];
                session.PopulateSession(userAuth, authRepo);
                return true;
            }
            return false;
        }
    }

    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
    {
        if (request != null)
        {
            if (!LoginMatchesSession(session, request.UserName))
            {
                return false;
            }
        }

        return session != null && session.IsAuthenticated && !session.UserAuthName.IsNullOrEmpty();
    }

    public override Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default)
    {
        //new CredentialsAuthValidator().ValidateAndThrow(request);
        return AuthenticateAsync(authService, session, request.UserName, request.Password, token);
    }

    protected async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, string userName, string password, CancellationToken token=default)
    {
        if (!LoginMatchesSession(session, userName))
        {
            await authService.RemoveSessionAsync(token).ConfigAwait();
            session = await authService.GetSessionAsync(token: token).ConfigAwait();
        }

        if (TryAuthenticate(authService, userName, password))
        {
            session.IsAuthenticated = true;
            session.UserAuthName ??= userName;

            var response = await OnAuthenticatedAsync(authService, session, null, null, token).ConfigAwait();
            if (response != null)
                return response;

            return new AuthenticateResponse
            {
                UserId = session.UserAuthId,
                UserName = userName,
                SessionId = session.Id,
            };
        }

        throw HttpError.Unauthorized(ErrorMessages.InvalidUsernameOrPassword.Localize(authService.Request));
    }

    public override async Task<IHttpResult> OnAuthenticatedAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token=default)
    {
        session.AuthProvider = Name;
        if (session is AuthUserSession userSession)
        {
            await LoadUserAuthInfoAsync(userSession, tokens, authInfo, token).ConfigAwait();
            HostContext.TryResolve<IAuthMetadataProvider>().SafeAddMetadata(tokens, authInfo);

            LoadUserAuthFilter?.Invoke(userSession, tokens, authInfo);
            if (LoadUserAuthInfoFilterAsync != null)
                await LoadUserAuthInfoFilterAsync(userSession, tokens, authInfo, token);
        }

        if (session is IAuthSessionExtended authSession)
        {
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            var failed = authSession.Validate(authService, session, tokens, authInfo)
                         ?? await authSession.ValidateAsync(authService, session, tokens, authInfo, token) 
                         ?? AuthEvents.Validate(authService, session, tokens, authInfo)
                         ?? (AuthEvents is IAuthEventsAsync asyncEvents 
                             ? await asyncEvents.ValidateAsync(authService, session, tokens, authInfo, token)
                             : null);
            if (failed != null)
            {
                await authService.RemoveSessionAsync(token).ConfigAwait();
                return failed;
            }
        }

        var authRepo = GetUserAuthRepositoryAsync(authService.Request);
        await using (authRepo as IAsyncDisposable)
        {
            if (authRepo != null)
            {
                if (tokens != null)
                {
                    authInfo.ForEach((x, y) => tokens.Items[x] = y);
                    session.UserAuthId = (await authRepo.CreateOrMergeAuthSessionAsync(session, tokens, token)).UserAuthId.ToString();
                }

                foreach (var oAuthToken in session.GetAuthTokens())
                {
                    var authProvider = AuthenticateService.GetAuthProvider(oAuthToken.Provider);

                    var userAuthProvider = authProvider as OAuthProvider;
                    userAuthProvider?.LoadUserOAuthProviderAsync(session, oAuthToken);
                }

                var failed = await ValidateAccountAsync(authService, authRepo, session, tokens, token).ConfigAwait();
                if (failed != null)
                    return failed;
            }
        }

        try
        {
            session.IsAuthenticated = true;
            session.OnAuthenticated(authService, session, tokens, authInfo);
            if (session is IAuthSessionExtended sessionExt)
                await sessionExt.OnAuthenticatedAsync(authService, session, tokens, authInfo, token).ConfigAwait();
            AuthEvents.OnAuthenticated(authService.Request, session, authService, tokens, authInfo);
            if (AuthEvents is IAuthEventsAsync asyncEvents)
                await asyncEvents.OnAuthenticatedAsync(authService.Request, session, authService, tokens, authInfo, token).ConfigAwait();
        }
        finally
        {
            await this.SaveSessionAsync(authService, session, SessionExpiry, token).ConfigAwait();
            authService.Request.CompletedAuthentication();
        }

        return null;
    }

    public override Task OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
    {
        var digestHelper = new DigestAuthFunctions();
        httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
        httpRes.AddHeader(HttpHeaders.WwwAuthenticate,
            $"{Provider} realm=\"{AuthRealm}\", nonce=\"{digestHelper.GetNonce(httpReq.UserHostAddress, PrivateKey)}\", qop=\"auth\"");
        return HostContext.AppHost.HandleShortCircuitedErrors(httpReq, httpRes, httpReq.Dto);
    }

    public async Task PreAuthenticateAsync(IRequest req, IResponse res)
    {
        var digestAuth = req.GetDigestAuth();
        if (digestAuth != null)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            using var authService = HostContext.ResolveService<AuthenticateService>(req);
            var response = await authService.PostAsync(new Authenticate
            {
                provider = Name,
                UserName = digestAuth["username"]
            }).ConfigAwait();
        }
    }
}