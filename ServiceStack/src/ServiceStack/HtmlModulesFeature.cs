#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.HtmlModules;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

#if NET8_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
#endif

namespace ServiceStack;

/// <summary>
/// Simple, lightweight and high-performant HTML templating solution 
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
    /// &lt;!--shared:custom-meta--&gt;
    /// &lt;!--file:/path/to/single.html--&gt; or /*file:/path/to/single.txt*/
    /// &lt;!--files:/dir/components/*.html--&gt; or /*files:/dir/*.css*/
    /// </summary>
    public List<IHtmlModulesHandler> Handlers { get; set; } = new()
    {
        new FileHandler("file"),
        new FilesHandler("files"),
        new FilesHandler("module")
        {
            Header = FilesTransformer.ModuleHeader,
            Footer = FilesTransformer.ModuleFooter,
        },
        new FileHandler("vfs") { VirtualFilesResolver = ctx => HostContext.VirtualFiles },
        new FileHandler("vfs[]") { VirtualFilesResolver = ctx => HostContext.VirtualFileSources },
        new GatewayHandler("gateway"),
    };

    /// <summary>
    /// File Transformer to use when reading files
    /// </summary>
    public Func<IVirtualFile, string>? FileContentsResolver { get; set; }
    
    public List<HtmlModule> Modules { get; set; }
    public HtmlModulesFeature(params HtmlModule[] modules) => Modules = modules.ToList();
    public List<Action<IAppHost, HtmlModule>> OnConfigure { get; set; } = new();
    public IVirtualPathProvider? VirtualFiles { get; set; }

    /// <summary>
    /// File Transformer options
    ///  - defaults to FilesTransformer.Default
    ///  - disable with FileTransformer.None
    /// </summary>
    public FilesTransformer? FilesTransformer { get; set; }

    /// <summary>
    /// Whether to enable ETag HTTP Caching when not in DebugMode
    /// </summary>
    public bool? EnableHttpCaching { get; set; }

    /// <summary>
    /// Whether to enable cached compressed responses
    /// </summary>
    public bool? EnableCompression { get; set; }

    /// <summary>
    /// The HTTP CacheControl Header to use (default: public, max-age=3600, must-revalidate)
    /// </summary>
    public string? CacheControl { get; set; }

    public const string DefaultCacheControl = "public, max-age=3600, must-revalidate";

    /// <summary>
    /// Whether to include FilesTransformer["html"].LineTransformers in main index.html
    /// </summary>
    public bool IncludeHtmlLineTransformers { get; set; } = true;

    public HtmlModulesFeature Configure(Action<IAppHost, HtmlModule> configure)
    {
        OnConfigure.Add(configure);
        return this;
    }

    public void Register(IAppHost appHost)
    {
        var debugMode = appHost.Config.DebugMode;
        EnableHttpCaching ??= !debugMode;
        EnableCompression ??= !debugMode;
        FilesTransformer ??= FilesTransformer.Defaults(debugMode);
        
        FileContentsResolver ??= FilesTransformer.ReadAll;
        VirtualFiles ??= appHost.VirtualFiles;
        foreach (var component in Modules)
        {
            if (IncludeHtmlLineTransformers && FilesTransformer.FileExtensions.TryGetValue("html", out var ext))
                component.LineTransformers.AddRange(ext.LineTransformers);

            component.EnableHttpCaching ??= EnableHttpCaching;
            component.EnableCompression ??= EnableCompression;
            component.CacheControl ??= CacheControl;
            component.Feature = this;
            component.VirtualFiles ??= VirtualFiles;
            component.FileContentsResolver ??= FileContentsResolver;
            foreach (var configure in component.OnConfigure)
            {
                configure(appHost, component);
            }
            foreach (var configure in OnConfigure)
            {
                configure(appHost, component);
            }
            component.Register(appHost);
        }
    }

    /// <summary>
    /// Flush HtmlModules cache so it's output is recreated on next request
    /// </summary>
    public void Flush()
    {
        foreach (var component in Modules)
        {
            component.Flush();
        }
    }
}

public class HtmlModuleContext
{
    public HtmlModule Module { get; }
    public IRequest Request { get; }
    public IVirtualPathProvider VirtualFiles => Module.VirtualFiles!;
    public bool DebugMode => HostContext.DebugMode;
    public ServiceStackHost AppHost => HostContext.AppHost;

    /// <summary>
    /// Resolve file from the Module configured VirtualFiles
    /// </summary>
    public IVirtualFile AssertFile(string virtualPath) => AssertFile(VirtualFiles, virtualPath);

