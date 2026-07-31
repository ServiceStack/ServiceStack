using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Generated/uploaded media catalogue (port of llms-py's "gallery" extension): every file written
/// to the content-addressed cache is recorded in the ChatMedia table via the cache_saved filter,
/// queryable by the gallery UI.
/// </summary>
public class GalleryExtension() : ChatExtension("gallery"), IMediaApi
{
    ChatDb db = null!;

    public override void Install(ExtensionContext ctx)
    {
        db = ctx.Feature.ChatDb ?? throw new Exception(
            "ChatFeature.ChatDb is required by the gallery extension — register an IDbConnectionFactory");

        ctx.RegisterCacheSavedFilter(OnCacheSaved);

        ctx.AddGet("media", req =>
        {
            var query = new JsonObject();
            foreach (var key in req.Request.QueryString.AllKeys)
            {
                if (key != null)
                    query[key] = req.Request.QueryString[key];
            }
            var rows = db.QueryMedia(query, req.UserName);
            return Task.FromResult<object?>(rows.ToDtos(ToClientDto));
        });

        ctx.AddGet("media/totals", req =>
        {
            var totals = new JsonArray();
            foreach (var (type, count) in db.MediaTotals(req.UserName))
            {
                totals.Add(new JsonObject { ["type"] = type, ["count"] = count });
            }
            return Task.FromResult<object?>(totals);
        });

        ctx.AddDelete("media/{hash}", req =>
        {
            db.DeleteMediaByHash(req.GetPathParam("hash"), req.UserName);
            return Task.FromResult<object?>(new JsonObject());
        });

        ctx.Media = this;
    }

    /// <summary>
    /// The gallery UI binds item.url straight into &lt;img src&gt;/&lt;audio src&gt;/download links without
    /// going through ai.resolveUrl (llms-py serves from the site root, so it never needs to), so
    /// the mounted path has to be applied here. IMediaApi.QueryMedia keeps the stored URL.
    /// </summary>
    JsonObject ToClientDto(ChatMedia x)
    {
        var dto = x.ToDto();
        dto["url"] = Ctx.Feature.ResolveClientUrl(x.Url);
        return dto;
    }

    /// <summary>Record cache writes as media rows (port of on_cache_save + GalleryDB.insert_media)</summary>
    void OnCacheSaved(CacheSavedContext context)
    {
        try
        {
            var info = context.Info;
            var mimeType = info.GetString("type") ?? "";
            var mediaType = mimeType.LeftPart('/');
            Log.LogInformation("cache saved: {Url}", context.Url);

            var media = new ChatMedia
            {
                // normalize to the same partition key the queries filter on
                User = context.User ?? ChatDb.DefaultUser,
                Name = info.GetString("name"),
                Type = mediaType is "image" or "video" or "audio" ? mediaType : null,
                Prompt = info.GetString("prompt"),
                Model = info.GetString("model"),
                Created = DateTime.Now,
                Cost = info.GetDouble("cost"),
                Seed = info.GetLong("seed"),
                Url = info.GetString("url") ?? context.Url,
                Hash = info.GetString("hash"),
                Width = info.GetInt("width"),
                Height = info.GetInt("height"),
                Size = info.GetLong("size"),
                Duration = info.GetDouble("duration"),
            };
            media.Hash ??= media.Url.LastRightPart('/').LeftPart('.');
            // prefer the ratio the generation actually requested; fall back to measured dimensions
            media.AspectRatio = info.GetString("aspect_ratio")
                ?? (media is { Width: > 0, Height: > 0 }
                    ? ChatDb.ClosestAspectRatio(media.Width.Value, media.Height.Value)
                    : null);

            // keys that aren't media columns are kept in metadata (Python parity)
            var knownKeys = ChatDb.MediaColumns.Keys.ToSet();
            var metadata = new JsonObject();
            foreach (var entry in info)
            {
                if (!knownKeys.Contains(entry.Key))
                    metadata[entry.Key] = entry.Value?.DeepClone();
            }
            if (metadata.Count > 0)
                media.Metadata = metadata.ToJsonString(ChatJson.Options);

            db.InsertMedia(media);
        }
        catch (Exception e)
        {
            Log.LogError(e, "Failed to record media for {Url}", context.Url);
        }
    }

    // ── IMediaApi ──

    public List<JsonObject> QueryMedia(JsonObject query, string? user = null) =>
        db.QueryMedia(query, user).Select(x => x.ToDto()).ToList();

    public Task UpdateMediaAsync(long id, JsonObject media, string? user = null)
    {
        var row = db.QueryMedia(new JsonObject { ["id"] = id }, user).FirstOrDefault();
        if (row == null)
            return Task.CompletedTask;
        if (media.ContainsKey("caption")) row.Caption = media.GetString("caption");
        if (media.ContainsKey("description")) row.Description = media.GetString("description");
        if (media.ContainsKey("reactions")) row.Reactions = ChatDtos.ToJson(media["reactions"]);
        if (media.ContainsKey("rating")) row.Rating = media.GetString("rating");
        if (media.ContainsKey("publishedAt")) row.PublishedAt = ChatDtos.ParseDate(media["publishedAt"]);
        if (media.ContainsKey("publishedUrl")) row.PublishedUrl = media.GetString("publishedUrl");
        db.UpdateMedia(row);
        return Task.CompletedTask;
    }
}
