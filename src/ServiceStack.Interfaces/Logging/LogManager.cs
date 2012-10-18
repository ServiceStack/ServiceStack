using System;
using ServiceStack.Logging.Support.Logging;

namespace ServiceStack.Logging
{
    /// <summary>
    /// Logging API for this library. You can inject your own implementation otherwise
    /// will use the DebugLogFactory to write to System.Diagnostics.Debug
    /// </summary>
    public class LogManager
    {
        private static ILogFactory logFactory;

        /// <summary>
        /// Gets or sets the log factory.
        /// Use this to override the factory that is used to create loggers
        /// </summary>
        /// <value>The log factory.</value>
        public static ILogFactory LogFactory
        {
            get
            {
                if (logFactory == null)
                {
                    return new DebugLogFactory();
                }
                return logFactory;
            }
            set { logFactory = value; }
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static ILog GetLogger(Type type)
        {
            return LogFactory.GetLogger(type);
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public static ILog GetLogger(string typeName)
        {
            return LogFactory.GetLogger(typeName);
        }
    }
}
