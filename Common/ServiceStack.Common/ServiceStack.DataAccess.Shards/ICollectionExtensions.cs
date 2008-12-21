/*
// $Id: ICollectionExtensions.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.Collections.Generic;

namespace ServiceStack.DataAccess.Shards
{
	internal static class ICollectionExtensions
	{
		public static TValue SafeGetValue<TKey, TValue>(this IDictionary<TKey, TValue> map, TKey key)
			where TValue : class
		{
			TValue value;
			return map.TryGetValue(key, out value) ? value : default(TValue);
		}
	}
}