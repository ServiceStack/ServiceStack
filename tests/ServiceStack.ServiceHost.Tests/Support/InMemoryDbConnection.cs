using System.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.ServiceHost.Tests.Support
{
    /// <summary>
    /// LAMO hack I'm forced to do to because I can't register a simple delegate 
    /// to create my instance type
    /// </summary>
    public class InMemoryDbConnection
        : IDbConnection
    {
        private readonly IDbConnection inner;
        public InMemoryDbConnection()
        {
            this.inner = ":memory:".OpenDbConnection();
        }

        public void Dispose()
        {
            this.inner.Dispose();
        }

        public IDbTransaction BeginTransaction()
        {
            return this.inner.BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return this.inner.BeginTransaction(il);
        }

        public void Close()
        {
            this.inner.Close();
        }

        public void ChangeDatabase(string databaseName)
        {
            this.inner.ChangeDatabase(databaseName);
        }

        public IDbCommand CreateCommand()
        {
            return this.inner.CreateCommand();
        }

        public void Open()
        {
            this.inner.Open();
        }

        public string ConnectionString
        {
            get { return this.inner.ConnectionString; }
            set { this.inner.ConnectionString = value; }
        }

        public int ConnectionTimeout
        {
            get { return this.inner.ConnectionTimeout; }
        }

        public string Database
        {
            get { return this.inner.Database; }
        }

        public ConnectionState State
        {
            get { return this.inner.State; }
        }
    }
}