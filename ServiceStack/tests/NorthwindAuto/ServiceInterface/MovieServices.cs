using ServiceStack;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface;

public class MovieServices : Service
{
    public static Dictionary<string, Movie> Movies { get; set; } = new();

    public MovieServices()
    {
        Movies = new Dictionary<string, Movie>();

        var myMovie = new Movie
        {
            MovieID = "DF77F8AD-CAFA-4470-94DC-D5FD2331515F", MovieNo = 1, Name = "Star Wars: Episode V",
            Description = "Talking frog convinces a boy to kill his dad."
        };
        Movies.Add(myMovie.MovieID, myMovie);

        myMovie = new Movie
        {
            MovieID = "1173DAB3-0DC9-4D28-AC68-75AF6581B6ED", MovieNo = 2, Name = "Forrest Gump",
            Description = "Drug addicted girl takes advantage of a mentally challenged boy for 3 decades."
        };
        Movies.Add(myMovie.MovieID, myMovie);
    }

    public Movie Get(MovieGETRequest request)
    {
        return Movies[request.MovieID];
    }

    public Movie Post(MoviePOSTRequest request)
    {
        string newID = Guid.NewGuid().ToString();
        var myMovie = new Movie
            { MovieID = newID, MovieNo = Movies.Count + 1, Name = request.Name, Description = request.Description };
        Movies.Add(myMovie.MovieID, myMovie);

        return myMovie;
    }
}
