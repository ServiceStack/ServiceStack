using ServiceStack.Host.Handlers;
using ServiceStack.IO;

#if NET8_0_OR_GREATER
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
#endif

namespace ServiceStack;

public class CleanUrlsFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.CleanUrls;
    public string[] Extensions { get; set; } = ["html"];
    
    public void Register(IAppHost appHost)
    {
        var fs = ((ServiceStackHost)appHost).GetVirtualFileSource<FileSystemVirtualFiles>();
        appHost.CatchAllHandlers.Add((string httpMethod, string pathInfo, string filePath) =>
        {
            if (httpMethod == HttpMethods.Get && pathInfo.IndexOfAny('.') == -1)
            {
                foreach (var ext in Extensions)
                {
                    var relativePath = pathInfo + "." + ext;
                    var file = fs.GetFile(relativePath);
                    if (file != null)
                        return new StaticFileHandler(file);
                }
            }
            return null;
        });
        
#if NET8_0_OR_GREATER
        var host = (AppHostBase)appHost;
        host.MapEndpoints(routeBuilder => {
            routeBuilder.MapGet("{*path:nonfile}", httpContext => {
                var path = httpContext.Request.Path.Value;
                if (path.IndexOfAny('.') == -1)
                {
                    foreach (var ext in Extensions)
                    {
                        var relativePath = path + "." + ext;
                        var file = fs.GetFile(relativePath);
                        if (file != null)
                            return httpContext.ProcessRequestAsync(new StaticFileHandler(file));
                    }
                }
                return Task.CompletedTask;
            })
            .WithMetadata<string>(nameof(CleanUrlsFeature));
        });
#endif        
    }
}
