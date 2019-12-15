using System;
using Neo4j.Driver.V1;

namespace ServiceStack.Caching.Neo4j
{
    internal static class DriverExtensions
    {
        public static T ReadTxQuery<T>(this IDriver driver, Func<ITransaction, T> work)
        {
            using (var session = driver.Session())
            {
                return session.ReadTransaction(work);
            }
        }

        public static void WriteTxQuery(this IDriver driver, Action<ITransaction> txWorkFn)
        {
            using (var session = driver.Session())
            {
                session.WriteTransaction(txWorkFn);
            }
        }
        
        public static T WriteTxQuery<T>(this IDriver driver, Func<ITransaction, T> txWorkFn)
        {
            using (var session = driver.Session())
            {
                return session.WriteTransaction(txWorkFn);
            }
        }
    }
}