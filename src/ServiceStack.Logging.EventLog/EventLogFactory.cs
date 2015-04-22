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
        private readonly bool debugEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogFactory"/> class.
        /// </summary>
        /// <param name="eventLogName">Name of the event log. Default is 'ServiceStack.Logging.EventLog'</param>
        /// <param name="eventLogSource">The event log source. Default is 'Application'</param>
        /// <param name="debugEnabled">Whether to write EventLog entries for DEBUG logs</param>
        public EventLogFactory(string eventLogName = null, string eventLogSource = null, bool debugEnabled = false)
        {
            this.eventLogName = eventLogName ?? "ServiceStack.Logging.EventLog";
            this.eventLogSource = eventLogSource ?? "Application";
            this.debugEnabled = debugEnabled;
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
            return new EventLogger(eventLogName, eventLogSource)
            {
                IsDebugEnabled = debugEnabled
            };
        }
    }
}