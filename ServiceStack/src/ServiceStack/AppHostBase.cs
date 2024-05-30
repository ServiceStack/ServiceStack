#if NETFRAMEWORK
using System;
using System.Reflection;
using System.Web;
using ServiceStack.Host;
using ServiceStack.Host.AspNet;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Inherit from this class if you want to host your web services inside an
/// ASP.NET application.
/// </summary>
public abstract class AppHostBase : ServiceStackHost
{
    protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices)
        : base(serviceName, assembliesWithServices)
    {
#if !NET472            
        CookiesExtensions.Init();
#endif
    }

    public override string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
    {
        if (httpReq == null)
            return (Config.WebHostUrl ?? "/").CombineWith(virtualPath.TrimStart('~'));

        virtualPath = virtualPath.SanitizedVirtualPath();
        return httpReq.GetAbsoluteUrl(virtualPath);
    }

    public override string ResolvePhysicalPath(string virtualPath, IRequest httpReq)
    {
        var path = ((AspNetRequest)httpReq).HttpRequest.PhysicalPath;
        return path;
    }

    public override IRequest TryGetCurrentRequest()
    {
        try
        {
            return HasStarted ? HttpContext.Current.ToRequest() : null;
        }
        catch
        {
            return null;
        }
    }

    public override string MapProjectPath(string relativePath)
    {
        return relativePath.MapHostAbsolutePath();
    }

    public override string GetBaseUrl(IRequest httpReq)
    {
        var useHttps = UseHttps(httpReq);
        var baseUrl = Config.WebHostUrl;
        if (baseUrl != null)
            return baseUrl.NormalizeScheme(useHttps);

        var handlerPath = Config.HandlerFactoryPath;
        baseUrl = httpReq.AbsoluteUri.InferBaseUrl(fromPathInfo: httpReq.PathInfo);

        if (baseUrl != null)
        {
            if (handlerPath == null || baseUrl.EndsWith(handlerPath))
                return baseUrl.NormalizeScheme(useHttps);
        }

        var aspReq = (HttpRequestBase)httpReq.OriginalRequest;
        baseUrl = aspReq.Url.Scheme + "://" + aspReq.Url.Authority +
                  aspReq.ApplicationPath?.TrimEnd('/') + "/";

        return baseUrl
            .NormalizeScheme(useHttps)
            .CombineWith(handlerPath)
            .TrimEnd('/');
    }
}
#endif
