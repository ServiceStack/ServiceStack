using System;

namespace ServiceStack.Messaging
{
	public interface IMessage<T>
	{
		Guid MessageId { get; }

		DateTime CreatedDate { get; }

		long Priority { get; set; }

		int RetryAttempts { get; }

		string ReplyTo { get; set; }

		IMessageError Error { get; }

		T Body { get; }
	}
}