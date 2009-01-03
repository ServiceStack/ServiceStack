using @DomainModelNamespace@;
using ServiceStack.DesignPatterns.Translator;
using DtoTypes = @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@Translators.Version100.DomainToService
{
	public class FilmToDtoTranslator : ITranslator<DtoTypes.Film, Film>
	{
		public static readonly FilmToDtoTranslator Instance = new FilmToDtoTranslator();

		public DtoTypes.Film Parse(Film from)
		{
			if (from == null) return null;
			var to = new DtoTypes.Film {
				Id = from.Id,
				Title = from.Title,
				Description = from.Description,
				Language = from.Language,
				OriginalLanguage = from.OriginalLanguage,
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