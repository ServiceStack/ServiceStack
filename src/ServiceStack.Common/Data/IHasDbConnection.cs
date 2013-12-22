//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !SL5 && !XBOX
using System.Data;

namespace ServiceStack.Data
{
	public interface IHasDbConnection
	{
		IDbConnection DbConnection { get; }
	}
}
#endif