using System;
using System.Text;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Util static generic class to create unique queue names for types
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class QueueNames<T>
    {
        static QueueNames() 
        {
            var utf8 = new UTF8Encoding(false);

            Priority = QueueNames.ResolveQueueNameFn(typeof(T).Name, ".priorityq");
            PriorityBytes = utf8.GetBytes(Priority);
            In = QueueNames.ResolveQueueNameFn(typeof(T).Name, ".inq");
            InBytes = utf8.GetBytes(In);
            Out = QueueNames.ResolveQueueNameFn(typeof(T).Name, ".outq");
            OutBytes = utf8.GetBytes(Out);
            Dlq = QueueNames.ResolveQueueNameFn(typeof(T).Name, ".dlq");
            DlqBytes = utf8.GetBytes(Dlq);
        }

        public static string Priority { get; private set; }
        public static byte[] PriorityBytes { get; private set; }

        public static string In { get; private set; }
        public static byte[] InBytes { get; private set; }

        public static string Out { get; private set; }
        public static byte[] OutBytes { get; private set; }

        public static string Dlq { get; private set; }
        public static byte[] DlqBytes { get; private set; }

        public static string[] AllQueueNames
        {
            get
            {
                return new[] {
                    In,
                    Priority,
                    Out,
                    Dlq,
                };
            }
        }
    }

    /// <summary>
    /// Util class to create unique queue names for runtime types
    /// </summary>
    public class QueueNames
    {
        public static string Exchange = "mx.servicestack";
        public static string ExchangeDlq = "mx.servicestack.dlq";
        public static string ExchangeTopic = "mx.servicestack.topic";

        public static string MqPrefix = "mq:";
        public static string QueuePrefix = "";

        public static string TempMqPrefix = MqPrefix + "tmp:";
        public static string TopicIn = MqPrefix + "topic:in";
        public static string TopicOut = MqPrefix + "topic:out";

        public static Func<string, string, string> ResolveQueueNameFn = ResolveQueueName;

        public static string ResolveQueueName(string typeName, string queueSuffix)
        {
            return QueuePrefix + MqPrefix + typeName + queueSuffix;
        }

        public static bool IsTempQueue(string queueName)
        {
            return queueName != null 
                && queueName.StartsWith(TempMqPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static void SetQueuePrefix(string prefix)
        {
            TopicIn = prefix + MqPrefix + "topic:in";
            TopicOut = prefix + MqPrefix + "topic:out";
            QueuePrefix = prefix;
            TempMqPrefix = prefix + MqPrefix + "tmp:";
        }

        private readonly Type messageType;

        public QueueNames(Type messageType)
        {
            this.messageType = messageType;
        }

        public string Priority
        {
            get { return ResolveQueueNameFn(messageType.Name, ".priorityq"); }
        }

        public string In
        {
            get { return ResolveQueueNameFn(messageType.Name, ".inq"); }
        }

        public string Out
        {
            get { return ResolveQueueNameFn(messageType.Name, ".outq"); }
        }

        public string Dlq
        {
            get { return ResolveQueueNameFn(messageType.Name, ".dlq"); }
        }

        public static string GetTempQueueName()
        {
            return TempMqPrefix + Guid.NewGuid().ToString("n");
        }
    }

}