using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Caching
{
    public interface IRemoveByPatternAsync
    {
        /// <summary>
        /// Removes items from cache that have keys matching the specified wildcard pattern
        /// </summary>
        /// <param name="pattern">The wildcard, where "*" means any sequence of characters and "?" means any single character.</param>
        Task RemoveByPatternAsync(string pattern, CancellationToken token=default);
        
        /// <summary>
        /// Removes items from the cache based on the specified regular expression pattern
        /// </summary>
        /// <param name="regex">Regular expression pattern to search cache keys</param>
        Task RemoveByRegexAsync(string regex, CancellationToken token=default);
    }
}