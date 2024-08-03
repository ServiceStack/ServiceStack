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
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.Model;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack;

public class CommandsFeature : IPlugin, IConfigureServices, IHasStringId, IPreInitPlugin
{
    public string Id => Plugins.AdminCommands;

    /// <summary>
    /// Limit API access to users in role
    /// </summary>
    public string AccessRole { get; set; } = RoleNames.Admin;

    public const int DefaultCapacity = 250;
    public int ResultsCapacity { get; set; } = DefaultCapacity;
    public int FailuresCapacity { get; set; } = DefaultCapacity;
    public int TimingsCapacity { get; set; } = 1000;
    public RetryPolicy DefaultRetryPolicy { get; set; } = new(
        Times:3, Behavior:RetryBehavior.FullJitterBackoff, DelayMs:100, MaxDelayMs:60_000, DelayFirst:false);
    
    public Func<Exception, bool> SkipRetryingExceptions { get; set; } = ex => false;

    public List<Type> SkipRetryingExceptionTypes { get; set; } = [
        typeof(ArgumentException),
        typeof(ArgumentNullException),
        typeof(ValidationException),
        typeof(ValidationError),
    ];

    public List<Func<IEnumerable<Type>>> TypeResolvers { get; set; } = [
        ScanServiceAssemblies
    ];
    
    /// <summary>
    /// Ignore commands or Request DTOs from being logged
    /// </summary>
    public List<string> Ignore { get; set; } = [
        nameof(ViewCommands),
        nameof(ExecuteCommand),
    ];
    
    public Func<CommandResult,bool>? ShouldIgnore { get; set; }

    public List<Type> RegisterServices { get; set; } = [
        typeof(CommandsService),
    ];

    public List<CommandInfo> CommandInfos { get; set; } = [];

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
    
    public Type GetRequestType(Type commandType)
    {
        var executeMethod = commandType.GetMethods()
            .FirstOrDefault(x => x.Name == "ExecuteAsync" && x.GetParameters().Length == 1);
        if (executeMethod == null)
            throw new NotSupportedException("ExecuteAsync method not found on " + commandType.Name);
        return executeMethod.GetParameters()[0].ParameterType;
    }

