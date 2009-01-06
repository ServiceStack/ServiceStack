using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;
using DtoTypes = Sakila.ServiceModel.Version100.Types;

namespace Sakila.ServiceModelTranslators.Version100.DomainToService
{
	public class CustomerToDtoTranslator : ITranslator<DtoTypes.Customer, Customer>
	{
		public static readonly CustomerToDtoTranslator Instance = new CustomerToDtoTranslator();

		public DtoTypes.Customer Parse(Customer from)
		{
			if (from == null) return null;
			var to = new DtoTypes.Customer {
				Id = from.Id,
			};
			return to;
		}
	}
}
