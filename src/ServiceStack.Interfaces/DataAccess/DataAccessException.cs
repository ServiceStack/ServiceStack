using System;
using System.Runtime.Serialization;

namespace ServiceStack.DataAccess
{
	public class DataAccessException : Exception
	{
		public DataAccessException()
		{
		}

		public DataAccessException(string message) 
			: base(message)
		{
		}

		public DataAccessException(string message, Exception innerException) 
			: base(message, innerException)
		{
		}

#if !SILVERLIGHT && !MONOTOUCH && !XBOX
		protected DataAccessException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}
#endif

	}
}