using System;

namespace ServiceStack
{
    public class HttpCacheEntry
    {
        public HttpCacheEntry(object response)
        {
            Response = response;
            Created = DateTime.UtcNow;
        }

        public DateTime Created { get; set; }
        public string ETag { get; set; }
        public DateTime? LastModified { get; set; }
        public bool MustRevalidate { get; set; } 
        public bool NoCache { get; set; }
        public TimeSpan? Age { get; set; }
        public TimeSpan MaxAge { get; set; }
        public DateTime Expires { get; set; }
        public object Response { get; set; }

        public void InitMaxAge(TimeSpan maxAge)
        {
            MaxAge = maxAge;
            Expires = Created + maxAge;
        }

        public bool ShouldRevalidate()
        {
            return NoCache || DateTime.UtcNow > Expires; //always implies MustRevalidate
        }
    }
}