using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Messaging
{
	public interface IMessageProducer
		: IDisposable
	{
		void Publish<T>(T processMessageFn);
		void Publish<T>(IMessage<T> processMessageFn);
	}

}
