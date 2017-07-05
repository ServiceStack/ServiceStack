using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Threading.Tasks;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

#if NETSTANDARD1_6
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack
{
    public class ServerHtmlFeature : IPlugin
    {
        public string PageExtension { get; set; } = "html";

        public string LayoutName { get; set; } = "layout";
        
        public string DefaultLayout { get; set; } = "_layout.html";
        
        public bool CheckModifiedPages { get; set; }

        public ServerHtmlPages HtmlPages { get; set; }

        public void Register(IAppHost appHost)
        {
            DefaultLayout = $"_{LayoutName}.{PageExtension}";
            
            HtmlPages = new ServerHtmlPages(appHost, this);
            appHost.Register<IHtmlPages>(HtmlPages);
            appHost.CatchAllHandlers.Add(HtmlPages.RequestHandler);
        }
    }

    public class ServerHtmlPages : IHtmlPages
    {
        public static string Layout = "layout";
        
        private readonly IAppHost appHost;
        private readonly ServerHtmlFeature feature;

        public ServerHtmlPages(IAppHost appHost, ServerHtmlFeature feature)
        {
            this.appHost = appHost;
            this.feature = feature;
        }

        static readonly ConcurrentDictionary<string, byte> catchAllPathsNotFound = new ConcurrentDictionary<string, byte>();
        
        static readonly ConcurrentDictionary<string, ServerHtmlPage> pageMap = new ConcurrentDictionary<string, ServerHtmlPage>(); 

        public virtual IHttpHandler RequestHandler(string httpMethod, string pathInfo, string filePath)
        {
            if (catchAllPathsNotFound.ContainsKey(pathInfo))
                return null;

            var page = feature.HtmlPages.GetPage(pathInfo);

            if (page != null)
                return new ServerHtmlHandler(page);

            if (catchAllPathsNotFound.Count > 10000) //prevent DDOS
                catchAllPathsNotFound.Clear();

            catchAllPathsNotFound[pathInfo] = 1;
            return null;
        }

        public ServerHtmlPage ResolveLayoutPage(ServerHtmlPage page)
        {
            if (!page.HasInit)
                throw new ArgumentException($"Page {page.File.VirtualPath} has not been initialized");

            var layoutWithoutExt = (page.Layout ?? feature.DefaultLayout).LeftPart('.');

            var dir = page.File.Directory;
            do
            {
                var layoutPath = dir.VirtualPath.CombineWith(layoutWithoutExt);

                if (pageMap.TryGetValue(layoutPath, out ServerHtmlPage layoutPage))
                    return layoutPage;
                
                var layoutFile = dir.GetFile($"{layoutWithoutExt}.{feature.PageExtension}");
                if (layoutFile != null)
                    return pageMap[layoutPath] = new ServerHtmlPage(layoutFile);

                dir = dir.ParentDirectory;

            } while (!dir.IsRoot);
            
            return null;
        }

        public ServerHtmlPage GetPage(string path)
        {
            var santizePath = path.Replace('\\','/').TrimPrefixes("/");

            if (pageMap.TryGetValue(santizePath, out ServerHtmlPage page))
                return page;
            
            var file = appHost.VirtualFileSources.GetFile(santizePath);
            if (file != null)
                return pageMap[file.VirtualPath.WithoutExtension()] = new ServerHtmlPage(file);

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
        public bool IsCompleteHtml { get; set; }
        public bool HasInit { get; private set; }
        private readonly object semaphore = new object();

        public ServerHtmlPage(IVirtualFile file)
        {
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

            var feature = HostContext.GetPlugin<ServerHtmlFeature>();

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

                IsCompleteHtml = PageHtml.StartsWith("<!DOCTYPE HTML>")
                     || PageHtml.StartsWithIgnoreCase("<html");

                HasInit = true;

                if (!IsCompleteHtml)
                {
                    if (PageVars.TryGetValue(ServerHtmlPages.Layout, out string layout))
                        Layout = layout;

                    LayoutPage = feature.HtmlPages.ResolveLayoutPage(this);
                }
            }

            if (LayoutPage != null)
            {
                if (!LayoutPage.HasInit)
                {
                    await LayoutPage.Load();
                }
                else if (feature.CheckModifiedPages || HostContext.DebugMode)
                {
                    LayoutPage.File.Refresh();
                    if (LayoutPage.File.LastModified > LayoutPage.LastModified)
                        await LayoutPage.Load();
                }
            }

            return this;
        }
    }

    public static class ServerHtmlUtils
    {
        static char[] VarDelimiters = { '|', '}' };

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
    }
}