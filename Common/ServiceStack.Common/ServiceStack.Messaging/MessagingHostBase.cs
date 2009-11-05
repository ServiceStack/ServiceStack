using Funq;

namespace ServiceStack.Messaging
{
	public abstract class MessagingHostBase
		: IFunqlet
	{
		public void Init()
		{
			
		}

		public abstract void Configure(Container container);
	}
}
