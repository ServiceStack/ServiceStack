using System;

namespace ServiceStack.Caching.Neo4j
{
    public interface ICacheEntry
    {
        string Id { get; set; }
        string Data { get; set; }
        DateTime? ExpiryDate { get; set; }
        DateTime CreatedDate { get; set; }
        DateTime ModifiedDate { get; set; }
    }

    public class CacheEntry : ICacheEntry
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}