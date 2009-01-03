using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.Sakila.Logic.Translators.DataToDomain
{
	public class FilmFromDataTranslator : ITranslator<Film, DataAccess.DataModel.Film>
	{
		public static readonly FilmFromDataTranslator Instance = new FilmFromDataTranslator();

		public Film Parse(DataAccess.DataModel.Film from)
		{
			var to = new Film {
				Id = from.Id,
				Title = from.Title,
				Description = from.Description,
				Language = from.LanguageMember.Name,
				OriginalLanguage = from.OriginalLanguageMember.Name,
				LastUpdate = from.LastUpdate,
				Rating = from.Rating,
				Length = from.Length,
				ReleaseYear = from.ReleaseYear,
				RentalDuration = from.RentalDuration,
				RentalRate = from.RentalRate,
				ReplacementCost = from.ReplacementCost,
				SpecialFeature = from.SpecialFeature,
			};

			return to;
		}
	}
}