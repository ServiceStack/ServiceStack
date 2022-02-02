using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Text.Tests.Support
{
    public static class MoviesData
    {
        public static List<Movie> Movies = new List<Movie>
        {
            new Movie { ImdbId = "tt0111161", Title = "The Shawshank Redemption", Rating = 9.2m, Director = "Frank Darabont", ReleaseDate = new DateTime(1995,2,17), TagLine = "Fear can hold you prisoner. Hope can set you free.", Genres = new List<string>{"Crime","Drama"}, },
            new Movie { ImdbId = "tt0068646", Title = "The Godfather", Rating = 9.2m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1972,3,24), TagLine = "An offer you can't refuse.", Genres = new List<string> {"Crime","Drama", "Thriller"}, },
            new Movie { ImdbId = "tt1375666", Title = "Inception", Rating = 9.2m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2010,7,16), TagLine = "Your mind is the scene of the crime", Genres = new List<string>{"Action", "Mystery", "Sci-Fi", "Thriller"}, },
            new Movie { ImdbId = "tt0071562", Title = "The Godfather: Part II", Rating = 9.0m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1974,12,20), Genres = new List<string> {"Crime","Drama", "Thriller"}, },
            new Movie { ImdbId = "tt0060196", Title = "The Good, the Bad and the Ugly", Rating = 9.0m, Director = "Sergio Leone", ReleaseDate = new DateTime(1967,12,29), TagLine = "They formed an alliance of hate to steal a fortune in dead man's gold", Genres = new List<string>{"Adventure","Western"}, },
            new Movie { ImdbId = "tt0678100", Title = "\"Mr. Lee\"", Rating = 8.4m, Director = "Steven Long Mitchell", ReleaseDate = new DateTime(1999,2,6), Genres = new List<string> {"Drama", "Mystery", "Sci-Fi", "Thriller"}, },
            new Movie { ImdbId = "tt9793324", Title = "\"Anon\" Review", Rating = 0.0m, Director = null, ReleaseDate = new DateTime(2018,11,15), Genres = new List<string> {"Sci-Fi"}, },
        };

    }

    public class Movies : List<Movie>, IEquatable<Movies>
    {
        public Movies() {}

        public Movies(IEnumerable<Movie> collection) : base(collection) {}

        public bool Equals(Movies other)
        {
            return base.Equals(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Movies) obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract]
    public class Movie
    {
        public Movie()
        {
            this.Genres = new List<string>();
        }

        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        [AutoIncrement]
        public int Id { get; set; }

        [DataMember(Order = 3, EmitDefaultValue = false, IsRequired = false)]
        public string ImdbId { get; set; }

        [DataMember(Order = 2, EmitDefaultValue = false, IsRequired = false)]
        public string Title { get; set; }

        [DataMember(Order = 4, EmitDefaultValue = false, IsRequired = false)]
        public decimal Rating { get; set; }

        [DataMember(Order = 5, EmitDefaultValue = true, IsRequired = false)]
        public string Director { get; set; }

        [DataMember(Order = 6, EmitDefaultValue = false, IsRequired = false)]
        public DateTime ReleaseDate { get; set; }

        [DataMember(Order = 7, EmitDefaultValue = false, IsRequired = false)]
        public string TagLine { get; set; }

        [DataMember(Order = 8, EmitDefaultValue = false, IsRequired = false)]
        public List<string> Genres { get; set; }

        #region AutoGen ReSharper code, only required by tests
        public bool Equals(Movie other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.ImdbId, ImdbId)
                && Equals(other.Title, Title)
                && other.Rating == Rating
                && Equals(other.Director, Director)
                && other.ReleaseDate.Equals(ReleaseDate)
                && Equals(other.TagLine, TagLine)
                && Genres.EquivalentTo(other.Genres);
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
            return ImdbId != null ? ImdbId.GetHashCode() : 0;
        }
        #endregion
    }

    [DataContract]
    public class MovieResponse : IEquatable<MovieResponse>
    {
        [DataMember]
        public Movie Movie { get; set; }

        public bool Equals(MovieResponse other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Movie, other.Movie);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MovieResponse)obj);
        }

        public override int GetHashCode()
        {
            return (Movie != null ? Movie.GetHashCode() : 0);
        }
    }

    [DataContract]
    public class MoviesResponse : IEquatable<MoviesResponse>
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public List<Movie> Movies { get; set; }

        public bool Equals(MoviesResponse other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Movies.EquivalentTo(other.Movies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MoviesResponse)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Movies != null ? Movies.GetHashCode() : 0);
            }
        }
    }

    [Csv(CsvBehavior.FirstEnumerable)]
    public class MoviesResponse2 : IEquatable<MoviesResponse2>
    {
        public int Id { get; set; }

        public List<Movie> Movies { get; set; }

        public bool Equals(MoviesResponse2 other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Movies.EquivalentTo(other.Movies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MoviesResponse2)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ (Movies != null ? Movies.GetHashCode() : 0);
            }
        }
    }

}