#if !NETFRAMEWORK

using System;
using System.Reflection;

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

namespace ServiceStack
{
    // System.Configuration.ConfigurationManager also in https://www.nuget.org/packages/System.Configuration.ConfigurationManager/
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
