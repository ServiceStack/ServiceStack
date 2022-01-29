using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.Auth
{
    public class AuthMetadataProvider : IAuthMetadataProvider
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(AuthMetadataProvider));

        public const string ProfileUrlKey = "profileUrl";

        public string NoProfileImgUrl { get; set; }

        public AuthMetadataProvider()
        {
            NoProfileImgUrl = Svg.GetDataUri(Svg.Icons.DefaultProfile);
        }

        public virtual void AddMetadata(IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            AddProfileUrl(tokens, authInfo);
        }

        public virtual void AddProfileUrl(IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            if (tokens == null || authInfo == null)
                return;
            
            var items = tokens.Items ?? (tokens.Items = new Dictionary<string, string>());
            if (items.ContainsKey(ProfileUrlKey))
                return;

            try
            {
                //Provide Fallback to retrieve avatar urls in-case built-in access fails
                if (tokens.Provider == FacebookAuthProvider.Name)
                {
                    items[ProfileUrlKey] = GetRedirectUrlIfAny(
                        $"http://avatars.io/facebook/{tokens.UserName}?size=medium");
                }
                else if (tokens.Provider == TwitterAuthProvider.Name)
                {
                    items[ProfileUrlKey] = GetRedirectUrlIfAny(
                        $"http://avatars.io/twitter/{tokens.UserName}?size=medium");
                }

                if (!items.ContainsKey(ProfileUrlKey) && !tokens.Email.IsNullOrEmpty())
                    items[ProfileUrlKey] = tokens.Email.ToGravatarUrl(size: 64);
            }
            catch (Exception ex)
            {
                Log.Error("Error AddProfileUrl to: {0}>{1}".Fmt(tokens.Provider, tokens.UserName), ex);
            }
        }

        // Strip out any user identifying information on the url
        public static string GetRedirectUrlIfAny(string url)
        {
            var finalUrl = url;
            try
            {
                var ignore = url.GetBytesFromUrl(
                    requestFilter: req =>
                    {
                        req.With(c => c.UserAgent = "ServiceStack");
#if !NET6_0_OR_GREATER
                        req.AllowAutoRedirect = false;
#endif
                    },
                    responseFilter: res => finalUrl = res.GetHeader(HttpHeaders.Location) ?? finalUrl);
            }
            catch { }

            return finalUrl;
        }

        public virtual string GetProfileUrl(IAuthSession authSession, string defaultUrl = null)
        {
            if (authSession == null)
                return defaultUrl ?? NoProfileImgUrl;

            if (!string.IsNullOrEmpty(authSession.ProfileUrl))
                return authSession.ProfileUrl;

            foreach (var authTokens in authSession.ProviderOAuthAccess.Safe())
            {
                if (authTokens.Items != null)
                {
                    if (authTokens.Items.TryGetValue(ProfileUrlKey, out var profileUrl))
                        return profileUrl.SanitizeOAuthUrl();
                }
            }

            return defaultUrl ?? NoProfileImgUrl;
        }
    }

    public interface IAuthMetadataProvider
    {
        void AddMetadata(IAuthTokens tokens, Dictionary<string, string> authInfo);

        string GetProfileUrl(IAuthSession authSession, string defaultUrl = null);
    }

    public static class AuthMetadataProviderExtensions
    {
        public static void SafeAddMetadata(this IAuthMetadataProvider provider, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            provider?.AddMetadata(tokens, authInfo);
        }
    }
}