#nullable enable

using ServiceStack.Host.Handlers;
using ServiceStack.IO;

namespace ServiceStack;

public class CleanUrlsFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.CleanUrls;
    public string[] Extensions { get; set; } = ["html"];
    
    public FileSystemVirtualFiles? VirtualFiles { get; set; }

    public void Register(IAppHost appHost)
    {
        VirtualFiles = ((ServiceStackHost)appHost).GetVirtualFileSource<FileSystemVirtualFiles>();
        appHost.FallbackHandlers.Add(req => GetHandler(req.Verb, req.PathInfo));
    }

    public HttpAsyncTaskHandler? GetHandler(string httpMethod, string pathInfo)
    {
        if (httpMethod == HttpMethods.Get && pathInfo.IndexOfAny('.') == -1)
        {
            foreach (var ext in Extensions)
            {
                var relativePath = pathInfo + "." + ext;
                var file = VirtualFiles!.GetFile(relativePath);
                if (file != null)
                    return new StaticFileHandler(file);
            }
        }
        return null;
    }
}
