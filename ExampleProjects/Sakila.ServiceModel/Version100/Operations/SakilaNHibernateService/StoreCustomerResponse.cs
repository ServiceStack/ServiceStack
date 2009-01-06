using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Operations.SakilaNHibernateService
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class StoreCustomerResponse : SakilaService.StoreCustomerResponse
	{
	}
}