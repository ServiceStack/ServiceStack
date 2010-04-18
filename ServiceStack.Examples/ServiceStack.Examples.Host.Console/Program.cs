using System;
using System.Threading;

namespace ServiceStack.Examples.Host.Console
{
	class Program
	{
		private const string ListeningOn = "http://localhost:82/";

		static void Main(string[] args)
		{
			var appHost = new AppHost();
			appHost.Init();
			appHost.Start(ListeningOn);

			System.Console.WriteLine("AppHost Created at {0}, listening on {1}",
				DateTime.Now, ListeningOn);

			Thread.Sleep(Timeout.Infinite);
			System.Console.WriteLine("ReadLine()");
			System.Console.ReadLine();
		}
	}
}
