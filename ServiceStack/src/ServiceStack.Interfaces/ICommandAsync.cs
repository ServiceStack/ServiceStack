#nullable enable
using System;
using System.Threading.Tasks;

namespace ServiceStack;

public interface IAsyncCommand<in T> : IAsyncCommand
{
    Task ExecuteAsync(T request);
}
public interface IAsyncCommand { }

public interface ICommandExecutor
{
    TCommand Command<TCommand>() where TCommand : IAsyncCommand;
    Task ExecuteAsync<TRequest>(IAsyncCommand<TRequest> command, TRequest request);
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CommandAttribute(Type commandType) : AttributeBase
{
    public Type CommandType { get; } = commandType;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CommandAttribute<T>() : CommandAttribute(typeof(T)) where T : IAsyncCommand;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
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

public enum RetryStrategy
{
    /// <summary>
    /// Specifies that the operation should be retried with a linear backoff strategy.
    /// </summary>
    LinearBackoff,

    /// <summary>
    /// Specifies that the operation should be retried with an exponential backoff strategy.
    /// </summary>
    ExponentialBackoff,
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class RetryAttribute(int times)
    : AttributeBase
{
    public int Times => times;
}
