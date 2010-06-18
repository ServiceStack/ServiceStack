using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Tests.Examples
{
	[TestFixture]
	public class SimpleLocks
	{
		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			using (var redisClient = new RedisClient(TestConfig.SingleHost))
			{
				redisClient.FlushAll();
			}
		}

		[Test]
		public void Use_multiple_redis_clients_to_safely_execute()
		{
			//The number of concurrent clients to run
			const int noOfClients = 5;
			var asyncResults = new List<IAsyncResult>(noOfClients);
			for (var i = 1; i <= noOfClients; i++)
			{
				var clientNo = i;
				var actionFn = (Action)delegate
				{
					var redisClient = new RedisClient(TestConfig.SingleHost);
					using (redisClient.AcquireLock("testlock"))
					{
						Console.WriteLine("client {0} acquired lock", clientNo);
						var counter = redisClient.Get<int>("atomic-counter");

						//Add an artificial delay to demonstrate locking behaviour
						Thread.Sleep(100);

						redisClient.Set("atomic-counter", counter + 1);
						Console.WriteLine("client {0} released lock", clientNo);
					}
				};

				//Asynchronously invoke the above delegate in a background thread
				asyncResults.Add(actionFn.BeginInvoke(null, null));
			}

			//Wait at most 1 second for all the threads to complete
			asyncResults.WaitAll(TimeSpan.FromSeconds(1));

			//Print out the 'atomic-counter' result
			using (var redisClient = new RedisClient(TestConfig.SingleHost))
			{
				var counter = redisClient.Get<int>("atomic-counter");
				Console.WriteLine("atomic-counter after 1sec: {0}", counter);
			}
		}

		[Test]
		public void Acquiring_lock_with_timeout()
		{
			var redisClient = new RedisClient(TestConfig.SingleHost);

			//Initialize and set counter to '1'
			redisClient.IncrementValue("atomic-counter"); 
			
			//Acquire lock and never release it
			redisClient.AcquireLock("testlock");

			var waitFor = TimeSpan.FromSeconds(2);
			var now = DateTime.Now;

			try
			{
				using (var newClient = new RedisClient(TestConfig.SingleHost))
				{
					//Attempt to acquire a lock with a 2 second timeout
					using (newClient.AcquireLock("testlock", waitFor))
					{
						//If lock was acquired this would be incremented to '2'
						redisClient.IncrementValue("atomic-counter"); 
					}
				}
			}
			catch (TimeoutException tex)
			{
				var timeTaken = DateTime.Now - now;
				Console.WriteLine("After '{0}', Received TimeoutException: '{1}'", timeTaken, tex.Message);

				var counter = redisClient.Get<int>("atomic-counter");
				Console.WriteLine("atomic-counter remains at '{0}'", counter);
			}
		}

	}


}
