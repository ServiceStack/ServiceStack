using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Templates
{
    public interface IPageResult {}

    // Render a Template Page to the Response OutputStream
    public class PageResult : IPageResult, IStreamWriterAsync, IHasOptions
    {
        public TemplatePage Page { get; set; }

        public TemplatePage LayoutPage { get; set; }

        public object Model { get; set;  }
        
        /// <summary>
        /// Add additional Args available to all pages 
        /// </summary>
        public Dictionary<string, object> Args { get; set; }

        /// <summary>
        /// Add additional template filters available to all pages
        /// </summary>
        public List<TemplateFilter> TemplateFilters { get; set; }

        public IDictionary<string, string> Options { get; set; }

        /// <summary>
        /// Specify the Content-Type of the Response 
        /// </summary>
        public string ContentType
        {
            get => Options.TryGetValue(HttpHeaders.ContentType, out string contentType) ? contentType : null;
            set => Options[HttpHeaders.ContentType] = value;
        }
        
        /// <summary>
        /// Transform the Page output using a chain of filters
        /// </summary>
        public List<Func<Stream,Task<Stream>>> PageFilters { get; set; }

        /// <summary>
        /// Transform the entire output using a chain of filters
        /// </summary>
        public List<Func<Stream,Task<Stream>>> OutputFilters { get; set; }

        public PageResult(TemplatePage page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Args = new Dictionary<string, object>();
            TemplateFilters = new List<TemplateFilter>();
            PageFilters = new List<Func<Stream,Task<Stream>>>();
            OutputFilters = new List<Func<Stream,Task<Stream>>>();
            Options = new Dictionary<string, string>
            {
                { HttpHeaders.ContentType, Page.Format.ContentType },
            };
        }

        public async Task WriteToAsync(Stream responseStream, CancellationToken token = default(CancellationToken))
        {
            if (OutputFilters.Count == 0)
            {
                await WriteToAsyncInternal(responseStream, token);
                return;
            }

            using (var ms = MemoryStreamFactory.GetStream())
            {
                await WriteToAsyncInternal(ms, token);
                Stream stream = ms;

                foreach (var filter in OutputFilters)
                {
                    stream.Position = 0;
                    stream = await filter(stream);
                }

                using (stream)
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(responseStream);
                }
            }
        }

        internal async Task WriteToAsyncInternal(Stream responseStream, CancellationToken token)
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
                            await WritePageAsync(responseStream, token);
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
                await WritePageAsync(responseStream, token);
            }
        }

        public async Task WritePageAsync(Stream responseStream, CancellationToken token = default(CancellationToken))
        {
            if (PageFilters.Count == 0)
            {
                await WritePageAsyncInternal(responseStream, token);
                return;
            }

            using (var ms = MemoryStreamFactory.GetStream())
            {
                await WritePageAsyncInternal(ms, token);
                Stream stream = ms;

                foreach (var filter in PageFilters)
                {
                    stream.Position = 0;
                    stream = await filter(stream);
                }

                using (stream)
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(responseStream);
                }
            }
        }

        internal async Task WritePageAsyncInternal(Stream responseStream, CancellationToken token = default(CancellationToken))
        {
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