using System;

namespace ServiceStack.Messaging
{
	public class Message<T>
		: IMessage<T>
	{
		public Message()
		{
		}

		public Message(T body)
		{
			this.MessageId = Guid.NewGuid();
			this.CreatedDate = DateTime.UtcNow;
			Body = body;
		}

		public Guid MessageId { get; set; }

		public DateTime CreatedDate { get; set; }

		public long Priority { get; set; }

		public int RetryAttempts { get; set; }

		public string ReplyTo { get; set; }

		public IMessageError Error { get; set; }

		public T Body { get; set; }
	}
}