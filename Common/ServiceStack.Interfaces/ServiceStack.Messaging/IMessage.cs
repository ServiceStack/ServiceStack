using System;

namespace ServiceStack.Messaging
{
	public interface IMessage<T> 
	{
		int MessageId { get; }
		DateTime CreatedDate { get; }
		int RetryCount { get; }
		T Body { get; }
	}
}