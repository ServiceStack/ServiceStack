using System;
using System.Collections.Generic;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Contains helper methods that ease working with null <see cref="MiniProfiler"/>s.
    /// </summary>
    public static class MiniProfilerExtensions
    {
        /// <summary>
        /// Wraps <paramref name="selector"/> in a <see cref="Step(MiniProfiler, string)"/> call and executes it, returning its result.
        /// </summary>
        /// <typeparam name="T">the type of result.</typeparam>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="selector">Method to execute and profile.</param>
        /// <param name="name">The <see cref="Timing"/> step name used to label the profiler results.</param>
        /// <returns>the profiled result.</returns>
        /// <exception cref="ArgumentNullException">Throws when <paramref name="selector"/> is <c>null</c>.</exception>
        public static T Inline<T>(this MiniProfiler profiler, Func<T> selector, string name)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (profiler == null) return selector();
            using (profiler.StepImpl(name))
            {
                return selector();
            }
        }

        /// <summary>
        /// Returns an <see cref="Timing"/> (<see cref="IDisposable"/>) that will time the code between its creation and disposal.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting Timing's lifetime.</param>
        /// <returns>the profile step</returns>
        public static Timing Step(this MiniProfiler profiler, string name) => profiler?.StepImpl(name);

        /// <summary>
        /// Returns an <see cref="Timing"/> (<see cref="IDisposable"/>) that will time the code between its creation and disposal.
        /// Will only save the <see cref="Timing"/> if total time taken exceeds <paramref name="minSaveMs" />.
        /// </summary>
        /// <param name="profiler">The current profiling session or <c>null</c>.</param>
        /// <param name="name">A descriptive name for the code that is encapsulated by the resulting Timing's lifetime.</param>
        /// <param name="minSaveMs">The minimum amount of time that needs to elapse in order for this result to be recorded.</param>
        /// <param name="includeChildren">Should the amount of time spent in child timings be included when comparing total time
        /// profiled with <paramref name="minSaveMs"/>? If true, will include children. If false will ignore children.</param>
        /// <returns></returns>
        /// <remarks>If <paramref name="includeChildren"/> is set to true and a child is removed due to its use of StepIf, then the 
        /// time spent in that time will also not count for the current StepIf calculation.</remarks>
        public static Timing StepIf(this MiniProfiler profiler, string name, decimal minSaveMs, bool includeChildren = false)
        {
            return profiler?.StepImpl(name, minSaveMs, includeChildren);
        }

        /// <summary>
        /// Returns a new <see cref="CustomTiming"/> that will automatically set its <see cref="Profiling.CustomTiming.StartMilliseconds"/>
        /// and <see cref="Profiling.CustomTiming.DurationMilliseconds"/>
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="category">The category under which this timing will be recorded.</param>
        /// <param name="commandString">The command string that will be recorded along with this timing, for display in the MiniProfiler results.</param>
        /// <param name="executeType">Execute Type to be associated with the Custom Timing. Example: Get, Set, Insert, Delete</param>
        /// <remarks>
        /// Should be used like the <see cref="Step(MiniProfiler, string)"/> extension method
        /// </remarks>
        public static CustomTiming CustomTiming(this MiniProfiler profiler, string category, string commandString, string executeType = null)
        {
            return CustomTimingIf(profiler, category, commandString, 0, executeType: executeType);
        }

        /// <summary>
        /// Returns a new <see cref="CustomTiming"/> that will automatically set its <see cref="Profiling.CustomTiming.StartMilliseconds"/>
        /// and <see cref="Profiling.CustomTiming.DurationMilliseconds"/>. Will only save the new <see cref="Timing"/> if the total elapsed time
        /// takes more than <paramef name="minSaveMs" />.
        /// </summary>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <param name="category">The category under which this timing will be recorded.</param>
        /// <param name="commandString">The command string that will be recorded along with this timing, for display in the MiniProfiler results.</param>
        /// <param name="executeType">Execute Type to be associated with the Custom Timing. Example: Get, Set, Insert, Delete</param>
        /// <param name="minSaveMs">The minimum amount of time that needs to elapse in order for this result to be recorded.</param>
        /// <remarks>
        /// Should be used like the <see cref="Step(MiniProfiler, string)"/> extension method 
        /// </remarks>
        public static CustomTiming CustomTimingIf(this MiniProfiler profiler, string category, string commandString, decimal minSaveMs, string executeType = null)
        {
            if (profiler == null || profiler.Head == null || !profiler.IsActive) return null;

            var result = new CustomTiming(profiler, commandString, minSaveMs)
            {
                ExecuteType = executeType,
                Category = category
            };

            // THREADING: revisit
            profiler.Head.AddCustomTiming(category, result);

            return result;
        }

        /// <summary>
        /// Returns an <see cref="IDisposable"/> that will ignore profiling between its creation and disposal.
        /// </summary>
        /// <remarks>
        /// This is mainly useful in situations where you want to ignore database profiling for known hot spots,
        /// but it is safe to use in a nested step such that you can ignore sub-sections of a profiled step.
        /// </remarks>
        /// <param name="profiler">The current profiling session or null.</param>
        /// <returns>the profile step</returns>
        //public static IDisposable Ignore(this MiniProfiler profiler) => profiler?.IgnoreImpl();

        /// <summary>
        /// Adds <paramref name="externalProfiler"/>'s <see cref="Timing"/> hierarchy to this profiler's current Timing step,
        /// allowing other threads, remote calls, etc. to be profiled and joined into this profiling session.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to add to.</param>
        /// <param name="externalProfiler">The <see cref="MiniProfiler"/> to append to <paramref name="profiler"/>'s tree.</param>
        public static void AddProfilerResults(this MiniProfiler profiler, MiniProfiler externalProfiler)
        {
            if (profiler?.Head == null || externalProfiler == null) return;
            profiler.Head.AddChild(externalProfiler.Root);
        }

        /// <summary>
        /// Adds the <paramref name="text"/> and <paramref name="url"/> pair to <paramref name="profiler"/>'s 
        /// <see cref="MiniProfiler.CustomLinks"/> dictionary; will be displayed on the client in the bottom of the profiler popup.
        /// </summary>
        /// <param name="profiler">The <see cref="MiniProfiler"/> to add the link to.</param>
        /// <param name="text">The text label for the link.</param>
        /// <param name="url">The URL the link goes to.</param>
        public static void AddCustomLink(this MiniProfiler profiler, string text, string url)
        {
            if (profiler == null || !profiler.IsActive) return;

            lock (profiler)
            {
                profiler.CustomLinks = profiler.CustomLinks ?? new Dictionary<string, string>();
            }

            profiler.CustomLinks[text] = url;
        }
    }
}