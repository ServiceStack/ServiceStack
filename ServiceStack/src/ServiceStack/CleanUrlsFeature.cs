using ServiceStack.Host.Handlers;
using ServiceStack.IO;

namespace ServiceStack;

public class CleanUrlsFeature : IPlugin, Model.IHasStringId
{
    public string Id { get; set; } = Plugins.CleanUrls;
    public string[] Extensions { get; set; } = { "html" };
    
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
    }
}
