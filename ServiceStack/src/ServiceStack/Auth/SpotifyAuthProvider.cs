using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth;

/// <summary>
/// Create an OAuth2 App at: https://developer.spotify.com/dashboard/applications
/// The Apps Callback URL should match the CallbackUrl here.
/// Spotify OAuth2 info: https://developer.spotify.com/documentation/general/guides/authorization-guide/
/// </summary>
public class SpotifyAuthProvider : OAuth2Provider
{
    public const string Name = "spotify";
    public static string Realm = DefaultAuthorizeUrl;

    const string DefaultAuthorizeUrl = "https://accounts.spotify.com/authorize";
    const string DefaultAccessTokenUrl = "https://accounts.spotify.com/api/token";
    const string DefaultUserProfileUrl = "https://api.spotify.com/v1/me";

    public SpotifyAuthProvider(IAppSettings appSettings)
        : base(appSettings, Realm, Name, "ClientId", "ClientSecret")
    {
        AuthorizeUrl = appSettings.Get($"oauth.{Name}.AuthorizeUrl", DefaultAuthorizeUrl);
        AccessTokenUrl = appSettings.Get($"oauth.{Name}.AccessTokenUrl", DefaultAccessTokenUrl);
        UserProfileUrl = appSettings.Get($"oauth.{Name}.UserProfileUrl", DefaultUserProfileUrl);

        if (Scopes == null || Scopes.Length == 0)
        {
            Scopes = new[]
            {
                "user-read-private",
                "user-read-email"
            };
        }

        // You can customize the icon and sign-in button here.
        NavItem = new NavItem
        {
            Href = "/auth/" + Name,
            Label = "Sign In with Spotify",
            Id = "btn-" + Name
        };
        Icon = Svg.ImageSvg(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"256\" height=\"256\" viewBox=\"0 0 256 256\"><path fill=\"#1ED760\" d=\"M128 0C57.308 0 0 57.309 0 128c0 70.696 57.309 128 128 128c70.697 0 128-57.304 128-128C256 57.314 198.697.007 127.998.007l.001-.006Zm58.699 184.614c-2.293 3.76-7.215 4.952-10.975 2.644c-30.053-18.357-67.885-22.515-112.44-12.335a7.981 7.981 0 0 1-9.552-6.007a7.968 7.968 0 0 1 6-9.553c48.76-11.14 90.583-6.344 124.323 14.276c3.76 2.308 4.952 7.215 2.644 10.975Zm15.667-34.853c-2.89 4.695-9.034 6.178-13.726 3.289c-34.406-21.148-86.853-27.273-127.548-14.92c-5.278 1.594-10.852-1.38-12.454-6.649c-1.59-5.278 1.386-10.842 6.655-12.446c46.485-14.106 104.275-7.273 143.787 17.007c4.692 2.89 6.175 9.034 3.286 13.72v-.001Zm1.345-36.293C162.457 88.964 94.394 86.71 55.007 98.666c-6.325 1.918-13.014-1.653-14.93-7.978c-1.917-6.328 1.65-13.012 7.98-14.935C93.27 62.027 168.434 64.68 215.929 92.876c5.702 3.376 7.566 10.724 4.188 16.405c-3.362 5.69-10.73 7.565-16.4 4.187h-.006Z\"/></svg>");
    }
    
    protected override async Task<string> GetAccessTokenJsonAsync(string code, AuthContext ctx,
        CancellationToken token = new())
    {
        var payload = $"code={code}&redirect_uri={CallbackUrl.UrlEncode()}&grant_type=authorization_code";
    
        var url = AccessTokenUrlFilter(ctx, AccessTokenUrl);
        var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ConsumerKey}:{ConsumerSecret}"));
    
        var contents = await url.PostToUrlAsync(payload, 
            requestFilter: req => req.Headers.Add("Authorization", $"Basic {base64String}"),
            token: token).ConfigAwait();
    
        return contents;
    }

    protected override async Task<Dictionary<string, string>> CreateAuthInfoAsync(string accessToken,
        CancellationToken token = new())
    {
        var json = await DefaultUserProfileUrl
            .GetJsonFromUrlAsync(request => { request.Headers.Add("Authorization", "Bearer " + accessToken); },
                token: token).ConfigAwait();
        var obj = (Dictionary<string,object>) JSON.parse(json);
        
        obj.Add("name", obj["display_name"]);
        obj.MoveKey("id", "user_id");
        if (obj.TryGetValue("images", out var oImages) && oImages is List<object> { Count: > 0 } images)
        {
            var firstImage = (Dictionary<string, object>)images[0];
            if (firstImage.TryGetValue("url", out var oUrl) && oUrl is string url)
            {
                obj[AuthMetadataProvider.ProfileUrlKey] = url;
            }
        }
        var objStr = obj.ToStringDictionary();
        return objStr;
    }
}
