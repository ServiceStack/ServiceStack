using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Extensions;

namespace ServiceStack.SakilaDb4o.Logic.LogicCommands
{
	public class GetAllCustomersLogicCommand : LogicCommandBase<IList<Customer>>
	{
		public override IList<Customer> Execute()
		{
			var customers = Provider.GetAll<Customer>();
			return customers;
		}
	}
}