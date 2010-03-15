using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Messaging.Tests.Services;

namespace ServiceStack.Messaging.Tests
{
	public abstract class BasicServiceMessagingTests
		: MessagingHostTestBase
	{
		[Test]
		public void Normal_GreetService_client_and_server_example()
		{
			var service = new GreetService();
			using (var serviceHost = CreateMessagingService())
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

		[Test]
		public void Publish_before_starting_host_GreetService_client_and_server_example()
		{
			var service = new GreetService();
			using (var serviceHost = CreateMessagingService())
			{
				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					client.Publish(new Greet { Name = "World!" });
				}

				serviceHost.RegisterHandler<Greet>(service.ExecuteAsync);
				serviceHost.Start();

				Assert.That(service.Result, Is.EqualTo("Hello, World!"));
			}
		}

		[Test]
		public void AlwaysFailsService_ends_up_in_dlq_after_3_attempts()
		{
			var service = new GreetService();
			using (var serviceHost = CreateMessagingService())
			{
				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					client.Publish(new Greet { Name = "World!" });
				}

				serviceHost.RegisterHandler<Greet>(service.ExecuteAsync);
				serviceHost.Start();

				Assert.That(service.Result, Is.EqualTo("Hello, World!"));
			}
		}
	}
}