using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Common.Extensions;
using ServiceStack.Sakila.Logic.LogicInterface.Requests;
using ServiceStack.Sakila.Logic.Translators.DataToDomain;
using ServiceStack.Validation;
using DataModel = ServiceStack.Sakila.DataAccess.DataModel;

namespace ServiceStack.Sakila.Logic.LogicCommands
{
	public class GetCustomersLogicCommand : LogicCommandBase<List<Customer>>
	{
		public CustomersRequest Request { get; set; }

		public override List<Customer> Execute()
		{
			ThrowAnyValidationErrors(Validate());
			var dbCustomers = Provider.GetCustomers(this.Request.CustomerIds);
			return CustomerTranslator.Instance.ParseAll(dbCustomers);
		}

		public override ValidationResult Validate()
		{
			var validationResult = base.Validate();
			return validationResult;
		}
	}
}