using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Routing;

namespace MyApp.Client.Shared;

public record NavItem(string Label, string Route, NavLinkMatch Match, string Icon, string Class)
{
    public static List<NavItem> ParseCsv(string csv)
    {
        var to = new List<NavItem>();
        var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var fields = line.Split(',').Select(x => x.Trim()).ToArray()!;
            var route = fields[1];
            var exact = route.EndsWith('$');
            var item = new NavItem(
                Label: fields[0], 
                Route: exact ? route[0..^1] : route, 
                Match: exact ? NavLinkMatch.All : NavLinkMatch.Prefix,
                Class: "",
                Icon: $"/img/nav/{fields[2]}.svg");
            to.Add(item);
        }
        return to;
    }
}

public partial class Sidebar
{
}
