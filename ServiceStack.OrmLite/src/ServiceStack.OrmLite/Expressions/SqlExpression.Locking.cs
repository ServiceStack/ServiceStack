using System;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T>
    {
        private bool forUpdate;
        private bool forUpdateSkipLocked;

        /// <summary>
        /// Whether this query locks the rows it selects, from ForUpdate()
        /// </summary>
        public bool IsForUpdate => forUpdate;

        /// <summary>
        /// Lock the selected rows of the query's table until the end of the current transaction, so other transactions
        /// can't update, delete or lock them, e.g:
        /// <para>db.Single(db.From&lt;Account&gt;().Where(x =&gt; x.Id == id).ForUpdate())</para>
        /// Uses FOR UPDATE in PostgreSQL, MySQL and Oracle, WITH (UPDLOCK, ROWLOCK) in SQL Server, and is ignored in
        /// SQLite which only has database-level locks. Locks are only held when used inside a transaction.
        /// </summary>
        /// <param name="skipLocked">Skip rows locked by other transactions instead of waiting for them, e.g. so
        /// multiple workers can take different jobs from the same queue table (SKIP LOCKED / READPAST)</param>
        public virtual SqlExpression<T> ForUpdate(bool skipLocked = false)
        {
            forUpdate = true;
            forUpdateSkipLocked = skipLocked;
            return this;
        }

        /// <summary>
        /// The body of the SELECT statement with the dialect's table lock hint on the query's table, if any
        /// </summary>
        private string GetLockedBodyExpression()
        {
            var body = BodyExpression;
            var hint = DialectProvider.GetForUpdateTableHint(forUpdateSkipLocked);
            if (string.IsNullOrEmpty(hint))
                return body;

            // The hint follows the query's table and alias, before any joins
            var fromTable = " \nFROM " + DialectProvider.GetQuotedTableName(modelDef)
                + (TableAlias != null ? " " + DialectProvider.GetQuotedName(TableAlias) : "");
            if (!body.StartsWith(fromTable, StringComparison.Ordinal))
                throw new NotSupportedException("ForUpdate() can't be used with a custom FROM expression");

            return fromTable + " " + hint + body.Substring(fromTable.Length);
        }

        /// <summary>
        /// Adds the dialect's lock clause to the end of the SELECT statement, if any
        /// </summary>
        private string AppendLockClause(string sql)
        {
            // Only lock rows of the query's table when joined with other tables
            var hasJoins = !string.IsNullOrEmpty(fromExpression)
                && fromExpression.IndexOf("JOIN ", StringComparison.OrdinalIgnoreCase) >= 0;
            var lockTable = hasJoins
                ? DialectProvider.GetQuotedName(TableAlias ?? DialectProvider.NamingStrategy.GetTableName(modelDef))
                : null;

            var clause = DialectProvider.GetForUpdateClause(lockTable, forUpdateSkipLocked);
            return string.IsNullOrEmpty(clause)
                ? sql
                : sql + "\n" + clause;
        }

        private void AssertCanLock()
        {
            if (HasSetOperations)
                throw new NotSupportedException("ForUpdate() can't be used with set operations like Union()");
            if (HasCommonTableExpression)
                throw new NotSupportedException("ForUpdate() can't be used with common table expressions like WithRecursive()");
            if (HasTopPerGroup)
                throw new NotSupportedException("ForUpdate() can't be used with TopPerGroup()");
        }
    }
}
