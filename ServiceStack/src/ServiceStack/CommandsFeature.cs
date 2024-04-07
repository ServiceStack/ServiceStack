#if NET6_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Web;

namespace ServiceStack;

public class CommandsFeature : IPlugin, IConfigureServices, Model.IHasStringId
{
    public string Id => "commands";

    /// <summary>
    /// Limit API access to users in role
    /// </summary>
    public string AccessRole { get; set; } = RoleNames.Admin;

    public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new() {
        [typeof(ViewCommandsService)] = ["/" + "commands".Localize()],
    };

    public List<(Type, ServiceLifetime)> RegisterTypes { get; set; } =
    [
        (typeof(IDbConnection), ServiceLifetime.Transient),
        (typeof(Redis.IRedisClient), ServiceLifetime.Singleton),
        (typeof(Redis.IRedisClientAsync), ServiceLifetime.Singleton),
        (typeof(Messaging.IMessageProducer), ServiceLifetime.Singleton),
    ];

    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<ICommandExecutor>(c => new CommandExecutor(this, c));

        ServiceLifetime ToServiceLifetime(Lifetime lifetime) => lifetime switch {
            Lifetime.Scoped => ServiceLifetime.Scoped,
            Lifetime.Singleton => ServiceLifetime.Singleton,
            _ => ServiceLifetime.Transient
        };
        
        foreach (var requestType in ServiceStackHost.InitOptions.ResolveAssemblyRequestTypes())
        {
            var requestProps = TypeProperties.Get(requestType).PublicPropertyInfos;
            foreach (var prop in requestProps)
            {
                var commandAttr = prop.GetCustomAttribute<CommandAttribute>();
                if (commandAttr == null)
                    continue;

                services.Add(commandAttr.CommandType, commandAttr.CommandType, ToServiceLifetime(commandAttr.Lifetime));
            }
        }

        foreach (var registerType in RegisterTypes)
        {
            if (registerType.Item1 == typeof(IDbConnection) && !services.Exists<IDbConnection>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetDbConnection(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(ServiceStack.Redis.IRedisClient) && !services.Exists<ServiceStack.Redis.IRedisClient>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetRedisClient(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(ServiceStack.Redis.IRedisClientAsync) && !services.Exists<ServiceStack.Redis.IRedisClientAsync>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetRedisClientAsync(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(ServiceStack.Messaging.IMessageProducer) && !services.Exists<ServiceStack.Messaging.IMessageProducer>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetMessageProducer(), registerType.Item2);
            }
        }

        services.RegisterServices(ServiceRoutes);
    }

    private ILogger<CommandsFeature>? log;

    public void Register(IAppHost appHost)
    {
        log = appHost.GetApplicationServices().GetRequiredService<ILogger<CommandsFeature>>();
    }

    class CommandExecutor(CommandsFeature feature, IServiceProvider services) : ICommandExecutor
    {
        public TCommand Command<TCommand>() where TCommand : IAsyncCommand => services.GetRequiredService<TCommand>();

        public Task ExecuteAsync<T>(IAsyncCommand<T> command, T request)
        {
            return feature.ExecuteCommandAsync(command, request);
        }
    }

    public Task ExecuteCommandAsync<TCommand, TRequest>(TCommand command, TRequest request) 
        where TCommand : IAsyncCommand<TRequest> 
    {
        ArgumentNullException.ThrowIfNull(request);
        return ExecuteCommandAsync(command.GetType(), dto => command.ExecuteAsync((TRequest)dto), request);
    }

    public async Task ExecuteCommandAsync(Type commandType, Func<object,Task> execFn, object requestDto)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await execFn(requestDto);
            log!.LogDebug("{Command} took {ElapsedMilliseconds}ms to execute", commandType.Name, sw.ElapsedMilliseconds);

            AddCommandResult(new()
            {
                Name = commandType.Name,
                Ms = sw.ElapsedMilliseconds,
                At = DateTime.UtcNow,
            });
        }
        catch (Exception e)
        {
            var requestBody = requestDto.ToJsv();
            log!.LogError(e, "{Command}({Request}) failed: {Message}", commandType.Name, requestBody, e.Message);

            AddCommandResult(new()
            {
                Name = commandType.Name,
                Ms = sw.ElapsedMilliseconds,
                At = DateTime.UtcNow,
                Request = requestBody,
                Error = e.Message,
            });
        }
    }

    public async Task ExecuteCommandsAsync<T>(IServiceProvider services, T requestDto) where T : class
    {
        var obj = requestDto.ToObjectDictionary();
        foreach (var commandProp in TypeProperties.Get(typeof(T)).PublicPropertyInfos)
        {
            var commandType = commandProp.GetCustomAttribute<CommandAttribute>()?.CommandType;
            if (commandType == null)
                continue;
            if (!obj.TryGetValue(commandProp.Name, out var requestProp) || requestProp == null)
                continue;

            var oCommand = services.GetRequiredService(commandType);
            var method = commandType.GetMethod("ExecuteAsync")
                ?? throw new NotSupportedException("ExecuteAsync method not found on " + commandType.Name);
                
            async Task Exec(object commandArg)
            {
                var methodInvoker = GetInvoker(method);
                await methodInvoker(oCommand, commandArg);
            }

            await ExecuteCommandAsync(commandType, Exec, requestProp);
        }
    }
    
    public const int DefaultCapacity = 250;
    public ConcurrentQueue<CommandResult> CommandResults { get; set; } = [];
    public ConcurrentQueue<CommandResult> CommandFailures { get; set; } = new();
    
    public ConcurrentDictionary<string, CommandSummary> CommandTotals { get; set; } = new();

    public void AddCommandResult(CommandResult result)
    {
        var ms = result.Ms ?? 0;
        if (result.Error == null)
        {
            CommandResults.Enqueue(result);
            while (CommandResults.Count > DefaultCapacity)
                CommandResults.TryDequeue(out _);

            CommandTotals.AddOrUpdate(result.Name, 
                _ => new CommandSummary { Name = result.Name, Count = 1, TotalMs = ms, MinMs = ms > 0 ? ms : int.MinValue },
                (_, summary) => 
                {
                    summary.Count++;
                    summary.TotalMs += ms;
                    summary.MaxMs = Math.Max(summary.MaxMs, ms);
                    if (ms > 0)
                    {
                        summary.MinMs = Math.Min(summary.MinMs, ms);
                    }
                    return summary;
                });
        }
        else
        {
            CommandFailures.Enqueue(result);
            while (CommandFailures.Count > DefaultCapacity)
                CommandFailures.TryDequeue(out _);

            CommandTotals.AddOrUpdate(result.Name, 
                _ => new CommandSummary { Name = result.Name, Failed = 1, Count = 0, TotalMs = 0, MinMs = int.MinValue, LastError = result.Error },
                (_, summary) =>
                {
                    summary.Failed++;
                    summary.LastError = result.Error;
                    return summary;
                });
        }
    }
    
    public delegate Task AsyncMethodInvoker(object instance, object arg);
    static readonly ConcurrentDictionary<MethodInfo, AsyncMethodInvoker> invokerCache = new();

    public static AsyncMethodInvoker GetInvokerToCache(MethodInfo method)
    {
        if (method.IsStatic)
            throw new NotSupportedException("Static Method not supported");
            
        var paramInstance = Expression.Parameter(typeof(object), "instance");
        var paramArg = Expression.Parameter(typeof(object), "arg");

        var convertFromMethod = typeof(ServiceStack.TypeExtensions).GetStaticMethod(nameof(ServiceStack.TypeExtensions.ConvertFromObject));

        var convertParam = convertFromMethod.MakeGenericMethod(method.GetParameters()[0].ParameterType);
        var paramTypeArg = Expression.Call(convertParam, paramArg); 

        var convertReturn = convertFromMethod.MakeGenericMethod(method.ReturnType);

        var methodCall = Expression.Call(Expression.TypeAs(paramInstance, method.DeclaringType!), method, paramTypeArg);

        var lambda = Expression.Lambda(typeof(AsyncMethodInvoker), 
            Expression.Call(convertReturn, methodCall), 
            paramInstance, 
            paramArg);

        var fn = (AsyncMethodInvoker)lambda.Compile();
        return fn;
    }

    /// <summary>
    /// Create an Invoker for public instance methods
    /// </summary>
    public AsyncMethodInvoker GetInvoker(MethodInfo method)
    {
        if (invokerCache.TryGetValue(method, out var fn))
            return fn;
        fn = GetInvokerToCache(method);
        invokerCache[method] = fn;
        return fn;
    }
}

