using System.Collections.Generic;
using ServiceStack.DataAccess.Criteria;

namespace ServiceStack.DataAccess
{
	public interface IQueryablePersistenceProvider : IPersistenceProvider, IQueryable
	{
		IList<T> GetAll<T>(ICriteria criteria)
			where T : class, new();
	}
}