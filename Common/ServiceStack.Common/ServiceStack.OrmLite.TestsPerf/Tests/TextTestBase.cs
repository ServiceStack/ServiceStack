using System;
using System.Text;

namespace ServiceStack.OrmLite.TestsPerf.Tests
{
	public class TextTestBase
	{
		protected StringBuilder SbLog = new StringBuilder();

		public virtual void Log(string message)
		{
#if DEBUG
			Console.WriteLine(message);
#endif
			SbLog.AppendLine(message);
		}

		public virtual void Log(string message, params object[] args)
		{
#if DEBUG
			Console.WriteLine(message, args);
#endif
			SbLog.AppendFormat(message, args);
			SbLog.AppendLine();
		}
	}
}