using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Standalone.TypeSerializer.ConsoleApp
{
	class Program
	{
		public class Person
		{
			public string Name { get; set; }

			public int Age { get; set; }
		}

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
