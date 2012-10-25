using System;
using ServiceStack.Logging;

namespace ServiceStack.Common.Utils
{
    public static class FuncUtils
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FuncUtils));

        /// <summary>
        /// Invokes the action provided and returns true if no excpetion was thrown.
        /// Otherwise logs the exception and returns false if an exception was thrown.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static bool TryExec(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return false;
        }

        public static T TryExec<T>(Func<T> func)
        {
            return TryExec(func, default(T));
        }

        public static T TryExec<T>(Func<T> func, T defaultValue)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
            return default(T);
        }
    }

}