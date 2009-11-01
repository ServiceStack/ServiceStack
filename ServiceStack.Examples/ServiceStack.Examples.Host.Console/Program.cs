using System.Threading;
using ServiceStack.Examples.ServiceInterface;

namespace ServiceStack.Examples.Host.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			var appHost = new AppHost("ServiceStack Examples", typeof(GetFactorialHandler).Assembly);
			appHost.Init();

			Thread.Sleep(Timeout.Infinite);
			System.Console.WriteLine("ReadLine()");
			System.Console.ReadLine();
		}
	}
}
