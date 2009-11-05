using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.ServiceHost;

namespace ServiceStack.Messaging.Tests
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

	public class GreetService : IService<Greet>
	{
		public object Execute(Greet request)
		{
			return new GreetResponse { Result = "Hello, " + request.Name };
		}
	}


	[TestFixture]
	public class MessagingHostTestBase
	{
		[Test]
		public void Test_GreetService_client_and_server_example()
		{

		}
	}

}
