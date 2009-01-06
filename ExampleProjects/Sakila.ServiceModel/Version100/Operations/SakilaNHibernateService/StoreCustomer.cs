using System.Collections.Generic;
using System.Runtime.Serialization;
using Sakila.ServiceModel.Version100.Types;

namespace Sakila.ServiceModel.Version100.Operations.SakilaNHibernateService
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class StoreCustomer : SakilaService.StoreCustomer
	{
	}
}