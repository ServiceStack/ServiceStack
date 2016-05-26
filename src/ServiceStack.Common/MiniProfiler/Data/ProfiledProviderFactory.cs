using System.Data.Common;

namespace ServiceStack.MiniProfiler.Data
{
    /// <summary>
    /// Wrapper for a db provider factory to enable profiling
    /// </summary>
    public class ProfiledProviderFactory : DbProviderFactory
    {
        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public static ProfiledProviderFactory Instance = new ProfiledProviderFactory();

        protected ProfiledProviderFactory()
	    {
	    }

        protected IDbProfiler Profiler { get; private set; }
        protected DbProviderFactory WrappedFactory { get; private set; }

        /// <summary>
        /// Allow to re-init the provider factory.
        /// </summary>
        /// <param name="profiler"></param>
        /// <param name="wrappedFactory"></param>
        public void InitProfiledDbProviderFactory(IDbProfiler profiler, DbProviderFactory wrappedFactory)
        {
            Profiler = profiler;
            WrappedFactory = wrappedFactory;
        }

        /// <summary>
        /// proxy
        /// </summary>
        /// <param name="profiler"></param>
        /// <param name="wrappedFactory"></param>
        public ProfiledProviderFactory(IDbProfiler profiler, DbProviderFactory wrappedFactory)
        {
            Profiler = profiler;
            WrappedFactory = wrappedFactory;
        }

        /// <summary>
        /// proxy
        /// </summary>
        public override bool CanCreateDataSourceEnumerator
        {
            get
            {
                return WrappedFactory.CanCreateDataSourceEnumerator;
            }
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            return WrappedFactory.CreateDataSourceEnumerator();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbCommand CreateCommand()
        {
            return new ProfiledCommand(WrappedFactory.CreateCommand(), null, Profiler);
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbConnection CreateConnection()
        {
            return new ProfiledConnection(WrappedFactory.CreateConnection(), Profiler);
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbParameter CreateParameter()
        {
            return WrappedFactory.CreateParameter();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return WrappedFactory.CreateConnectionStringBuilder();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbCommandBuilder CreateCommandBuilder()
        {
            return WrappedFactory.CreateCommandBuilder();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override DbDataAdapter CreateDataAdapter()
        {
            return WrappedFactory.CreateDataAdapter();
        }
        /// <summary>
        /// proxy
        /// </summary>
        public override System.Security.CodeAccessPermission CreatePermission(System.Security.Permissions.PermissionState state)
        {
            return WrappedFactory.CreatePermission(state);
        }
    }
}
