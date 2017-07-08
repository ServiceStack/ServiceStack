using System.Collections.Concurrent;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.Templates;
using ServiceStack.Web;

namespace ServiceStack
{
    public class TemplatePagesFeature : TemplatePagesContext, IPlugin
    {
        public static int PreventDosMaxSize = 10000;
        private static readonly ConcurrentDictionary<string, byte> catchAllPathsNotFound = new ConcurrentDictionary<string, byte>();

        public void Register(IAppHost appHost)
        {
            DebugMode = appHost.Config.DebugMode;
            VirtualFileSources = appHost.VirtualFileSources;
            appHost.Register(Pages);
            appHost.CatchAllHandlers.Add(RequestHandler);
        }

        protected virtual IHttpHandler RequestHandler(string httpMethod, string pathInfo, string filePath)
        {
            if (catchAllPathsNotFound.ContainsKey(pathInfo))
                return null;

            var page = Pages.GetOrCreatePage(pathInfo);

            if (page != null)
            {
                if (page.File.Name.StartsWith("_"))
                    return new ForbiddenHttpHandler();
                
                return new TemplatePagesHandler(page);
            }
            
            if (!pathInfo.EndsWith("/") && VirtualFileSources.DirectoryExists(pathInfo.TrimPrefixes("/")))
                return new RedirectHttpHandler { RelativeUrl = pathInfo + "/", StatusCode = HttpStatusCode.MovedPermanently };

            if (catchAllPathsNotFound.Count > PreventDosMaxSize)
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
            var result = new PageResult(page)
            {
                LayoutPage = layoutPage
            };

            await result.WriteToAsync(httpRes.OutputStream);
        }
    }
    
}