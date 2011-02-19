using System;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Messaging
{
	public interface IMessage
		: IHasId<Guid>
	{
		DateTime CreatedDate { get; }

		long Priority { get; set; }

		int RetryAttempts { get; }

		string ReplyTo { get; set; }

		MessageError Error { get; }
	}

	public interface IMessage<T>
		: IMessage
	{
		T Body { get; }
	}
}