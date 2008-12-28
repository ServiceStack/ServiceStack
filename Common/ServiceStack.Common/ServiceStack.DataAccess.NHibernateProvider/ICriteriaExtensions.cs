/*
// $Id: ICriteriaExtensions.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

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