using System;
using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	public interface IQueryableByPredicate
	{
		IList<Extent> Query<Extent>(Predicate<Extent> match);
	}
}