using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	public interface IQueryableByExample
	{
		IList<Extent> QueryByExample<Extent>(object template);
	}
}