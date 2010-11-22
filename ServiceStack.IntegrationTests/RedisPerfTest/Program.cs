using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;

namespace RedisPerfTest
{
	class Program
	{
		const string KeyMaster = "key:";
		const string ValueMaster = "value:";
		private const int Iterations = 1000;
		private const int LogEveryTimes = 100;

		static void Main(string[] args)
		{
			var host = args.Length > 0 ? args[0] : "localhost";
			var port = args.Length > 1 ? int.Parse(args[1]) : 6379;

			var redisClient = new RedisClient(host, port);
				
			var before = DateTime.Now;
			for (var i = 0; i < Iterations; i++)
			{
				var key = KeyMaster + i;
				redisClient.Set(key, ValueMaster);

				//if (i % LogEveryTimes == 0)
				//    Console.WriteLine("Time taken at {0}: {1}ms", i, (DateTime.Now - before).TotalMilliseconds);
			}

			for (int i = 0; i < Iterations; i++)
			{
				var key = KeyMaster + i;
				redisClient.Get<string>(key);

				//if (i % LogEveryTimes == 0)
				//    Console.WriteLine("Time taken at {0}: {1}ms", i, (DateTime.Now - before).TotalMilliseconds);
			}

			Console.WriteLine("Total Time Taken: {0}ms", (DateTime.Now - before).TotalMilliseconds);
		}
	}
}
