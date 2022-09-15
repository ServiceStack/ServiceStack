using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System.Collections.Specialized;

namespace ServiceStack.Blazor;

public static class NavigationUtils
{
    public static NavLinkMatch LinkMatch(this NavItem item) => item.Exact == true ? NavLinkMatch.All : NavLinkMatch.Prefix;

    //https://jasonwatmore.com/post/2020/08/09/blazor-webassembly-get-query-string-parameters-with-navigation-manager
    public static NameValueCollection QueryString(this NavigationManager nav) =>
        System.Web.HttpUtility.ParseQueryString(new Uri(nav.Uri).Query);

    public static string? QueryString(this NavigationManager nav, string key) => nav.QueryString()[key];

    public static string GetReturnUrl(this NavigationManager nav)
    {
        var returnUrl = nav.QueryString("return");
        if (returnUrl == null || returnUrl.IsEmpty())
            return "/";
        return returnUrl;
    }

    public static string GetLoginUrl(this NavigationManager nav)
    {
        var returnTo = nav.ToBaseRelativePath(nav.Uri);
        if (returnTo.TrimStart('/').StartsWith(BlazorConfig.Instance.RedirectSignIn.TrimStart('/')))
            return returnTo;
        var loginUrl = BlazorConfig.Instance.RedirectSignIn.SetQueryParam("return", returnTo);
        return loginUrl;
    }
}
