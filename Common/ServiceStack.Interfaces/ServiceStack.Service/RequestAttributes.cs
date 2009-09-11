using System;

namespace ServiceStack.Service
{
	public interface IRequestAttributes
	{
		bool AcceptsGzip { get; }

		bool AcceptsDeflate { get; }
	}
}