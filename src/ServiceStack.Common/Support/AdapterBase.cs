using System;
using ServiceStack.Logging;

namespace ServiceStack.Common.Support
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
#if !NETFX_CORE && !WINDOWS_PHONE
            this.Log.DebugFormat("Executing action '{0}'", action.Method.Name);
#endif
            try
            {
                T result = action();
                TimeSpan timeTaken = DateTime.UtcNow - before;
#if !NETFX_CORE && !WINDOWS_PHONE
                this.Log.DebugFormat("Action '{0}' executed. Took {1} ms.", action.Method.Name, timeTaken.TotalMilliseconds);
#endif
                return result;
            }
            catch (Exception ex)
            {
#if !NETFX_CORE && !WINDOWS_PHONE
                this.Log.ErrorFormat("There was an error executing Action '{0}'. Message: {1}", action.Method.Name, ex.Message);
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
#if !NETFX_CORE && !WINDOWS_PHONE
            this.Log.DebugFormat("Executing action '{0}'", action.Method.Name);
#endif
            try
            {
                action();
                TimeSpan timeTaken = DateTime.UtcNow - before;
#if !NETFX_CORE && !WINDOWS_PHONE
                this.Log.DebugFormat("Action '{0}' executed. Took {1} ms.", action.Method.Name, timeTaken.TotalMilliseconds);
#endif
            }
            catch (Exception ex)
            {
#if !NETFX_CORE && !WINDOWS_PHONE
                this.Log.ErrorFormat("There was an error executing Action '{0}'. Message: {1}", action.Method.Name, ex.Message);
#endif
                throw;
            }
        }
    }
}