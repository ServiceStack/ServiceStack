using Autofac;
using DependencyInjection;
using NUnit.Framework;
using ServiceStack.Messaging.Tests.Services;

namespace ServiceStack.Messaging.Tests
{
	public abstract class TransientServiceMessagingTests
		: MessagingHostTestBase
	{
		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();

			DependencyInjector.Register(c => new GreetService {
				MessageFactory = c.Resolve<IMessageFactory>()
			});
			DependencyInjector.Register(c => new AlwaysFailService {
				MessageFactory = c.Resolve<IMessageFactory>()
			});
			DependencyInjector.Register(c => new UnRetryableFailService {
				MessageFactory = c.Resolve<IMessageFactory>()
			});
		}

		[Test]
		public void Normal_GreetService_client_and_server_example()
		{
			var service = DependencyInjector.Resolve<GreetService>();
			using (var serviceHost = CreateMessagingService())
			{
				serviceHost.RegisterHandler<Greet>(service.ExecuteAsync);

				serviceHost.Start();

				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					client.Publish(new Greet { Name = "World!" });
				}

				Assert.That(service.Result, Is.EqualTo("Hello, World!"));
				Assert.That(service.TimesCalled, Is.EqualTo(1));
			}
		}

		[Test]
		public void Publish_before_starting_host_GreetService_client_and_server_example()
		{
			var service = DependencyInjector.Resolve<GreetService>();
			using (var serviceHost = CreateMessagingService())
			{
				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					client.Publish(new Greet { Name = "World!" });
				}

				serviceHost.RegisterHandler<Greet>(service.ExecuteAsync);
				serviceHost.Start();

				Assert.That(service.Result, Is.EqualTo("Hello, World!"));
				Assert.That(service.TimesCalled, Is.EqualTo(1));
			}
		}

		[Test]
		public void AlwaysFailsService_ends_up_in_dlq_after_3_attempts()
		{
			var service = DependencyInjector.Resolve<AlwaysFailService>();
			var request = new AlwaysFail { Name = "World!" };
			using (var serviceHost = CreateMessagingService())
			{
				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					client.Publish(request);
				}

				serviceHost.RegisterHandler<AlwaysFail>(service.ExecuteAsync);
				serviceHost.Start();

				Assert.That(service.Result, Is.Null);
				Assert.That(service.TimesCalled, Is.EqualTo(3));

				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					var dlqMessage = client.GetAsync(QueueNames<AlwaysFail>.Dlq)
						.ToMessage<AlwaysFail>();

					Assert.That(dlqMessage, Is.Not.Null);
					Assert.That(dlqMessage.GetBody().Name, Is.EqualTo(request.Name));
				}
			}
		}

		[Test]
		public void UnRetryableFailService_ends_up_in_dlq_after_1_attempt()
		{
			var service = DependencyInjector.Resolve<UnRetryableFailService>();
			var request = new UnRetryableFail { Name = "World!" };
			using (var serviceHost = CreateMessagingService())
			{
				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					client.Publish(request);
				}

				serviceHost.RegisterHandler<UnRetryableFail>(service.ExecuteAsync);
				serviceHost.Start();

				Assert.That(service.Result, Is.Null);
				Assert.That(service.TimesCalled, Is.EqualTo(1));

				using (var client = serviceHost.MessageFactory.CreateMessageQueueClient())
				{
					var dlqMessage = client.GetAsync(QueueNames<UnRetryableFail>.Dlq)
						.ToMessage<UnRetryableFail>();

					Assert.That(dlqMessage, Is.Not.Null);
					Assert.That(dlqMessage.GetBody().Name, Is.EqualTo(request.Name));
				}
			}
		}

	}
}