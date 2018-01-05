#if !NETSTANDARD2_0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.MiniProfiler.Helpers;
using ServiceStack.MiniProfiler.UI;
using ServiceStack.Text;
//using System.Web.Routing;
//using System.Web.Script.Serialization;

namespace ServiceStack.MiniProfiler
{
    public class MiniProfilerAdapter : IProfiler
    {
        public IProfiler Current => MiniProfiler.Current;

        public IProfiler Start() => MiniProfiler.Start(ProfileLevel.Info);
        public void Stop() => MiniProfiler.Stop(discardResults:false);
        public IDisposable Step(string name) => Current?.Step(name);

        public IHtmlString RenderIncludes(RenderPosition? position = null, bool? showTrivial = null, bool? showTimeWithChildren = null,
            int? maxTracesToShow = null, bool xhtml = false, bool? showControls = null)
        {
            var result = Current?.RenderIncludes(position, showTrivial, showTimeWithChildren, maxTracesToShow, xhtml, showControls);
            return result ?? HtmlString.Empty;
        }
    }
    
    /// <summary>
    /// A single MiniProfiler can be used to represent any number of steps/levels in a call-graph, via Step()
    /// </summary>
    /// <remarks>Totally baller.</remarks>
    [Exclude(Feature.Soap)]
    [DataContract]
    public partial class MiniProfiler : IProfiler 
    {
        /// <summary>
        /// Identifies this Profiler so it may be stored/cached.
        /// </summary>
        [DataMember(Order = 1, Name = "Id")]
        public Guid Id { get; set; }

