namespace ServiceStack.AI;

/// <summary>
/// Cost/token analytics dashboards (port of extensions/analytics). UI-only — the synced
/// chat/ext/analytics/index.mjs consumes the app extension's requests/summary APIs.
/// </summary>
public class AnalyticsExtension() : ChatExtension("analytics")
{
    public override void Install(ExtensionContext ctx)
    {
        // static files + UI extension registration are automatic for synced chat/ext/analytics assets
    }
}
