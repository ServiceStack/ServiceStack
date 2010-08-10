using System;
using System.Collections.Generic;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	public class ConfigureDatabase
	{
		public static List<Movie> Top5Movies = new List<Movie>
       	{
       		new Movie { Id = "tt0111161", Title = "The Shawshank Redemption", Rating = 9.2m, Director = "Frank Darabont", ReleaseDate = new DateTime(1995,2,17), TagLine = "Fear can hold you prisoner. Hope can set you free.", Genres = new List<string>{"Crime","Drama"}, },
       		new Movie { Id = "tt0068646", Title = "The Godfather", Rating = 9.2m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1972,3,24), TagLine = "An offer you can't refuse.", Genres = new List<string> {"Crime","Drama", "Thriller"}, },
       		new Movie { Id = "tt1375666", Title = "Inception", Rating = 9.2m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2010,7,16), TagLine = "Your mind is the scene of the crime", Genres = new List<string>{"Action", "Mystery", "Sci-Fi", "Thriller"}, },
       		new Movie { Id = "tt0071562", Title = "The Godfather: Part II", Rating = 9.0m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1974,12,20), Genres = new List<string> {"Crime","Drama", "Thriller"}, },
       		new Movie { Id = "tt0060196", Title = "The Good, the Bad and the Ugly", Rating = 9.0m, Director = "Sergio Leone", ReleaseDate = new DateTime(1967,12,29), TagLine = "They formed an alliance of hate to steal a fortune in dead man's gold", Genres = new List<string>{"Adventure","Western"}, },
       	};

		public static void Init(IDbConnectionFactory connectionFactory)
		{
			try
			{
				using (var dbConn = connectionFactory.OpenDbConnection())
				using (var dbCmd = dbConn.CreateCommand())
				{
					dbCmd.CreateTable<Movie>(false);
					dbCmd.SaveAll(Top5Movies);
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}
	}
}