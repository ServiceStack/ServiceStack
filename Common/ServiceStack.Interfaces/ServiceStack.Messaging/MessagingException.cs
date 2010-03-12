using System;
using System.Runtime.Serialization;

namespace ServiceStack.Messaging
{
	public class MessagingException
		: Exception, IMessageError
	{
		public MessagingException()
		{
		}

		public MessagingException(string message) : base(message)
		{
		}

		public MessagingException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected MessagingException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public string ErrorCode { get; set; }
	}
}