public static class CommandExtensions
{
    public static Task ExecuteCommandsAsync<T>(this IRequest? req, T requestDto) where T : class
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(requestDto);
        
        var services = req.TryResolve<IServiceProvider>();
        if (services == null)
            throw new NotSupportedException(nameof(IServiceProvider) + " not available");
        var feature = HostContext.AssertPlugin<CommandsFeature>();
        return feature.ExecuteCommandsAsync(services, requestDto);
    }
}


public class CommandResult
{
    public string Name { get; set; }
    public long? Ms { get; set; }
    public DateTime At { get; set; }
    public string Request { get; set; }
    public string? Error { get; set; }
}

public class CommandSummary
{
    public string Name { get; set; }
    public long Count { get; set; }
    public long Failed { get; set; }
    public long TotalMs { get; set; }
    public long MinMs { get; set; }
    public long MaxMs { get; set; }
    public int AverageMs => (int) Math.Floor(TotalMs / (double)Count);
    public string? LastError { get; set; }
}

[ExcludeMetadata]
public class ViewCommands : IGet, IReturn<ViewCommandsResponse>
{
    public bool? Clear { get; set; }
}
public class ViewCommandsResponse
{
    public List<CommandSummary> CommandTotals { get; set; }
    public List<CommandResult> LatestCommands { get; set; }
    public List<CommandResult> LatestFailed { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[DefaultRequest(typeof(ViewCommands))]
public class ViewCommandsService : Service
{
    public async Task<object> Any(ViewCommands request)
    {
        var feature = HostContext.AssertPlugin<CommandsFeature>();
        if (!HostContext.DebugMode)
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AccessRole);

        var to = new ViewCommandsResponse
        {
            LatestCommands = new(feature.CommandResults),
            LatestFailed = new(feature.CommandFailures),
            CommandTotals = new(feature.CommandTotals.Values)
        };
        if (request.Clear == true)
        {
            feature.CommandResults.Clear();
            feature.CommandFailures.Clear();
            feature.CommandTotals.Clear();
        }
        return to;
    }
}
#endif