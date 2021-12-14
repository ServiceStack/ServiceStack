using System;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Components;
using ServiceStack;

namespace MyApp.Client;

public static class AppExtensions
{
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
        if (returnTo.TrimStart('/').StartsWith("signin"))
            return returnTo;
        var loginUrl = "/signin" + (!string.IsNullOrEmpty(returnTo) 
                ? $"?return={returnTo}"
                : "");
        return loginUrl;
    }
}
