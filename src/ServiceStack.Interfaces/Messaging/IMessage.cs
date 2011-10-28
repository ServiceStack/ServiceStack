using System;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Messaging
{
	public interface IMessage
		: IHasId<Guid>
	{
		DateTime CreatedDate { get; }

		long Priority { get; set; }

		int RetryAttempts { get; set; }

		Guid? ReplyId { get; set; }

		string ReplyTo { get; set; }

		MessageError Error { get; set; }
	}

	public interface IMessage<T>
		: IMessage
	{
		T Body { get; }
	}
}