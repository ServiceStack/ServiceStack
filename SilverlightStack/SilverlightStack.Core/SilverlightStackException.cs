using System;

namespace SilverlightStack
{
	public class SilverlightStackException : Exception
	{
		public SilverlightStackException()
		{
		}

		public SilverlightStackException(string message) : base(message)
		{
		}

		public SilverlightStackException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}