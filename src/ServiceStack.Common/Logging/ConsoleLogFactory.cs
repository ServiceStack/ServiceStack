#if !NETFX_CORE
using System;

namespace ServiceStack.Logging
{
    /// <summary>
    /// Creates a Console Logger, that logs all messages to: System.Console
    /// 
    /// Made public so its testable
    /// </summary>
    public class ConsoleLogFactory : ILogFactory
    {
        private readonly bool debugEnabled;

        public ConsoleLogFactory(bool debugEnabled = true)
        {
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
            return new ConsoleLogger(type) { IsDebugEnabled = debugEnabled };
        }

        public ILog GetLogger(string typeName)
        {
            return new ConsoleLogger(typeName) { IsDebugEnabled = debugEnabled };
        }
    }
}
#endif
