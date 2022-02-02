using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;

namespace ServiceStack.MiniProfiler.Storage
{
    /// <summary>
    /// Understands how to save MiniProfiler results to a MSSQL database, allowing more permanent storage and
    /// querying of slow results.
    /// </summary>
    public abstract class DatabaseStorageBase : IStorage
    {
        /// <summary>
        /// How we connect to the database used to save/load MiniProfiler results.
        /// </summary>
        protected string ConnectionString { get; set; }

        /// <summary>
        /// Returns a new SqlServerDatabaseStorage object that will insert into the database identified by connectionString.
        /// </summary>
        public DatabaseStorageBase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Saves 'profiler' to a database under its <see cref="MiniProfiler.Id"/>.
        /// </summary>
        public abstract void Save(MiniProfiler profiler);

        /// <summary>
        /// Returns the MiniProfiler identified by 'id' from the database or null when no MiniProfiler exists under that 'id'.
        /// </summary>
        public abstract MiniProfiler Load(Guid id);

        /// <summary>
        /// Returns a list of <see cref="MiniProfiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current MiniProfiler.Settings.UserProvider.</param>
        public abstract List<Guid> GetUnviewedIds(string user);

        /// <summary>
        /// Returns a DbConnection for your specific provider.
        /// </summary>
        protected abstract DbConnection GetConnection();

        /// <summary>
        /// Returns a DbConnection already opened for execution.
        /// </summary>
        protected DbConnection GetOpenConnection()
        {
            var result = GetConnection();
            if (result.State != System.Data.ConnectionState.Open)
                result.Open();
            return result;
        }
    }
}
