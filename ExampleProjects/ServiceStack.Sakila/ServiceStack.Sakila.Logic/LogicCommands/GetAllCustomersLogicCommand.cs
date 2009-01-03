using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Extensions;
using ServiceStack.Sakila.Logic.Translators.DataToDomain;

namespace ServiceStack.Sakila.Logic.LogicCommands
{
	public class GetAllCustomersLogicCommand : LogicCommandBase<List<Customer>>
	{
		public override List<Customer> Execute()
		{
			var dbCustomers = Provider.Data.GetAll<DataAccess.DataModel.Customer>();
			return CustomerFromDataTranslator.Instance.ParseAll(dbCustomers);
		}
	}
}