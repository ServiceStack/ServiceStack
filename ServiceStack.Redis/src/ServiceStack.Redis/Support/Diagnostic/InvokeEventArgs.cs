using System;
using System.Reflection;

namespace ServiceStack.Redis.Support.Diagnostic
{
    /// <summary>
    /// Provides access to the method reflection data as part of the before/after event
    /// </summary>
    public class InvokeEventArgs : EventArgs
    {
        public MethodInfo MethodInfo { get; private set; }

        public InvokeEventArgs(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
        }
    }
}