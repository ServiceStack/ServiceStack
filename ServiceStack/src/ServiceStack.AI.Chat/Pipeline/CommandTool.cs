using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>
/// Turns a ServiceStack Command into an LLM tool: its request type generates the JSON Schema the
/// Agent fills in, <see cref="ToolAttribute"/> and <c>[Description]</c> supply the metadata it
/// selects on, and calls execute through <see cref="CommandsFeature"/> — so a tool call gets the
/// same DI, validation, retries, timings and error logging as every other command in the App,
/// and shows up in its command history.
/// </summary>
public static class CommandTool
{
    /// <summary>Build the tool a Command is exposed to LLMs as</summary>
    public static ChatTool Create(Type commandType, ChatFeature feature, string? group = null)
    {
        var requestType = RequestTypeOf(commandType)
            ?? throw new ArgumentException(
                $"{commandType.Name} is not a Command: it doesn't implement IAsyncCommand<T>", nameof(commandType));
        var hasResult = ResultTypeOf(commandType) != null;

        // fail at startup rather than at the first tool call, when only the LLM would see it
        if (HostContext.AppHost?.GetPlugin<CommandsFeature>() == null)
            throw new NotSupportedException(
                $"Registering {commandType.Name} as a tool requires the CommandsFeature plugin");

        var attr = commandType.FirstAttribute<ToolAttribute>();
        var name = attr?.Name ?? DefaultName(commandType);

        var definition = new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = Describe(commandType, attr),
                // the same schema generator the App's APIs use, so a tool reads the [Description]
                // and [ApiAllowableValues] already on the request type
                ["parameters"] = ChatJson.Parse(ApiToolSchema.ToJsonSchema(requestType).ToJson())?.AsObject()
                    ?? new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() },
            },
        };

        return new ChatTool
        {
            Definition = definition,
            Handler = (args, context) => ExecuteAsync(commandType, requestType, hasResult, feature, args, context),
            Group = group,
            Safety = attr?.Safety ?? ToolSafety.Auto,
        };
    }

    static async Task<object?> ExecuteAsync(Type commandType, Type requestType, bool hasResult,
        ChatFeature feature, JsonObject args, ChatContext context)
    {
        var request = requestType == typeof(NoArgs)
            ? NoArgs.Value
            // ServiceStack's JSON serializer, which is case-insensitive and forgiving about the
            // shapes an LLM produces (numbers as strings, enums by name)
            : ServiceStack.Text.JsonSerializer.DeserializeFromString(args.ToJsonString(ChatJson.Options), requestType)
              ?? requestType.CreateInstance();

        // commands are auto-registered by CommandsFeature; CreateInstance covers one it didn't scan
        var command = feature.Services.GetService(commandType)
            ?? ActivatorUtilities.CreateInstance(feature.Services, commandType);

        // run as the user this tool is acting for: the command's own Request, and anything it
        // resolves from it, is the caller's — not the App's
        if (context.Request != null && command is IRequiresRequest requiresRequest)
            requiresRequest.Request = context.Request;

        var commands = HostContext.AssertPlugin<CommandsFeature>();
        var result = await commands.ExecuteCommandAsync(command, request, context.CancellationToken).ConfigAwait();

        // ExecuteCommandAsync reports failures instead of throwing; rethrow so the tool loop
        // reports it to the LLM the same way it reports every other tool error
        if (result.Exception != null)
            ExceptionDispatchInfo.Capture(result.Exception).Throw();

        return hasResult
            ? TypeProperties.Get(commandType).GetAccessor(nameof(IHasResult<object>.Result))?.PublicGetter(command)
            : new JsonObject { ["success"] = true };
    }

    /// <summary>The TRequest of the IAsyncCommand&lt;TRequest&gt; this Command implements</summary>
    public static Type? RequestTypeOf(Type commandType) => ClosedGenericArg(commandType, typeof(IAsyncCommand<>));

    /// <summary>The TResult of IHasResult&lt;TResult&gt;, or null for a Command that returns nothing</summary>
    public static Type? ResultTypeOf(Type commandType) => ClosedGenericArg(commandType, typeof(IHasResult<>));

    static Type? ClosedGenericArg(Type type, Type genericInterface) => type.GetInterfaces()
        .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterface)
        ?.GetGenericArguments()[0];

    /// <summary>MyBookingSummaryCommand → "my_booking_summary", the convention LLM tools are named by</summary>
    public static string DefaultName(Type commandType)
    {
        var name = commandType.Name;
        if (name.EndsWith("Command", StringComparison.Ordinal) && name.Length > "Command".Length)
            name = name[..^"Command".Length];
        return name.ToLowercaseUnderscore();
    }

    /// <summary>
    /// What the Agent reads to decide whether to call this tool: what it does ([Description]),
    /// when to reach for it ([Tool(WhenToUse)]) and what a call looks like ([Tool(Examples)]).
    /// </summary>
    static string Describe(Type commandType, ToolAttribute? attr)
    {
        var parts = new List<string>();
        if (commandType.GetDescription() is { } description && !string.IsNullOrEmpty(description))
            parts.Add(description.TrimEnd('.') + ".");
        if (!string.IsNullOrEmpty(attr?.WhenToUse))
            parts.Add($"Use when {attr!.WhenToUse!.TrimEnd('.')}.");
        if (attr?.Examples is { Length: > 0 } examples)
            parts.Add("Examples: " + string.Join(" ", examples));
        return parts.Count > 0
            ? string.Join(" ", parts)
            : DefaultName(commandType).Replace('_', ' ');
    }
}
