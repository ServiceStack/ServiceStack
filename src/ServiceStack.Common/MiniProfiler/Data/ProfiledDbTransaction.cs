using System;
using System.Data.Common;
using System.Data;
using ServiceStack.Data;

#pragma warning disable 1591 // xml doc comments warnings

namespace ServiceStack.MiniProfiler.Data
{
    public class ProfiledDbTransaction : DbTransaction, IHasDbTransaction
    {
        private ProfiledConnection db;
        private DbTransaction trans;

        public ProfiledDbTransaction(DbTransaction transaction, ProfiledConnection connection)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            this.trans = transaction;
            this.db = connection;
        }

        protected override DbConnection DbConnection => db;

        public IDbTransaction DbTransaction => trans;

        public override IsolationLevel IsolationLevel => trans.IsolationLevel;

        public override void Commit()
        {
            trans.Commit();
        }

        public override void Rollback()
        {
            trans.Rollback();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                trans?.Dispose();
            }
            trans = null;
            db = null;
            base.Dispose(disposing);
        }
    }
}

#pragma warning restore 1591 // xml doc comments warnings