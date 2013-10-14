using System;

namespace ServiceStack.Logging.EventLog
{
    /// <summary>
    /// ILogFactory used to create an EventLogger
    /// </summary>
    public class EventLogFactory : ILogFactory
    {
        private readonly string eventLogName;
        private readonly string eventLogSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogFactory"/> class.
        /// </summary>
        /// <param name="eventLogName">Name of the event log.</param>
        public EventLogFactory(string eventLogName) : this(eventLogName, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogFactory"/> class.
        /// </summary>
        /// <param name="eventLogName">Name of the event log. Default is 'ServiceStack.Logging.EventLog'</param>
        /// <param name="eventLogSource">The event log source. Default is 'Application'</param>
        public EventLogFactory(string eventLogName, string eventLogSource)
        {
            this.eventLogName = eventLogName ?? "ServiceStack.Logging.EventLog";
            this.eventLogSource = eventLogSource ?? "Application";
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public ILog GetLogger(Type type)
        {
            return GetLogger(type.ToString());
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns></returns>
        public ILog GetLogger(string typeName)
        {
            return new EventLogger(eventLogName, eventLogSource);
        }
    }
}