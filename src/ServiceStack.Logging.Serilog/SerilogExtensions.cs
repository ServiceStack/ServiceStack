namespace ServiceStack.Logging.Serilog
{
    using System;
    using System.Collections.Generic;
    using global::Serilog.Core;

    public static class SerilogExtensions
    {

        /// <summary>
        /// Create a logger that marks log events as being from the specified
        /// source type.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="source">Type generating log messages in the context.</param>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public static ILog ForContext(this ILog logger, Type source)
        {
            return (logger as SerilogLogger)?.ForContext(source);
        }

        /// <summary>
        /// Create a logger that marks log events as being from the specified
        /// source type.
        /// </summary>
        /// <typeparam name="TSource">Type generating log messages in the context.</typeparam>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public static ILog ForContext<TSource>(this ILog logger)
        {
            return (logger as SerilogLogger)?.ForContext<TSource>();
        }

        /// <summary>
        /// Create a logger that enriches log events via the provided enrichers.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="enricher">Enricher that applies in the context.</param>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public static ILog ForContext(this ILog logger, ILogEventEnricher enricher)
        {
            return (logger as SerilogLogger)?.ForContext(enricher);
        }

        /// <summary>
        /// Create a logger that enriches log events via the provided enrichers.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="enrichers">Enrichers that apply in the context.</param>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public static ILog ForContext(this ILog logger, IEnumerable<ILogEventEnricher> enrichers)
        {
            return (logger as SerilogLogger)?.ForContext(enrichers);
        }

        /// <summary>
        /// Create a logger that enriches log events with the specified property.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="propertyName">The name of the property. Must be non-empty.</param>
        /// <param name="value">The property value.</param>
        /// <param name="destructureObjects">If true, the value will be serialized as a structured
        /// object if possible; if false, the object will be recorded as a scalar or simple array.</param>
        /// <returns>A logger that will enrich log events as specified.</returns>
        public static ILog ForContext(this ILog logger, string propertyName, object value, bool destructureObjects = false)
        {
            return (logger as SerilogLogger)?.ForContext(propertyName, value, destructureObjects);
        }
    }
}