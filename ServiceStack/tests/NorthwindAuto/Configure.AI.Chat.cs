using System.ComponentModel;
using System.Text.Json.Nodes;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.AI;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(MyApp.ConfigureAiChat))]

namespace MyApp;

/// <summary>
/// AI Chat using this host's ASP.NET Identity users, signed in with the Chat UI's own
/// username/password form (AuthType=Credentials installs the 'credentials' extension, which
/// authenticates with the Authenticate API). Set RequireAuth = false for open access, where all
/// chat data is stored under the "default" user, matching llms-py's behaviour with no auth
/// extension installed.
/// </summary>
public class ConfigureAiChat : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {

            services.AddPlugin(new ChatFeature {
                // RequireAuth = false, // open access, runs as the "default" user
                Extensions = {
                    new BookingToolsExtension(),
                },
                RequireAuth = true,
                // RequiredRole = "Admin",
                // Publish =
                // {
                //     Enabled = true,
                // },
                Tools =
                {
                    EnableCodeExecution = true,
                    EnableFilesystemTools = true,
                },
                ApiTools = {
                    IncludeTags = ["todos", "CoffeeShop"]
                },
                // Add this App's own tools to the built-in extensions
                // Expose these tools to external AI Agents over MCP at /chat/mcp
                Mcp = {
                    ToolGroups = ["api_tools", "bookings"],
                    RejectToolsRequiringApproval = false, // allow Agents to call any tool in the above groups without approval
                },
                Setup = (ctx => {
                    // ctx.Mcp.ToolGroups = ["api_tools", "core_tools", "booking_tools"];
                }),
            });

            services.ConfigurePlugin<MetadataFeature>(feature => {
                feature.AddPluginLink("/chat", "AI Chat");
            });
            
            services.AddPlugin(new PdfFeature {
                PdfCodeGen = new() {
                    Namespace = "MyApp.ServiceModel.Pdf",
                    OutputPath = Path.Combine(context.HostingEnvironment.ContentRootPath, "ServiceModel/Pdf"),
                    // Exclude = ["invoice"],
                }
            });
       })
       .ConfigureAppHost(afterAppHostInit:appHost => {
            var log = appHost.GetApplicationServices().GetRequiredService<ILogger<ConfigureAiChat>>();
            log.LogInformation("AI Chat configured");
            
            // Generate a typed model for every template in App_Data/pdf with:
            //     dotnet run --AppTasks=pdf
            // Templates you've since edited the model of are left alone, as are any named in Exclude.
            AppTasks.Register("pdf", _ => appHost.GetPlugin<PdfFeature>().GeneratePdfs());
            
        });
}

/// <summary>
/// Example of an App registering its own LLM tools. Tools are registered in Install() and are
/// then available to the Chat UI's tool loop, and to external AI Agents over MCP when their
/// group is named in ChatFeature.Mcp.
/// </summary>
public class BookingToolsExtension() : ChatExtension("booking_tools")
{
    /// <summary>Extension Name, is also the default group name for tools registered without one</summary>

    IDbConnectionFactory dbFactory = null!;

    public override void Install(ExtensionContext ctx)
    {
        // Install() runs after the App's services are available
        dbFactory = ctx.Feature.Services.GetRequiredService<IDbConnectionFactory>();

        // The tool definition is the OpenAI function schema the LLM sees: name it for what it
        // does and describe it for the model, not for the developer — this is the only thing an
        // Agent has to decide whether to call it
        /*
        ctx.RegisterTool(new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = "booking_summary_manual",
                ["description"] = "Summarise this hotel's room bookings: how many, what they're "
                    + "worth, and a breakdown by room type. Use for questions about occupancy or "
                    + "revenue instead of querying bookings one by one.",
                ["parameters"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["from"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Only count bookings starting on or after this "
                                + "ISO-8601 date, e.g. '2026-01-01'. Omit for all bookings.",
                        },
                        ["room_type"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray("Single", "Double", "Queen", "Twin", "Suite"),
                            ["description"] = "Only count bookings of this room type",
                        },
                    },
                },
            },
        }, BookingSummaryManualAsync);
         */

        // Or implement the tool as a ServiceStack Command: its request type generates the same
        // schema, [Tool]/[Description] describe it, and calls run through CommandsFeature
        ctx.RegisterTool<BookingSummaryCommand>("bookings");
    }

