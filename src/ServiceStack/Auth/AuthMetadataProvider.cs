using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.Auth
{
    public class AuthMetadataProvider : IAuthMetadataProvider
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(AuthMetadataProvider));

        public const string DefaultNoProfileImgUrl = "https://raw.githubusercontent.com/ServiceStack/Assets/master/img/apps/no-profile64.png";

        public const string ProfileUrlKey = "profileUrl";

        public string NoProfileImgUrl { get; set; }

        public AuthMetadataProvider()
        {
            NoProfileImgUrl = DefaultNoProfileImgUrl;
        }

        public virtual void AddProfileUrl(IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            var items = tokens.Items ?? (tokens.Items = new Dictionary<string, string>());

            try
            {
                if (!items.ContainsKey(ProfileUrlKey) && !tokens.Email.IsNullOrEmpty())
                    items[ProfileUrlKey] = tokens.Email.ToGravatarUrl(size: 64);

                if (tokens.Provider == FacebookAuthProvider.Name)
                {
                    tokens.DisplayName = tokens.DisplayName ?? tokens.DisplayName;
                    items[ProfileUrlKey] = "http://avatars.io/facebook/{0}?size=medium".Fmt(tokens.UserName)
                        .GetRedirectUrlIfAny() ?? items[ProfileUrlKey];
                }
                else if (tokens.Provider == TwitterAuthProvider.Name)
                {
                    tokens.DisplayName = tokens.UserName ?? tokens.DisplayName;
                    items[ProfileUrlKey] = "http://avatars.io/twitter/{0}?size=medium".Fmt(tokens.UserName)
                        .GetRedirectUrlIfAny() ?? items[ProfileUrlKey];
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error AddProfileUrl to: {0}>{1}".Fmt(tokens.Provider, tokens.UserName), ex);
            }
        }

        public virtual string GetProfileUrl(IAuthSession authSession)
        {
            if (authSession == null)
                return NoProfileImgUrl;

            foreach (var authTokens in authSession.ProviderOAuthAccess)
            {
                if (authTokens.Items != null)
                {
                    string profileUrl;
                    if (authTokens.Items.TryGetValue(ProfileUrlKey, out profileUrl))
                        return profileUrl;
                }
            }

            return NoProfileImgUrl;
        }
    }

    public interface IAuthMetadataProvider
    {
        void AddProfileUrl(IAuthTokens tokens, Dictionary<string, string> authInfo);

        string GetProfileUrl(IAuthSession authSession);
    }

    public static class AuthMetadataProviderExtensions
    {
        public static void AddMetadata(this IAuthMetadataProvider provider, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            if (provider == null)
                return;

            provider.AddProfileUrl(tokens, authInfo);
        }

        public static string GetProfileUrl(this IAuthSession authSession)
        {
            var profile = HostContext.TryResolve<IAuthMetadataProvider>();
            return profile == null ? null : profile.GetProfileUrl(authSession);
        }
    }
}