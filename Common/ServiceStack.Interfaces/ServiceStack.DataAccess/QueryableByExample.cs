using System;
using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	public interface IQueryableByExample
	{
		IList<Extent> QueryByExample<Extent>(object template);
	}

	public interface IQueryableByPredicate
	{
		IList<Extent> Query<Extent>(Predicate<Extent> match);
	}

	public interface IQueryableByComparer
	{
		IList<Extent> Query<Extent>(IComparer<Extent> comparer);
	}

	public interface IQueryable : IQueryableByExample, IQueryableByPredicate, IQueryableByComparer
	{
		//IList<Extent> Query<Extent>();
		//IList<ElementType> Query<ElementType>(Type extent);
		//IList<Extent> Query<Extent>(Predicate<Extent> match, IComparer<Extent> comparer);
		//IList<Extent> Query<Extent>(Predicate<Extent> match, Comparison<Extent> comparison);
	}
}