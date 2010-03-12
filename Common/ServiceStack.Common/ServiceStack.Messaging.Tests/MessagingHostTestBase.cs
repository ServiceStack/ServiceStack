using System.Runtime.Serialization;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
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


	[TestFixture]
	public class MessagingHostTestBase
	{
		[Test]
		public void Test_GreetService_client_and_server_example()
		{
			var service = new GreetService();
			using (var serviceHost = new InMemoryMessagingService())
			{
				serviceHost.RegisterHandler<Greet>(service.ExecuteAsync);

				serviceHost.Start();

				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					client.Publish(new Greet { Name = "World!" });
				}

				Assert.That(service.Result, Is.EqualTo("Hello, World!"));
			}
		}
	}

}
