using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;
using ServiceStack.Text;

#if NETSTANDARD1_6
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack
{
    public interface IHtmlPages
    {
        ServerHtmlPage GetPage(string path);
    }
    
    public interface IHtmlResult {}

    public class HtmlResult : IHtmlResult, IStreamWriterAsync, IHasOptions
    {
        public ServerHtmlPage Page { get; set; }

        public ServerHtmlPage LayoutPage { get; set; }

        public object Model
        {
            get => Args.TryGetValue("model", out object model) ? model : null;
            set => Args["model"] = value;
        }
        
        public Dictionary<string, object> Args { get; set; }
        
        public List<ServerHtmlFilter> Filters { get; set; }

        public IDictionary<string, string> Options { get; set; }

        private ServerHtmlFeature feature;

        public HtmlResult(ServerHtmlPage page)
        {
            Page = page;
            Args = new Dictionary<string, object>();
            Filters = new List<ServerHtmlFilter>();
            Options = new Dictionary<string, string>
            {
                { HttpHeaders.ContentType, MimeTypes.Html },
            };
            feature = HostContext.GetPlugin<ServerHtmlFeature>();
        }

        public async Task WriteToAsync(Stream responseStream, CancellationToken token = new CancellationToken())
        {
            if (LayoutPage != null)
            {
                await Task.WhenAll(LayoutPage.Init(), Page.Init());
            }
            else
            {
                await Page.Init();
                if (Page.LayoutPage != null)
                {
                    LayoutPage = Page.LayoutPage;
                    await LayoutPage.Init();
                }
            }

            token.ThrowIfCancellationRequested();

            if (LayoutPage != null)
            {
                foreach (var fragment in LayoutPage.PageFragments)
                {
                    if (fragment is ServerHtmlStringFragment str)
                    {
                        var bytes = str.Value.ToString().ToUtf8Bytes();
                        await responseStream.WriteAsync(bytes, 0, bytes.Length, token);
                    }
                    else if (fragment is ServerHtmlVariableFragment var)
                    {
                        if (var.Name.Equals("body"))
                        {
                            await WritePage(Page, responseStream, token);
                        }
                        else
                        {
                            var bytes = var.OriginalText.ToString().ToUtf8Bytes();
                            await responseStream.WriteAsync(bytes, 0, bytes.Length, token);
                        }
                    }
                }
            }
            else
            {
                await WritePage(Page, responseStream, token);
            }
        }

        public static async Task WritePage(ServerHtmlPage page, Stream responseStream, CancellationToken token = new CancellationToken())
        {
            if (page == null)
                return;
            
            foreach (var fragment in page.PageFragments)
            {
                if (fragment is ServerHtmlStringFragment str)
                {
                    var bytes = str.Value.ToString().ToUtf8Bytes();
                    await responseStream.WriteAsync(bytes, 0, bytes.Length, token);
                }
                else if (fragment is ServerHtmlVariableFragment var)
                {
                    var bytes = var.OriginalText.ToString().ToUtf8Bytes();
                    await responseStream.WriteAsync(bytes, 0, bytes.Length, token);
                }
            }
        }
    }

    public class ServerHtmlHandler : HttpAsyncTaskHandler
    {
        private readonly ServerHtmlPage page;
        private readonly ServerHtmlPage layoutPage;

        public ServerHtmlHandler(ServerHtmlPage page, ServerHtmlPage layoutPage = null)
        {
            this.page = page;
            this.layoutPage = layoutPage;
        }

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var result = new HtmlResult(page)
            {
                LayoutPage = layoutPage
            };

            await result.WriteToAsync(httpRes.OutputStream);
        }

        public override bool RunAsAsync() => true;
    }

    public class ServerHtmlFilter {}

    public abstract class ServerHtmlFragment {}

    public class ServerHtmlVariableFragment : ServerHtmlFragment
    {
        public StringSegment OriginalText { get; set; }
        public StringSegment Name { get; set; }
        public List<Command> FilterCommands { get; set; }

        public ServerHtmlVariableFragment(StringSegment originalText, StringSegment name, List<Command> filterCommands)
        {
            OriginalText = originalText;
            Name = name;
            FilterCommands = filterCommands;
        }
    }

    public class ServerHtmlStringFragment : ServerHtmlFragment
    {
        public StringSegment Value { get; set; }

        public ServerHtmlStringFragment(StringSegment value)
        {
            Value = value;
        }
    }
}