using System.Collections.Generic;
using System.Web;

namespace ServiceStack
{
	public class NotAcceptableException : HttpException
	{
		public IEnumerable<string> Allowed { get; private set; }

		public NotAcceptableException() : this(new List<string>())
		{
		}

		public NotAcceptableException(IEnumerable<string> allowed)
		{
			Allowed = allowed;
		}
	}
}
