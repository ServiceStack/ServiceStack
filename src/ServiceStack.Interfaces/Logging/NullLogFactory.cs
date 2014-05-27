using System;

namespace ServiceStack.Logging
{
    /// <summary>
    /// Creates a Debug Logger, that logs all messages to: System.Diagnostics.Debug
    /// 
    /// Made public so its testable
    /// </summary>
	public class NullLogFactory : ILogFactory
    {
        private readonly bool debugEnabled;

        public NullLogFactory(bool debugEnabled=false)
        {
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
			return new NullDebugLogger(type) { IsDebugEnabled = debugEnabled };
        }

        public ILog GetLogger(string typeName)
        {
            return new NullDebugLogger(typeName) { IsDebugEnabled = debugEnabled };
        }
    }
}
