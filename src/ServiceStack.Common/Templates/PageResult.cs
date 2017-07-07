using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Templates
{
    public interface IPageResult {}

    public class PageResult : IPageResult, IStreamWriterAsync, IHasOptions
    {
        public TemplatePage Page { get; set; }

        public TemplatePage LayoutPage { get; set; }

        public object Model { get; set;  }
        
        public Dictionary<string, object> Args { get; set; }
        
        public List<TemplateFilter> Filters { get; set; }

        public IDictionary<string, string> Options { get; set; }

        public PageResult(TemplatePage page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Args = new Dictionary<string, object>();
            Filters = new List<TemplateFilter>();
            Options = new Dictionary<string, string>
            {
                { HttpHeaders.ContentType, Page.Format.ContentType },
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
                    if (fragment is PageStringFragment str)
                    {
                        await responseStream.WriteAsync(str.ValueBytes, token);
                    }
                    else if (fragment is PageVariableFragment var)
                    {
                        if (var.Name.Equals("body"))
                        {
                            await WritePage(responseStream, token);
                        }
                        else
                        {
                            var bytes = Page.Format.EncodeValue(GetValue(var));
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
                if (fragment is PageStringFragment str)
                {
                    await responseStream.WriteAsync(str.ValueBytes, token);
                }
                else if (fragment is PageVariableFragment var)
                {
                    var bytes = Page.Format.EncodeValue(GetValue(var));
                    await responseStream.WriteAsync(bytes, token);
                }
            }
        }

        public object GetValue(PageVariableFragment var)
        {
            return Args.TryGetValue(var.NameString, out object value) 
                ? value 
                : Page.GetValue(var) ?? 
                  (LayoutPage != null && LayoutPage != Page.LayoutPage ? LayoutPage.GetValue(var) : null);
        }
    }
}