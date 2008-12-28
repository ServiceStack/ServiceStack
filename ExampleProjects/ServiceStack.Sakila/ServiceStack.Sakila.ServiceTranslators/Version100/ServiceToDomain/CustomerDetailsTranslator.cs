using Ddn.Common.DesignPatterns.Translator;
using Sakila.DomainModel.SakilaService;
using Dto = Sakila.ServiceModel.Version100.Types;

namespace ServiceStack.Sakila.ServiceTranslators.Version100.ServiceToDomain
{
	public class CustomerDetailsTranslator : ITranslator<CustomerDetails, Dto.CustomerDetails>
	{
		public static readonly CustomerDetailsTranslator Instance = new CustomerDetailsTranslator();

		public CustomerDetails Parse(Dto.CustomerDetails from)
		{
			if (from == null) return null;
			var to = new CustomerDetails {
				CustomerName = from.CustomerName,
				Email = from.Email,
				Title = from.Title,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Country = from.Country,
				LanguageCode = from.LanguageCode,
				CanNotifyEmail = from.CanNotifyEmail,
				StoreCreditCard = from.StoreCreditCard,
				SingleClickBuyEnabled = from.SingleClickBuyEnabled,	
			};
			return to;
		}
	}
}