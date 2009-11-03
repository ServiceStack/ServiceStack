using System.Threading;

namespace ServiceStack.Examples.Host.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			var appHost = new AppHost();
			appHost.Init();

			Thread.Sleep(Timeout.Infinite);
			System.Console.WriteLine("ReadLine()");
			System.Console.ReadLine();
		}
	}
}
