using ServiceStack;
using ServiceStack.Host.Handlers;
using ServiceStack.NativeTypes;
using ServiceStack.Text;
using ServiceStack.Web;

namespace MyApp;

public class UiFeatureDev : IPlugin
{
    public void Register(IAppHost appHost)
    {
        appHost.RawHttpHandlers.Add(req =>  req.PathInfo == "/ui.css"
            ? new StaticFileHandler(appHost.VirtualFiles.GetFile("/ui/ui.css"))
            : req.PathInfo.StartsWith("/ui")
                ? new StaticContentHandler(appHost.VirtualFiles.GetFile("/ui/ui.html").ReadAllText()
                        .Replace("<base href=\"\">",$"<base href=\"{req.ResolveAbsoluteUrl("~/ui/")}\">" + 
                                                    "\n<script src=\"/js/hot-fileloader.js\"></script>")
                        .Replace("{/*APP*/}", ScopedJson(req))
                    , MimeTypes.Html)
                : null);
    }

    private static string ScopedJson(IHttpRequest req)
    {
        using var scope = JsConfig.With(new Config { TextCase = TextCase.CamelCase });
        return req.TryResolve<INativeTypesMetadata>().ToAppMetadata(req).ToJson();
    }
}