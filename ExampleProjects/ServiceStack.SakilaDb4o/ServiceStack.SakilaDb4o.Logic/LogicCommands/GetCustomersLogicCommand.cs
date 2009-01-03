using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Extensions;
using ServiceStack.SakilaDb4o.Logic.LogicInterface.Requests;

namespace ServiceStack.SakilaDb4o.Logic.LogicCommands
{
	public class GetCustomersLogicCommand : LogicCommandBase<IList<Customer>>
	{
		public CustomersRequest Request { get; set; }

		public override IList<Customer> Execute()
		{
			var customers = Provider.GetByIds<Customer>(this.Request.CustomerIds);
			return customers;
		}
	}
}