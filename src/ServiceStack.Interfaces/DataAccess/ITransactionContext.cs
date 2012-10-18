using System;

namespace ServiceStack.DataAccess
{
    public interface ITransactionContext : IDisposable
    {
        bool Commit();
        bool Rollback();
    }
}