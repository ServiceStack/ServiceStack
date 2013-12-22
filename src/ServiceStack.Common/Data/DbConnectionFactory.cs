#if !SL5
using System;
using System.Data;

namespace ServiceStack.Data
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly Func<IDbConnection> connectionFactoryFn;

        public DbConnectionFactory(Func<IDbConnection> connectionFactoryFn)
        {
            this.connectionFactoryFn = connectionFactoryFn;
        }

        public IDbConnection OpenDbConnection()
        {
            var dbConn = CreateDbConnection();
            dbConn.Open();
            return dbConn;
        }

        public IDbConnection CreateDbConnection()
        {
            return connectionFactoryFn();
        }
    }
}
#endif