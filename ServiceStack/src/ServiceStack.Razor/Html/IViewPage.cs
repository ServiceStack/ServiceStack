using System;

namespace ServiceStack
{
	public interface IViewPage
	{
		bool IsCompiled { get; }

		void Compile(bool force=false);
	}
}