        /// <summary>
        /// A display name for this profiling session.
        /// </summary>
        [DataMember(Order = 2, Name = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// When this profiler was instantiated.
        /// </summary>
        [DataMember(Order = 3, Name = "Started")]
        public DateTime Started { get; set; }

        /// <summary>
        /// Where this profiler was run.
        /// </summary>
        [DataMember(Order = 4, Name = "MachineName")]
        public string MachineName { get; set; }

        /// <summary>
        /// Allows filtering of <see cref="Timing"/> steps based on what <see cref="ProfileLevel"/> 
        /// the steps are created with.
        /// </summary>
        [DataMember(Order = 5, Name = "Level")]
        public ProfileLevel Level { get; set; }

        private Timing _root;
        /// <summary>
        /// The first <see cref="Timing"/> that is created and started when this profiler is instantiated.
        /// All other <see cref="Timing"/>s will be children of this one.
        /// </summary>
        [DataMember(Order = 6, Name = "Root")]
        public Timing Root
        {
            get { return _root; }
            set
            {
                _root = value;

                // when being deserialized, we need to go through and set all child timings' parents
                if (_root.HasChildren)
                {
                    var timings = new Stack<Timing>();

                    timings.Push(_root);

                    while (timings.Count > 0)
                    {
                        var timing = timings.Pop();

                        if (timing.HasChildren)
                        {
                            var children = timing.Children;

                            for (int i = children.Count - 1; i >= 0; i--)
                            {
                                children[i].ParentTiming = timing;
                                timings.Push(children[i]); // FLORIDA!  TODO: refactor this and other stack creation methods into one 
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A string identifying the user/client that is profiling this request.  Set <see cref="MiniProfiler.Settings.UserProvider"/>
        /// with an <see cref="IUserProvider"/>-implementing class to provide a custom value.
        /// </summary>
        /// <remarks>
        /// If this is not set manually at some point, the <see cref="MiniProfiler.Settings.UserProvider"/> implementation will be used;
        /// by default, this will be the current request's ip address.
        /// </remarks>
        [DataMember(Order = 7, Name = "User")]
        public string User { get; set; }

        /// <summary>
        /// Returns true when this MiniProfiler has been viewed by the <see cref="User"/> that recorded it.
        /// </summary>
        /// <remarks>
        /// Allows POSTs that result in a redirect to be profiled. <see cref="MiniProfiler.Settings.Storage"/> implementation
        /// will keep a list of all profilers that haven't been fetched down.
        /// </remarks>
        [DataMember(Order = 8, Name = "HasUserViewed")]
        public bool HasUserViewed { get; set; }

        /// <summary>
        /// Starts when this profiler is instantiated. Each <see cref="Timing"/> step will use this Stopwatch's current ticks as
        /// their starting time.
        /// </summary>
        private readonly IStopwatch _sw;
        /// <summary>
        /// For unit testing, returns the timer.
        /// </summary>
        internal IStopwatch Stopwatch { get { return _sw; } }

        /// <summary>
        /// Milliseconds, to one decimal place, that this MiniProfiler ran.
        /// </summary>
		[DataMember(Order = 9, Name = "DurationMilliseconds")]
        public decimal DurationMilliseconds
        {
            get { return _root.DurationMilliseconds ?? GetRoundedMilliseconds(ElapsedTicks); }
        }

        /// <summary>
        /// Returns true when <see cref="Root"/> or any of its <see cref="Timing.Children"/> are <see cref="Timing.IsTrivial"/>.
        /// </summary>
		[DataMember(Order = 10, Name = "HasTrivialTimings")]
		public bool HasTrivialTimings
        {
            get
            {
                foreach (var t in GetTimingHierarchy())
                {
                    if (t.IsTrivial)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns true when all child <see cref="Timing"/>s are <see cref="Timing.IsTrivial"/>.
        /// </summary>
		[DataMember(Order = 11, Name = "HasAllTrivialTimings")]
		public bool HasAllTrivialTimings
        {
            get
            {
                foreach (var t in GetTimingHierarchy())
                {
                    if (!t.IsTrivial)
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Any Timing step with a duration less than or equal to this will be hidden by default in the UI; defaults to 2.0 ms.
        /// </summary>
		[DataMember(Order = 12, Name = "TrivialDurationThresholdMilliseconds")]
		public decimal TrivialDurationThresholdMilliseconds
        {
            get { return Settings.TrivialDurationThresholdMilliseconds; }
        }

        /// <summary>
        /// Ticks since this MiniProfiler was started.
        /// </summary>
		internal long ElapsedTicks { get { return _sw.ElapsedTicks; } }

        /// <summary>
        /// Json representing the collection of CustomTimings relating to this Profiler
        /// </summary>
        /// <remarks>
        /// Is used when storing the Profiler in SqlStorage
        /// </remarks>
        [DataMember(Order = 13, Name = "Json")]
        public string Json { get; set; }

        /// <summary>
        /// Points to the currently executing Timing. 
        /// </summary>
        public Timing Head { get; set; }


        /// <summary>
        /// Creates and starts a new MiniProfiler for the root <paramref name="url"/>, filtering <see cref="Timing"/> steps to <paramref name="level"/>.
        /// </summary>
        public MiniProfiler(string url, ProfileLevel level = ProfileLevel.Info)
        {
            Id = Guid.NewGuid();
            Level = level;
            SqlProfiler = new SqlProfiler(this);
            MachineName = Environment.MachineName;
            Started = DateTime.UtcNow;

            // stopwatch must start before any child Timings are instantiated
            _sw = Settings.StopwatchProvider();
            Root = new Timing(this, parent: null, name: url);
        }


        /// <summary>
        /// Returns the <see cref="Root"/>'s <see cref="Timing.Name"/> and <see cref="DurationMilliseconds"/> this profiler recorded.
        /// </summary>
        public override string ToString()
        {
            return Root != null ? Root.Name + " (" + DurationMilliseconds + " ms)" : "";
        }

        /// <summary>
        /// Returns true if Ids match.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj != null && obj is MiniProfiler && Id.Equals(((MiniProfiler)obj).Id);
        }

        /// <summary>
        /// Returns hashcode of Id.
        /// </summary>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public IDisposable Step(string name)
        {
            return this.Step(name, ProfileLevel.Info);
        }

        /// <summary>
        /// Obsolete - used for serialization.
        /// </summary>
        [Obsolete("Used for serialization")]
        public MiniProfiler()
        {
        }

        internal IDisposable StepImpl(string name, ProfileLevel level = ProfileLevel.Info)
        {
            if (level > this.Level) return null;
            return new Timing(this, Head, name);
        }

        internal bool StopImpl()
        {
            if (!_sw.IsRunning)
                return false;

            _sw.Stop();
            foreach (var timing in GetTimingHierarchy()) timing.Stop();

            return true;
        }

        internal void AddDataImpl(string key, string value)
        {
            if (Head == null)
                return;

            Head.AddKeyValue(key, value);
        }

        /// <summary>
        /// Walks the <see cref="Timing"/> hierarchy contained in this profiler, starting with <see cref="Root"/>, and returns each Timing found.
        /// </summary>
        public IEnumerable<Timing> GetTimingHierarchy()
        {
            var timings = new Stack<Timing>();

            timings.Push(_root);

            while (timings.Count > 0)
            {
                var timing = timings.Pop();

                yield return timing;

                if (timing.HasChildren)
                {
                    var children = timing.Children;
                    for (int i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                }
            }
        }

        /// <summary>
        /// Returns milliseconds based on Stopwatch's Frequency.
        /// </summary>
        internal decimal GetRoundedMilliseconds(long stopwatchElapsedTicks)
        {
            long z = 10000 * stopwatchElapsedTicks;
            decimal msTimesTen = (int)(z / _sw.Frequency);
            return msTimesTen / 10;
        }

        public IProfiler Start() => Start(ProfileLevel.Info);
        public void Stop() => Stop(discardResults: false);

        /// <summary>
        /// Starts a new MiniProfiler based on the current <see cref="IProfilerProvider"/>. This new profiler can be accessed by
        /// <see cref="MiniProfiler.Current"/>
        /// </summary>
        public static MiniProfiler Start(ProfileLevel level)
        {
            Settings.EnsureProfilerProvider();
            return Settings.ProfilerProvider.Start(level);
        }

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        public static void Stop(bool discardResults)
        {
            Settings.EnsureProfilerProvider();
            Settings.ProfilerProvider.Stop(discardResults);
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal. Use this method when you
        /// do not wish to include the MvcMiniProfiler namespace for the <see cref="MiniProfilerExtensions.Step"/> extension method.
        /// </summary>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start"/> is called.</param>
        public static IDisposable StepStatic(string name, ProfileLevel level = ProfileLevel.Info)
        {
            return MiniProfilerExtensions.Step(Current, name, level);
        }

        /// <summary>
        /// Returns the css and javascript includes needed to display the MiniProfiler results UI.
        /// </summary>
        /// <param name="position">Which side of the page the profiler popup button should be displayed on (defaults to left)</param>
        /// <param name="showTrivial">Whether to show trivial timings by default (defaults to false)</param>
        /// <param name="showTimeWithChildren">Whether to show time the time with children column by default (defaults to false)</param>
        /// <param name="maxTracesToShow">The maximum number of trace popups to show before removing the oldest (defaults to 15)</param>
        /// <param name="xhtml">xhtml rendering mode, ensure script tag is closed ... etc</param>
        /// <param name="showControls">when true, shows buttons to minimize and clear MiniProfiler results</param>
        /// <returns>Script and link elements normally; an empty string when there is no active profiling session.</returns>
        public IHtmlString RenderIncludes(RenderPosition? position = null, bool? showTrivial = null, bool? showTimeWithChildren = null, int? maxTracesToShow = null, bool xhtml = false, bool? showControls = null)
        {
            return MiniProfilerHandler.RenderIncludes(Current, position, showTrivial, showTimeWithChildren, maxTracesToShow, xhtml, showControls);
        }

        /// <summary>
        /// Gets the currently running MiniProfiler for the current HttpContext; null if no MiniProfiler was <see cref="Start"/>ed.
        /// </summary>
        public static MiniProfiler Current
        {
            get
            {
                Settings.EnsureProfilerProvider();
                return Settings.ProfilerProvider.GetCurrentProfiler();
            }
        }


        /// <summary>
        /// Renders the current <see cref="MiniProfiler"/> to json.
        /// </summary>
        public static string ToJson()
        {
            return ToJson(Current);
        }

        /// <summary>
        /// Renders the parameter <see cref="MiniProfiler"/> to json.
        /// </summary>
        public static string ToJson(MiniProfiler profiler)
        {
            if (profiler == null) return null;

			profiler.OnDeserialized(default(StreamingContext)); //Added
            var result = JsonSerializer.SerializeToString(profiler);
            return result;
        }

        /// <summary>
        /// Deserializes the json string parameter to a <see cref="MiniProfiler"/>.
        /// </summary>
        public static MiniProfiler FromJson(string json)
        {
			if (json.IsNullOrWhiteSpace()) return null;

			var result = JsonSerializer.DeserializeFromString<MiniProfiler>(json);
            return result;
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext ctx)
        {
            HasSqlTimings = GetTimingHierarchy().Any(t => t.HasSqlTimings);
            HasDuplicateSqlTimings = GetTimingHierarchy().Any(t => t.HasDuplicateSqlTimings);
            if (_root != null)
            {
                _root.RebuildParentTimings();
            }
        }

        /// <summary>
        /// Create a DEEP clone of this object
        /// </summary>
        /// <returns></returns>
        public MiniProfiler Clone()
        {
            var serializer = new DataContractSerializer(typeof(MiniProfiler), null, int.MaxValue, false, true, null);
            using (var ms = new System.IO.MemoryStream())
            {
                serializer.WriteObject(ms, this);
                ms.Position = 0;
                return (MiniProfiler)serializer.ReadObject(ms);
            }
        }
    }

    /// <summary>
    /// Categorizes individual <see cref="Timing"/> steps to allow filtering.
    /// </summary>
    public enum ProfileLevel : byte
    {
        /// <summary>
        /// Default level given to Timings.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Useful when profiling many items in a loop, but you don't wish to always see this detail.
        /// </summary>
        Verbose = 1
    }

    /// <summary>
    /// Contains helper methods that ease working with null <see cref="MiniProfiler"/>s.
    /// </summary>
    public static class MiniProfilerExtensions
    {
        public static Func<MiniProfiler, string, IDisposable> CustomStepFn { get; set; }

        /// <summary>
        /// Wraps <paramref name="selector"/> in a <see cref="Step"/> call and executes it, returning its result.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="selector">Method to execute and profile.</param>
        /// <param name="name">The <see cref="Timing"/> step name used to label the profiler results.</param>
        /// <returns></returns>
        public static T Inline<T>(this MiniProfiler profiler, Func<T> selector, string name)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            if (profiler == null) return selector();
            using (profiler.StepImpl(name))
            {
                return selector();
            }
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will time the code between its creation and disposal.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting IDisposable's lifetime.</param>
        /// <param name="level">This step's visibility level; allows filtering when <see cref="MiniProfiler.Start"/> is called.</param>
        public static IDisposable Step(this IProfiler profiler, string name, ProfileLevel level)
        {
            var miniProfiler = profiler.GetMiniProfiler();
            if (CustomStepFn != null)
                return CustomStepFn(miniProfiler, name);

            return profiler == null ? null : miniProfiler.StepImpl(name, level);
        }

        // TODO: get this working in the UI
        //public static void AddData(this MiniProfiler profiler, string key, string value)
        //{
        //    if (profiler != null) profiler.AddDataImpl(key, value);
        //}

        /// <summary>
        /// Adds <paramref name="externalProfiler"/>'s <see cref="Timing"/> hierarchy to this profiler's current Timing step,
        /// allowing other threads, remote calls, etc. to be profiled and joined into this profiling session.
        /// </summary>
        public static void AddProfilerResults(this MiniProfiler profiler, MiniProfiler externalProfiler)
        {
            if (profiler == null || profiler.Head == null || externalProfiler == null) return;
            profiler.Head.AddChild(externalProfiler.Root);
        }

        /// <summary>
        /// Returns an html-encoded string with a text-representation of <paramref name="profiler"/>; returns "" when profiler is null.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        public static IHtmlString Render(this MiniProfiler profiler)
        {
            if (profiler == null) return new HtmlString("");

            var text = StringBuilderCache.Allocate()
                .Append(HttpUtility.HtmlEncode(Environment.MachineName)).Append(" at ").Append(DateTime.UtcNow).AppendLine();

            Stack<Timing> timings = new Stack<Timing>();
            timings.Push(profiler.Root);
            while (timings.Count > 0)
            {
                var timing = timings.Pop();
                string name = HttpUtility.HtmlEncode(timing.Name);
                text.AppendFormat("{2} {0} = {1:###,##0.##}ms", name, timing.DurationMilliseconds, new string('>', timing.Depth)).AppendLine();
                if (timing.HasChildren)
                {
                    IList<Timing> children = timing.Children;
                    for (int i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                }
            }
            return new HtmlString(StringBuilderCache.ReturnAndFree(text));
        }

        public static MiniProfiler GetMiniProfiler(this IProfiler profiler)
        {
            if (profiler is MiniProfiler miniProfiler)
                return miniProfiler;
            
            if (profiler is MiniProfilerAdapter profilerAdapter)
                return (MiniProfiler)profilerAdapter.Current;
            
            throw new NotSupportedException($"Expected MiniProfiler but was '{profiler.GetType().Name}'. Ensure MiniProfilerFeature plugin is registered.");
        }

    }
}

#endif