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
            var isolationLevel = Transaction.IsolationLevel;
            var id = Diagnostics.OrmLite.WriteTransactionCommitBefore(isolationLevel, db);
            Exception e = null;
            try
            {
                Transaction.Commit();
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                if (e != null)
                    Diagnostics.OrmLite.WriteTransactionCommitError(id, isolationLevel, db, e);
                else
                    Diagnostics.OrmLite.WriteTransactionCommitAfter(id, isolationLevel, db);
            }
        }

        public void Rollback()
        {
            var isolationLevel = Transaction.IsolationLevel;
            var id = Diagnostics.OrmLite.WriteTransactionRollbackBefore(isolationLevel, db, null);
            Exception e = null;
            try
            {
                Transaction.Rollback();
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
                if (e != null)
                    Diagnostics.OrmLite.WriteTransactionRollbackError(id, isolationLevel, db, null, e);
                else
                    Diagnostics.OrmLite.WriteTransactionRollbackAfter(id, isolationLevel, db, null);
            }
        }

        public IDbConnection Connection => Transaction.Connection;

        public IsolationLevel IsolationLevel => Transaction.IsolationLevel;
    }
}