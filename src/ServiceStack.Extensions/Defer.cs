using System;

namespace ServiceStack
{
    public struct Defer : IDisposable
    {
        private readonly Action fn;
        public Defer(Action fn) => this.fn = fn ?? throw new ArgumentNullException(nameof(fn));
        public void Dispose() => fn();
    }
}