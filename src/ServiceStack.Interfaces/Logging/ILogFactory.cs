using System;

namespace ServiceStack.Logging
{
    /// <summary>
    /// Factory to create ILog instances
    /// </summary>
    public interface ILogFactory
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        ILog GetLogger(Type type);

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        ILog GetLogger(string typeName);
    }
}
