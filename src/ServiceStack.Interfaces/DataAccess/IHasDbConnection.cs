#if !SILVERLIGHT && !XBOX
using System.Data;

namespace ServiceStack.DataAccess
{
	public interface IHasDbConnection
	{
		IDbConnection DbConnection { get; }
	}
}
#endif