using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public static class AuthProviderExtensions
    {
        private static ILog Log = LogManager.GetLogger(typeof(AuthProviderExtensions));

        public static bool IsAuthorizedSafe(this IAuthProvider authProvider, IAuthSession session, IAuthTokens tokens)
        {
            return authProvider != null && authProvider.IsAuthorized(session, tokens);
        }

        public static string SanitizeOAuthUrl(this string url)
        {
            return (url ?? "").Replace("\\/", "/");
        }

        internal static bool PopulateFromRequestIfHasSessionId(this IRequest req, object requestDto)
        {
            var hasSession = requestDto as IHasSessionId;
            if (hasSession?.SessionId != null)
            {
                req.SetSessionId(hasSession.SessionId);
                return true;
            }
            return false;
        }

        public static bool PopulateRequestDtoIfAuthenticated(this IRequest req, object requestDto)
        {
            if (requestDto is IHasSessionId hasSession && hasSession.SessionId == null)
            {
                hasSession.SessionId = req.GetSessionId();
                return hasSession.SessionId != null;
            }
            if (requestDto is IHasBearerToken hasToken && hasToken.BearerToken == null)
            {
                hasToken.BearerToken = req.GetJwtToken();
                return hasToken.BearerToken != null;
            }
            return false;
        }

        internal static string NotLogoutUrl(this string url)
        {
            return url == null || url.EndsWith("/auth/logout")
                ? null
                : url;
        }

        [Obsolete("Use SaveSessionAsync")]
        public static void SaveSession(this IAuthProvider provider, IServiceBase authService, IAuthSession session, TimeSpan? sessionExpiry = null)
        {
            var persistSession = !(provider is AuthProvider authProvider) || authProvider.PersistSession;
            if (persistSession)
            {
                authService.SaveSession(session, sessionExpiry);
            }
            else
            {
                authService.Request.Items[Keywords.Session] = session;
            }
        }

        public static async Task SaveSessionAsync(this IAuthProvider provider, IServiceBase authService, IAuthSession session, TimeSpan? sessionExpiry = null, CancellationToken token=default)
        {
            var persistSession = provider is not AuthProvider authProvider || authProvider.PersistSession;
            if (persistSession)
            {
                await authService.SaveSessionAsync(session, sessionExpiry, token).ConfigAwait();
            }
            else
            {
                authService.Request.Items[Keywords.Session] = session;
            }
        }

        public static void GetHashAndSaltString(string password, out string hash, out string salt)
        {
            // When using IAuthRepository outside of an AppHost
            var appHost = HostContext.AppHost;
            if (appHost == null)
            {
                var hasher = new PasswordHasher();
                salt = null;
                hash = hasher.HashPassword(password);
                return;
            }
            
            var passwordHasher = !appHost.Config.UseSaltedHash
                ? appHost.TryResolve<IPasswordHasher>()
                : null;

            if (passwordHasher != null)
            {
                salt = null; // IPasswordHasher stores its Salt in PasswordHash
                hash = passwordHasher.HashPassword(password);
            }
            else
            {
                var hashProvider = appHost.Resolve<IHashProvider>();
                hashProvider.GetHashAndSaltString(password, out hash, out salt);
            }
        }

        public static void PopulatePasswordHashes(this IUserAuth newUser, string password, IUserAuth existingUser = null)
        {
            if (newUser == null)
                throw new ArgumentNullException(nameof(newUser));
            
            var hash = existingUser?.PasswordHash;
            var salt = existingUser?.Salt;

            if (password != null)
            {
                GetHashAndSaltString(password, out hash, out salt);
            }

            newUser.PasswordHash = hash;
            newUser.Salt = salt;
            
            newUser.PopulateDigestAuthHash(password, existingUser);
        }

        private static void PopulateDigestAuthHash(this IUserAuth newUser, string password, IUserAuth existingUser = null)
        {
            var createDigestAuthHashes = HostContext.GetPlugin<AuthFeature>()?.CreateDigestAuthHashes;
            if (createDigestAuthHashes == true)
            {
                if (existingUser == null)
                {
                    var digestHelper = new DigestAuthFunctions();
                    newUser.DigestHa1Hash = digestHelper.CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                }
                else
                {
                    newUser.DigestHa1Hash = existingUser.DigestHa1Hash;

                    // If either one changes the digest hash has to be recalculated
                    if (password != null || existingUser.UserName != newUser.UserName)
                        newUser.DigestHa1Hash = new DigestAuthFunctions().CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
                }
            }
            else if (createDigestAuthHashes == false)
            {
                newUser.DigestHa1Hash = null;
            }
        }

        public static bool VerifyPassword(this IUserAuth userAuth, string providedPassword, out bool needsRehash)
        {
            needsRehash = false;
            if (userAuth == null)
                throw new ArgumentNullException(nameof(userAuth));

            if (userAuth.PasswordHash == null)
                return false;

            var passwordHasher = HostContext.TryResolve<IPasswordHasher>();

            var usedOriginalSaltedHash = userAuth.Salt != null;
            if (usedOriginalSaltedHash)
            {
                var oldSaltedHashProvider = HostContext.Resolve<IHashProvider>();
                if (oldSaltedHashProvider.VerifyHashString(providedPassword, userAuth.PasswordHash, userAuth.Salt))
                {
                    needsRehash = !HostContext.Config.UseSaltedHash;
                    return true;
                }

                return false;
            }

            if (passwordHasher == null)
            {
                if (Log.IsDebugEnabled)
                    Log.Debug("Found newer PasswordHash without Salt but no registered IPasswordHasher to verify it");

                return false;
            }

            if (passwordHasher.VerifyPassword(userAuth.PasswordHash, providedPassword, out needsRehash))
            {
                needsRehash = HostContext.Config.UseSaltedHash;
                return true;
            }

            if (HostContext.Config.FallbackPasswordHashers.Count > 0)
            {
                var decodedHashedPassword = Convert.FromBase64String(userAuth.PasswordHash);
                if (decodedHashedPassword.Length == 0)
                {
                    if (Log.IsDebugEnabled)
                        Log.Debug("userAuth.PasswordHash is empty");

                    return false;
                }

                var formatMarker = decodedHashedPassword[0];

                foreach (var oldPasswordHasher in HostContext.Config.FallbackPasswordHashers)
                {
                    if (oldPasswordHasher.Version == formatMarker)
                    {
                        if (oldPasswordHasher.VerifyPassword(userAuth.PasswordHash, providedPassword, out _))
                        {
                            needsRehash = true;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool VerifyDigestAuth(this IUserAuth userAuth, Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence)
        {
            if (userAuth == null)
                throw new ArgumentNullException(nameof(userAuth));

            return new DigestAuthFunctions().ValidateResponse(digestHeaders, privateKey, nonceTimeOut, userAuth.DigestHa1Hash, sequence);
        }
    }
}