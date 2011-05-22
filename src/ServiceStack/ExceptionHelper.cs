using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ServiceStack
{
    /// <summary>
    /// Provides extension method for preserving the stacktrace of an exception that needs to be thrown again.
    /// </summary>
    internal static class ExceptionHelper
    {
        private static Action<Exception> _preserveInternalException;

        static ExceptionHelper()
        {
            MethodInfo preserveStackTrace = typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
            _preserveInternalException = (Action<Exception>)Delegate.CreateDelegate(typeof(Action<Exception>), preserveStackTrace);
        }

        /// <summary>
        /// Will cause the current stacktrace of the exception to be preserved even if you throw it again.
        /// </summary>
        /// <param name="ex"></param>
        public static TException PreserveStackTrace<TException>(this TException ex)
            where TException : Exception
        {
            _preserveInternalException(ex);
            return ex;
        }
    }
}
