using System;
using System.Data;
using ServiceStack.Data;

namespace ServiceStack.OrmLite
{
    public class OrmLiteTransaction : IDbTransaction, IHasDbTransaction
    {
        public IDbTransaction Transaction { get; set; }
        public IDbTransaction DbTransaction => Transaction;

        private readonly IDbConnection db;

        public OrmLiteTransaction(IDbConnection db, IDbTransaction transaction)
        {
            this.db = db;
            this.Transaction = transaction;

            //If OrmLite managed connection assign to connection, otherwise use OrmLiteContext
            if (this.db is ISetDbTransaction ormLiteConn)
            {
                ormLiteConn.Transaction = this.Transaction = transaction;
            }
            else
            {
                OrmLiteContext.TSTransaction = this.Transaction = transaction;
            }
        }

        public void Dispose()
        {
            try
            {
                Transaction.Dispose();
            }
            finally
            {
                if (this.db is ISetDbTransaction ormLiteConn)
                {
                    ormLiteConn.Transaction = null;
                }
                else
                {
                    OrmLiteContext.TSTransaction = null;
                }
            }
        }

        public void Commit()
        {
            var id = Diagnostics.OrmLite.WriteTransactionCommitBefore(Transaction.IsolationLevel, db);
            try
            {
                Transaction.Commit();
            }
            catch (Exception ex)
            {
                Diagnostics.OrmLite.WriteTransactionCommitError(id, Transaction.IsolationLevel, db, ex);
                throw;
            }
            finally
            {
                Diagnostics.OrmLite.WriteTransactionCommitAfter(id, Transaction.IsolationLevel, db);
            }
        }

        public void Rollback()
        {
            var id = Diagnostics.OrmLite.WriteTransactionRollbackBefore(Transaction.IsolationLevel, db, null);
            try
            {
                Transaction.Rollback();
            }
            catch (Exception ex)
            {
                Diagnostics.OrmLite.WriteTransactionCommitError(id, Transaction.IsolationLevel, db, ex);
                throw;
            }
            finally
            {
                Diagnostics.OrmLite.WriteTransactionCommitAfter(id, Transaction.IsolationLevel, db);
            }
        }

        public IDbConnection Connection => Transaction.Connection;

        public IsolationLevel IsolationLevel => Transaction.IsolationLevel;
    }
}