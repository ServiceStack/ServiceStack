using System;
using Serilog;

namespace ServiceStack.Logging.Serilog
{
    /// <summary>
    /// Implementation of <see cref="ILogFactory"/> that creates a <see cref="Serilog"/> <see cref="ILog"/> Logger.
    /// </summary>
    public class SerilogFactory : ILogFactory
    {
        private readonly ILogger logger;
        public ILogger Logger => logger;

        public SerilogFactory() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogFactory"/> class.
        /// </summary>
        public SerilogFactory(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public ILog GetLogger(Type type)
        {
            return logger != null
                ? new SerilogLogger(logger.ForContext(type))
                : new SerilogLogger(type);
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public ILog GetLogger(string typeName)
        {
            return GetLogger(Type.GetType(typeName));
        }
    }
}
