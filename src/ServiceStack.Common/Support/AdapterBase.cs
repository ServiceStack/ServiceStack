using System;
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
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected T Execute<T>(Func<T> action)
        {
            DateTime before = DateTime.UtcNow;
#if !NETFX_CORE && !WP
            this.Log.Debug($"Executing action '{action.Method().Name}'");
#endif
            try
            {
                T result = action();
                TimeSpan timeTaken = DateTime.UtcNow - before;
#if !NETFX_CORE && !WP
                this.Log.Debug($"Action '{action.Method().Name}' executed. Took {timeTaken.TotalMilliseconds} ms.");
#endif
                return result;
            }
            catch (Exception ex)
            {
#if !NETFX_CORE && !WP
                this.Log.Error($"There was an error executing Action '{action.Method().Name}'. Message: {ex.Message}");
#endif
                throw;
            }
        }

        /// <summary>
        /// Executes the specified action (for void methods).
        /// </summary>
        /// <param name="action">The action.</param>
        protected void Execute(Action action)
        {
            DateTime before = DateTime.UtcNow;
#if !NETFX_CORE && !WP
            this.Log.Debug($"Executing action '{action.Method().Name}'");
#endif
            try
            {
                action();
                TimeSpan timeTaken = DateTime.UtcNow - before;
#if !NETFX_CORE && !WP
                this.Log.Debug($"Action '{action.Method().Name}' executed. Took {timeTaken.TotalMilliseconds} ms.");
#endif
            }
            catch (Exception ex)
            {
#if !NETFX_CORE && !WP
                this.Log.Error($"There was an error executing Action '{action.Method().Name}'. Message: {ex.Message}");
#endif
                throw;
            }
        }
    }
}