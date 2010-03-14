using System;
using ServiceStack.DesignPatterns.Model;

namespace ServiceStack.Messaging
{
	public interface IMessage<T>
		: IHasId<Guid>
	{
		DateTime CreatedDate { get; }

		long Priority { get; set; }

		int RetryAttempts { get; }

		string ReplyTo { get; set; }

		IMessageError Error { get; }

		T Body { get; }
	}
}