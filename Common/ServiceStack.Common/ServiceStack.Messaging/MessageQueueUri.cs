namespace ServiceStack.Messaging
{
	public class MessageQueueUri
	{
		public string HostName { get; set; }
		public string ServiceName { get; set; }

		private	string QueueName
		{
			get
			{
				return this.HostName + "." + this.ServiceName;
			}
		}

		public string ExchangeQueueName
		{
			get
			{
				return this.QueueName + ".MX";
			}
		}

		public string InboxQueueName
		{
			get
			{
				return this.QueueName + ".IN";
			}
		}


		public string OutboxQueueName
		{
			get
			{
				return this.QueueName + ".OUT";
			}
		}

		public string Uri
		{
			get
			{
				return string.Format("amqp://{0}.{1}", 
					this.HostName, this.ServiceName);
			}
		}

		public static string Parse(string uri)
		{
			var protocolPos = uri.IndexOf("://");
			if (protocolPos != -1)
			{
			}
			return null;
		}
	}
}