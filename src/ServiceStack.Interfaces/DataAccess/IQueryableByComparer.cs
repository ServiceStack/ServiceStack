using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	public interface IQueryableByComparer
	{
		IList<Extent> Query<Extent>(IComparer<Extent> comparer);
	}
}