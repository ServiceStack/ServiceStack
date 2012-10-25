using System;
using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.Common
{
    public static class DisposableExtensions
    {
        public static void Dispose(this IEnumerable<IDisposable> resources, ILog log)
        {
            foreach (var disposable in resources)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    if (log != null)
                    {
                        log.Error(string.Format("Error disposing of '{0}'", disposable.GetType().FullName), ex);
                    }
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