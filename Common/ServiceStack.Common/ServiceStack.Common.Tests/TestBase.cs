using System;

namespace ServiceStack.Common.Tests
{
	public class TestBase
	{
		public virtual void Log(string message, params object[] args)
		{
#if DEBUG
#endif
			Console.WriteLine(message, args);
		}
	}
}