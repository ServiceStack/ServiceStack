#if !NETSTANDARD1_6

using System.Data.Common;

namespace ServiceStack.MiniProfiler.Data
{
    /// <summary>
    /// Wrapper for a db provider factory to enable profiling
    /// </summary>
    public class ProfiledDbProviderFactory : ProfiledProviderFactory
    {
        /// <summary>
        /// Every provider factory must have an Instance public field
        /// </summary>
        public new static ProfiledDbProviderFactory Instance = new ProfiledDbProviderFactory();

        /// <summary>
        /// Used for db provider apis internally 
        /// </summary>
        private ProfiledDbProviderFactory ()
	    {
	    }

        /// <summary>
        /// proxy
        /// </summary>
        public override DbCommand CreateCommand()
        {
            return new ProfiledDbCommand(WrappedFactory.CreateCommand(), null, Profiler);
        }

        /// <summary>
        /// proxy
        /// </summary>
        public override DbConnection CreateConnection()
        {
            return new ProfiledDbConnection(WrappedFactory.CreateConnection(), Profiler);
        }
    }
}

#endif
