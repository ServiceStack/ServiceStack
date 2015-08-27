using System;

namespace ServiceStack.Logging
{
    /// <summary>
    /// Creates a Debug Logger, that logs all messages to: System.Diagnostics.Debug
    /// 
    /// Made public so its testable
    /// </summary>
    public class DebugLogFactory : ILogFactory
    {
        private readonly bool debugEnabled;

        public DebugLogFactory(bool debugEnabled = true)
        {
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
            return new DebugLogger(type) { IsDebugEnabled = debugEnabled };
        }

        public ILog GetLogger(string typeName)
        {
            return new DebugLogger(typeName) { IsDebugEnabled = debugEnabled };
        }
    }
}
