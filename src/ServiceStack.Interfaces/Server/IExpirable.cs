using System;

namespace ServiceStack.Server
{
	public interface IExpirable
	{
		DateTime? LastModified { get; }
	}
}