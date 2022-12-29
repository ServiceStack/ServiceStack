using System;

namespace ServiceStack
{
    /// <summary>
    /// Useful class for C# 8 using declaration to defer action til outside of scope, e.g:
    /// using var defer = new Defer(() => response.Close());
    /// </summary>
    public struct Defer : IDisposable
    {
        private readonly Action fn;
        public Defer(Action fn) => this.fn = fn ?? throw new ArgumentNullException(nameof(fn));
        public void Dispose() => fn();
    }
}