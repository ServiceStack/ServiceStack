using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Azure.Messaging;
using ServiceStack.Configuration;
#if NET472
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
#endif

namespace ServiceStack.Azure.Tests.Messaging
{
	public class AzureServiceBusMqServerConcurrencyTests
	{
		static string ConnectionString
		{
			get
			{
				var connString = Environment.GetEnvironmentVariable("AZURE_BUS_CONNECTION_STRING");
				if (connString != null)
					return connString;

				var assembly = typeof(AzureServiceBusMqServerIntroTests).Assembly;
				var path = new Uri(assembly.CodeBase).LocalPath;
				var configFile = Path.Combine(Path.GetDirectoryName(path), "settings.config");

				return new TextFileSettings(configFile).Get("ConnectionString");
			}
		}

		public AzureServiceBusMqServerConcurrencyTests()
		{
#if !NETCORE
			NamespaceManager nm = NamespaceManager.CreateFromConnectionString(ConnectionString);
			Parallel.ForEach(nm.GetQueues(), qd =>
			{
				var sbClient =
					QueueClient.CreateFromConnectionString(ConnectionString, qd.Path, ReceiveMode.ReceiveAndDelete);
				BrokeredMessage msg = null;
				while ((msg = sbClient.Receive(new TimeSpan(0, 0, 1))) != null)
				{
				}
			});
#endif
		}
		
		[Test]
		[TestCase(4, 10)]
		public void Can_handle_requests_concurrently_in_4_threads(int noOfThreads, int msgs)
		{
			var timesCalled = 0;
			using var mqHost = new ServiceBusMqServer(ConnectionString);
			var queueNames = QueueNames<Wait>.AllQueueNames.Select(SafeQueueName).ToList();
#if NETCORE
			queueNames.ForEach(q => mqHost.ManagementClient.DeleteQueueAsync(q).GetAwaiter().GetResult());
#else
			queueNames.ForEach(q => mqHost.NamespaceManager.DeleteQueue(q));
#endif
			
			mqHost.RegisterHandler<Wait>(m => {
				Interlocked.Increment(ref timesCalled);
				Thread.Sleep(m.GetBody().ForMs);
				return null;
			}, noOfThreads);

			mqHost.Start();

			var dto = new Wait {ForMs = 100};
			using var mqClient = mqHost.CreateMessageQueueClient();
			msgs.Times(i => mqClient.Publish(dto));
			
			ExecUtils.RetryUntilTrue(() => timesCalled ==  msgs, TimeSpan.FromSeconds(5));
		}
		
		internal static string SafeQueueName(string queueName) =>
			queueName?.Replace(":", ".").Replace("[]", "Array");
		
		public class Wait
		{
			public int ForMs { get; set; }
		}

	}
}