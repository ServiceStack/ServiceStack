using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack
{
    public static class DisposableExtensions
    {
        public static void Dispose(this IEnumerable<IDisposable> resources, ILog log)
        {
            foreach (var disposable in resources)
            {
                if (disposable == null)
                    continue;

                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    log?.Error($"Error disposing of '{disposable.GetType().FullName}'", ex);
                }
            }
        }

        public static void Dispose(this IEnumerable<IDisposable> resources)
        {
            Dispose(resources, null);
        }

        public static void Dispose(params IDisposable[] disposables)
        {
            Dispose(disposables, null);
        }

        public static void Run<T>(this T disposable, Action<T> runActionThenDispose)
            where T : IDisposable
        {
            using (disposable)
            {
                runActionThenDispose(disposable);
            }
        }
    }
}