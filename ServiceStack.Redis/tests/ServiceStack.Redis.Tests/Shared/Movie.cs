using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Common;

namespace ServiceStack.Common.Tests.Models
{
    [DataContract]
    public class Movie
    {
        public Movie()
        {
            this.Genres = new List<string>();
        }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public decimal Rating { get; set; }

        [DataMember]
        public string Director { get; set; }

        [DataMember]
        public DateTime ReleaseDate { get; set; }

        [DataMember]
        public string TagLine { get; set; }

        [DataMember]
        public List<string> Genres { get; set; }

        public bool Equals(Movie other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Id, Id) && Equals(other.Title, Title) && other.Rating == Rating && Equals(other.Director, Director) && other.ReleaseDate.Equals(ReleaseDate) && Equals(other.TagLine, TagLine) && Genres.EquivalentTo(other.Genres);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Movie)) return false;
            return Equals((Movie)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (Id != null ? Id.GetHashCode() : 0);
                result = (result * 397) ^ (Title != null ? Title.GetHashCode() : 0);
                result = (result * 397) ^ Rating.GetHashCode();
                result = (result * 397) ^ (Director != null ? Director.GetHashCode() : 0);
                result = (result * 397) ^ ReleaseDate.GetHashCode();
                result = (result * 397) ^ (TagLine != null ? TagLine.GetHashCode() : 0);
                result = (result * 397) ^ (Genres != null ? Genres.GetHashCode() : 0);
                return result;
            }
        }
    }
}