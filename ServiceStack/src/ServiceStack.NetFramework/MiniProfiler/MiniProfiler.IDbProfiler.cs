#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Runtime.Serialization;
using ServiceStack.MiniProfiler.Data;

namespace ServiceStack.MiniProfiler
{
    partial class MiniProfiler : IDbProfiler
    {
        /// <summary>
        /// Contains information about queries executed during this profiling session.
        /// </summary>
        internal SqlProfiler SqlProfiler { get; private set; }

        /// <summary>
        /// Returns all currently open commands on this connection
        /// </summary>
        public SqlTiming[] GetInProgressCommands()
        {
            return SqlProfiler?.GetInProgressCommands();
        }

        /// <summary>
        /// Milliseconds, to one decimal place, that this MiniProfiler was executing sql.
        /// </summary>
		[DataMember(Order = 14, Name = "DurationMillisecondsInSql")]
		public decimal DurationMillisecondsInSql
        {
            get { return GetTimingHierarchy().Sum(t => t.HasSqlTimings ? t.SqlTimings.Sum(s => s.DurationMilliseconds) : 0); }
        }

        /// <summary>
        /// Returns true when we have profiled queries.
        /// </summary>
		[DataMember(Order = 15, Name = "HasSqlTimings")]
		public bool HasSqlTimings { get; set; }

        /// <summary>
        /// Returns true when any child Timings have duplicate queries.
        /// </summary>
		[DataMember(Order = 16, Name = "HasDuplicateSqlTimings")]
		public bool HasDuplicateSqlTimings { get; set; }

        /// <summary>
        /// How many sql data readers were executed in all <see cref="Timing"/> steps.
        /// </summary>
		[DataMember(Order = 17, Name = "ExecutedReaders")]
		public int ExecutedReaders => GetExecutedCount(ExecuteType.Reader);

        /// <summary>
        /// How many sql scalar queries were executed in all <see cref="Timing"/> steps.
        /// </summary>
		[DataMember(Order = 18, Name = "ExecutedScalars")]
		public int ExecutedScalars => GetExecutedCount(ExecuteType.Scalar);

        /// <summary>
        /// How many sql non-query statements were executed in all <see cref="Timing"/> steps.
        /// </summary>
		[DataMember(Order = 19, Name = "ExecutedNonQueries")]
		public int ExecutedNonQueries => GetExecutedCount(ExecuteType.NonQuery);

        /// <summary>
        /// Returns all <see cref="SqlTiming"/> results contained in all child <see cref="Timing"/> steps.
        /// </summary>
        public List<SqlTiming> GetSqlTimings()
        {
            return GetTimingHierarchy().Where(t => t.HasSqlTimings).SelectMany(t => t.SqlTimings).ToList();
        }

        /// <summary>
        /// Contains any sql statements that are executed, along with how many times those statements are executed.
        /// </summary>
        private readonly Dictionary<string, int> _sqlExecutionCounts = new Dictionary<string, int>();

        /// <summary>
        /// Adds <paramref name="stats"/> to the current <see cref="Timing"/>.
        /// </summary>
        internal void AddSqlTiming(SqlTiming stats)
        {
            if (Head == null)
                return;

            stats.IsDuplicate = _sqlExecutionCounts.TryGetValue(stats.CommandString, out var count);
            _sqlExecutionCounts[stats.CommandString] = count + 1;

            HasSqlTimings = true;
            if (stats.IsDuplicate)
            {
                HasDuplicateSqlTimings = true;
            }

            Head.AddSqlTiming(stats);
        }

        /// <summary>
        /// Returns the number of sql statements of <paramref name="type"/> that were executed in all <see cref="Timing"/>s.
        /// </summary>
        private int GetExecutedCount(ExecuteType type)
        {
            return HasSqlTimings ? GetTimingHierarchy().Sum(t => t.GetExecutedCount(type)) : 0;
        }


        // IDbProfiler methods

        void IDbProfiler.ExecuteStart(DbCommand profiledDbCommand, ExecuteType executeType)
        {
            SqlProfiler.ExecuteStart(profiledDbCommand, executeType);
        }

        void IDbProfiler.ExecuteFinish(DbCommand profiledDbCommand, ExecuteType executeType, DbDataReader reader)
        {
            if (reader != null)
            {
                SqlProfiler.ExecuteFinish(profiledDbCommand, executeType, reader);
            }
            else
            {
                SqlProfiler.ExecuteFinish(profiledDbCommand, executeType);
            }
        }

        void IDbProfiler.ReaderFinish(DbDataReader reader)
        {
            SqlProfiler.ReaderFinish(reader);
        }

        void IDbProfiler.OnError(DbCommand profiledDbCommand, ExecuteType executeType, Exception exception)
        {
            // TODO: implement errors aggregation and presentation
        }


        bool _isActive;
        bool IDbProfiler.IsActive { get { return _isActive; } }
        internal bool IsActive { set { _isActive = value; } }

    }
}

#endif
