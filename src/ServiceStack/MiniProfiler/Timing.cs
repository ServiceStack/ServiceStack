using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.MiniProfiler.Storage;
//using System.Web.Script.Serialization;

namespace ServiceStack.MiniProfiler
{
    /// <summary>
    /// An individual profiling step that can contain child steps.
    /// </summary>
    [Exclude(Feature.Soap)]
    [DataContract]
    public class Timing : IDisposable
    {
        /// <summary>
        /// Unique identifer for this timing; set during construction.
        /// </summary>
        [DataMember(Order = 1)]
        public Guid Id { get; set; }

        /// <summary>
        /// Text displayed when this Timing is rendered.
        /// </summary>
        [DataMember(Order = 2)]
        public string Name { get; set; }

        /// <summary>
        /// How long this Timing step took in ms; includes any <see cref="Children"/> Timings' durations.
        /// </summary>
        [DataMember(Order = 3)]
        public decimal? DurationMilliseconds { get; set; }

        /// <summary>
        /// The offset from the start of profiling.
        /// </summary>
        [DataMember(Order = 4)]
        public decimal StartMilliseconds { get; set; }

        /// <summary>
        /// All sub-steps that occur within this Timing step. Add new children through <see cref="AddChild"/>
        /// </summary>
        [DataMember(Order = 5)]
        public List<Timing> Children { get; set; }

        /// <summary>
        /// Stores arbitrary key/value strings on this Timing step. Add new tuples through <see cref="AddKeyValue"/>.
        /// </summary>
        [DataMember(Order = 6)]
        public Dictionary<string, string> KeyValues { get; set; }

        /// <summary>
        /// Any queries that occurred during this Timing step.
        /// </summary>
        [DataMember(Order = 7)]
        public List<SqlTiming> SqlTimings { get; set; }

        /// <summary>
        /// Needed for database deserialization and JSON serialization.
        /// </summary>
		[DataMember(Order = 8)]
		public Guid? ParentTimingId { get; set; }

