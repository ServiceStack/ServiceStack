using Ddn.Common.DesignPatterns.Translator;
using Sakila.DomainModel.SakilaService;
using Dto = Sakila.ServiceModel.Version100.Types;

namespace ServiceStack.Sakila.ServiceTranslators.Version100.DomainToService
{
	public class CustomerDetailsTranslator : ITranslator<Dto.CustomerDetails, CustomerDetails>
	{
		public static readonly CustomerDetailsTranslator Instance = new CustomerDetailsTranslator();

		public Dto.CustomerDetails Parse(CustomerDetails from)
		{

			if (from == null) return null;
			var to = new Dto.CustomerDetails {
				CustomerName = from.CustomerName,
				Email = from.Email,
				Title = from.Title,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Country = from.Country,
				LanguageCode = from.LanguageCode,
				CanNotifyEmail = from.CanNotifyEmail,
				SingleClickBuyEnabled = from.SingleClickBuyEnabled,
			};
			return to;
		}
	}
}