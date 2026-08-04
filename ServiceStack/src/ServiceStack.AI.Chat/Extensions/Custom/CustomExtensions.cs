namespace ServiceStack.AI;

/// <summary>
/// This App's own Chat extension: the C# half of chat/custom/, which is the one part of the UI
/// sync.sh never touches. Register routes, tools and filters here the same way the built-in
/// extensions do, and put the UI that goes with them in chat/custom/ (served at /custom/**).
/// </summary>
public class CustomExtension() : ChatExtension("custom")
{
    public override void Install(ExtensionContext ctx)
    {
        // '/' escapes the /ext/<name> prefix: chat/custom/ is served at /custom/, not /ext/custom/
        ctx.RegisterUiExtension("/custom/index.mjs");
    }
}
