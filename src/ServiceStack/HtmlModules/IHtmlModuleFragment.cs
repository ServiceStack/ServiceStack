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

public class HtmlTokenFragment : IHtmlModuleFragment
{
    public string Token { get; }
    private readonly Func<HtmlModuleContext, ReadOnlyMemory<byte>> fn;
    public HtmlTokenFragment(string token, Func<HtmlModuleContext, ReadOnlyMemory<byte>> fn)
    {
        this.Token = token;
        this.fn = fn;
    }
    public async Task WriteToAsync(HtmlModuleContext ctx, Stream responseStream, CancellationToken token = default) => 
        await responseStream.WriteAsync(fn(ctx), token).ConfigAwait();
}

public class HtmlHandlerFragment : IHtmlModuleFragment
{
    public string Token { get; }
    public string Args { get; }
    private readonly Func<HtmlModuleContext, string, ReadOnlyMemory<byte>> fn;
    public HtmlHandlerFragment(string token, string args, Func<HtmlModuleContext, string, ReadOnlyMemory<byte>> fn)
    {
        this.Token = token;
        this.Args = args;
        this.fn = fn;
    }
    public async Task WriteToAsync(HtmlModuleContext ctx, Stream responseStream, CancellationToken token = default) => 
        await responseStream.WriteAsync(fn(ctx, Args), token).ConfigAwait();
}