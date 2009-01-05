using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Extensions;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface.Requests;
using ServiceStack.SakilaNHibernate.Logic.Translators.DataToDomain;

namespace ServiceStack.SakilaNHibernate.Logic.LogicCommands
{
	public class GetCustomersLogicCommand : LogicCommandBase<List<Customer>>
	{
		public CustomersRequest Request { get; set; }

		public override List<Customer> Execute()
		{
			var dbCustomers = Provider.GetByIds<DataAccess.DataModel.Customer>(
				this.Request.CustomerIds.ConvertAll(x => (ushort)x));

			return CustomerFromDataTranslator.Instance.ParseAll(dbCustomers);
		}
	}
}