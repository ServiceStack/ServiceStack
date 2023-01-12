﻿using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ServiceStack.Text;

namespace ServiceStack.Blazor;

public static class JsUtils
{
    public static async Task Log(this IJSRuntime js, params object[] args)
    {
        var log = BlazorConfig.Instance.GetLog();
        if (log != null)
        {
            foreach (var arg in args)
            {
                log.LogInformation(arg.Dump());
            }
        }
        await js.ConsoleLog(args);
    }

    public static async Task ConsoleLog(this IJSRuntime js, params object[] args)
    {
        await js.InvokeVoidAsync("console.log", args);
    }

    public static void SetTitle(this IJSInProcessRuntime js, string title) =>
        js.InvokeVoid("JS.elInvoke", "document", "title", title);
    public static async Task SetTitleAsync(this IJSRuntime js, string title) =>
        await js.InvokeVoidAsync("JS.elInvoke", "document", "title", title);

    public static async Task<List<NavItem>> GetNavItemsAsync(this IJSRuntime js, string name)
    {
        var csv = await js.InvokeAsync<string>("JS.get", name);
        return ParseNavItemsCsv(csv);
    }
    
    public static List<NavItem> ParseNavItemsCsv(string csv)
    {
        if (csv == null) 
            return new();

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

    public static async Task OpenAsync(this IJSRuntime js, string url, string? target = null)
    {
        await js.InvokeVoidAsync("window.open", url, target);
    }
}
