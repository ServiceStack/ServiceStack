using System;

namespace ServiceStack.Logging.Serilog
{
    /// <summary>
    /// Implementation of <see cref="ILogFactory"/> that creates a <see cref="Serilog"/> <see cref="ILog"/> Logger.
    /// </summary>
    public class SerilogFactory : ILogFactory
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public ILog GetLogger(Type type)
        {
            return new SerilogLogger(type);
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public ILog GetLogger(string typeName)
        {
            return new SerilogLogger(Type.GetType(typeName));
        }
    }
}
