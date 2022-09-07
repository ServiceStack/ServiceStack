using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using ServiceStack;

namespace MyApp.Client;

public static class AppUtils
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

    public static async Task<List<NavItem>> GetNavItemsAsync(this IJSRuntime js, string name)
    {
        var csv = await js.InvokeAsync<string>("JS.get", name);
        return ParseNavItemsCsv(csv);
    }

    public static List<NavItem> ParseNavItemsCsv(string csv)
    {
        csv = csv.Trim();
        var to = new List<NavItem>();
        var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var fields = line.Split(',').Select(x => x.Trim()).ToArray()!;
            var route = fields[1];
            var exact = route.EndsWith('$');
            var item = new NavItem { 
                Label = fields[0],
                Href = exact ? route[0..^1] : route,
                Exact = exact, 
                IconSrc = fields.Length > 2 ? fields[2] : null,
            };
            to.Add(item);
        }
        return to;
    }

    public static NavLinkMatch LinkMatch(this NavItem item) => item.Exact == true ? NavLinkMatch.All : NavLinkMatch.Prefix;

}
