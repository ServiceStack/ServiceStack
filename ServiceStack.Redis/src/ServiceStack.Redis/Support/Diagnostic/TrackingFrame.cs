using System;

namespace ServiceStack.Redis.Support.Diagnostic
{
    /// <summary>
    /// Stores details about the context in which an IRedisClient is allocated. 
    /// </summary>
    public class TrackingFrame : IEquatable<TrackingFrame>
    {
        public Guid Id { get; set; }

        public Type ProvidedToInstanceOfType { get; set; }

        public DateTime Initialised { get; set; }

        public bool Equals(TrackingFrame other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TrackingFrame)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}