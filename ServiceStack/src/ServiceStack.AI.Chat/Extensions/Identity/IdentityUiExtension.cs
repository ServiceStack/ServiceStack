namespace ServiceStack.AI;

/// <summary>
/// C#-only UI extension serving chat/ext/identity/index.mjs, which swaps the stock API-key
/// SignIn component for a redirect to the host's Identity Auth login page (AuthType=OAuth).
/// </summary>
public class IdentityUiExtension : IChatExtension
{
    public string Name => ChatExtension.Identity;

    public void Install(ExtensionContext ctx)
    {
        // static files + UI extension registration are automatic for chat/ext/identity assets
    }
}
