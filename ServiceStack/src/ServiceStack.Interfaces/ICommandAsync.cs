#nullable enable
using System;
using System.Threading.Tasks;

namespace ServiceStack;

public class NoArgs { }
public abstract class AsyncCommand : IAsyncCommand<NoArgs>
{
    public Task ExecuteAsync(NoArgs request) => ExecuteAsync();
    protected abstract Task ExecuteAsync();
}

public interface IAsyncCommand<in T> : IAsyncCommand
{
    Task ExecuteAsync(T request);
}
public interface IAsyncCommand { }

public interface IHasResult<out T>
{
    T Result { get; }
}
public interface IAsyncCommand<in TRequest, out TResult> : IAsyncCommand<TRequest>, IHasResult<TResult> { }

public interface ICommandExecutor
{
    TCommand Command<TCommand>() where TCommand : IAsyncCommand;
    Task ExecuteAsync<TRequest>(IAsyncCommand<TRequest> command, TRequest request);
    Task<TResult> ExecuteWithResultAsync<TRequest, TResult>(IAsyncCommand<TRequest, TResult> command, TRequest request);
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CommandAttribute(Type commandType) : AttributeBase
{
    public Type CommandType { get; } = commandType;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CommandAttribute<T>() : CommandAttribute(typeof(T)) where T : IAsyncCommand;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class LifetimeAttribute(Lifetime lifetime = Lifetime.Transient)
    : AttributeBase
{
    public Lifetime Lifetime => lifetime;
}

public enum Lifetime
{
    /// <summary>
    /// Specifies that a single instance of the service will be created.
    /// </summary>
    Singleton,
    
    /// <summary>
    /// Specifies that a new instance of the service will be created for each scope.
    /// </summary>
    /// <remarks>
    /// In ASP.NET Core applications a scope is created around each server request.
    /// </remarks>
    Scoped,
    
    /// <summary>
    /// Specifies that a new instance of the service will be created every time it is requested.
    /// </summary>
    Transient,
}

public enum RetryBehavior
{
    /// <summary>
    /// Use the default retry behavior.
    /// </summary>
    Default,
    
    /// <summary>
    /// Always retry the operation after the same delay.
    /// </summary>
    Standard,
    
    /// <summary>
    /// Specifies that the operation should be retried with a linear backoff delay strategy.
    /// </summary>
    LinearBackoff,

    /// <summary>
    /// Specifies that the operation should be retried with an exponential backoff strategy.
    /// </summary>
    ExponentialBackoff,

    /// <summary>
    /// Specifies that the operation should be retried with a full jittered exponential backoff strategy.
    /// </summary>
    FullJitterBackoff,
}

/// <summary>
/// Specifies that the operation should be retried if it fails.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class RetryAttribute
    : AttributeBase
{
    /// <summary>
    /// How many times to retry the operation.
    /// </summary>
    public int Times { get; set; } = -1;
    /// <summary>
    /// The retry behavior to use.
    /// </summary>
    public RetryBehavior Behavior { get; set; } = RetryBehavior.Default;
    /// <summary>
    /// The initial delay in milliseconds.
    /// </summary>
    public int DelayMs { get; set; } = -1;
    /// <summary>
    /// The maximum delay in milliseconds.
    /// </summary>
    public int MaxDelayMs { get; set; } = -1;
    /// <summary>
    /// Whether to delay the first retry.
    /// </summary>
    public bool DelayFirst { get; set; } = false;
}

/// <summary>
/// The Retry policy to use for the operation 
/// </summary>
/// <param name="Times">How many times to retry the operation</param>
/// <param name="Behavior">The retry behavior to use</param>
/// <param name="DelayMs">The initial delay in milliseconds</param>
/// <param name="MaxDelayMs">The maximum delay in milliseconds</param>
/// <param name="DelayFirst">Whether to delay the first retry</param>
public record struct RetryPolicy
(
    int Times,
    RetryBehavior Behavior,
    int DelayMs,
    int MaxDelayMs,
    bool DelayFirst
);