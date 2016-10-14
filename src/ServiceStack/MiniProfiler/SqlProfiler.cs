#if !NETSTANDARD1_6

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;
using ServiceStack.MiniProfiler.Data;

namespace ServiceStack.MiniProfiler
{

    // TODO: refactor this out into MiniProfiler
    /// <summary>
    /// Contains helper code to time sql statements.
    /// </summary>
    public class SqlProfiler
    {
        ConcurrentDictionary<Tuple<object, ExecuteType>, SqlTiming> inProgress = new ConcurrentDictionary<Tuple<object, ExecuteType>, SqlTiming>();
        ConcurrentDictionary<DbDataReader, SqlTiming> inProgressReaders = new ConcurrentDictionary<DbDataReader, SqlTiming>();

        /// <summary>
        /// The profiling session this SqlProfiler is part of.
        /// </summary>
        public Profiler Profiler { get; private set; }

        /// <summary>
        /// Returns a new SqlProfiler to be used in the 'profiler' session.
        /// </summary>
        public SqlProfiler(Profiler profiler)
        {
            Profiler = profiler;
        }

        /// <summary>
        /// Tracks when 'command' is started.
        /// </summary>
        public void ExecuteStartImpl(DbCommand command, ExecuteType type)
        {
            var id = Tuple.Create((object)command, type);
            var sqlTiming = new SqlTiming(command, type, Profiler);

            inProgress[id] = sqlTiming;
        }
        /// <summary>
        /// Returns all currently open commands on this connection
        /// </summary>
        public SqlTiming[] GetInProgressCommands()
        {
            return inProgress.Values.OrderBy(x => x.StartMilliseconds).ToArray();
        }
        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        public void ExecuteFinishImpl(DbCommand command, ExecuteType type, DbDataReader reader = null)
        {
            var id = Tuple.Create((object)command, type);
            var current = inProgress[id];
            current.ExecutionComplete(isReader: reader != null);
            SqlTiming ignore;
            inProgress.TryRemove(id, out ignore);
            if (reader != null)
            {
                inProgressReaders[reader] = current;
            }
        }

        /// <summary>
        /// Called when 'reader' finishes its iterations and is closed.
        /// </summary>
        public void ReaderFinishedImpl(DbDataReader reader)
        {
            SqlTiming stat;
            // this reader may have been disposed/closed by reader code, not by our using()
            if (inProgressReaders.TryGetValue(reader, out stat))
            {
                stat.ReaderFetchComplete();
                SqlTiming ignore;
                inProgressReaders.TryRemove(reader, out ignore);
            }
        }
    }

    /// <summary>
    /// Helper methods that allow operation on SqlProfilers, regardless of their instantiation.
    /// </summary>
    public static class SqlProfilerExtensions
    {
        /// <summary>
        /// Tracks when 'command' is started.
        /// </summary>
        public static void ExecuteStart(this SqlProfiler sqlProfiler, DbCommand command, ExecuteType type)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ExecuteStartImpl(command, type);
        }

        /// <summary>
        /// Finishes profiling for 'command', recording durations.
        /// </summary>
        public static void ExecuteFinish(this SqlProfiler sqlProfiler, DbCommand command, ExecuteType type, DbDataReader reader = null)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ExecuteFinishImpl(command, type, reader);
        }

        /// <summary>
        /// Called when 'reader' finishes its iterations and is closed.
        /// </summary>
        public static void ReaderFinish(this SqlProfiler sqlProfiler, DbDataReader reader)
        {
            if (sqlProfiler == null) return;
            sqlProfiler.ReaderFinishedImpl(reader);
        }

    }
}

#endif
