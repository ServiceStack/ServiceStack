namespace ServiceStack.Logging.Serilog
{
    using System;
    using System.Collections.Generic;
    using global::Serilog.Core;

    public static class SerilogExtensions
    {
        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Debug" /> level and associated exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>Log.Debug(ex, "Swallowing a mundane exception.");</example>
        public static void Debug(this ILog logger, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Debug(exception, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Information" /> level and associated exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public static void Info(this ILog logger, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Info(exception, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Warning" /> level and associated exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public static void Warn(this ILog logger, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Warn(exception, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Error" /> level and associated exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public static void Error(this ILog logger, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Error(exception, messageTemplate, propertyValues);
        }

        /// <summary>
        /// Write a log event with the <see cref="F:Serilog.Events.LogEventLevel.Fatal" /> level and associated exception.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>Log.Fatal(ex, "Process terminating.");</example>
        public static void Fatal(this ILog logger, Exception exception, string messageTemplate, params object[] propertyValues)
        {
            (logger as SerilogLogger)?.Fatal(exception, messageTemplate, propertyValues);
        }

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