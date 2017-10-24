#if NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ServiceStack.Web;

namespace System.Threading
{
    public static class ThreadExtensions
    {
        public static void Abort(this Thread thread)
        {
            MethodInfo abort = null;
            foreach (var m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
            }
            if (abort == null)
                throw new NotImplementedException();

            abort.Invoke(thread, new object[0]);
        }

        public static void Interrupt(this Thread thread)
        {
            MethodInfo interrupt = null;
            foreach (var m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name.Equals("InterruptInternal") && m.GetParameters().Length == 0) interrupt = m;
            }
            if (interrupt == null)
                throw new NotImplementedException();

            interrupt.Invoke(thread, new object[0]);
        }

        public static bool Join(this Thread thread, TimeSpan timeSpan)
        {
            return thread.Join((int)timeSpan.TotalMilliseconds);
        }
    }
}

namespace System.Configuration
{
    public class ConfigurationErrorsException : Exception
    {
        public ConfigurationErrorsException() {}
        public ConfigurationErrorsException(string message) : base(message) {}
        public ConfigurationErrorsException(string message, Exception innerException) 
            : base(message, innerException) {}
    }
}

namespace ServiceStack.Platforms
{
    public partial class PlatformNetCore : Platform
    {

    }
}

#endif
