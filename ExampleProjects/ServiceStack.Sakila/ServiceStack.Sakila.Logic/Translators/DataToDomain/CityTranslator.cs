using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.Sakila.Logic.Translators.DataToDomain
{
	public class CityTranslator : ITranslator<City, DataAccess.DataModel.City>
	{
		public static readonly CityTranslator Instance = new CityTranslator();

		public City Parse(DataAccess.DataModel.City from)
		{
			if (from == null) return null;
			var to = new City {
				Id = from.Id,
                Country = new Country{Id = from.CountryMember.Id, Name = from.CountryMember.Name },
                Name = from.Name,
			};

			return to;
		}
	}
}