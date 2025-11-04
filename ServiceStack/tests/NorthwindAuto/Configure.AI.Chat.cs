using ServiceStack;
using ServiceStack.AI;
using ServiceStack.Data;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Web;

[assembly: HostingStartup(typeof(MyApp.ConfigureAiChat))]

namespace MyApp;

public class ConfigureAiChat : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            
            var vfs = new FileSystemVirtualFiles(context.HostingEnvironment.ContentRootPath);
            services.AddPlugin(new ChatFeature {
                ConfigJson = vfs.GetFile("wwwroot/chat/llms.json").ReadAllText(),
                ValidateRequest = async req => null,
                EnableProviders = [
                    "openrouter"
                ]
            });
            
            // Persist Chat History
            services.AddSingleton<IChatStore, ChatStore>();
             
            services.ConfigurePlugin<MetadataFeature>(feature => {
                feature.AddPluginLink("/chat", "AI Chat");
            });
       });
}

public class ChatStore(ILogger<ChatStore> log, IDbConnectionFactory dbFactory) : IChatStore
{
    public async Task ChatCompletedAsync(ChatCompletion request, ChatResponse response, IRequest req)
    {
        using var db = await dbFactory.OpenAsync();
        log.LogDebug("ChatCompletedAsync:\n{Request}\nAnswer: {Answer}", ClientConfig.ToSystemJson(request), response.GetAnswer());
        await db.InsertAsync(req.ToChatCompletionLog(request, response));
    }

    public async Task ChatFailedAsync(ChatCompletion request, Exception ex, IRequest req)
    {
        using var db = await dbFactory.OpenAsync();
        log.LogWarning(ex, "ChatFailedAsync:\n{Request}", ClientConfig.ToSystemJson(request));
        await db.InsertAsync(req.ToChatCompletionLog(request, ex));
    }

    public void InitSchema()
    {
        using var db = dbFactory.Open();
        db.CreateTableIfNotExists<ChatCompletionLog>();
    }
}

/// <summary>
/// Use Partitioned DB Table in PostgreSQL
/// </summary>
/// <param name="log"></param>
/// <param name="dbFactory"></param>
public class PostgresChatStore(ILogger<PostgresChatStore> log, IDbConnectionFactory dbFactory) : IChatStore
{
    public async Task ChatCompletedAsync(ChatCompletion request, ChatResponse response, IRequest req)
    {
        log.LogDebug("ChatCompletedAsync:\n{Request}\nAnswer: {Answer}", ClientConfig.ToSystemJson(request), response.GetAnswer());
        using var db = PostgresUtils.OpenMonthDb<ChatCompletionLog>(dbFactory, DateTime.UtcNow);
        await db.InsertAsync(req.ToChatCompletionLog(request, response));
    }

    public async Task ChatFailedAsync(ChatCompletion request, Exception ex, IRequest req)
    {
        log.LogWarning(ex, "ChatFailedAsync:\n{Request}", ClientConfig.ToSystemJson(request));
        using var db = PostgresUtils.OpenMonthDb<ChatCompletionLog>(dbFactory, DateTime.UtcNow);
        await db.InsertAsync(req.ToChatCompletionLog(request, ex));
    }

    public void InitSchema()
    {
        using var db = dbFactory.Open();
        PostgresUtils.CreatePartitionTableIfNotExists<ChatCompletionLog>(db, x => x.CreatedDate);
    }
}