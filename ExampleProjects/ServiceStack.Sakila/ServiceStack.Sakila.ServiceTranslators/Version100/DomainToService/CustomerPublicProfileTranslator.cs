using Ddn.Common.DesignPatterns.Translator;
using Sakila.DomainModel.SakilaService;
using DtoTypes = Sakila.ServiceModel.Version100.Types;

namespace ServiceStack.Sakila.ServiceTranslators.Version100.DomainToService
{
	public class CustomerPublicProfileTranslator : ITranslator<DtoTypes.CustomerPublicProfile, Customer>
	{
		public static readonly CustomerPublicProfileTranslator Instance = new CustomerPublicProfileTranslator();

		public DtoTypes.CustomerPublicProfile Parse(Customer from)
		{
			if (from == null) return null;
			var to = new DtoTypes.CustomerPublicProfile {
				GlobalId = from.GlobalId,				
				CustomerName = from.CustomerDetails.CustomerName,
                FirstName = from.CustomerDetails.FirstName,
                LastName = from.CustomerDetails.LastName,
                Country = from.CustomerDetails.Country,
				LanguageCode = from.CustomerDetails.LanguageCode,
			};
			return to;
		}
	}
}