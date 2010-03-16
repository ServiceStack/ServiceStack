using System.Runtime.Serialization;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Messaging.Tests.Services
{
	[DataContract]
	public class Greet
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class GreetResponse
	{
		[DataMember]
		public string Result { get; set; }
	}

	public class GreetService
		: AsyncServiceBase<Greet>
	{
		public string Result { get; set; }

		protected override object Run(Greet request)
		{
			Result = "Hello, " + request.Name;
			return new GreetResponse { Result = Result };
		}

		public void ExecuteAsync(IMessage<Greet> message)
		{
			Execute(message.Body);
		}
	}

}