using System.Collections.Generic;

namespace ServiceStack.DataAccess.Db4oProvider
{
	public interface IResultSet<T>
	{
		long Offset { get; }
		long TotalCount { get; }
		IEnumerable<T> Results { get; }
	}
}