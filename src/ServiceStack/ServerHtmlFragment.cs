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

        public object Model { get; set;  }
        
        public Dictionary<string, object> Args { get; set; }
        
        public List<ServerHtmlFilter> Filters { get; set; }

        public IDictionary<string, string> Options { get; set; }

        public HtmlResult(ServerHtmlPage page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Args = new Dictionary<string, object>();
            Filters = new List<ServerHtmlFilter>();
            Options = new Dictionary<string, string>
            {
                { HttpHeaders.ContentType, MimeTypes.Html },
            };
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
                        await responseStream.WriteAsync(str.ValueBytes, token);
                    }
                    else if (fragment is ServerHtmlVariableFragment var)
                    {
                        if (var.Name.Equals("body"))
                        {
                            await WritePage(responseStream, token);
                        }
                        else
                        {
                            var bytes = Page.Feature.EncodeValue(GetValue(var));
                            await responseStream.WriteAsync(bytes, token);
                        }
                    }
                }
            }
            else
            {
                await WritePage(responseStream, token);
            }
        }

        public async Task WritePage(Stream responseStream, CancellationToken token = new CancellationToken())
        {
            if (Page == null)
                return;
            
            foreach (var fragment in Page.PageFragments)
            {
                if (fragment is ServerHtmlStringFragment str)
                {
                    await responseStream.WriteAsync(str.ValueBytes, token);
                }
                else if (fragment is ServerHtmlVariableFragment var)
                {
                    var bytes = Page.Feature.EncodeValue(GetValue(var));
                    await responseStream.WriteAsync(bytes, token);
                }
            }
        }

        public object GetValue(ServerHtmlVariableFragment var)
        {
            return Args.TryGetValue(var.NameString, out object value) 
                ? value 
                : Page.GetValue(var) ?? 
                  (LayoutPage != null && LayoutPage != Page.LayoutPage ? LayoutPage.GetValue(var) : null);
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
        private byte[] originalTextBytes;
        public byte[] OriginalTextBytes => originalTextBytes ?? (originalTextBytes = OriginalText.ToUtf8Bytes());
        
        public StringSegment Name { get; set; }
        private string nameString;
        public string NameString => nameString ?? (nameString = Name.Value);
        
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

        private byte[] valueBytes;
        public byte[] ValueBytes => valueBytes ?? (valueBytes = Value.ToUtf8Bytes());

        public ServerHtmlStringFragment(StringSegment value)
        {
            Value = value;
        }
    }
}