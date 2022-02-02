using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using ServiceStack.Common;

namespace ServiceStack.Common.Tests.Models{
	
	public class Movie{
		
		public string Director
		{
			get;
			set;
		}
	
		
		public List<string> Genres
		{
			get;
			set;
		}
	
		
		public string Id
		{
			get;
			set;
		}
	
		
		public decimal Rating
		{
			get;
			set;
		}
	
		
		public DateTime ReleaseDate
		{
			get;
			set;
		}
	
		
		public string TagLine
		{
			get;
			set;
		}
	
		
		public string Title
		{
			get;
			set;
		}
	
		public Movie()
		{
			this.Genres = new List<string>();
		}
	
		
		public bool Equals(Movie other)
        {
            return !object.ReferenceEquals(null, other) && (object.ReferenceEquals(this, other) || (object.Equals(other.Id, this.Id) && object.Equals(other.Title, this.Title) && other.Rating == this.Rating && object.Equals(other.Director, this.Director) && other.ReleaseDate.Equals(this.ReleaseDate) && object.Equals(other.TagLine, this.TagLine) && EnumerableExtensions.EquivalentTo<string>(this.Genres, other.Genres)));
        }
        public override bool Equals(object obj)
        {
            return !object.ReferenceEquals(null, obj) && (object.ReferenceEquals(this, obj) || (obj.GetType() == typeof(Movie) && this.Equals((Movie)obj)));
        }
		
		
		public override int GetHashCode()
		{
			int result = (this.Id != null) ? this.Id.GetHashCode() : 0;
	            result = (result * 397 ^ ((this.Title != null) ? this.Title.GetHashCode() : 0));
	            result = (result * 397 ^ this.Rating.GetHashCode());
	            result = (result * 397 ^ ((this.Director != null) ? this.Director.GetHashCode() : 0));
	            result = (result * 397 ^ this.ReleaseDate.GetHashCode());
	            result = (result * 397 ^ ((this.TagLine != null) ? this.TagLine.GetHashCode() : 0));
	            return result * 397 ^ ((this.Genres != null) ? this.Genres.GetHashCode() : 0);
		}
	}
}