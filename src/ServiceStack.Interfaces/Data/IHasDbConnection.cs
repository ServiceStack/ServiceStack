#if !SILVERLIGHT && !XBOX
using System.Data;

namespace ServiceStack.Data
{
	public interface IHasDbConnection
	{
		IDbConnection DbConnection { get; }
	}
}
#endif