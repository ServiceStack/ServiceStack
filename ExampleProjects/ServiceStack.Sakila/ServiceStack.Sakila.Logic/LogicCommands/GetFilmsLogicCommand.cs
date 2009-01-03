using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Extensions;
using ServiceStack.Sakila.Logic.Translators.DataToDomain;

namespace ServiceStack.Sakila.Logic.LogicCommands
{
	public class GetFilmsLogicCommand : LogicCommandBase<List<Film>>
	{
		public List<int> FilmIds { get; set; }

		public override List<Film> Execute()
		{
			ThrowAnyValidationErrors(Validate());
			var dbFilms = Provider.GetFilms(this.FilmIds);
			return FilmFromDataTranslator.Instance.ParseAll(dbFilms);
		}
	}
}