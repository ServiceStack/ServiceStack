using System;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace ServiceStack.Extensions.Tests;

/// <summary>
/// Logger provider that writes to NUnit's TestContext for visibility in test runners
/// </summary>
public class NUnitLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new NUnitLogger(categoryName);

    public void Dispose() { }

    private class NUnitLogger(string categoryName) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            TestContext.WriteLine($"[{logLevel}] {categoryName}: {message}");
            if (exception != null)
                TestContext.WriteLine(exception.ToString());
        }
    }
}