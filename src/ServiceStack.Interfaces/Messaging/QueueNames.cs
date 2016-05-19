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

			Priority = "mq:" + typeof(T).Name + ".priorityq";
			PriorityBytes = utf8.GetBytes(Priority);
			In = "mq:" + typeof(T).Name + ".inq";
			InBytes = utf8.GetBytes(In);
			Out = "mq:" + typeof(T).Name + ".outq";
			OutBytes = utf8.GetBytes(Out);
			Dlq = "mq:" + typeof(T).Name + ".dlq";
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
	}

	/// <summary>
	/// Util class to create unique queue names for runtime types
	/// </summary>
	public class QueueNames
	{
		public static string TopicIn = "mq:topic:in";
		public static string TopicOut = "mq:topic:out";
		public static string QueuePrefix = "";

		public static void SetQueuePrefix(string prefix)
		{
			TopicIn = prefix + "mq:topic:in";
			TopicOut = prefix + "mq:topic:out";
			QueuePrefix = prefix;
		}

		private readonly Type messageType;

		public QueueNames(Type messageType)
		{
			this.messageType = messageType;
		}

		public string Priority
		{
			get { return QueuePrefix + "mq:" + messageType.Name + ".priorityq"; }
		}

		public string In
		{
			get { return QueuePrefix + "mq:" + messageType.Name + ".inq"; }
		}

		public string Out
		{
			get { return QueuePrefix + "mq:" + messageType.Name + ".outq"; }
		}

		public string Dlq
		{
			get { return QueuePrefix + "mq:" + messageType.Name + ".dlq"; }
		}
	}

}