    public IVirtualFile AssertFile(IVirtualPathProvider vfs, string virtualPath) =>
        AppHost.VirtualFileSources.GetMemoryVirtualFiles().GetFile(virtualPath)
        ?? vfs.GetFile(virtualPath)
        ?? throw HttpError.NotFound($"{virtualPath} does not exist");

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
    public bool? EnableHttpCaching { get; set; }
    public bool? EnableCompression { get; set; }
    public string? CacheControl { get; set; }
    public HtmlModulesFeature? Feature { get; set; }
    public string DirPath { get; set; }
    public string BasePath { get; set; }
    public IVirtualPathProvider? VirtualFiles { get; set; }

    public string IndexFile { get; set; } = "index.html";
    public List<string> PublicPaths { get; set; } = new() {
        "/assets",
        "/lib",
    };

    public List<string> DynamicPageQueryStrings { get; set; } = new();

    public Dictionary<string, Func<HtmlModuleContext, ReadOnlyMemory<byte>>> Tokens { get; set; }
    public List<IHtmlModulesHandler> Handlers { get; set; } = new();

    public List<HtmlModuleLine> LineTransformers { get; set; } = new();

    /// <summary>
    /// File resolver to use to read file contents
    /// </summary>
    public Func<IVirtualFile, string>? FileContentsResolver { get; set; }
    
    public List<Action<IAppHost, HtmlModule>> OnConfigure { get; set; } = new();

