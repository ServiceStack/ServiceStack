using System;
using System.Data.Common;
using System.Data;

#pragma warning disable 1591 // xml doc comments warnings

namespace MvcMiniProfiler.Data
{
    public class ProfiledDbTransaction : DbTransaction
    {
        private ProfiledDbConnection _conn;
        private DbTransaction _trans;

        public ProfiledDbTransaction(DbTransaction transaction, ProfiledDbConnection connection)
        {
            if (transaction == null) throw new ArgumentNullException("transaction");
            if (connection == null) throw new ArgumentNullException("connection");
            this._trans = transaction;
            this._conn = connection;
        }

        protected override DbConnection DbConnection
        {
            get { return _conn; }
        }

        internal DbTransaction WrappedTransaction
        {
            get { return _trans; }
        }

        public override IsolationLevel IsolationLevel
        {
            get { return _trans.IsolationLevel; }
        }

        public override void Commit()
        {
            _trans.Commit();
        }

        public override void Rollback()
        {
            _trans.Rollback();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _trans != null)
            {
                _trans.Dispose();
            }
            _trans = null;
            _conn = null;
            base.Dispose(disposing);
        }
    }
}

#pragma warning restore 1591 // xml doc comments warnings