using Sakila.ServiceModel.Version100.Operations.SakilaService;
using Sakila.ServiceModelTranslators.Version100.DomainToService;
using ServiceStack.Common.Extensions;
using ServiceStack.LogicFacade;
using ServiceStack.ServiceInterface;
using ServiceStack.Sakila.Logic.LogicInterface;	
using ServiceStack.Sakila.ServiceInterface.Translators;
using ServiceStack.Validation;

namespace ServiceStack.Sakila.ServiceInterface.Version100
{
	[MessagingRestriction(MessagingRestriction.HttpPost)]
	public class GetFilmsPort : IService
	{
		public object Execute(ICallContext context)
		{
			var request = context.Request.Get<GetFilms>();

			var facade = context.Request.Get<ISakilaServiceFacade>();
			try
			{
				var results = facade.GetFilms(request.FilmIds);

				return new GetFilmsResponse {
					Films = FilmToDtoTranslator.Instance.ParseAll(results)
				};
			}
			catch (ValidationException ve)
			{
				return new GetCustomersResponse { ResponseStatus = ResponseStatusTranslator.Instance.Parse(ve) };
			}
		}
	}
}
