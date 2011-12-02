using System;
using System.Collections.Generic;

namespace ServiceStack.MiniProfiler.Storage
{
    /// <summary>
    /// Provides saving and loading <see cref="Profiler"/>s to a storage medium.
    /// </summary>
    public interface IStorage
    {
        /// <summary>
        /// Stores <paramref name="profiler"/> under its <see cref="Profiler.Id"/>.
        /// </summary>
        /// <param name="profiler">The results of a profiling session.</param>
        /// <remarks>
        /// Should also ensure the profiler is stored as being unviewed by its profiling <see cref="Profiler.User"/>.
        /// </remarks>
        void Save(Profiler profiler);

        /// <summary>
        /// Returns a <see cref="Profiler"/> from storage based on <paramref name="id"/>, which should map to <see cref="Profiler.Id"/>.
        /// </summary>
        /// <remarks>
        /// Should also update that the resulting profiler has been marked as viewed by its profiling <see cref="Profiler.User"/>.
        /// </remarks>
        Profiler Load(Guid id);

        /// <summary>
        /// Returns a list of <see cref="Profiler.Id"/>s that haven't been seen by <paramref name="user"/>.
        /// </summary>
        /// <param name="user">User identified by the current <see cref="Profiler.Settings.UserProvider"/>.</param>
        List<Guid> GetUnviewedIds(string user);

    }
}
