using System;

namespace ServiceStack.Messaging
{
	/// <summary>
	/// Basic implementation of IMessage[T]
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Message<T>
		: IMessage<T>
	{
		public Message()
		{
		}

		public Message(T body)
		{
			this.Id = Guid.NewGuid();
			this.CreatedDate = DateTime.UtcNow;
			Body = body;
		}

		public Guid Id { get; set; }

		public DateTime CreatedDate { get; set; }

		public long Priority { get; set; }

		public int RetryAttempts { get; set; }

		public string ReplyTo { get; set; }

		public IMessageError Error { get; set; }

		public T Body { get; set; }
	}
}