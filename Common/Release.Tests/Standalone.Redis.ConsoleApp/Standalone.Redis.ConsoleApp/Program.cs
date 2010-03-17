using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;

namespace Standalone.Redis.ConsoleApp
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var redisClient = new RedisClient("chi-dev-mem1"))
			{
				redisClient.Set("release-test", "works");
				var result = redisClient.Get<string>("release-test");
				Console.WriteLine("Result: " + result);
			}

			Console.ReadKey();
		}
	}
}