    public void Configure(IServiceCollection services)
    {
        services.AddTransient<ICommandExecutor>(c => new CommandExecutor(this, c));

        foreach (var typeResolver in TypeResolvers)
        {
            var commandTypes = typeResolver();
            foreach (var commandType in commandTypes)
            {
                var asyncCommand = commandType.IsOrHasGenericInterfaceTypeOf(typeof(IAsyncCommand<>));
                if (!asyncCommand)
                    continue;

                if (CommandInfos.All(x => x.Name != commandType.Name))
                {
                    var requestType = GetRequestType(commandType);
                    var info = new CommandInfo
                    {
                        Type = commandType,
                        Name = commandType.Name,
                        Tag = commandType.GetCustomAttribute<TagAttribute>()?.Name,
                        Request = requestType.ToMetadataType(),
                    };
                    CommandInfos.Add(info);
                }
                
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

        foreach (var serviceType in RegisterServices)
        {
            services.RegisterService(serviceType);
        }
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

    public ValidationFeature? ValidationFeature { get; set; }

    public ILogger<CommandsFeature>? Log { get; set; }

    public void Register(IAppHost appHost)
    {
        if (appHost is ServiceStackHost host)
            host.AddTimings = true;

        ValidationFeature ??= appHost.GetPlugin<ValidationFeature>();
        Log ??= appHost.GetApplicationServices().GetRequiredService<ILogger<CommandsFeature>>();
        
        appHost.AddToAppMetadata(meta =>
        {
            meta.Plugins.Commands = new()
            {
                Commands = CommandInfos,
            };
        });
    }

    class CommandExecutor(CommandsFeature feature, IServiceProvider services) : ICommandExecutor
    {
        public TCommand Command<TCommand>() where TCommand : IAsyncCommand => services.GetRequiredService<TCommand>();

        public Task ExecuteAsync<T>(IAsyncCommand<T> command, T request)
        {
            return feature.ExecuteCommandAsync(command, request);
        }

        public async Task<TResult> ExecuteWithResultAsync<TRequest, TResult>(IAsyncCommand<TRequest, TResult> command, TRequest request)
        {
            return await feature.ExecuteCommandWithResultAsync(command, request).ConfigAwait();
        }
    }

    public async Task ExecuteCommandAsync<TCommand, TRequest>(TCommand command, TRequest request) 
        where TCommand : IAsyncCommand<TRequest> 
    {
        ArgumentNullException.ThrowIfNull(request);
        await ExecuteCommandAsync(command.GetType(), dto => command.ExecuteAsync((TRequest)dto), request).ConfigAwait();
    }
    
    public async Task<TResult> ExecuteCommandWithResultAsync<TRequest, TResult>(IAsyncCommand<TRequest, TResult> command, TRequest request) 
    {
        ArgumentNullException.ThrowIfNull(request);
        await ExecuteCommandAsync(command.GetType(), dto => command.ExecuteAsync((TRequest)dto), request).ConfigAwait();
        return command.Result;
    }
    
    public RetryPolicy? GetRetryPolicy(Type commandType)
    {
        var retryAttr = commandType.FirstAttribute<RetryAttribute>();
        if (retryAttr != null)
        {
            return new(
                Times:retryAttr.Times > 0 ? retryAttr.Times : DefaultRetryPolicy.Times,
                Behavior:retryAttr.Behavior == RetryBehavior.Default ? DefaultRetryPolicy.Behavior : retryAttr.Behavior,
                DelayMs:retryAttr.DelayMs > 0 ? retryAttr.DelayMs : DefaultRetryPolicy.DelayMs,
                MaxDelayMs:retryAttr.MaxDelayMs > 0 ? retryAttr.MaxDelayMs : DefaultRetryPolicy.MaxDelayMs,
                DelayFirst:retryAttr.DelayFirst
            );
        }
        return null;
    }

    public async Task<CommandResult> ExecuteCommandAsync(Type commandType, Func<object,Task> execFn, object requestDto)
    {
        var result = new CommandResult { Type = CommandResult.Command, Name = commandType.Name, At = DateTime.UtcNow };
        RetryPolicy? retryPolicy = null;
        var retries = 0;
        var sw = Stopwatch.StartNew();
        
        while (true)
        {
            try
            {
                if (ValidationFeature != null)
                {
                    await ValidationFeature.ValidateRequestAsync(requestDto, new BasicHttpRequest());
                }
        
                await execFn(requestDto);
                Log!.LogDebug("{Command} took {ElapsedMilliseconds}ms to execute", commandType.Name, sw.ElapsedMilliseconds);

                result.Ms = sw.ElapsedMilliseconds;
                if (retries > 0)
                    result.Retries = retries;
                AddCommandResult(result);
                return result;
            }
            catch (Exception e)
            {
                var attempt = retries + 1;
                var requestBody = requestDto.ToSafeJson();
                Log!.LogError(e, "{Command}({Request}) x{Attempt} failed: {Message}", 
                    commandType.Name, requestBody, attempt, e.Message);

                var errorResult = result.Clone();
                errorResult.Request = requestBody;
                errorResult.Attempt = attempt;
                errorResult.Error = e.ToResponseStatus();
                errorResult.Error.StackTrace ??= e.StackTrace;
                AddCommandResult(errorResult);

                // Only try commands annotated with [Retry]
                retryPolicy ??= GetRetryPolicy(commandType);
                if (retryPolicy == null)
                    return errorResult;

                if (SkipRetryingExceptions(e))
                    return errorResult;
                
                if (SkipRetryingExceptionTypes.Contains(e.GetType()))
                    return errorResult;

                // Handle WebServiceException
                if (e.InnerException != null && SkipRetryingExceptionTypes.Contains(e.InnerException.GetType()))
                    return errorResult;

                var retry = retryPolicy.Value;
                if (++retries > retry.Times)
                    return errorResult;

                var delayMs = ExecUtils.CalculateRetryDelayMs(attempt, retry);
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs);
                }
            }
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
            await ExecuteCommandAsync(oCommand, requestProp);
        }
    }

    public async Task<CommandResult> ExecuteCommandAsync(object oCommand, object commandRequest)
    {
        var commandType = oCommand.GetType();
        var method = commandType.GetMethod("ExecuteAsync")
            ?? throw new NotSupportedException("ExecuteAsync method not found on " + commandType.Name);
                
        async Task Exec(object commandArg)
        {
            var methodInvoker = GetInvoker(method);
            await methodInvoker(oCommand, commandArg);
        }

        return await ExecuteCommandAsync(commandType, Exec, commandRequest);
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
                    Failed = 0,
                    Retries = 0,
                    TotalMs = ms, 
                    MinMs = ms,
                    MaxMs = ms,
                    Timings = new([ms]),
                },
                (_, summary) => 
                {
                    summary.Count++;
                    if (result.Retries != null)
                        summary.Retries += result.Retries.Value;
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
                _ => new CommandSummary {
                    Type = result.Type,
                    Name = result.Name, 
                    Failed = 1, 
                    Retries = result.Retries.GetValueOrDefault(0), 
                    Count = 0, 
                    TotalMs = 0, 
                    MinMs = -1, 
                    MaxMs = -1, 
                    LastError = result.Error 
                },
                (_, summary) =>
                {
                    summary.Failed++;
                    if (result.Retries != null)
                        summary.Retries += result.Retries.Value;
                    summary.LastError = result.Error;
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
                Type = CommandResult.Api,
                Name = name,
                Ms = ms,
                At = DateTime.UtcNow,
            });
        }
        else
        {
            AddCommandResult(new()
            {
                Type = CommandResult.Api,
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

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature => {
            feature.AddAdminLink(AdminUiFeature.Commands, new LinkInfo {
                Id = "commands",
                Label = "Commands",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Command)),
                Show = $"role:{AccessRole}",
            });
        });
    }
}

