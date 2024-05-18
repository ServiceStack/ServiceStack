#if NET6_0_OR_GREATER
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Messaging;
using ServiceStack.Model;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack;

public class CommandsFeature : IPlugin, IConfigureServices, IHasStringId, IPreInitPlugin
{
    public string Id => Plugins.AdminCommands;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public const int DefaultCapacity = 250;
    public int ResultsCapacity { get; set; } = DefaultCapacity;
    public int FailuresCapacity { get; set; } = DefaultCapacity;
    public int TimingsCapacity { get; set; } = 1000;

    public List<Func<IEnumerable<Type>>> TypeResolvers { get; set; } = [
        ScanServiceAssemblies
    ];
    
    /// <summary>
    /// Ignore commands or Request DTOs from being logged
    /// </summary>
    public List<string> Ignore { get; set; } = [nameof(ViewCommands)];
    
    public Func<CommandResult,bool>? ShouldIgnore { get; set; }

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
        (typeof(IRedisClient), ServiceLifetime.Singleton),
        (typeof(IRedisClientAsync), ServiceLifetime.Singleton),
        (typeof(IMessageProducer), ServiceLifetime.Singleton),
    ];

    static ServiceLifetime ToServiceLifetime(Lifetime lifetime) => lifetime switch {
        Lifetime.Scoped => ServiceLifetime.Scoped,
        Lifetime.Singleton => ServiceLifetime.Singleton,
        _ => ServiceLifetime.Transient
    };

    public void Configure(IServiceCollection services)
    {
        services.AddTransient<ICommandExecutor>(c => new CommandExecutor(this, c));

        foreach (var typeResolver in TypeResolvers)
        {
            var commandTypes = typeResolver();
            foreach (var commandType in commandTypes)
            {
                if (services.Exists(commandType))
                    continue;
                
                var lifetimeAttr = commandType.FirstAttribute<LifetimeAttribute>();
                var lifetime = ToServiceLifetime(lifetimeAttr?.Lifetime ?? Lifetime.Transient);
                services.Add(commandType, commandType, lifetime);
            }
        }

        foreach (var registerType in RegisterTypes)
        {
            if (registerType.Item1 == typeof(IDbConnection) && !services.Exists<IDbConnection>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetDbConnection(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(IRedisClient) && !services.Exists<IRedisClient>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetRedisClient(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(IRedisClientAsync) && !services.Exists<IRedisClientAsync>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetRedisClientAsync(), registerType.Item2);
            }
            if (registerType.Item1 == typeof(IMessageProducer) && !services.Exists<IMessageProducer>())
            {
                services.Add(registerType.Item1, _ => HostContext.AppHost.GetMessageProducer(), registerType.Item2);
            }
        }

        services.RegisterServices(ServiceRoutes);
    }

    public static IEnumerable<Type> ScanServiceAssemblies()
    {
        var assemblies = ServiceStackHost.InitOptions.ResolveAllServiceAssemblies();
        foreach (var asm in assemblies)
        {
            foreach (var commandType in asm.GetTypes().Where(x => x.HasInterface(typeof(IAsyncCommand))))
            {
                yield return commandType;
            }
        }
    }

    private ILogger<CommandsFeature>? log;

    public void Register(IAppHost appHost)
    {
        if (appHost is ServiceStackHost host)
            host.AddTimings = true;
        
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
            var requestBody = requestDto.ToSafeJson();
            log!.LogError(e, "{Command}({Request}) failed: {Message}", commandType.Name, requestBody, e.Message);

            var error = e.ToResponseStatus();
            error.StackTrace ??= e.StackTrace;
            AddCommandResult(new()
            {
                Name = commandType.Name,
                Ms = sw.ElapsedMilliseconds,
                At = DateTime.UtcNow,
                Request = requestBody,
                Error = error,
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
    
    public ConcurrentQueue<CommandResult> CommandResults { get; set; } = [];
    public ConcurrentQueue<CommandResult> CommandFailures { get; set; } = new();
    
    public ConcurrentDictionary<string, CommandSummary> CommandTotals { get; set; } = new();

    public void AddCommandResult(CommandResult result)
    {
        if (Ignore.Contains(result.Name))
            return;
        if (ShouldIgnore != null && ShouldIgnore(result))
            return;
        
        var ms = (int)(result.Ms ?? 0);
        if (result.Error == null)
        {
            CommandResults.Enqueue(result);
            while (CommandResults.Count > ResultsCapacity)
                CommandResults.TryDequeue(out _);

            CommandTotals.AddOrUpdate(result.Name, 
                _ => new CommandSummary
                {
                    Type = result.Type,
                    Name = result.Name, 
                    Count = 1, 
                    TotalMs = ms, 
                    MinMs = ms,
                    MaxMs = ms,
                    Timings = new([ms]),
                },
                (_, summary) => 
                {
                    summary.Count++;
                    summary.TotalMs += ms;
                    summary.MaxMs = Math.Max(summary.MaxMs, ms);
                    summary.MinMs = summary.MinMs < 0 ? ms : Math.Min(summary.MinMs, ms);
                    summary.Timings.Enqueue(ms);
                    while (summary.Timings.Count > TimingsCapacity)
                        summary.Timings.TryDequeue(out var _);
                    return summary;
                });
        }
        else
        {
            CommandFailures.Enqueue(result);
            while (CommandFailures.Count > FailuresCapacity)
                CommandFailures.TryDequeue(out _);

            CommandTotals.AddOrUpdate(result.Name, 
                _ => new CommandSummary { Name = result.Name, Failed = 1, Count = 0, 
                    TotalMs = 0, MinMs = -1, MaxMs = -1, LastError = result.Error?.Message },
                (_, summary) =>
                {
                    summary.Failed++;
                    summary.LastError = result.Error?.Message;
                    return summary;
                });
        }
    }

    public void AddRequest(object requestDto, object response, TimeSpan elapsed)
    {
        var name = requestDto.GetType().Name;
        if (Ignore.Contains(name))
            return;
        
        var ms = (int)elapsed.TotalMilliseconds;
        var error = response.GetResponseStatus();
        if (error == null)
        {
            AddCommandResult(new()
            {
                Type = "API",
                Name = name,
                Ms = ms,
                At = DateTime.UtcNow,
            });
        }
        else
        {
            AddCommandResult(new()
            {
                Type = "API",
                Name = name,
                Ms = ms,
                At = DateTime.UtcNow,
                Request = requestDto.ToSafeJson(),
                Error = error,
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

public class CommandResult
{
    public string Type { get; set; } = "CMD";
    public string Name { get; set; }
    public long? Ms { get; set; }
    public DateTime At { get; set; }
    public string Request { get; set; }
    public ResponseStatus? Error { get; set; }

    public CommandResult Clone(Action<CommandResult>? configure = null) => X.Apply(new CommandResult
    {
        Type = Type,
        Name = Name,
        Ms = Ms,
        At = At,
        Request = Request,
        Error = Error,
    }, configure);
}

public class CommandSummary
{
    public string Type { get; set; } = "CMD";
    public string Name { get; set; }
    public int Count { get; set; }
    public int Failed { get; set; }
    public int TotalMs { get; set; }
    public int MinMs { get; set; }
    public int MaxMs { get; set; }
    public double AverageMs => Count == 0 ? 0 : Math.Round(TotalMs / (double)Count, 2);
    public double MedianMs => Math.Round(Timings.Median(), 2);
    public string? LastError { get; set; }
    public ConcurrentQueue<int> Timings { get; set; } = new();
}

public class ViewCommands : IGet, IReturn<ViewCommandsResponse>
{
    public List<string>? Include { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
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
            LatestCommands = [..feature.CommandResults],
            LatestFailed = [..feature.CommandFailures],
            CommandTotals = [..feature.CommandTotals.Values]
        };

        if (request.Include?.Contains(nameof(ResponseStatus.StackTrace)) != true)
        {
            to.LatestFailed = to.LatestFailed.Map(x => x.Clone(c =>
            {
                if (c.Error != null)
                    c.Error.StackTrace = null;
            }));
        }
        
        if (request.Skip != null)
        {
            to.LatestCommands = to.LatestCommands.Skip(request.Skip.Value).ToList();
            to.LatestFailed = to.LatestFailed.Skip(request.Skip.Value).ToList();
        }

        var take = request.Take ?? 50;
        to.LatestCommands = to.LatestCommands.Take(take).ToList();
        to.LatestFailed = to.LatestFailed.Take(take).ToList();
        
        return to;
    }
}

public static class CommandExtensions
{
    public static Task ExecuteAsync<TCommand, TRequest>(this ICommandExecutor executor, TRequest request) where TCommand : IAsyncCommand<TRequest>
    {
        var command = executor.Command<TCommand>();
        return executor.ExecuteAsync(command, request);
    }

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
    
    public static double Median(this IEnumerable<int> nums)
    {
        var array = nums.ToArray();
        if (array.Length == 0) return 0;
        if (array.Length == 1) return array[0];
        Array.Sort(array);
        var mid = Math.Min(array.Length / 2, array.Length - 1);
        return array.Length % 2 == 0 
            ? (array[mid] + array[mid - 1]) / 2.0 
            : array[mid];
    }    
}

#endif
