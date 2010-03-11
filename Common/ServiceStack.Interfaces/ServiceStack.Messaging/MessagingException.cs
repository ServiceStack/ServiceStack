using System;

namespace ServiceStack.Messaging
{
	public class MessagingException
		: Exception, IMessageError
	{
		public string ErrorCode { get; set; }
	}
}