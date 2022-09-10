using Microsoft.JSInterop;
using ServiceStack.Text;

namespace ServiceStack.Blazor;

public static class JSUtils
{
    public static async Task Log(this IJSRuntime js, params object[] args)
    {
        args.Each(x => Console.WriteLine(x.Dump()));
        await js.ConsoleLog(args);
    }

    public static async Task ConsoleLog(this IJSRuntime js, params object[] args)
    {
        await js.InvokeVoidAsync("console.log", args);
    }
}
