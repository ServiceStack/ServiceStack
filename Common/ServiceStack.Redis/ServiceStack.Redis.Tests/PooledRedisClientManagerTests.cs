using System;
using System.Collections.Generic;
using System.Threading;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class PooledRedisClientManagerTests
	{
		readonly string[] testReadWriteHosts = new[] {
			"readwrite1", "readwrite2:6000", "192.168.0.1", "localhost"
		};

		readonly string[] testReadOnlyHosts = new[] {
			"read1", "read2:7000", "127.0.0.1"
		};

		private string firstReadWriteHost;
		private string firstReadOnlyHost;

		private int testHostsPoolIndex;
		private string[] testHostsPool;

		private Mock<IRedisClientFactory> mockFactory;

		[SetUp]
		public void OnBeforeEachTest()
		{
			firstReadWriteHost = testReadWriteHosts[0];
			firstReadOnlyHost = testReadOnlyHosts[0];

			SetupRedisFactoryMock(testReadWriteHosts);
		}

		private void SetupRedisFactoryMock(string[] useHosts)
		{
			testHostsPoolIndex = 0;
			testHostsPool = useHosts;

			mockFactory = new Mock<IRedisClientFactory>();
			mockFactory.Expect(x => x.CreateRedisClient(
				It.IsAny<string>(), It.IsAny<int>()))
				.Returns(() => CreateWithHost(testHostsPool[testHostsPoolIndex++]));
		}

		public RedisClient CreateWithHost(string host)
		{
			var parts = host.Split(':');
			return parts.Length > 1
				? new RedisClient(parts[0], int.Parse(parts[1])) { }
				: new RedisClient(host);
		}

		public PooledRedisClientManager CreateManager(
			IRedisClientFactory usingFactory, string[] readWriteHosts, string[] readOnlyHosts)
		{
			return new PooledRedisClientManager(readWriteHosts, readOnlyHosts) {
				RedisClientFactory = usingFactory,
			};
		}

		public PooledRedisClientManager CreateManager(
			IRedisClientFactory usingFactory, params string[] readWriteHosts)
		{
			return new PooledRedisClientManager(readWriteHosts) {
				RedisClientFactory = usingFactory,
			};
		}

		public PooledRedisClientManager CreateManager(params string[] readWriteHosts)
		{
			return CreateManager(mockFactory.Object, readWriteHosts, readWriteHosts);
		}

		public PooledRedisClientManager CreateManager()
		{
			return CreateManager(mockFactory.Object, testReadWriteHosts, testReadOnlyHosts);
		}

		public PooledRedisClientManager CreateAndStartManager()
		{
			var manager = CreateManager();
			manager.Start();
			return manager;
		}

		[Test]
		public void Cant_get_client_without_calling_Start()
		{
			using (var manager = CreateManager())
			{
				try
				{
					var client = manager.GetClient();
				}
				catch (InvalidOperationException)
				{
					return;
				}
				Assert.Fail("Should throw");
			}
		}

		[Test]
		public void Can_get_client_after_calling_Start()
		{
			using (var manager = CreateManager())
			{
				manager.Start();
				var client = manager.GetClient();
			}
		}

		[Test]
		public void Can_get_ReadWrite_client()
		{
			using (var manager = CreateAndStartManager())
			{
				var client = manager.GetClient();

				AssertClientHasHost(client, firstReadWriteHost);

				mockFactory.VerifyAll();
			}
		}

		private static void AssertClientHasHost(IRedisClient client, string hostWithOptionalPort)
		{
			var parts = hostWithOptionalPort.Split(':');
			var port = parts.Length > 1 ? int.Parse(parts[1]) : RedisNativeClient.DefaultPort;

			Assert.That(client.Host, Is.EqualTo(parts[0]));
			Assert.That(client.Port, Is.EqualTo(port));
		}

		[Test]
		public void Can_get_ReadOnly_client()
		{
			SetupRedisFactoryMock(testReadOnlyHosts);

			using (var manager = CreateAndStartManager())
			{
				var client = manager.GetReadOnlyClient();

				AssertClientHasHost(client, firstReadOnlyHost);

				mockFactory.VerifyAll();
			}
		}

		[Test]
		public void Does_loop_through_ReadWrite_hosts()
		{
			using (var manager = CreateAndStartManager())
			{
				var client1 = manager.GetClient();
				client1.Dispose();
				var client2 = manager.GetClient();
				var client3 = manager.GetClient();
				var client4 = manager.GetClient();
				var client5 = manager.GetClient();

				AssertClientHasHost(client1, testReadWriteHosts[0]);
				AssertClientHasHost(client2, testReadWriteHosts[1]);
				AssertClientHasHost(client3, testReadWriteHosts[2]);
				AssertClientHasHost(client4, testReadWriteHosts[3]);
				AssertClientHasHost(client5, testReadWriteHosts[0]);

				mockFactory.VerifyAll();
			}
		}

		[Test]
		public void Does_loop_through_ReadOnly_hosts()
		{
			SetupRedisFactoryMock(testReadOnlyHosts);

			using (var manager = CreateAndStartManager())
			{
				var client1 = manager.GetReadOnlyClient();
				client1.Dispose();
				var client2 = manager.GetReadOnlyClient();
				client2.Dispose();
				var client3 = manager.GetReadOnlyClient();
				var client4 = manager.GetReadOnlyClient();
				var client5 = manager.GetReadOnlyClient();

				AssertClientHasHost(client1, testReadOnlyHosts[0]);
				AssertClientHasHost(client2, testReadOnlyHosts[1]);
				AssertClientHasHost(client3, testReadOnlyHosts[2]);
				AssertClientHasHost(client4, testReadOnlyHosts[0]);
				AssertClientHasHost(client5, testReadOnlyHosts[1]);

				mockFactory.VerifyAll();
			}
		}

		[Test]
		public void Does_block_ReadWrite_clients_pool()
		{
			using (var manager = CreateAndStartManager())
			{
				var delay = TimeSpan.FromSeconds(1);
				var client1 = manager.GetClient();
				var client2 = manager.GetClient();
				var client3 = manager.GetClient();
				var client4 = manager.GetClient();

				Action func = delegate {
					Thread.Sleep(delay + TimeSpan.FromSeconds(0.5));
					client4.Dispose();
				};

				func.BeginInvoke(null, null);

				var start = DateTime.Now;

				var client5 = manager.GetClient();

				Assert.That(DateTime.Now - start, Is.GreaterThanOrEqualTo(delay));

				AssertClientHasHost(client1, testReadWriteHosts[0]);
				AssertClientHasHost(client2, testReadWriteHosts[1]);
				AssertClientHasHost(client3, testReadWriteHosts[2]);
				AssertClientHasHost(client4, testReadWriteHosts[3]);
				AssertClientHasHost(client5, testReadWriteHosts[3]);

				mockFactory.VerifyAll();
			}
		}

		[Test]
		public void Does_block_ReadOnly_clients_pool()
		{
			SetupRedisFactoryMock(testReadOnlyHosts);

			var delay = TimeSpan.FromSeconds(1);

			using (var manager = CreateAndStartManager())
			{
				var client1 = manager.GetReadOnlyClient();
				var client2 = manager.GetReadOnlyClient();
				var client3 = manager.GetReadOnlyClient();

				Action func = delegate {
					Thread.Sleep(delay + TimeSpan.FromSeconds(0.5));
					client3.Dispose();
				};

				func.BeginInvoke(null, null);

				var start = DateTime.Now;

				var client4 = manager.GetReadOnlyClient();

				Assert.That(DateTime.Now - start, Is.GreaterThanOrEqualTo(delay));

				AssertClientHasHost(client1, testReadOnlyHosts[0]);
				AssertClientHasHost(client2, testReadOnlyHosts[1]);
				AssertClientHasHost(client3, testReadOnlyHosts[2]);
				AssertClientHasHost(client4, testReadOnlyHosts[2]);

				mockFactory.VerifyAll();
			}
		}

		[Test]
		public void Can_support_64_threads_using_the_client_simultaneously()
		{
			const int noOfConcurrentClients = 64; //WaitHandle.WaitAll limit is <= 64
			var clientUsageMap = new Dictionary<string, int>();

			var clientAsyncResults = new List<IAsyncResult>();
			using (var manager = CreateAndStartManager())
			{
				for (var i = 0; i < noOfConcurrentClients; i++)
				{
					var clientNo = i;
					var action = (Action)(() => UseClient(manager, clientNo, clientUsageMap));
					clientAsyncResults.Add(action.BeginInvoke(null, null));
				}
			}

			WaitHandle.WaitAll(clientAsyncResults.ConvertAll(x => x.AsyncWaitHandle).ToArray());

			Console.WriteLine(TypeSerializer.SerializeToString(clientUsageMap));

			var hostCount = 0;
			foreach (var entry in clientUsageMap)
			{
				Assert.That(entry.Value, Is.GreaterThanOrEqualTo(5), "Host has unproportianate distrobution: " + entry.Value);
				Assert.That(entry.Value, Is.LessThanOrEqualTo(30), "Host has unproportianate distrobution: " + entry.Value);
				hostCount += entry.Value;
			}

			Assert.That(hostCount, Is.EqualTo(noOfConcurrentClients), "Invalid no of clients used");
		}

		private static void UseClient(IRedisClientsManager manager, int clientNo, Dictionary<string, int> hostCountMap)
		{
			using (var client = manager.GetClient())
			{
				lock (hostCountMap)
				{
					int hostCount;
					if (!hostCountMap.TryGetValue(client.Host, out hostCount))
					{
						hostCount = 0;
					}

					hostCountMap[client.Host] = ++hostCount;
				}

				Console.WriteLine("Client '{0}' is using '{1}'", clientNo, client.Host);
			}
		}

	}
}