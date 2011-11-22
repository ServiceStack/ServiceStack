using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler
{
    /// <summary>
    /// BaseProfilerProvider.  This providers some helper methods which provide access to
    /// internals not otherwise available.
    /// To use, override the <see cref="Start"/>, <see cref="Stop"/> and <see cref="GetCurrentProfiler"/>
    /// methods.
    /// </summary>
    public abstract class BaseProfilerProvider : IProfilerProvider
    {
        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="GetCurrentProfiler"/> should return the new MiniProfiler.
        /// </summary>
        public abstract MiniProfiler Start(ProfileLevel level);

        /// <summary>
        /// Stops the current MiniProfiler (if any is currently running).
        /// <see cref="SaveProfiler"/> should be called if <paramref name="discardResults"/> is false
        /// </summary>
        /// <param name="discardResults">If true, any current results will be thrown away and nothing saved</param>
        public abstract void Stop(bool discardResults);

        /// <summary>
        /// Returns the current MiniProfiler.  This is used by <see cref="MiniProfiler.Current"/>.
        /// </summary>
        /// <returns></returns>
        public abstract MiniProfiler GetCurrentProfiler();

        /// <summary>
        /// Sets <paramref name="profiler"/> to be active (read to start profiling)
        /// This should be called once a new MiniProfiler has been created.
        /// </summary>
        /// <param name="profiler">The profiler to set to active</param>
        /// <exception cref="ArgumentNullException">If <paramref name="profiler"/> is null</exception>
        protected static void SetProfilerActive(MiniProfiler profiler)
        {
            if (profiler == null)
                throw new ArgumentNullException("profiler");

            profiler.IsActive = true;
        }

        /// <summary>
        /// Stops the profiler and marks it as inactive.
        /// </summary>
        /// <param name="profiler">The profiler to stop</param>
        /// <returns>True if successful, false if Stop had previously been called on this profiler</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="profiler"/> is null</exception>
        protected static bool StopProfiler(MiniProfiler profiler)
        {
            if (profiler == null)
                throw new ArgumentNullException("profiler");

            if (!profiler.StopImpl())
                return false;

            profiler.IsActive = false;
            return true;
        }

        /// <summary>
        /// Calls <see cref="MiniProfiler.Settings.EnsureStorageStrategy"/> to save the current
        /// profiler using the current storage settings
        /// </summary>
        /// <param name="current"></param>
        protected static void SaveProfiler(MiniProfiler current)
        {
            // because we fetch profiler results after the page loads, we have to put them somewhere in the meantime
            MvcMiniProfiler.MiniProfiler.Settings.EnsureStorageStrategy();
            MvcMiniProfiler.MiniProfiler.Settings.Storage.Save(current);
        }
    }
}
