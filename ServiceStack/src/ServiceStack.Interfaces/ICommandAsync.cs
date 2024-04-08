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
public class CommandAttribute(Type commandType, Lifetime lifetime = Lifetime.Transient) : AttributeBase
{
    public Type CommandType { get; } = commandType;
    public Lifetime Lifetime { get; } = lifetime;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CommandAttribute<T>(Lifetime lifetime = Lifetime.Transient) 
    : CommandAttribute(typeof(T), lifetime) where T : IAsyncCommand;

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
