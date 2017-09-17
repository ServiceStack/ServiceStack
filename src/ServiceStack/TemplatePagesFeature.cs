using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class TemplatePagesFeature : TemplateContext, IPlugin
    {
        public bool DisableHotReload { get; set; }

        public bool EnableDebugTemplate { get; set; }
        public bool EnableDebugTemplateToAll { get; set; }

        public string DebugDefaultTemplate { get; set; }
        
        public string ApiPath { get; set; }
        public string ApiDefaultContentType { get; set; } = MimeTypes.Json;

        public List<string> IgnorePaths { get; set; } = new List<string>
        {
            "/swagger-ui" //Swagger's handler needs to process index.html 
        };

        public TemplateServiceStackFilters ServiceStackFilters => TemplateFilters.FirstOrDefault(x => x is TemplateServiceStackFilters) as TemplateServiceStackFilters;

        public string HtmlExtension
        {
            get => PageFormats.First(x => x is HtmlPageFormat).Extension;
            set => PageFormats.First(x => x is HtmlPageFormat).Extension = value;
        }

        public bool ExcludeProtectedFilters
        {
            set
            {
                if (value) TemplateFilters.RemoveAll(x => x is TemplateProtectedFilters);
            }
        }

        public TemplatePagesFeature()
        {
            var appHost = HostContext.AssertAppHost();
            ScanAssemblies.AddRange(appHost.ServiceAssemblies);
            Container = appHost.Container;
            TemplateFilters.Add(new TemplateProtectedFilters());
            TemplateFilters.Add(new TemplateInfoFilters());
            TemplateFilters.Add(new TemplateServiceStackFilters());
            FilterTransformers["markdown"] = MarkdownPageFormat.TransformToHtml;
            SkipExecutingFiltersIfError = true;
        }

        public void Register(IAppHost appHost)
        {
            DebugMode = appHost.Config.DebugMode;
            VirtualFiles = appHost.VirtualFileSources;
            AppSettings = appHost.AppSettings;
            appHost.Register(Pages);
            appHost.Register(this);
            appHost.CatchAllHandlers.Add(RequestHandler);

            if (!DisableHotReload)
                appHost.RegisterService(typeof(TemplateHotReloadService));
            
            if (!string.IsNullOrEmpty(ApiPath))
                appHost.RegisterService(typeof(TemplateApiPagesService), 
                    (ApiPath[0] == '/' ? ApiPath : '/' + ApiPath).CombineWith("/{PageName}/{PathInfo*}"));

            if (DebugMode || EnableDebugTemplate || EnableDebugTemplateToAll)
            {
                appHost.RegisterService(typeof(TemplateMetadataDebugService), TemplateMetadataDebugService.Route);
                appHost.GetPlugin<MetadataFeature>().AddDebugLink(TemplateMetadataDebugService.Route, "Debug Templates");
            }

            Init();
        }

        private readonly ConcurrentDictionary<string, byte> catchAllPathsNotFound =
            new ConcurrentDictionary<string, byte>();

        protected virtual IHttpHandler RequestHandler(string httpMethod, string pathInfo, string filePath)
        {
            if (!DebugMode && catchAllPathsNotFound.ContainsKey(pathInfo))
                return null;

            foreach (var ignorePath in IgnorePaths)
            {
                if (pathInfo.StartsWith(ignorePath))
                {
                    catchAllPathsNotFound[pathInfo] = 1;
                    return null;
                }
            }

            var codePage = Pages.GetCodePage(pathInfo);
            if (codePage != null)
                return new TemplateCodePageHandler(codePage);

            var page = Pages.GetPage(pathInfo);
            if (page != null)
            {
                if (page.File.Name.StartsWith("_"))
                    return new ForbiddenHttpHandler();
                return new TemplatePageHandler(page);
            }

            if (!DebugMode)
            {
                if (catchAllPathsNotFound.Count > 10000) //prevent DOS
                    catchAllPathsNotFound.Clear();
                catchAllPathsNotFound[pathInfo] = 1;
            }
            
            return null;
        }
    }

    [ExcludeMetadata]
    [Route("/templates/hotreload/page")]
    public class HotReloadPage : IReturn<HotReloadPageResponse>
    {
        public string Path { get; set; }
        public string ETag { get; set; }
    }

    public class HotReloadPageResponse
    {
        public string ETag { get; set; }
        public bool Reload { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DefaultRequest(typeof(HotReloadPage))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class TemplateHotReloadService : Service
    {
        public ITemplatePages Pages { get; set; }

        public async Task<HotReloadPageResponse> Any(HotReloadPage request)
        {
            if (!HostContext.DebugMode)
                throw new NotImplementedException("set 'debug true' in web.settings to enable this service");

            var page = Pages.GetPage(request.Path ?? "/");
            if (page == null)
                throw HttpError.NotFound("Page not found: " + request.Path);

            if (!page.HasInit)
                await page.Init();

            var lastModified = Pages.GetLastModified(page);

            if (string.IsNullOrEmpty(request.ETag))
                return new HotReloadPageResponse { ETag = lastModified.Ticks.ToString() };

            var shouldReload = lastModified.Ticks > long.Parse(request.ETag);
            return new HotReloadPageResponse { Reload = shouldReload, ETag = lastModified.Ticks.ToString() };
        }
    }

    [ExcludeMetadata]
    public class ApiPages
    {
        public string PageName { get; set; }
        public string PathInfo { get; set; }
    }

    [DefaultRequest(typeof(ApiPages))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class TemplateApiPagesService : Service
    {
        public async Task<object> Any(ApiPages request) 
        {
            if (string.IsNullOrEmpty(request.PageName))
                throw new ArgumentNullException("PageName");

            var parts = string.IsNullOrEmpty(request.PathInfo)  
                ? TypeConstants.EmptyStringArray
                : request.PathInfo.SplitOnLast('.');

            var hasPathContentType = parts.Length > 1 && Host.ContentTypes.KnownFormats.Contains(parts[1]);
            var pathInfo = hasPathContentType
                ? parts[0]
                : request.PathInfo;
            
            var pathArgs = string.IsNullOrEmpty(pathInfo)
                ? TypeConstants.EmptyStringArray
                : pathInfo.Split('/');
            
            parts = request.PageName.SplitOnLast('.');
            var hasPageContentType = pathArgs.Length == 0 && parts.Length > 1 && Host.ContentTypes.KnownFormats.Contains(parts[1]);
            var pageName = hasPageContentType
                ? parts[0]
                : request.PageName;

            // Change .csv download file name
            base.Request.OperationName = pageName + (pathArgs.Length > 0 ? "_" + string.Join("_", pathArgs) : "");
            
            var feature = HostContext.GetPlugin<TemplatePagesFeature>();

            if (feature.ApiDefaultContentType != null &&
                !hasPathContentType &&
                !hasPageContentType &&
                base.Request.QueryString["format"] == null && base.Request.ResponseContentType == MimeTypes.Html)
            {
                base.Request.ResponseContentType = feature.ApiDefaultContentType;
            }

            var pagePath = feature.ApiPath.CombineWith(pageName).TrimStart('/');
            var page = base.Request.GetPage(pagePath);
            if (page == null)
                throw HttpError.NotFound($"No API Page was found at '{pagePath}'");
            
            var requestArgs = base.Request.GetTemplateRequestParams();
            requestArgs[TemplateConstants.PathInfo] = request.PathInfo;
            requestArgs[TemplateConstants.PathArgs] = pathArgs; 

            var pageResult = new PageResult(page) {
                NoLayout = true,
                RethrowExceptions = true,
                Args = requestArgs
            };

            var discardedOutput = await pageResult.RenderToStringAsync();

            if (!pageResult.Args.TryGetValue("return", out object response))
                throw HttpError.NotFound($"The API Page did not specify a response. Use the 'return' filter to set a return value for the page.");

            if (response is Task<object> responseTask)
                response = await responseTask;
            
            var httpResultHeaders = (pageResult.Args.TryGetValue("returnArgs", out object returnArgs) ? returnArgs : null).ToStringDictionary();

            var result = new HttpResult(response);
            httpResultHeaders.Each(x => result.Options[x.Key] = x.Value);
            return result;
        }
    }

    [ExcludeMetadata]
    public class TemplateMetadataDebug : IReturn<string>
    {
        public string Template { get; set; }
        public string AuthSecret { get; set; }
    }

    [ReturnExceptionsInJsonAttribute]
    [DefaultRequest(typeof(TemplateMetadataDebug))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class TemplateMetadataDebugService : Service
    {
        public static string Route = "/metadata/debug"; 
        
        public static string DefaultTemplate = @"<table><tr><td style='width:50%'><pre>
Service Name              {{ appHost.ServiceName }}
Handler Path              {{ appConfig.HandlerFactoryPath }}
VirtualFiles Path         {{ appVirtualFilesPath }}
VirtualFileSources Path   {{ appVirtualFileSourcesPath }}
OS Environment Variable   {{ 'OS' | envVariable }}
ServiceStack Version      {{ envServiceStackVersion }}

Request: 
  - RemoteIp              {{ request.RemoteIp }}
  - UserHostAddress       {{ request.UserHostAddress }}
  - PathInfo              {{ request.PathInfo }}
  - UserAgent             {{ request.UserAgent }}

Session:
  - ss-id                 {{ userSessionId }}
  - ss-pid                {{ userPermanentSessionId }}
  - ss-opt                {{ userSessionOptions | join }}

User: 
  - IsAuthenticated       {{ userSession | select: { it.IsAuthenticated } }}
  - UserName              {{ userSession | select: { it.UserName } }}
  - LastName              {{ userSession | select: { it.LastName } }}
  - Is Admin              {{ userHasRole('Admin') }}
  - Has Permission        {{ userHasPermission('ThePermission') }}

Plugins: {{ plugins | select: \n  - { it | typeName } }}
</pre></td><td style='width:50%'> 
{{ meta.Operations | take(10) | map('{ Request: it.Name, Response: it.ResponseType.Name, Service: it.ServiceType.Name }') | htmlDump({ caption: 'First 10 Services'}) }}
<table><caption>Network Information</caption>
<tr><th>    IPv4 Addresses                            </th><th>              IPv6 Addresses                            </th></tr>
<td><pre>{{ networkIpv4Addresses | select: \n{ it } }}</pre></td><td><pre>{{ networkIpv6Addresses | select: \n{ it } }}</pre><td></tr></pre></td>
</tr></table>";
        
        public object Any(TemplateMetadataDebug request)
        {
            if (string.IsNullOrEmpty(request.Template))
                return null;

            var feature = HostContext.GetPlugin<TemplatePagesFeature>();
            if (!HostContext.DebugMode && !feature.EnableDebugTemplateToAll)
            {
                if (HostContext.Config.AdminAuthSecret == null || HostContext.Config.AdminAuthSecret != request.AuthSecret)
                {
                    RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);
                }
            }
            
            var context = new TemplateContext
            {
                TemplateFilters = { new TemplateInfoFilters() },
                Args =
                {
                    {TemplateConstants.Request, base.Request},
                    {"request", base.Request},
                    {"appHost", HostContext.AppHost},
                    {"appConfig", HostContext.Config},
                    {"appVirtualFilesPath", HostContext.VirtualFiles.RootDirectory.RealPath},
                    {"appVirtualFileSourcesPath", HostContext.VirtualFileSources.RootDirectory.RealPath},
                    {"meta", HostContext.Metadata},
                }
            }.Init();

            feature.Args.Each(x => context.Args[x.Key] = x.Value);

            var result = context.EvaluateTemplate(request.Template);
            return new HttpResult(result) { ContentType = MimeTypes.PlainText }; 
        }

        public object GetHtml(TemplateMetadataDebug request)
        {
            var feature = HostContext.GetPlugin<TemplatePagesFeature>();
            if (!HostContext.DebugMode && !feature.EnableDebugTemplateToAll)
                RequiredRoleAttribute.AssertRequiredRoles(Request, RoleNames.Admin);
            
            if (request.Template != null)
                return Any(request);

            var defaultTemplate = feature.DebugDefaultTemplate ?? DefaultTemplate;

            var html = HtmlTemplates.GetMetadataDebugTemplate();
            html = html.Replace("{0}", defaultTemplate);

            var authsecret = Request.GetParam(Keywords.AuthSecret);
            if (HostContext.Config.AdminAuthSecret != null &&
                HostContext.Config.AdminAuthSecret == authsecret)
            {
                html = html.Replace("{ template: template }", 
                    "{ template: template, authsecret:" + feature.DefaultFilters.jsQuotedString(authsecret).ToRawString() + " }");
            }
 
            return html;
        }
    }

    public class TemplatePageHandler : HttpAsyncTaskHandler
    {
        private readonly TemplatePage page;
        private readonly TemplatePage layoutPage;

        public TemplatePageHandler(TemplatePage page, TemplatePage layoutPage = null)
        {
            this.RequestName = !string.IsNullOrEmpty(page.VirtualPath) ? page.VirtualPath : nameof(TemplatePageHandler);
            this.page = page;
            this.layoutPage = layoutPage;
        }

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {            
            var result = new PageResult(page)
            {
                Args = httpReq.GetTemplateRequestParams(),
                LayoutPage = layoutPage
            };
            try
            {
                await result.WriteToAsync(httpRes.OutputStream);
            }
            catch (Exception ex)
            {
                await page.Format.OnViewException(result, httpReq, ex);
            }
        }
    }

    public class TemplateCodePageHandler : HttpAsyncTaskHandler
    {
        private readonly TemplateCodePage page;
        private readonly TemplatePage layoutPage;

        public TemplateCodePageHandler(TemplateCodePage page, TemplatePage layoutPage = null)
        {
            this.page = page;
            this.layoutPage = layoutPage;
        }

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            var requiresRequest = page as IRequiresRequest;
            if (requiresRequest != null)
                requiresRequest.Request = httpReq;

            var result = new PageResult(page)
            {
                Args = httpReq.GetTemplateRequestParams(),
                LayoutPage = layoutPage
            };

            try
            {
                await result.WriteToAsync(httpRes.OutputStream);
            }
            catch (Exception ex)
            {
                await page.Format.OnViewException(result, httpReq, ex);
            }
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

    public abstract class ServiceStackCodePage : TemplateCodePage, IRequiresRequest
    {
        public IRequest Request { get; set; }

        public virtual IResolver GetResolver() => Service.GlobalResolver;

        public virtual T TryResolve<T>()
        {
            return this.GetResolver() == null
                ? default(T)
                : this.GetResolver().TryResolve<T>();
        }

        public virtual T ResolveService<T>()
        {
            var service = TryResolve<T>();
            return HostContext.ResolveService(this.Request, service);
        }

        protected virtual IResponse Response => Request?.Response;

        private ICacheClient cache;
        public virtual ICacheClient Cache => cache ?? (cache = HostContext.AppHost.GetCacheClient(Request));

        private MemoryCacheClient localCache;

        public virtual MemoryCacheClient LocalCache =>
            localCache ?? (localCache = HostContext.AppHost.GetMemoryCacheClient(Request));

        private IDbConnection db;
        public virtual IDbConnection Db => db ?? (db = HostContext.AppHost.GetDbConnection(Request));

        private IRedisClient redis;
        public virtual IRedisClient Redis => redis ?? (redis = HostContext.AppHost.GetRedisClient(Request));

        private IMessageProducer messageProducer;

        public virtual IMessageProducer MessageProducer =>
            messageProducer ?? (messageProducer = HostContext.AppHost.GetMessageProducer(Request));

        private ISessionFactory sessionFactory;

        public virtual ISessionFactory SessionFactory =>
            sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache);

        private IAuthRepository authRepository;

        public virtual IAuthRepository AuthRepository =>
            authRepository ?? (authRepository = HostContext.AppHost.GetAuthRepository(Request));

        private IServiceGateway gateway;
        public virtual IServiceGateway Gateway => gateway ?? (gateway = HostContext.AppHost.GetServiceGateway(Request));

        public IVirtualPathProvider VirtualFileSources => HostContext.VirtualFileSources;

        public IVirtualFiles VirtualFiles => HostContext.VirtualFiles;

        private ISession session;

        public virtual ISession SessionBag => session ?? (session = TryResolve<ISession>() //Easier to mock
            ?? SessionFactory.GetOrCreateSession(Request, Response));

        public virtual IAuthSession GetSession(bool reload = false)
        {
            var req = this.Request;
            if (req.GetSessionId() == null)
                req.Response.CreateSessionIds(req);
            return req.GetSession(reload);
        }

        protected virtual TUserSession SessionAs<TUserSession>()
        {
            if (HostContext.TestMode)
            {
                var mockSession = TryResolve<TUserSession>();
                if (Equals(mockSession, default(TUserSession)))
                    mockSession = TryResolve<IAuthSession>() is TUserSession
                        ? (TUserSession) TryResolve<IAuthSession>()
                        : default(TUserSession);

                if (!Equals(mockSession, default(TUserSession)))
                    return mockSession;
            }

            return SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
        }

        public virtual bool IsAuthenticated => this.GetSession().IsAuthenticated;

        public virtual void PublishMessage<T>(T message)
        {
            if (MessageProducer == null)
                throw new NullReferenceException("No IMessageFactory was registered, cannot PublishMessage");

            MessageProducer.Publish(message);
        }

        public override void Dispose()
        {
            db?.Dispose();
            redis?.Dispose();
            messageProducer?.Dispose();
            using (authRepository as IDisposable) {}
        }
    }

    public static class TemplatePagesFeatureExtensions
    {
        public static Dictionary<string, object> GetTemplateRequestParams(this IRequest request)
        {
            var reqParams = request.GetRequestParams();
            reqParams["RawUrl"] = request.RawUrl;
            reqParams[TemplateConstants.PathInfo] = request.OriginalPathInfo;
            reqParams["AbsoluteUri"] = request.AbsoluteUri;
            reqParams["Verb"] = reqParams["Method"] = request.Verb;

            var to = reqParams.ToObjectDictionary();
            to[TemplateConstants.Request] = request;
            return to;
        }
        
        public static TemplateCodePage GetCodePage(this IRequest request, string virtualPath)
        {
            return HostContext.AssertPlugin<TemplatePagesFeature>().GetCodePage(virtualPath).With(request);
        }
        
        public static TemplatePage GetPage(this IRequest request, string virtualPath)
        {
            return HostContext.AssertPlugin<TemplatePagesFeature>().GetPage(virtualPath);
        }
        
        public static TemplatePage OneTimePage(this IRequest request, string contents, string ext=null)
        {
            return HostContext.AssertPlugin<TemplatePagesFeature>().OneTimePage(contents, ext);
        }
        
        public static TemplateCodePage With(this TemplateCodePage page, IRequest request)
        {
            if (page is IRequiresRequest requiresRequest)
                requiresRequest.Request = request;
            return page;
        }
    }

    public interface IAutoQueryDbFilters
    {
        object sendToAutoQuery(TemplateScopeContext scope, object dto, string requestName, object options);
    }
}