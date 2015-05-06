using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.MiniProfiler.Helpers;
//using System.Web.Script.Serialization;

namespace ServiceStack.MiniProfiler
{
    /// <summary>
    /// Profiles a single sql execution.
    /// </summary>
    [Exclude(Feature.Soap)]
    [DataContract]
    public class SqlTiming
    {
        /// <summary>
        /// Unique identifier for this SqlTiming.
        /// </summary>
		//[ScriptIgnore]
        public Guid Id { get; set; }

        /// <summary>
        /// Category of sql statement executed.
        /// </summary>
        [DataMember(Order = 1)]
        public ExecuteType ExecuteType { get; set; }

        /// <summary>
        /// The sql that was executed.
        /// </summary>
		//[ScriptIgnore]
        [DataMember(Order = 2)]
        public string CommandString { get; set; }

        /// <summary>
        /// The command string with special formatting applied based on MiniProfiler.Settings.SqlFormatter
        /// </summary>
		[DataMember(Order = 3)]
		public string FormattedCommandString
        {
            get
            {
                if (Profiler.Settings.SqlFormatter == null) return CommandString;

                return Profiler.Settings.SqlFormatter.FormatSql(this);
            }
        }

        /// <summary>
        /// Roughly where in the calling code that this sql was executed.
        /// </summary>
        [DataMember(Order = 4)]
        public string StackTraceSnippet { get; set; }

        /// <summary>
        /// Offset from main MiniProfiler start that this sql began.
        /// </summary>
        [DataMember(Order = 5)]
        public decimal StartMilliseconds { get; set; }

        /// <summary>
        /// How long this sql statement took to execute.
        /// </summary>
        [DataMember(Order = 6)]
        public decimal DurationMilliseconds { get; set; }

        /// <summary>
        /// When executing readers, how long it took to come back initially from the database, 
        /// before all records are fetched and reader is closed.
        /// </summary>
        [DataMember(Order = 7)]
        public decimal FirstFetchDurationMilliseconds { get; set; }

        /// <summary>
        /// Stores any parameter names and values used by the profiled DbCommand.
        /// </summary>
        [DataMember(Order = 8)]
        public List<SqlTimingParameter> Parameters { get; set; }

        /// <summary>
        /// Id of the Timing this statement was executed in.
        /// </summary>
        /// <remarks>
        /// Needed for database deserialization.
        /// </remarks>
		[DataMember(Order = 9)]
		public Guid? ParentTimingId { get; set; }

        private Timing _parentTiming;
        /// <summary>
        /// The Timing step that this sql execution occurred in.
        /// </summary>
        //[ScriptIgnore]
        public Timing ParentTiming
        {
            get { return _parentTiming; }
            set
            {
                _parentTiming = value;

                if (value != null && ParentTimingId != value.Id)
                    ParentTimingId = value.Id;
            }
        }

        /// <summary>
        /// True when other identical sql statements have been executed during this MiniProfiler session.
        /// </summary>
        [DataMember(Order = 10)]
        public bool IsDuplicate { get; set; }

        private long _startTicks;
        private Profiler _profiler;

        /// <summary>
        /// Creates a new SqlTiming to profile 'command'.
        /// </summary>
        public SqlTiming(DbCommand command, ExecuteType type, Profiler profiler)
        {
            Id = Guid.NewGuid();

            CommandString = AddSpacesToParameters(command.CommandText);
            Parameters = GetCommandParameters(command);
            ExecuteType = type;

            if (!Profiler.Settings.ExcludeStackTraceSnippetFromSqlTimings)
                StackTraceSnippet = ServiceStack.MiniProfiler.Helpers.StackTraceSnippet.Get();

            _profiler = profiler;
            if (_profiler != null)
            {
                _profiler.AddSqlTiming(this);
                _startTicks = _profiler.ElapsedTicks;
                StartMilliseconds = _profiler.GetRoundedMilliseconds(_startTicks);
            }
        }

        /// <summary>
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public SqlTiming()
        {
        }

        /// <summary>
        /// Returns a snippet of the sql command and the duration.
        /// </summary>
        public override string ToString()
        {
            return CommandString.Truncate(30) + " (" + DurationMilliseconds + " ms)";
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj != null && obj is SqlTiming && Id.Equals(((SqlTiming)obj).Id);
        }

        /// <summary>
        /// Returns hashcode of Id.
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Called when command execution is finished to determine this SqlTiming's duration.
        /// </summary>
        public void ExecutionComplete(bool isReader)
        {
            if (isReader)
            {
                FirstFetchDurationMilliseconds = GetDurationMilliseconds();
            }
            else
            {
                DurationMilliseconds = GetDurationMilliseconds();
            }
        }

        /// <summary>
        /// Called when database reader is closed, ending profiling for <see cref="ServiceStack.MiniProfiler.Data.ExecuteType.Reader"/> SqlTimings.
        /// </summary>
        public void ReaderFetchComplete()
        {
            DurationMilliseconds = GetDurationMilliseconds();
        }

        private decimal GetDurationMilliseconds()
        {
            return _profiler.GetRoundedMilliseconds(_profiler.ElapsedTicks - _startTicks);
        }

        /// <summary>
        /// To help with display, put some space around sammiched commas
        /// </summary>
        private string AddSpacesToParameters(string commandString)
        {
            return Regex.Replace(commandString, @",([^\s])", ", $1");
        }

        private List<SqlTimingParameter> GetCommandParameters(DbCommand command)
        {
            if (command.Parameters == null || command.Parameters.Count == 0) return null;

            var result = new List<SqlTimingParameter>();

            foreach (DbParameter dbParameter in command.Parameters)
            {
				if (!dbParameter.ParameterName.IsNullOrWhiteSpace())
                {
                    var formattedParameterValue = GetFormattedParameterValue(dbParameter);
                    result.Add(new SqlTimingParameter
                    {
                        ParentSqlTimingId = Id,
                        Name = dbParameter.ParameterName.Trim(),
                        Value = formattedParameterValue,
                        DbType = dbParameter.DbType.ToString(),
                        Size = dbParameter.Size
                    });
                }
            }

            return result;
        }

        private static string GetFormattedParameterValue(DbParameter dbParameter)
        {
            object rawValue = dbParameter.Value;
            if (rawValue == null || rawValue == DBNull.Value)
            {
                return null;
            }

            return rawValue is DateTime
                    ? ((DateTime)rawValue).ToString("s", System.Globalization.CultureInfo.InvariantCulture)
                    : rawValue.ToString();
        }
    }
}