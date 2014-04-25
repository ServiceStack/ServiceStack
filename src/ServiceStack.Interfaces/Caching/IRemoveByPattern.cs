namespace ServiceStack.Caching
{
    public interface IRemoveByPattern
    {
        /// <summary>
        /// Removes items from cache that have keys matching the specified wildcard pattern
        /// </summary>
        /// <param name="pattern">The wildcard, where "*" means any sequence of characters and "?" means any single character.</param>
        void RemoveByPattern(string pattern);
        /// <summary>
        /// Removes items from the cache based on the specified regular expression pattern
        /// </summary>
        /// <param name="regex">Regular expression pattern to search cache keys</param>
        void RemoveByRegex(string regex);
    }
}
