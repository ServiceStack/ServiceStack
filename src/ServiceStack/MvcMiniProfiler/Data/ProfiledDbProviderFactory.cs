using System;
using System.Data.Common;
using MvcMiniProfiler;

namespace MvcMiniProfiler.Data
{
    /// <summary>
    /// Wrapper for a db provider factory to enable profiling
    /// </summary>
    public class ProfiledDbProviderFactory : DbProviderFactory
    {

        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static ProfiledDbProviderFactory Instance = new ProfiledDbProviderFactory();

        private IDbProfiler profiler;
        private DbProviderFactory tail;


        /// <summary>
        /// Used for db provider apis internally 
        /// </summary>
        private ProfiledDbProviderFactory ()
	    {

	    }

        /// <summary>
        /// Allow to re-init the provider factory.
        /// </summary>
        /// <param name="profiler"></param>
        /// <param name="tail"></param>
        public void InitProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory tail)
        {
            this.profiler = profiler;
            this.tail = tail;
        }

        /// <summary>
        /// proxy
        /// </summary>
        /// <param name="profiler"></param>
        /// <param name="tail"></param>
        public ProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory tail)
        {
            this.profiler = profiler;
            this.tail = tail;
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                return tail.CanCreateDataSourceEnumerator;
            }
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            return tail.CreateDataSourceEnumerator();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbCommand CreateCommand()
        {
            return new ProfiledDbCommand(tail.CreateCommand(), null, profiler);
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbConnection CreateConnection()
        {
            return new ProfiledDbConnection(tail.CreateConnection(), profiler);
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbParameter CreateParameter()
        {
            return tail.CreateParameter();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return tail.CreateConnectionStringBuilder();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            return tail.CreateCommandBuilder();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbDataAdapter CreateDataAdapter()
        {
            return tail.CreateDataAdapter();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override System.Security.CodeAccessPermission CreatePermission(System.Security.Permissions.PermissionState state)
        {
            return tail.CreatePermission(state);
        }

    }
}
