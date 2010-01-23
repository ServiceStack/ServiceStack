using System.Collections.Generic;

namespace ServiceStack.DataAccess.Db4oProvider
{
	public class Db4OResultSet<T> : IResultSet<T>
	{
		public Db4OResultSet()
		{
			Results = new List<T>();
		}

		public Db4OResultSet(IEnumerable<T> results)
		{
			Results = new List<T>(results);
		}

		public long Offset { get; set; }
		public long TotalCount { get; set; }
		public IEnumerable<T> Results { get; private set; }
	}
}