public class CommandResult
{
    public const string Command = "CMD";
    public const string Api = "API";

    public string Type { get; set; } = Command;
    public string Name { get; set; }
    public long? Ms { get; set; }
    public DateTime At { get; set; }
    public string Request { get; set; }
    public int? Retries { get; set; }
    public int Attempt { get; set; }
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
    public string Type { get; set; } = CommandResult.Command;
    public string Name { get; set; }
    public int Count { get; set; }
    public int Failed { get; set; }
    public int Retries { get; set; }
    public int TotalMs { get; set; }
    public int MinMs { get; set; }
    public int MaxMs { get; set; }
    public double AverageMs => Count == 0 ? 0 : Math.Round(TotalMs / (double)Count, 2);
    public double MedianMs => Math.Round(Timings.Median(), 2);
    public ResponseStatus? LastError { get; set; }
    public ConcurrentQueue<int> Timings { get; set; } = new();
}

[ExcludeMetadata]
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

[ExcludeMetadata]
public class ExecuteCommand : IPost, IReturn<ExecuteCommandResponse>
{
    public string Command { get; set; }
    public string? RequestJson { get; set; }
}
public class ExecuteCommandResponse
{
    public CommandResult? CommandResult { get; set; }
    public string? Result { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}
    
[Restrict(VisibilityTo = RequestAttributes.Localhost)]
public class CommandsService(ILogger<CommandsService> log) : Service
{
    public async Task<object> Any(ViewCommands request)
    {
        var feature = HostContext.AssertPlugin<CommandsFeature>();
        if (!HostContext.DebugMode)
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AccessRole);

        var to = new ViewCommandsResponse
        {
            LatestCommands = [..feature.CommandResults.OrderByDescending(x => x.At)],
            LatestFailed = [..feature.CommandFailures.OrderByDescending(x => x.At)],
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

    public async Task<object> Any(ExecuteCommand request)
    {
        var feature = HostContext.AssertPlugin<CommandsFeature>();
        if (!HostContext.DebugMode)
            await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AccessRole);

        var commandInfo = feature.CommandInfos.FirstOrDefault(x => x.Name == request.Command);
        if (commandInfo == null)
            throw HttpError.NotFound("Command does not exist");

        var commandType = commandInfo.Type;
        var requestType = commandInfo.Request.Type;
        var commandRequest = string.IsNullOrEmpty(request.RequestJson)
            ? requestType.CreateInstance()
            : Text.JsonSerializer.DeserializeFromString(request.RequestJson, requestType);

        var services = Request.GetServiceProvider();
        var command = services.GetRequiredService(commandType);
        
        var commandResult = await feature.ExecuteCommandAsync(command, commandRequest);

        var resultProp = commandType.GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
        var resultAccessor = TypeProperties.Get(commandType).GetAccessor("Result");
        var result = resultProp != null
            ? resultAccessor.PublicGetter(command)
            : null;
        string? resultString = null;

        try
        {
            resultString = result switch
            {
                null => null,
                string s => s,
                StringBuilder sb => sb.ToString(),
                _ => result.ToSafeJson(),
            };
        }
        catch (Exception e)
        {
            log.LogWarning("Couldn't serialize {CommandType} result {ResultType}: {Message}",
                commandType.Name, resultProp?.PropertyType.Name ?? "null", e.Message);
        }

        var to = new ExecuteCommandResponse
        {
            CommandResult = commandResult,
            Result = resultString,
        };
        
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
        
        var services = req.GetServiceProvider();
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
