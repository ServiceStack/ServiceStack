using System;
using System.Runtime.Serialization;

namespace StackExchange.Profiling
{
    /// <summary>
    /// A custom timing that usually represents a Remote Procedure Call, allowing better
    /// visibility into these longer-running calls.
    /// </summary>
    [DataContract]
    public class CustomTiming : IDisposable
    {
        private readonly decimal? _minSaveMs;

        /// <summary>
        /// Don't use this.
        /// </summary>
        [Obsolete("Used for serialization")]
        public CustomTiming() { /* serialization only */ }

        /// <summary>
        /// Returns a new CustomTiming, also initializing its <see cref="Id"/> and, optionally, its <see cref="StackTraceSnippet"/>.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to attach the timing so.</param>
        /// <param name="commandString">The descriptive command string for this timing, e.g. a URL or database command.</param>
        /// <param name="minSaveMs">(Optional) The minimum time required to actually save this timing (e.g. do we care?).</param>
        public CustomTiming(MiniProfiler profiler, string commandString, decimal? minSaveMs = null)
        {
            _profiler = profiler;
            _startTicks = profiler.ElapsedTicks;
            _minSaveMs = minSaveMs;
            CommandString = commandString;

            Id = Guid.NewGuid();
            StartMilliseconds = profiler.GetRoundedMilliseconds(profiler.ElapsedTicks);

            if (true/*!MiniProfiler.Settings.ExcludeStackTraceSnippetFromCustomTimings*/)
            {
                StackTraceSnippet = Helpers.StackTraceSnippet.Get();
            }
        }

        private readonly MiniProfiler _profiler;
        private readonly long _startTicks;

        /// <summary>
        /// Unique identifier for this <see cref="CustomTiming"/>.
        /// </summary>
        [DataMember(Order = 1)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the command that was executed, e.g. "select * from Table" or "INCR my:redis:key"
        /// </summary>
        [DataMember(Order = 2)]
        public string CommandString { get; set; }

        // TODO: should this just match the key that the List<CustomTiming> is stored under in Timing.CustomTimings?
        /// <summary>
        /// Short name describing what kind of custom timing this is, e.g. "Get", "Query", "Fetch".
        /// </summary>
        [DataMember(Order = 3)]
        public string ExecuteType { get; set; }

        /// <summary>
        /// Gets or sets roughly where in the calling code that this custom timing was executed.
        /// </summary>
        [DataMember(Order = 4)]
        public string StackTraceSnippet { get; set; }

        /// <summary>
        /// Gets or sets the offset from main <c>MiniProfiler</c> start that this custom command began.
        /// </summary>
        [DataMember(Order = 5)]
        public decimal StartMilliseconds { get; set; }

        /// <summary>
        /// Gets or sets how long this custom command statement took to execute.
        /// </summary>
        [DataMember(Order = 6)]
        public decimal? DurationMilliseconds { get; set; }

        /// <summary>
        /// OPTIONAL - how long this tim took to come back initially from the remote server, 
        /// before all data is fetched and command is completed.
        /// </summary>
        [DataMember(Order = 7)]
        public decimal? FirstFetchDurationMilliseconds { get; set; }

        /// <summary>
        /// Whether this operation errored.
        /// </summary>
        [DataMember(Order = 8)]
        public bool Errored { get; set; }

        internal string Category { get; set; }

        /// <summary>
        /// OPTIONAL - call after receiving the first response from your Remote Procedure Call to 
        /// properly set <see cref="FirstFetchDurationMilliseconds"/>.
        /// </summary>
        public void FirstFetchCompleted()
        {
            FirstFetchDurationMilliseconds = FirstFetchDurationMilliseconds ?? _profiler.GetDurationMilliseconds(_startTicks);
        }

        /// <summary>
        /// Stops this timing, setting <see cref="DurationMilliseconds"/>.
        /// </summary>
        public void Stop()
        {
            DurationMilliseconds = DurationMilliseconds ?? _profiler.GetDurationMilliseconds(_startTicks);

            if (_minSaveMs.HasValue && _minSaveMs.Value > 0 && DurationMilliseconds < _minSaveMs.Value)
            {
                _profiler.Head.RemoveCustomTiming(Category, this);
            }
        }

        void IDisposable.Dispose() => Stop();

        /// <summary>
        /// Returns <see cref="CommandString"/> for debugging.
        /// </summary>
        public override string ToString() => CommandString;
    }
}
