using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler
{

    /// <summary>
    /// A provider used to create <see cref="MiniProfiler"/> instances and maintain the current instance.
    /// </summary>
    public interface IProfilerProvider
    {
        /// <summary>
        /// Starts a new MiniProfiler and sets it to be current.  By the end of this method
        /// <see cref="GetCurrentProfiler"/> should return the new MiniProfiler.
        /// </summary>
        MiniProfiler Start(ProfileLevel level);

        /// <summary>
        /// Ends the current profiling session, if one exists.
        /// </summary>
        /// <param name="discardResults">
        /// When true, clears the <see cref="MiniProfiler.Current"/> for this HttpContext, allowing profiling to 
        /// be prematurely stopped and discarded. Useful for when a specific route does not need to be profiled.
        /// </param>
        void Stop(bool discardResults);

        /// <summary>
        /// Returns the current MiniProfiler.  This is used by <see cref="MiniProfiler.Current"/>.
        /// </summary>
        /// <returns></returns>
        MiniProfiler GetCurrentProfiler();
    }
}
