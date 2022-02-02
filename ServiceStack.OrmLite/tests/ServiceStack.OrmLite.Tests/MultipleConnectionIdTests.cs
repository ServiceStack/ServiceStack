using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class MultipleConnectionIdTests : OrmLiteProvidersTestBase
    {
        public MultipleConnectionIdTests(DialectContext context) : base(context) {}

        private int _waitingThreadCount;
        private int _waitingThreadsReleasedCounter;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<MultipleConnection>();
            }
        }

        [Test]
        [IgnoreDialect(Dialect.Sqlite, "doesn't support concurrent writers")]
        public void TwoSimultaneousInsertsGetDifferentIds()
        {
            var dataArray = new[]
            {
                new MultipleConnection {Data = "one"},
                new MultipleConnection {Data = "two"}
            };

            var originalExecFilter = OrmLiteConfig.ExecFilter;
            try
            {
                OrmLiteConfig.ExecFilter = new PostExecuteActionExecFilter(
                    originalExecFilter, 
                    cmd => PauseForOtherThreadsAfterInserts(cmd, 2),
                    DialectProvider);
                
                Parallel.ForEach(dataArray, data => {
                    using (var db = OpenDbConnection())
                    {
                        data.Id = db.Insert(new MultipleConnection {Data = data.Data}, selectIdentity: true);

                        Assert.That(data.Id, Is.Not.EqualTo(0));
                    }
                });
            }
            finally
            {
                OrmLiteConfig.ExecFilter = originalExecFilter;
            }

            Assert.That(dataArray[1].Id, Is.Not.EqualTo(dataArray[0].Id));
        }

        private void PauseForOtherThreadsAfterInserts(IDbCommand cmd, int numberOfThreads)
        {
            if (!cmd.CommandText.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase))
                return;

            var initialReleasedCounter = _waitingThreadsReleasedCounter;
            Interlocked.Increment(ref _waitingThreadCount);
            try
            {
                var waitUntil = DateTime.UtcNow.AddSeconds(2);
                while ((_waitingThreadCount < numberOfThreads) && (initialReleasedCounter == _waitingThreadsReleasedCounter))
                {
                    if (DateTime.UtcNow >= waitUntil)
                        throw new Exception("There were not enough waiting threads after timeout");
                    Thread.Sleep(1);
                }

                Interlocked.Increment(ref _waitingThreadsReleasedCounter);
            }
            finally
            {
                Interlocked.Decrement(ref _waitingThreadCount);
            }
        }

        [Test]
        [IgnoreDialect(Dialect.Sqlite, "doesn't support concurrent writers")]
        public void TwoSimultaneousSavesGetDifferentIds()
        {
            var dataArray = new[]
            {
                new MultipleConnection {Data = "one"},
                new MultipleConnection {Data = "two"}
            };

            var originalExecFilter = OrmLiteConfig.ExecFilter;
            try
            {
                OrmLiteConfig.ExecFilter = new PostExecuteActionExecFilter(
                    originalExecFilter, 
                    cmd => PauseForOtherThreadsAfterInserts(cmd, 2),
                    DialectProvider);

                Parallel.ForEach(dataArray, data =>
                {
                    using (var db = OpenDbConnection())
                    {
                        db.Save(data);

                        Assert.That(data.Id, Is.Not.EqualTo(0));
                    }
                });
            }
            finally
            {
                OrmLiteConfig.ExecFilter = originalExecFilter;
            }

            Assert.That(dataArray[1].Id, Is.Not.EqualTo(dataArray[0].Id));
        }

        private class PostExecuteActionExecFilter : IOrmLiteExecFilter, IHasDialectProvider
        {
            private readonly IOrmLiteExecFilter inner;
            private readonly Action<IDbCommand> postExecuteAction;
            public IOrmLiteDialectProvider DialectProvider { get; }

            public PostExecuteActionExecFilter(IOrmLiteExecFilter inner, Action<IDbCommand> postExecuteAction, IOrmLiteDialectProvider dialectProvider)
            {
                this.inner = inner;
                this.postExecuteAction = postExecuteAction;
                this.DialectProvider = dialectProvider;
            }

            public SqlExpression<T> SqlExpression<T>(IDbConnection dbConn)
            {
                return inner.SqlExpression<T>(dbConn);
            }

            public IDbCommand CreateCommand(IDbConnection dbConn)
            {
                var innerCommand = inner.CreateCommand(dbConn);
                return new PostExecuteActionCommand(innerCommand, postExecuteAction, DialectProvider);
            }

            public void DisposeCommand(IDbCommand dbCmd, IDbConnection dbConn)
            {
                inner.DisposeCommand(dbCmd, dbConn);
            }

            public T Exec<T>(IDbConnection dbConn, Func<IDbCommand, T> filter)
            {
                var cmd = CreateCommand(dbConn);
                try
                {
                    return filter(cmd);
                }
                finally
                {
                    DisposeCommand(cmd, dbConn);
                }
            }

            public IDbCommand Exec(IDbConnection dbConn, Func<IDbCommand, IDbCommand> filter)
            {
                var cmd = CreateCommand(dbConn);
                return filter(cmd);
            }

            public async Task<T> Exec<T>(IDbConnection dbConn, Func<IDbCommand, Task<T>> filter)
            {
                var cmd = CreateCommand(dbConn);
                try
                {
                    return await filter(cmd);
                }
                finally
                {
                    DisposeCommand(cmd, dbConn);
                }
            }

            public async Task<IDbCommand> Exec(IDbConnection dbConn, Func<IDbCommand, Task<IDbCommand>> filter)
            {
                var cmd = CreateCommand(dbConn);
                return await filter(cmd);
            }

            public void Exec(IDbConnection dbConn, Action<IDbCommand> filter)
            {
                var cmd = CreateCommand(dbConn);
                try
                {
                    filter(cmd);
                }
                finally
                {
                    DisposeCommand(cmd, dbConn);
                }
            }

            public async Task Exec(IDbConnection dbConn, Func<IDbCommand, Task> filter)
            {
                var cmd = CreateCommand(dbConn);
                try
                {
                    await filter(cmd);
                }
                finally
                {
                    DisposeCommand(cmd, dbConn);
                }
            }

            public IEnumerable<T> ExecLazy<T>(IDbConnection dbConn, Func<IDbCommand, IEnumerable<T>> filter)
            {
                var cmd = CreateCommand(dbConn);
                try
                {
                    var results = filter(cmd);

                    foreach (var item in results)
                    {
                        yield return item;
                    }
                }
                finally
                {
                    DisposeCommand(cmd, dbConn);
                }
            }
        }

        private class PostExecuteActionCommand : IDbCommand, IHasDialectProvider
        {
            private readonly IDbCommand inner;
            private readonly Action<IDbCommand> postExecuteAction;
            public IOrmLiteDialectProvider DialectProvider { get; }

            public PostExecuteActionCommand(IDbCommand inner, Action<IDbCommand> postExecuteAction, IOrmLiteDialectProvider dialectProvider)
            {
                this.inner = inner;
                this.postExecuteAction = postExecuteAction;
                this.DialectProvider = dialectProvider;
            }

            public void Dispose()
            {
                inner.Dispose();
            }

            public void Prepare()
            {
                inner.Prepare();
            }

            public void Cancel()
            {
                inner.Cancel();
            }

            public IDbDataParameter CreateParameter()
            {
                return inner.CreateParameter();
            }

            public int ExecuteNonQuery()
            {
                var result = inner.ExecuteNonQuery();
                postExecuteAction(this);
                return result;
            }

            public IDataReader ExecuteReader()
            {
                var result = inner.ExecuteReader();
                postExecuteAction(this);
                return result;
            }

            public IDataReader ExecuteReader(CommandBehavior behavior)
            {
                var result = inner.ExecuteReader(behavior);
                postExecuteAction(this);
                return result;
            }

            public object ExecuteScalar()
            {
                var result = inner.ExecuteScalar();
                postExecuteAction(this);
                return result;
            }

            public IDbConnection Connection
            {
                get => inner.Connection;
                set => inner.Connection = value;
            }

            public IDbTransaction Transaction
            {
                get => inner.Transaction;
                set => inner.Transaction = value;
            }

            public string CommandText
            {
                get => inner.CommandText;
                set => inner.CommandText = value;
            }

            public int CommandTimeout
            {
                get => inner.CommandTimeout;
                set => inner.CommandTimeout = value;
            }

            public CommandType CommandType
            {
                get => inner.CommandType;
                set => inner.CommandType = value;
            }

            public IDataParameterCollection Parameters => inner.Parameters;

            public UpdateRowSource UpdatedRowSource
            {
                get => inner.UpdatedRowSource;
                set => inner.UpdatedRowSource = value;
            }
        }
    }

    public class MultipleConnection
    {
        [AutoIncrement]
        public long Id { get; set; }

        public string Data { get; set; }
    }
}