        private Timing _parentTiming;
        /// <summary>
        /// Which Timing this Timing is under - the duration that this step takes will be added to its parent's duration.
        /// </summary>
        /// <remarks>This will be null for the root (initial) Timing.</remarks>
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
        /// Rebuilds all the parent timings on deserialization calls
        /// </summary>
        public void RebuildParentTimings()
        {
            if (SqlTimings != null)
            {
                foreach (var timing in SqlTimings)
                {
                    timing.ParentTiming = this;
                }
            }
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    child.ParentTiming = this;
                    child.RebuildParentTimings();
                }
            }
        }

        /// <summary>
        /// Gets the elapsed milliseconds in this step without any children's durations.
        /// </summary>
		[DataMember]
		public decimal DurationWithoutChildrenMilliseconds
        {
            get
            {
                var result = DurationMilliseconds.GetValueOrDefault();

                if (HasChildren)
                {
                    foreach (var child in Children)
                    {
                        result -= child.DurationMilliseconds.GetValueOrDefault();
                    }
                }

                return Math.Round(result, 1);
            }
        }

        /// <summary>
        /// Gets the aggregate elapsed milliseconds of all SqlTimings executed in this Timing, excluding Children Timings.
        /// </summary>
		[DataMember]
		public decimal SqlTimingsDurationMilliseconds
        {
            get { return HasSqlTimings ? Math.Round(SqlTimings.Sum(s => s.DurationMilliseconds), 1) : 0; }
        }

        /// <summary>
        /// Returns true when this <see cref="DurationWithoutChildrenMilliseconds"/> is less than the configured
        /// <see cref="MiniProfiler.Profiler.Settings.TrivialDurationThresholdMilliseconds"/>, by default 2.0 ms.
        /// </summary>
		[DataMember]
		public bool IsTrivial
        {
            get { return DurationWithoutChildrenMilliseconds <= MiniProfiler.Profiler.Settings.TrivialDurationThresholdMilliseconds; }
        }

        /// <summary>
        /// Reference to the containing profiler, allowing this Timing to affect the Head and get Stopwatch readings.
        /// </summary>
        internal Profiler Profiler { get; private set; }

        /// <summary>
        /// Offset from parent MiniProfiler's creation that this Timing was created.
        /// </summary>
        private readonly long _startTicks;

        /// <summary>
        /// Returns true when this Timing has inner Timing steps.
        /// </summary>
		[DataMember]
		public bool HasChildren
        {
            get { return Children != null && Children.Count > 0; }
        }

        /// <summary>
        /// Returns true if this Timing step collected sql execution timings.
        /// </summary>
		[DataMember]
		public bool HasSqlTimings
        {
            get { return SqlTimings != null && SqlTimings.Count > 0; }
        }

        /// <summary>
        /// Returns true if any <see cref="SqlTiming"/>s executed in this step are detected as duplicate statements.
        /// </summary>
		[DataMember]
		public bool HasDuplicateSqlTimings
        {
            get { return HasSqlTimings && SqlTimings.Any(s => s.IsDuplicate); }
        }

        /// <summary>
        /// Returns true when this Timing is the first one created in a MiniProfiler session.
        /// </summary>
		[DataMember]
		public bool IsRoot
        {
            get { return ParentTiming == null; }
        }

        /// <summary>
        /// How far away this Timing is from the Profiler's Root.
        /// </summary>
        [DataMember]
        public Int16 Depth
        {
            get
            {
                Int16 result = 0;
                var parent = ParentTiming;

                while (parent != null)
                {
                    parent = parent.ParentTiming;
                    result++;
                }

                return result;
            }
        }

        /// <summary>
        /// How many sql data readers were executed in this Timing step. Does not include queries in any child Timings.
        /// </summary>
		[DataMember]
		public int ExecutedReaders
        {
            get { return GetExecutedCount(ExecuteType.Reader); }
        }

        /// <summary>
        /// How many sql scalar queries were executed in this Timing step. Does not include queries in any child Timings.
        /// </summary>
		[DataMember]
		public int ExecutedScalars
        {
            get { return GetExecutedCount(ExecuteType.Scalar); }
        }

        /// <summary>
        /// How many sql non-query statements were executed in this Timing step. Does not include queries in any child Timings.
        /// </summary>
		[DataMember]
		public int ExecutedNonQueries
        {
            get { return GetExecutedCount(ExecuteType.NonQuery); }
        }

        /// <summary>
        /// Creates a new Timing named 'name' in the 'profiler's session, with 'parent' as this Timing's immediate ancestor.
        /// </summary>
        public Timing(Profiler profiler, Timing parent, string name)
        {
            this.Id = Guid.NewGuid();
            Profiler = profiler;
            Profiler.Head = this;

            if (parent != null) // root will have no parent
            {
                parent.AddChild(this);
            }

            Name = name;

            _startTicks = profiler.ElapsedTicks;
            StartMilliseconds = profiler.GetRoundedMilliseconds(_startTicks);
        }
        /// <summary>
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public Timing()
        {
        }

        /// <summary>
        /// Returns this Timing's Name.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj != null && obj is Timing && Id.Equals(((Timing)obj).Id);
        }

        /// <summary>
        /// Returns hashcode of Id.
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Adds arbitrary string 'value' under 'key', allowing custom properties to be stored in this Timing step.
        /// </summary>
        public void AddKeyValue(string key, string value)
        {
            if (KeyValues == null)
                KeyValues = new Dictionary<string, string>();

            KeyValues[key] = value;
        }

        /// <summary>
        /// Completes this Timing's duration and sets the MiniProfiler's Head up one level.
        /// </summary>
        public void Stop()
        {
            if (DurationMilliseconds == null)
            {
                DurationMilliseconds = Profiler.GetRoundedMilliseconds(Profiler.ElapsedTicks - _startTicks);
                Profiler.Head = ParentTiming;
            }
        }

        void IDisposable.Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Add the parameter 'timing' to this Timing's Children collection.
        /// </summary>
        /// <remarks>
        /// Used outside this assembly for custom deserialization when creating an <see cref="IStorage"/> implementation.
        /// </remarks>
        public void AddChild(Timing timing)
        {
            if (Children == null)
                Children = new List<Timing>();

            Children.Add(timing);
            timing.ParentTiming = this;
        }

        /// <summary>
        /// Adds the parameter 'sqlTiming' to this Timing's SqlTimings collection.
        /// </summary>
        /// <param name="sqlTiming">A sql statement profiling that was executed in this Timing step.</param>
        /// <remarks>
        /// Used outside this assembly for custom deserialization when creating an <see cref="IStorage"/> implementation.
        /// </remarks>
        public void AddSqlTiming(SqlTiming sqlTiming)
        {
            if (SqlTimings == null)
                SqlTimings = new List<SqlTiming>();

            SqlTimings.Add(sqlTiming);
            sqlTiming.ParentTiming = this;
        }

        /// <summary>
        /// Returns the number of sql statements of <paramref name="type"/> that were executed in this <see cref="Timing"/>.
        /// </summary>
        internal int GetExecutedCount(ExecuteType type)
        {
            return HasSqlTimings ? SqlTimings.Count(s => s.ExecuteType == type) : 0;
        }
    }
}