#nullable enable
using System;
using System.Threading.Tasks;

namespace ServiceStack;

public interface ICommandAsync<in T> where T : class
{
    Task ExecuteAsync(T request);
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class CommandAttribute : AttributeBase
{
}
