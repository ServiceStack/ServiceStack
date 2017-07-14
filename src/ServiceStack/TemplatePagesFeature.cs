using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class TemplatePagesFeature : TemplatePagesContext, IPlugin
    {
        public string HtmlExtension
        {
            get => PageFormats.First(x => x is HtmlPageFormat).Extension;
            set => PageFormats.First(x => x is HtmlPageFormat).Extension = value;
        }

        public bool ExcludeProtectedFilters
        {
            set { if (value) TemplateFilters.RemoveAll(x => x is TemplateProtectedFilters); }
        }

        public TemplatePagesFeature()
        {
            var appHost = HostContext.AssertAppHost();
            ScanAssemblies.AddRange(appHost.ServiceAssemblies);
            Container = appHost.Container;
            TemplateFilters.Add(new TemplateProtectedFilters());
            FilterTransformers["markdown"] = MarkdownPageFormat.TransformToHtml;
        }

        public void Register(IAppHost appHost)
        {
            DebugMode = appHost.Config.DebugMode;
            VirtualFiles = appHost.VirtualFileSources;
            AppSettings = appHost.AppSettings;
            appHost.Register(Pages);
            appHost.Register(this);
            appHost.CatchAllHandlers.Add(RequestHandler);
            Init();
        }

        private readonly ConcurrentDictionary<string, byte> catchAllPathsNotFound = new ConcurrentDictionary<string, byte>();
        protected virtual IHttpHandler RequestHandler(string httpMethod, string pathInfo, string filePath)
        {
            if (catchAllPathsNotFound.ContainsKey(pathInfo)) return null;

            var page = Pages.GetPage(pathInfo);
            if (page != null)
            {
                if (page.File.Name.StartsWith("_"))
                    return new ForbiddenHttpHandler();
                return new TemplatePagesHandler(page);
            }
            
            if (!pathInfo.EndsWith("/") && VirtualFiles.DirectoryExists(pathInfo.TrimPrefixes("/")))
                return new RedirectHttpHandler { RelativeUrl = pathInfo + "/", StatusCode = HttpStatusCode.MovedPermanently };

            if (catchAllPathsNotFound.Count > 10000) //prevent DOS
                catchAllPathsNotFound.Clear();
            catchAllPathsNotFound[pathInfo] = 1;
            return null;
        }
    }

    public class TemplatePagesHandler : HttpAsyncTaskHandler
    {
        private readonly TemplatePage page;
        private readonly TemplatePage layoutPage;
        public TemplatePagesHandler(TemplatePage page, TemplatePage layoutPage = null)
        {
            this.page = page;
            this.layoutPage = layoutPage;
        }

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var result = new PageResult(page) { LayoutPage = layoutPage };
            await result.WriteToAsync(httpRes.OutputStream);
        }
    }

    public class MarkdownPageFormat : PageFormat
    {
        public MarkdownPageFormat()
        {
            Extension = "md";
            ContentType = MimeTypes.MarkdownText;
        }

        private static readonly MarkdownSharp.Markdown markdown = new MarkdownSharp.Markdown();
        public static async Task<Stream> TransformToHtml(Stream markdownStream)
        {
            using (var reader = new StreamReader(markdownStream))
            {
                var md = await reader.ReadToEndAsync();
                var html = markdown.Transform(md);
                return MemoryStreamFactory.GetStream(html.ToUtf8Bytes());
            }
        }
    }
}