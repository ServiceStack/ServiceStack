using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

#if NETSTANDARD1_6
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack
{
    public class ServerHtmlFeature : ServerHtmlContext, IPlugin
    {
        public void Register(IAppHost appHost)
        {
            VirtualFileSources = appHost.VirtualFileSources;
            appHost.Register<IHtmlPages>(HtmlPages);
            appHost.CatchAllHandlers.Add(RequestHandler);
        }

        static readonly ConcurrentDictionary<string, byte> catchAllPathsNotFound = new ConcurrentDictionary<string, byte>();

        protected virtual IHttpHandler RequestHandler(string httpMethod, string pathInfo, string filePath)
        {
            if (catchAllPathsNotFound.ContainsKey(pathInfo))
                return null;

            var page = HtmlPages.GetOrCreatePage(pathInfo);

            if (page != null)
            {
                if (page.File.Name.StartsWith("_"))
                    return new ForbiddenHttpHandler();
                
                return new ServerHtmlHandler(page);
            }
            
            if (!pathInfo.EndsWith("/") && VirtualFileSources.DirectoryExists(pathInfo.TrimPrefixes("/")))
                return new RedirectHttpHandler { RelativeUrl = pathInfo + "/", StatusCode = HttpStatusCode.MovedPermanently };

            if (catchAllPathsNotFound.Count > ServerHtmlContext.PreventDosMaxSize)
                catchAllPathsNotFound.Clear();

            catchAllPathsNotFound[pathInfo] = 1;
            return null;
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
    
    public class ServerHtmlContext
    {
        public static int PreventDosMaxSize = 10000; 
        
        public string PageExtension { get; set; } = "html";
        
        public string IndexPage { get; set; } = "index";

        public string DefaultLayoutPage { get; set; } = "_layout";
        
        public string LayoutVarName { get; set; } = "layout";

        public bool CheckModifiedPages { get; set; } = false;

        public IHtmlPages HtmlPages { get; set; }

        public IVirtualPathProvider VirtualFileSources { get; set; }

        public Func<object, string> EncodeValue { get; set; } = ServerHtmlUtils.HtmlEncodeValue;

        public Func<StringSegment, bool> IsCompletePage = page => 
            page.StartsWith("<!DOCTYPE HTML>") || page.StartsWithIgnoreCase("<html");
        
        public ServerHtmlContext()
        {
            HtmlPages = new ServerHtmlPages(this);
        }
    }
    
    public interface IHtmlPages
    {
        ServerHtmlPage ResolveLayoutPage(ServerHtmlPage page);
        ServerHtmlPage AddPage(string virtualPath, IVirtualFile file);
        ServerHtmlPage GetPage(string virtualPath);
        ServerHtmlPage GetOrCreatePage(string virtualPath);
    }
    
    public class ServerHtmlPages : IHtmlPages
    {
        public ServerHtmlContext Context { get; }

        public ServerHtmlPages(ServerHtmlContext context) => this.Context = context;

        public static string Layout = "layout";
        
        static readonly ConcurrentDictionary<string, ServerHtmlPage> pageMap = new ConcurrentDictionary<string, ServerHtmlPage>(); 

        public virtual ServerHtmlPage ResolveLayoutPage(ServerHtmlPage page)
        {
            if (!page.HasInit)
                throw new ArgumentException($"Page {page.File.VirtualPath} has not been initialized");

            var layoutWithoutExt = (page.Layout ?? Context.DefaultLayoutPage).LeftPart('.');

            var dir = page.File.Directory;
            do
            {
                var layoutPath = dir.VirtualPath.CombineWith(layoutWithoutExt);

                if (pageMap.TryGetValue(layoutPath, out ServerHtmlPage layoutPage))
                    return layoutPage;
                
                var layoutFile = dir.GetFile($"{layoutWithoutExt}.{Context.PageExtension}");
                if (layoutFile != null)
                    return AddPage(layoutPath, layoutFile);

                dir = dir.ParentDirectory;

            } while (!dir.IsRoot);
            
            return null;
        }

        public virtual ServerHtmlPage AddPage(string virtualPath, IVirtualFile file)
        {
            return pageMap[virtualPath] = new ServerHtmlPage(Context, file);
        }

        public virtual ServerHtmlPage GetPage(string path)
        {
            var santizePath = path.Replace('\\','/').TrimPrefixes("/").LastLeftPart('.');

            return pageMap.TryGetValue(santizePath, out ServerHtmlPage page) 
                ? page 
                : null;
        }

        public virtual ServerHtmlPage GetOrCreatePage(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            
            var santizePath = path.Replace('\\','/').TrimPrefixes("/").LastLeftPart('.');

            var page = GetPage(santizePath);
            if (page != null)
                return page;
            
            var file = !santizePath.EndsWith("/")
                ? Context.VirtualFileSources.GetFile($"{santizePath}.{Context.PageExtension}")
                : Context.VirtualFileSources.GetFile($"{santizePath}{Context.IndexPage}.{Context.PageExtension}");
            if (file != null)
                return AddPage(file.VirtualPath.WithoutExtension(), file);

            return null; 
        }
    }

    public class ServerHtmlPage
    {
        public IVirtualFile File { get; set; }
        public StringSegment ServerHtml { get; set; }
        public StringSegment PageHtml { get; set; }
        public Dictionary<string, string> PageVars { get; set; }
        public string Layout { get; set; }
        public ServerHtmlPage LayoutPage { get; set; }
        public List<ServerHtmlFragment> PageFragments { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsCompletePage { get; set; }
        public bool HasInit { get; private set; }

        public ServerHtmlContext Context { get; }
        private readonly object semaphore = new object();

        public ServerHtmlPage(ServerHtmlContext feature, IVirtualFile file)
        {
            this.Context = feature ?? throw new ArgumentNullException(nameof(feature));
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public async Task<ServerHtmlPage> Init()
        {
            return HasInit
                ? this
                : await Load();
        }
        
        public async Task<ServerHtmlPage> Load()
        {
            string serverHtml;
            using (var stream = File.OpenRead())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                serverHtml = await reader.ReadToEndAsync();
            }

            lock (semaphore)
            {
                LastModified = File.LastModified;

                ServerHtml = serverHtml.ToStringSegment();
                PageVars = new Dictionary<string, string>();

                var pos = 0;
                while (char.IsWhiteSpace(ServerHtml.GetChar(pos)))
                    pos++;

                ServerHtml.TryReadLine(out StringSegment line, ref pos);
                if (line.StartsWith("<!--"))
                {
                    while (ServerHtml.TryReadLine(out line, ref pos))
                    {
                        if (line.Trim().Length == 0)
                            continue;


                        if (line.StartsWith("-->"))
                            break;

                        var kvp = line.SplitOnFirst(':');
                        PageVars[kvp[0].Trim().ToString()] = kvp.Length > 1 ? kvp[1].Trim().ToString() : "";
                    }
                }
                else
                {
                    pos = 0;
                }

                PageHtml = ServerHtml.Subsegment(pos).TrimStart();
                PageFragments = ServerHtmlUtils.ParseServerHtml(PageHtml);
                IsCompletePage = Context.IsCompletePage(PageHtml);                

                HasInit = true;

                if (!IsCompletePage)
                {
                    if (PageVars.TryGetValue(ServerHtmlPages.Layout, out string layout))
                        Layout = layout;

                    LayoutPage = Context.HtmlPages.ResolveLayoutPage(this);
                }
            }

            if (LayoutPage != null)
            {
                if (!LayoutPage.HasInit)
                {
                    await LayoutPage.Load();
                }
                else if (Context.CheckModifiedPages || HostContext.DebugMode)
                {
                    LayoutPage.File.Refresh();
                    if (LayoutPage.File.LastModified > LayoutPage.LastModified)
                        await LayoutPage.Load();
                }
            }

            return this;
        }

        public object GetValue(ServerHtmlVariableFragment var)
        {
            return PageVars.TryGetValue(var.NameString, out string value)
                ? value
                : LayoutPage?.GetValue(var);
        }

        public string GetEncodedValue(ServerHtmlVariableFragment var)
        {
            return Context.EncodeValue(GetValue(var));
        }
    }

    public static class ServerHtmlUtils
    {
        static readonly char[] VarDelimiters = { '|', '}' };

        public static List<ServerHtmlFragment> ParseServerHtml(string htmlString)
        {
            return ParseServerHtml(new StringSegment(htmlString));
        }

        public static List<ServerHtmlFragment> ParseServerHtml(StringSegment html)
        {
            var to = new List<ServerHtmlFragment>();

            if (html.IsNullOrWhiteSpace())
                return to;
            
            int pos;
            var lastPos = 0;
            while ((pos = html.IndexOf("{{", lastPos)) != -1)
            {
                var block = html.Subsegment(lastPos, pos - lastPos);
                to.Add(new ServerHtmlStringFragment(block));

                var varStartPos = pos + 2;
                var varEndPos = html.IndexOfAny(VarDelimiters, varStartPos);
                var varName = html.Subsegment(varStartPos, varEndPos - varStartPos).Trim();
                if (varEndPos == -1)
                    throw new ArgumentException($"Invalid Server HTML Template at '{html.SafeSubsegment(50)}...'", nameof(html));

                List<Command> filterCommands = null;
                
                var isFilter = html.GetChar(varEndPos) == '|';
                if (isFilter)
                {
                    filterCommands = html.Subsegment(varEndPos + 1).ParseCommands(
                        separator: '|',
                        atEndIndex: (str, strPos) =>
                        {
                            while (str.Length > strPos && char.IsWhiteSpace(str.GetChar(strPos)))
                                strPos++;

                            if (str.Length > strPos + 1 && str.GetChar(strPos) == '}' && str.GetChar(strPos + 1) == '}')
                            {
                                varEndPos = varEndPos + 1 + strPos + 1;
                                return strPos;
                            }
                            return null;
                        });
                }
                else
                {
                    varEndPos += 1;
                }

                lastPos = varEndPos + 1;
                var originalText = html.Subsegment(pos, lastPos - pos);

                to.Add(new ServerHtmlVariableFragment(originalText, varName, filterCommands));
            }

            if (lastPos != html.Length - 1)
            {
                var lastBlock = lastPos == 0 ? html : html.Subsegment(lastPos);
                to.Add(new ServerHtmlStringFragment(lastBlock));
            }

            return to;
        }
        
        public static string HtmlEncodeValue(object value)
        {
            if (value == null)
                return string.Empty;
            
            if (value is IRawString rawString)
                return rawString.ToRawString();
            
            if (value is IHtmlString htmlString)
                return htmlString.ToHtmlString();

            var str = value.ToString();
            if (str == string.Empty)
                return string.Empty;

            return PclExportClient.Instance.HtmlEncode(str);
        }
    }
}