using System.Threading;

namespace RemoteInfo.Host.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			AppHost.Init();

			Thread.Sleep(Timeout.Infinite);
			System.Console.WriteLine("ReadLine()");
			System.Console.ReadLine();
		}
	}
}