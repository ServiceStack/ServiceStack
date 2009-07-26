using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServiceStack.Examples.Host.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			AppHost.Init();

			Thread.Sleep(Timeout.Infinite);
		}
	}
}
