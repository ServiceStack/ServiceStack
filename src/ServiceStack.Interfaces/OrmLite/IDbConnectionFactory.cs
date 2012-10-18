using System.Data;

namespace ServiceStack.OrmLite
{
    public interface IDbConnectionFactory
    {
        IDbConnection OpenDbConnection();
        IDbConnection CreateDbConnection();
    }
}