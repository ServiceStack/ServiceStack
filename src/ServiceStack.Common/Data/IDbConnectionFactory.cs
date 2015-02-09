#if !SL5
using System.Data;

namespace ServiceStack.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection OpenDbConnection();
        IDbConnection CreateDbConnection();
    }
}
#endif
