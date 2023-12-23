using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

public interface IHtmlModuleFragment
{
    Task WriteToAsync(HtmlModuleContext ctx, Stream responseStream, CancellationToken token = default);
}

public class HtmlTextFragment : IHtmlModuleFragment
{
    public ReadOnlyMemory<char> Text { get; }
    public ReadOnlyMemory<byte> TextUtf8 { get; }
    public HtmlTextFragment(string text) : this(text.AsMemory()) {}
    public HtmlTextFragment(ReadOnlyMemory<char> text)
    {
        Text = text;
        TextUtf8 = Text.ToUtf8();
    }
    public async Task WriteToAsync(HtmlModuleContext ctx, Stream responseStream, CancellationToken token = default) => 
        await responseStream.WriteAsync(TextUtf8, token).ConfigAwait();
}

public class HtmlTokenFragment(string token, Func<HtmlModuleContext, ReadOnlyMemory<byte>> fn)
    : IHtmlModuleFragment
{
    public string Token { get; } = token;

    public async Task WriteToAsync(HtmlModuleContext ctx, Stream responseStream, CancellationToken token = default) => 
        await responseStream.WriteAsync(fn(ctx), token).ConfigAwait();
}

public class HtmlHandlerFragment(string token, string args, Func<HtmlModuleContext, string, ReadOnlyMemory<byte>> fn)
    : IHtmlModuleFragment
{
    public string Token { get; } = token;
    public string Args { get; } = args;

    public async Task WriteToAsync(HtmlModuleContext ctx, Stream responseStream, CancellationToken token = default) => 
        await responseStream.WriteAsync(fn(ctx, Args), token).ConfigAwait();
}