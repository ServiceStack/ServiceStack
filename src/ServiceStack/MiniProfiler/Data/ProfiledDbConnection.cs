#if !NETSTANDARD1_6

using System;
using System.Data;
using System.Data.Common;

namespace ServiceStack.MiniProfiler.Data
{
    /// <summary>
    /// Wraps a database connection, allowing sql execution timings to be collected when a <see cref="MiniProfiler.Profiler"/> session is started.
    /// </summary>
    public class ProfiledDbConnection : ProfiledConnection, ICloneable
    {
        /// <summary>
        /// Returns a new <see cref="ProfiledDbConnection"/> that wraps <paramref name="connection"/>, 
        /// providing query execution profiling.  If profiler is null, no profiling will occur.
        /// </summary>
        /// <param name="connection">Your provider-specific flavor of connection, e.g. SqlConnection, OracleConnection</param>
        /// <param name="profiler">The currently started <see cref="IDbProfiler"/> or null.</param>
        /// <param name="autoDisposeConnection">Determines whether the ProfiledDbConnection will dispose the underlying connection.</param>
        public ProfiledDbConnection(DbConnection connection, IDbProfiler profiler, bool autoDisposeConnection = true)
            : base(connection, profiler, autoDisposeConnection)
        {
        }

        public ProfiledDbConnection(IDbConnection connection, IDbProfiler profiler, bool autoDisposeConnection=true)
            : base(connection, profiler, autoDisposeConnection)
        {
        }

        /// <summary>
        /// This will be made private; use <see cref="ProfiledConnection.InnerConnection"/>
        /// </summary>
        protected DbConnection _conn // TODO: in MiniProfiler 2.0, make private
        {
            get { return InnerConnection; }
            set { InnerConnection = value; }
        }

        /// <summary>
        /// This will be made private; use <see cref="Profiler"/>
        /// </summary>
        protected IDbProfiler _profiler // TODO: in MiniProfiler 2.0, make private
        {
            get { return Profiler; }
            set { Profiler = value; }
        }

        protected bool autoDisposeConnection // Wrapper property for backwards compatibility
        {
            get { return AutoDisposeConnection; }
            set { AutoDisposeConnection = value; }
        }

        protected override DbCommand CreateDbCommand()
        {
            return new ProfiledDbCommand(_conn.CreateCommand(), this, _profiler);
        }

        public ProfiledDbConnection Clone()
        {
            ICloneable tail = _conn as ICloneable;
            if (tail == null) throw new NotSupportedException("Underlying " + _conn.GetType().FullName + " is not cloneable");
            return new ProfiledDbConnection((DbConnection)tail.Clone(), _profiler, AutoDisposeConnection);
        }
        object ICloneable.Clone() { return Clone(); }
    }
}

#endif
