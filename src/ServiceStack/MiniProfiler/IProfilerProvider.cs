#if !NETSTANDARD1_6

namespace ServiceStack.MiniProfiler
{

    /// <summary>
    /// A provider used to create <see cref="Profiler"/> instances and maintain the current instance.
    /// </summary>
    public interface IProfilerProvider
    {
        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="GetCurrentProfiler"/> should return the new MiniProfiler.
        /// </summary>
        Profiler Start(ProfileLevel level);

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="Profiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        void Stop(bool discardResults);

        /// <summary>
        /// Returns the current MiniProfiler.  This is used by <see cref="Profiler.Current"/>.
        /// </summary>
        /// <returns></returns>
        Profiler GetCurrentProfiler();
    }
}

#endif
