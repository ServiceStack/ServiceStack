using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

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
		: IService<Greet>
	{
		public string Result { get; set; }

		public object Execute(Greet request)
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