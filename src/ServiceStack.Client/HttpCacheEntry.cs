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
        public long? ContentLength { get; set; }
        public object Response { get; set; }

        public void SetMaxAge(TimeSpan maxAge)
        {
            MaxAge = maxAge;
            Expires = maxAge > TimeSpan.Zero 
                ? Created + maxAge
                : Created - TimeSpan.FromSeconds(1); //auto expire
        }

        public bool HasExpired()
        {
            return DateTime.UtcNow > Expires;
        }

        public bool CanUseCacheOnError()
        {
            return !NoCache && !(MustRevalidate && HasExpired());
        }

        public bool ShouldRevalidate()
        {
            return NoCache || HasExpired(); //always implies MustRevalidate
        }
    }
}