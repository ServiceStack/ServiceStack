using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>Account-free static project exports, independently enabled and globally configured.</summary>
public class ShareStaticExtension() : ChatExtension("share_static")
{
    /// <summary>Optional complete host override of user/default/share_static/config.json. Configure before installation.</summary>
    public StaticPublishConfig? StaticPublish { get; set; }
    StaticPublishConfig settings=null!;

    public override void Install(ExtensionContext ctx)
    {
        var path=Path.Combine(ctx.GetUserPath(null),"share_static","config.json");
        var config=StaticPublish==null && File.Exists(path)?ChatJson.ParseObject(File.ReadAllText(path)):new JsonObject();
        settings=StaticPublish=StaticProjectPublisher.Configure(config,Directory.GetCurrentDirectory(),StaticPublish,
            HostContext.AppHost?.RootDirectory.RealPath);
        var publisher=new StaticProjectPublisher(ctx,settings);
        ctx.AddGet("config.json",_=>Task.FromResult<object?>(ChatJson.ToNode(settings)));
        ctx.AddPost("project/{id}/folder", async req => {
            var user = req.AssertUserName();
            try {
                return ChatResult.Json(await publisher.PublishAsync(
                    user, req.GetPathParam("id"), req.Request.RequestAborted).ConfigAwait());
            } catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.DecoderFallbackException) {
                throw new HttpError(500, "StaticPublishFailed", $"Unable to publish project to {settings.Directory}: {e.Message}", e);
            }
        });
        new ProjectOutputRoutes(ctx).Register();
    }
}
