using System;
using ServiceStack.Logging;

namespace ServiceStack.Logging.NLogger
{
    /// <summary>
    /// ILogFactory that creates an NLog ILog logger
    /// </summary>
    public class NLogFactory : ServiceStack.Logging.ILogFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NLogFactory"/> class.
        /// </summary>
        public NLogFactory() { }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public ILog GetLogger(Type type)
        {
            return new NLogLogger(type);
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public ILog GetLogger(string typeName)
        {
            return new NLogLogger(typeName);
        }
    }
}