    /// <summary>
    /// Tool handlers receive the args the LLM chose and the context it's running in. Return a
    /// string to pass through verbatim, or any object to send the LLM its JSON.
    /// </summary>
    async Task<object?> BookingSummaryManualAsync(JsonObject args, ChatContext context)
    {
        // context.User is the signed-in user this tool is acting for, and context.Request the
        // request it's running under — pass it to a Service Gateway to call APIs as that user
        using var db = await dbFactory.OpenAsync();

        // Cancelled is nullable: "!= true" is NULL in SQL, which would drop every uncancelled row
        var q = db.From<Booking>().Where(x => x.Cancelled == null || x.Cancelled == false);
        if (DateTime.TryParse(args.GetString("from"), out var from))
            q.And(x => x.BookingStartDate >= from);
        if (Enum.TryParse<RoomType>(args.GetString("room_type"), ignoreCase: true, out var roomType))
            q.And(x => x.RoomType == roomType);

        var bookings = await db.SelectAsync(q);

        var byRoomType = bookings.GroupBy(x => x.RoomType)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        // keep results small and pre-digested: an Agent pays for every row in its context window
        return new
        {
            Total = bookings.Count,
            TotalCost = bookings.Sum(x => x.Cost),
            ByRoomType = byRoomType,
            Bookings = bookings.Map(x => new { x.Id, x.Name, x.RoomNumber, x.BookingStartDate, x.Cost }),
        };
    }
    
    /// <summary>The Command's request type generates the tool's JSON Schema, so document it for
    /// the LLM here: [Description] becomes each argument's description, and an enum its values.</summary>
    public class BookingSummary
    {
        [Description("Only count bookings starting on or after this date")]
        public DateTime? From { get; set; }
        [Description("Only count bookings of this room type")]
        public RoomType? Type { get; set; }
    }
    public class BookingInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RoomNumber { get; set; }
        public DateTime BookingStartDate { get; set; }
        public decimal Cost { get; set; }
    }
    public class BookingSummaryResponse
    {
        public int Total { get; set; }
        public decimal TotalCost { get; set; }
        public Dictionary<string, int> ByRoomType { get; set; } = new();
        public List<BookingInfo> Bookings { get; set; }
    }
    
    [Description("Summarise this hotel's room bookings: how many, what they're worth, "
                 + "and a breakdown by room type")]
    [Tool("the user asks about occupancy or booking revenue",
        Name = "booking_summary", Safety = ToolSafety.ReadOnly)]
    public class BookingSummaryCommand(IDbConnectionFactory dbFactory) : AsyncCommandWithResult<BookingSummary, BookingSummaryResponse>
    {
        protected override async Task<BookingSummaryResponse> RunAsync(BookingSummary request, CancellationToken token)
        {
            using var db = await dbFactory.OpenAsync(token:token);

            // Cancelled is nullable: "!= true" is NULL in SQL, which would drop every uncancelled row
            var q = db.From<Booking>().Where(x => x.Cancelled == null || x.Cancelled == false);
            if (request.From.HasValue)
                q.And(x => x.BookingStartDate >= request.From.Value);
            if (request.Type.HasValue)
                q.And(x => x.RoomType == request.Type.Value);

            var bookings = await db.SelectAsync(q, token: token);

            var byRoomType = bookings.GroupBy(x => x.RoomType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            return new()
            {
                Total = bookings.Count,
                TotalCost = bookings.Sum(x => x.Cost),
                ByRoomType = byRoomType,
                Bookings = bookings.Map(x => new BookingInfo
                {
                    Id = x.Id,
                    Name = x.Name,
                    RoomNumber = x.RoomNumber,
                    BookingStartDate = x.BookingStartDate,
                    Cost = x.Cost
                }),
            };
        }
    }

}
