using System;

namespace ServiceStack.ServiceHost
{
	public interface IRequestAttributes
	{
		bool AcceptsGzip { get; }

		bool AcceptsDeflate { get; }
	}
}