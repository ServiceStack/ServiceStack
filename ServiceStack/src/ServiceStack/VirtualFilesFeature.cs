#nullable enable

using System.Web;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceStack;

public class VirtualFilesFeature : IPlugin
{
    public IAppHost? AppHost { get; set; }
    
    public HttpAsyncTaskHandler? StaticFilesHandler { get; set; }
    public HttpAsyncTaskHandler? ForbiddenHttpHandler { get; set; }
    
    public void Register(IAppHost appHost)
    {
        AppHost = appHost;
        StaticFilesHandler ??= HttpHandlerFactory.StaticFilesHandler as HttpAsyncTaskHandler;
        ForbiddenHttpHandler ??= HttpHandlerFactory.ForbiddenHttpHandler as HttpAsyncTaskHandler;
        
        appHost.FallbackHandlers.Add(GetHandler);
    }
    
    public HttpAsyncTaskHandler? GetHandler(IRequest httpReq)
    {
        var isFile = httpReq.IsFile();
        var isDirectory = httpReq.IsDirectory();
        
        if (isFile || isDirectory)
        {
            //If pathInfo is for Directory try again with redirect including '/' suffix
            if (AppHost!.Config.RedirectDirectoriesToTrailingSlashes && isDirectory && !httpReq.OriginalPathInfo.EndsWith("/"))
                return new RedirectHttpHandler { RelativeUrl = httpReq.PathInfo + '/' };
        
            if (isDirectory)
                return StaticFilesHandler ?? new StaticFileHandler();
        
            return ShouldAllow(httpReq.PathInfo)
                ? StaticFilesHandler ?? new StaticFileHandler()
                : ForbiddenHttpHandler ?? new ForbiddenHttpHandler();
        }
        return null;
    }
    
    // no handler registered 
    // serve the file from the filesystem, restricting to a safe-list of extensions
    public static bool ShouldAllow(string pathInfo)
    {
        if (string.IsNullOrEmpty(pathInfo) || pathInfo == "/")
            return true;

        var config = HostContext.Config;
        foreach (var path in config.ForbiddenPaths)
        {
            if (pathInfo.StartsWith(path))
                return false;
        }
            
        var parts = pathInfo.SplitOnLast('.');
        if (parts.Length == 1 || string.IsNullOrEmpty(parts[1]))
            return false;

        var fileExt = parts[1];
        if (config.AllowFileExtensions.Contains(fileExt))
            return true;

        foreach (var pathGlob in config.AllowFilePaths)
        {
            if (pathInfo.GlobPath(pathGlob))
                return true;
        }
            
        return false;
    }
}
