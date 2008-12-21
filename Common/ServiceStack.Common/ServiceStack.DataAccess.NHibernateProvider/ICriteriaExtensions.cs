using System.Collections.Generic;
using NHibernate;

namespace ServiceStack.DataAccess.NHibernateProvider
{
	public static class ICriteriaExtensions
	{
		public static IList<To> ToList<To>(this ICriteria criteria)
		{
			var list = new List<To>();
			foreach (var item in criteria.List())
			{
				list.Add((To)item);
			}
			return list;
		}
	}
}