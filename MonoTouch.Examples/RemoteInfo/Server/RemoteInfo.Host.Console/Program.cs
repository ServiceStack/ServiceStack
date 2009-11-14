using System.Threading;
using RemoteInfo.ServiceInterface;

namespace RemoteInfo.Host.Console
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