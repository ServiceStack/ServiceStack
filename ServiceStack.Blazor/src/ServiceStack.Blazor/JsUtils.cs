using Microsoft.JSInterop;

namespace ServiceStack.Blazor;

public static class JsUtils
{
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
            var item = new NavItem
            {
                Label = fields[0],
                Href = exact ? route[0..^1] : route,
                Exact = exact,
                IconSrc = fields.Length > 2 ? fields[2] : null,
            };
            to.Add(item);
        }
        return to;
    }
}
