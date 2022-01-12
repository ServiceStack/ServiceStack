#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.HtmlModules;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Simple, lightweight & high-performant HTML templating solution 
/// </summary>
public class HtmlModulesFeature : IPlugin, Model.IHasStringId
{
    public string Id => "module:" + string.Join(",", Modules.Select(x => x.BasePath).ToArray());
    
    public bool IgnoreIfError { get; set; }

    /// <summary>
    /// Define literal tokens to be replaced with dynamic fragments, e.g:
    /// &lt;base href=""&gt; = ctx => $"&lt;base href=\"{ctx.Request.ResolveAbsoluteUrl($"~{DirPath}/")}\"&gt;"
    /// </summary>
    public Dictionary<string, Func<HtmlModuleContext, ReadOnlyMemory<byte>>> Tokens { get; set; } = new();

    /// <summary>
    /// Define custom html handlers, e.g:
    /// &lt;!--shared:Brand,Input--&gt;
    /// &lt;!--file:/path/to/single.html--&gt; or /*file:/path/to/single.txt*/
    /// &lt;!--files:/dir/components/*.html--&gt; or /*files:/dir/*.css*/
    /// &lt;!---: remove html comment --&gt; or /**: remove JS comment */
    /// </summary>
    public List<IHtmlModulesHandler> Handlers { get; set; } = new()
    {
        new FileHandler("file"),
        new FilesHandler("files"),
        new RemoveJsLineComments(),
        new RemoveHtmlLineComments(),
    };

    /// <summary>
    /// File Transformer to use when reading files
    /// </summary>
    public Func<IVirtualFile, string>? FileContentsResolver { get; set; }
    
    public List<HtmlModule> Modules { get; set; }
    public HtmlModulesFeature(params HtmlModule[] modules) => Modules = modules.ToList();
    public Action<IAppHost, HtmlModule>? Configure { get; set; }
    public IVirtualPathProvider? VirtualFiles { get; set; }

    /// <summary>
    /// File Transformer options
    ///  - defaults to FilesTransformer.Default
    ///  - disable with FileTransformer.None
    /// </summary>
    public FilesTransformer? FilesTransformer { get; set; }
    
    public void Register(IAppHost appHost)
    {
        FilesTransformer ??= FilesTransformer.Default;
        FileContentsResolver ??= FilesTransformer.ReadAll;
        VirtualFiles ??= appHost.VirtualFiles;
        foreach (var component in Modules)
        {
            component.Feature = this;
            component.VirtualFiles ??= VirtualFiles;
            component.FileContentsResolver ??= FileContentsResolver;
            Configure?.Invoke(appHost, component);
            component.Register(appHost);
        }
    }
}

public class HtmlModuleContext
{
    public HtmlModule Module { get; }
    public IRequest Request { get; }
    public IVirtualPathProvider VirtualFiles => Module.VirtualFiles!;

    public IVirtualFile AssertFile(string virtualPath)
    {
        var file = VirtualFiles.GetFile(virtualPath);
        if (file == null)
            throw HttpError.NotFound($"{virtualPath} does not exist");
        return file;
    }

    public Func<IVirtualFile, string> FileContentsResolver => Module.FileContentsResolver != null
        ? Module.FileContentsResolver!
        : file => file.ReadAllText();

    public HtmlModuleContext(HtmlModule module, IRequest request)
    {
        Module = module;
        Request = request;
    }

    public ReadOnlyMemory<byte> Cache(string key, Func<string, ReadOnlyMemory<byte>> handler)
    {
        if (!HostContext.DebugMode)
            return Module.Cache.GetOrAdd(key, handler);
        
        return handler(key);
    }
}

public class HtmlModule
{
    public HtmlModulesFeature? Feature { get; set; }
    public string DirPath { get; set; }
    public string BasePath { get; set; }
    public IVirtualPathProvider? VirtualFiles { get; set; }

    public string IndexFile { get; set; } = "index.html";
    public List<string> PublicPaths { get; set; } = new() {
        "/assets"
    };

    public Dictionary<string, Func<HtmlModuleContext, ReadOnlyMemory<byte>>> Tokens { get; set; }
    public List<IHtmlModulesHandler> Handlers { get; set; } = new();

    /// <summary>
    /// File resolver to use to read file contents
    /// </summary>
    public Func<IVirtualFile, string>? FileContentsResolver { get; set; }

    public HtmlModule(string dirPath, string? basePath=null)
    {
        DirPath = dirPath.TrimEnd('/');
        BasePath = (basePath ?? DirPath).TrimEnd('/');
        Tokens = new() {
            ["<base href=\"\">"] = ctx => ($"<base href=\"{ctx.Request.ResolveAbsoluteUrl($"~{BasePath}/")}\">"
            + (HostContext.DebugMode ? "\n<script src=\"/js/hot-fileloader.js\"></script>" : "")).AsMemory().ToUtf8(),
        };
    }
    
    public ConcurrentDictionary<string, ReadOnlyMemory<byte>> Cache { get; } = new();

    struct FragmentTuple
    {
        internal int index;
        internal string token;
        internal IHtmlModuleFragment fragment;
        public FragmentTuple(int index, string token, IHtmlModuleFragment fragment)
        {
            this.index = index;
            this.token = token;
            this.fragment = fragment;
        }
    };

