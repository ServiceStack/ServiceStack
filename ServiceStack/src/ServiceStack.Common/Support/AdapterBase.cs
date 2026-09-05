using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;

namespace ServiceStack.Support
{
    /// <summary>
    /// Common functionality when creating adapters
    /// </summary>
    public abstract class AdapterBase
    {
        protected abstract ILog Log { get; }

        /// <summary>
        /// Executes the specified expression. 
        /// </summary>
        protected T Execute<T>(Func<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var log = Log;
            var isDebug = log?.IsDebugEnabled == true;
            if (isDebug)
                log.Debug($"Executing action '{action.Method.Name}'");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                T result = action();
                if (isDebug)
                {
                    log.Debug($"Action '{action.Method.Name}' executed. Took {sw.Elapsed.TotalMilliseconds} ms.");
                }
                return result;
            }
            catch (Exception ex)
            {
                log?.Error($"There was an error executing Action '{action.Method.Name}'. Message: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the specified expression. 
        /// </summary>
        protected async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var log = Log;
            var isDebug = log?.IsDebugEnabled == true;
            if (isDebug)
                log.Debug($"Executing action '{action.Method.Name}'");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await action();
                if (isDebug)
                {
                    log.Debug($"Action '{action.Method.Name}' executed. Took {sw.Elapsed.TotalMilliseconds} ms.");
                }
                return result;
            }
            catch (Exception ex)
            {
                log?.Error($"There was an error executing Action '{action.Method.Name}'. Message: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the specified expression. 
        /// </summary>
        protected async Task<T> ExecuteAsync<T>(Func<CancellationToken,Task<T>> action, CancellationToken token)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var log = Log;
            var isDebug = log?.IsDebugEnabled == true;
            if (isDebug)
                log.Debug($"Executing action '{action.Method.Name}'");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await action(token);
                if (isDebug)
                {
                    log.Debug($"Action '{action.Method.Name}' executed. Took {sw.Elapsed.TotalMilliseconds} ms.");
                }
                return result;
            }
            catch (Exception ex)
            {
                log?.Error($"There was an error executing Action '{action.Method.Name}'. Message: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the specified action (for void methods).
        /// </summary>
        protected void Execute(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var log = Log;
            var isDebug = log?.IsDebugEnabled == true;
            if (isDebug)
                log.Debug($"Executing action '{action.Method.Name}'");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                action();
                if (isDebug)
                {
                    log.Debug($"Action '{action.Method.Name}' executed. Took {sw.Elapsed.TotalMilliseconds} ms.");
                }
            }
            catch (Exception ex)
            {
                log?.Error($"There was an error executing Action '{action.Method.Name}'. Message: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the specified action (for void methods).
        /// </summary>
        protected async Task ExecuteAsync(Func<Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var log = Log;
            var isDebug = log?.IsDebugEnabled == true;
            if (isDebug)
                log.Debug($"Executing action '{action.Method.Name}'");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await action();
                if (isDebug)
                {
                    log.Debug($"Action '{action.Method.Name}' executed. Took {sw.Elapsed.TotalMilliseconds} ms.");
                }
            }
            catch (Exception ex)
            {
                log?.Error($"There was an error executing Action '{action.Method.Name}'. Message: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Executes the specified action (for void methods).
        /// </summary>
        protected async Task ExecuteAsync(Func<CancellationToken,Task> action, CancellationToken token)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var log = Log;
            var isDebug = log?.IsDebugEnabled == true;
            if (isDebug)
                log.Debug($"Executing action '{action.Method.Name}'");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                await action(token);
                if (isDebug)
                {
                    log.Debug($"Action '{action.Method.Name}' executed. Took {sw.Elapsed.TotalMilliseconds} ms.");
                }
            }
            catch (Exception ex)
            {
                log?.Error($"There was an error executing Action '{action.Method.Name}'. Message: {ex.Message}", ex);
                throw;
            }
        }
    }
}