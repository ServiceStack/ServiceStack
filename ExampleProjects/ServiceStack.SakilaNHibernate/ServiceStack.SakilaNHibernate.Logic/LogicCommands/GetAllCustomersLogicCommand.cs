using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Extensions;
using ServiceStack.SakilaNHibernate.Logic.Translators.DataToDomain;

namespace ServiceStack.SakilaNHibernate.Logic.LogicCommands
{
	public class GetAllCustomersLogicCommand : LogicCommandBase<List<Customer>>
	{
		public override List<Customer> Execute()
		{
			var dbCustomers = Provider.GetAll<DataAccess.DataModel.Customer>();
			return CustomerFromDataTranslator.Instance.ParseAll(dbCustomers);
		}
	}
}