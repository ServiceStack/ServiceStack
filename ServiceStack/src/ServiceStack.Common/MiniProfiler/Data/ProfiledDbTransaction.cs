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

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        public override System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return trans.CommitAsync(cancellationToken);
        }

        public override System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return trans.RollbackAsync(cancellationToken);
        }

        public override async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            if (trans != null)
            {
                await trans.DisposeAsync().ConfigureAwait(false);
            }
            trans = null;
            db = null;
            await base.DisposeAsync().ConfigureAwait(false);
        }
#endif

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