    IHtmlModuleFragment[]? indexFragments;
    IHtmlModuleFragment[] GetIndexFragments()
    {
        if (!HostContext.DebugMode && indexFragments != null)
            return indexFragments;

        var indexFile = VirtualFiles!.GetFile(DirPath.CombineWith(IndexFile));
        if (indexFile == null)
        {
            if (Feature!.IgnoreIfError) 
                return TypeConstants<IHtmlModuleFragment>.EmptyArray;
            throw HttpError.NotFound(DirPath.CombineWith(IndexFile) + " was not found");
        }
        var indexContents = indexFile.ReadAllText().AsMemory();
        
        var fragmentDefs = new List<FragmentTuple>();
        var tokenPos = 0;
        foreach (var entry in Tokens.Union(Feature?.Tokens ?? new()))
        {
            do
            {
                tokenPos = indexContents.IndexOf(entry.Key, tokenPos);
                if (tokenPos == -1) continue;
                fragmentDefs.Add(new(tokenPos, entry.Key, new HtmlTokenFragment(entry.Key, entry.Value)));
                tokenPos += entry.Key.Length;
            } while (tokenPos >= 0);
        }
        foreach (var handler in Handlers.Union(Feature?.Handlers ?? new()))
        {
            var htmlCommentPrefix = "<!--" + handler.Name + ":";
            var jsCommentPrefix = "/*" + handler.Name + ":";
            int htmlPos = 0;
            int jsPos = 0;
            do
            {
                if (htmlPos != -1)
                {
                    htmlPos = indexContents.IndexOf(htmlCommentPrefix, htmlPos);
                    if (htmlPos >= 0)
                    {
                        var endPos = indexContents.IndexOf("-->", htmlPos);
                        if (endPos == -1)
                            throw new Exception($"{htmlCommentPrefix} is missing -->");
                        var token = indexContents.Slice(htmlPos, (endPos - htmlPos) + "-->".Length).ToString();
                        var args = token.Substring(htmlCommentPrefix.Length, token.Length - htmlCommentPrefix.Length - "-->".Length).Trim();
                        fragmentDefs.Add(new(htmlPos, token, new HtmlHandlerFragment(token, args, handler.Execute)));
                        htmlPos = endPos;
                    }
                }

                if (jsPos != -1)
                {
                    jsPos = indexContents.IndexOf(jsCommentPrefix, jsPos);
                    if (jsPos >= 0)
                    {
                        var endPos = indexContents.IndexOf("*/", jsPos);
                        if (endPos == -1)
                            throw new Exception($"{jsCommentPrefix} is missing */");
                        var token = indexContents.Slice(jsPos, endPos - jsPos + "*/".Length).ToString();
                        var args = token.Substring(jsCommentPrefix.Length, token.Length - jsCommentPrefix.Length - "*/".Length).Trim();
                        fragmentDefs.Add(new(jsPos, token, new HtmlHandlerFragment(token, args, handler.Execute)));
                        jsPos = endPos;
                    }
                }
            } while (htmlPos >= 0 || jsPos >= 0);
        }
        fragmentDefs.Sort((a, b) => a.index.CompareTo(b.index));

        var fragments = new List<IHtmlModuleFragment>();
        var lastPos = 0;
        for (var i = 0; i < fragmentDefs.Count; i++)
        {
            var fragmentDef = fragmentDefs[i];
            var startPos = indexContents.IndexOf(fragmentDef.token);
            if (startPos == -1)
                throw new Exception($"Error parsing {IndexFile}, missing '{fragmentDef.token}'");
            
            fragments.Add(new HtmlTextFragment(indexContents.Slice(lastPos, startPos - lastPos)));
            fragments.Add(fragmentDef.fragment);
            
            lastPos = startPos + fragmentDef.token.Length;
        }
        fragments.Add(new HtmlTextFragment(indexContents.Slice(lastPos)));

        indexFragments = fragments.ToArray();
        return indexFragments;
    }

    public void Register(IAppHost appHost)
    {
        VirtualFiles ??= appHost.VirtualFiles;
        var fragments = GetIndexFragments(); //force parsing
        if (fragments.Length == 0) //Feature.IgnoreIfError
            return;

        appHost.RawHttpHandlers.Add(req =>
        {
            if (!req.PathInfo.StartsWith(BasePath))
                return null;
            
            foreach (var path in PublicPaths)
            {
                if (req.PathInfo.StartsWith(BasePath + path))
                {
                    var file = VirtualFiles.GetFile(DirPath + req.PathInfo.Substring(BasePath.Length));
                    return file != null
                        ? new StaticFileHandler(file)
                        : new NotFoundHttpHandler();
                }
            }

            return new CustomActionHandlerAsync(async (httpReq, httpRes) =>
            {
                try
                {
                    var fragments = GetIndexFragments();
                    var ms = MemoryStreamFactory.GetStream();
                    var ctx = new HtmlModuleContext(this, httpReq);
                    foreach (var fragment in fragments)
                    {
                        await fragment.WriteToAsync(ctx, ms).ConfigAwait();
                    }
                    httpRes.ContentType = MimeTypes.Html;
                    ms.Position = 0;
                    await ms.CopyToAsync(httpRes.OutputStream).ConfigAwait();
                }
                catch (Exception ex)
                {
                    await httpRes.WriteError(ex).ConfigAwait();
                }
            });
        });
    }
}