    public HtmlModule(string dirPath, string? basePath=null)
    {
        DirPath = dirPath.TrimEnd('/');
        BasePath = (basePath ?? DirPath).TrimEnd('/');
        Tokens = new() {
            ["<base href=\"\">"] = ctx => ($"<base href=\"{ctx.Request.ResolveAbsoluteUrl($"~{BasePath}/")}\">\n"
            + (ctx.DebugMode && ctx.AppHost.HasPlugin<HotReloadFeature>() ? "<script>\n" 
                    + ctx.AssertFile(ctx.AppHost.VirtualFileSources,"/js/hot-fileloader.js").ReadAllText() 
                + "\n</script>\n" : ""))
                .AsMemory().ToUtf8(),
            ["vfx=hash"] = _ => $"vfx={Env.ServiceStackVersion}".AsMemory().ToUtf8()
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

        var indexContentsString = indexFile.ReadAllText();
        var indexContents = indexContentsString.AsMemory();
        
        var fragmentDefs = new List<FragmentTuple>();
        foreach (var entry in Tokens.Union(Feature?.Tokens ?? new()))
        {
            var tokenPos = 0;
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
            var startPos = indexContents.IndexOf(fragmentDef.token, lastPos);
            if (startPos == -1)
                throw new Exception($"Error parsing {IndexFile}, missing '{fragmentDef.token}'");

            fragments.Add(new HtmlTextFragment(TransformContent(indexContents.Slice(lastPos, startPos - lastPos))));
            fragments.Add(fragmentDef.fragment);

            lastPos = startPos + fragmentDef.token.Length;
        }
        fragments.Add(new HtmlTextFragment(TransformContent(indexContents.Slice(lastPos))));

        indexFragments = fragments.ToArray();
        
        return indexFragments;
    }

    public ReadOnlyMemory<char> TransformContent(ReadOnlyMemory<char> content)
    {
        if (content.Length == 0 || LineTransformers.Count == 0)
            return content;

        int startIndex = 0;
        var sb = StringBuilderCache.Allocate();
        while (content.TryReadLine(out var line, ref startIndex))
        {
            foreach (var lineTransformer in LineTransformers)
            {
                line = lineTransformer.Transform(line);
                if (line.Length == 0)
                    break;
            }
            if (line.Length > 0)
            {
                sb.AppendLine(line);
            }
        }
        // Trim last new line to remove new lines between tokens & text fragments 
        if (sb.Length > 2)
        {
            if (sb[sb.Length - 1] == '\n') sb.Length -= 1;
            if (sb[sb.Length - 1] == '\r') sb.Length -= 1;
        }
        return StringBuilderCache.ReturnAndFree(sb).AsMemory();
    }

    private string? indexFileETag = null;

    private byte[]? cachedBytes; 
    private ConcurrentDictionary<string, byte[]> zipCache = new();

    public void Flush()
    {
        zipCache.Clear();
        indexFragments = null;
    }

    public Func<IHttpRequest, HttpAsyncTaskHandler?> GetHandler(IAppHost appHost)
    {
        return req =>
        {
            if (!req.PathInfo.StartsWith(BasePath))
                return null;

            foreach (var path in PublicPaths)
            {
                if (req.PathInfo.StartsWith(BasePath + path))
                {
                    var file = VirtualFiles!.GetFile(DirPath + req.PathInfo.Substring(BasePath.Length));
                    return file != null
                        ? new StaticFileHandler(file)
                        {
                            Filter = appHost.Config.DebugMode
                                ? (request, response, _) =>
                                {
                                    response.AddHeader(HttpHeaders.CacheControl, "no-cache, no-store, must-revalidate");
                                    response.AddHeader(HttpHeaders.Pragma, "no-cache");
                                    response.AddHeader(HttpHeaders.Expires, "0");
                                }
                                : null
                        }
                        : new NotFoundHttpHandler();
                }
            }

            return new CustomActionHandlerAsync(async (httpReq, httpRes) =>
            {
                try
                {
                    async Task RenderTo(Stream stream)
                    {
                        httpRes.ContentType = MimeTypes.HtmlUtf8;

                        var fragments = GetIndexFragments();
                        var ctx = new HtmlModuleContext(this, httpReq);
                        foreach (var fragment in fragments)
                        {
                            await fragment.WriteToAsync(ctx, stream).ConfigAwait();
                        }
                    }

                    var dynamicUi = DynamicPageQueryStrings.Any(x => httpReq.QueryString[x] != null);
                    if (dynamicUi)
                    {
                        await RenderTo(httpRes.OutputStream);
                        return;
                    }

                    if (!dynamicUi)
                    {
                        if (EnableHttpCaching == true && indexFileETag != null)
                        {
                            httpRes.ContentType ??= MimeTypes.HtmlUtf8;
                            httpRes.AddHeader(HttpHeaders.ContentType, httpRes.ContentType);

                            httpRes.AddHeader(HttpHeaders.ETag, indexFileETag);
                            if (httpRes.GetHeader(HttpHeaders.CacheControl) == null)
                                httpRes.AddHeader(HttpHeaders.CacheControl,
                                    CacheControl ?? HtmlModulesFeature.DefaultCacheControl);

                            if (req.ETagMatch(indexFileETag))
                            {
                                httpRes.EndNotModified();
                                return;
                            }
                        }

                        if (EnableCompression == true &&
                            await TryReturnCompressedResponse(httpReq, httpRes).ConfigAwait())
                            return;
                    }

                    using var ms = MemoryStreamFactory.GetStream();
                    await RenderTo(ms);
                    ms.Position = 0;

                    // If EnableHttpCaching, calculate ETag hash from entire processed file 
                    if (EnableHttpCaching == true && indexFileETag == null)
                    {
                        indexFileETag = ms.ToMd5Hash().Quoted();
                        httpRes.AddHeader(HttpHeaders.ETag, indexFileETag);
                        if (httpRes.GetHeader(HttpHeaders.CacheControl) == null)
                            httpRes.AddHeader(HttpHeaders.CacheControl,
                                CacheControl ?? HtmlModulesFeature.DefaultCacheControl);
                    }

                    if (EnableCompression == true)
                    {
                        cachedBytes = ms.ToArray();
                        if (await TryReturnCompressedResponse(httpReq, httpRes).ConfigAwait())
                            return;
                    }

                    await ms.CopyToAsync(httpRes.OutputStream).ConfigAwait();
                }
                catch (Exception ex)
                {
                    await httpRes.WriteError(ex).ConfigAwait();
                }
            });
        };
    }
    
    public void Register(IAppHost appHost)
    {
        VirtualFiles ??= appHost.VirtualFiles;
        var fragments = GetIndexFragments(); //force parsing
        if (fragments.Length == 0) //Feature.IgnoreIfError
            return;

        var handlerFn = GetHandler(appHost);
        appHost.RawHttpHandlers.Add(handlerFn);
        
#if NET8_0_OR_GREATER
        (appHost as IAppHostNetCore).MapEndpoints(routeBuilder =>
        {
            routeBuilder.MapGet(BasePath + "/{*path}", httpContext => {
                    var req = httpContext.ToRequest();
                    var handler = handlerFn(req);
                    if (handler != null)
                        return handler.ProcessRequestAsync(req, req.Response, httpContext.Request.Path);
                    return Task.CompletedTask;
                })
                .WithMetadata<string>(name:BasePath, tag:GetType().Name, contentType:MimeTypes.Html, additionalContentTypes:[MimeTypes.JavaScript]);
        });
#endif

    }

    private async Task<bool> TryReturnCompressedResponse(IRequest httpReq, IResponse httpRes)
    {
        var compressionType = httpReq.GetCompressionType();
        var compressor = compressionType != null && cachedBytes != null
            ? StreamCompressors.Get(compressionType)
            : null;
        if (compressor != null)
        {
            var zipBytes = zipCache.GetOrAdd(compressor.Encoding, _ => compressor.Compress(cachedBytes!));
            httpRes.AddHeader(HttpHeaders.ContentEncoding, compressor.Encoding);
            httpRes.AddHeader(HttpHeaders.ContentType, httpRes.ContentType);
            await httpRes.WriteAsync(zipBytes);
            return true;
        }
        return false;
    }
}