using System;

namespace ServiceStack.Messaging
{
	/// <summary>
	/// Util static generic class to create unique queue names for types
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public static class QueueNames<T>
	{
		public static string Priority
		{
			get { return "mq:" + typeof(T).Name + ".priorityq"; }
		}

		public static string In
		{
			get { return "mq:" + typeof(T).Name + ".inq"; }
		}

		public static string Out
		{
			get { return "mq:" + typeof(T).Name + ".outq"; }
		}

		public static string Dlq
		{
			get { return "mq:" + typeof(T).Name + ".dlq"; }
		}
	}

	/// <summary>
	/// Util class to create unique queue names for runtime types
	/// </summary>
	public class QueueNames
	{
		private readonly Type messageType;

		public QueueNames(Type messageType)
		{
			this.messageType = messageType;
		}

		public string Priority
		{
			get { return "mq:" + messageType.Name + ".priorityq"; }
		}

		public string In
		{
			get { return "mq:" + messageType.Name + ".inq"; }
		}

		public string Out
		{
			get { return "mq:" + messageType.Name + ".outq"; }
		}

		public string Dlq
		{
			get { return "mq:" + messageType.Name + ".dlq"; }
		}
	}

}