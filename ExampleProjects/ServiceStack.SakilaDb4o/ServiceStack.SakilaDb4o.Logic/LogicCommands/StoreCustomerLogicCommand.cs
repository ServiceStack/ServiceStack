using System;
using System.Collections.Generic;
using ServiceStack.Validation;
using Sakila.DomainModel;

namespace ServiceStack.SakilaDb4o.Logic.LogicCommands
{
	public class StoreCustomerLogicCommand : LogicCommandBase<bool>
	{
		public Customer Customer { get; set; }

		public override bool Execute()
		{

			using (var transaction = Provider.BeginTransaction())
			{
				var dbCustomer = new Customer {
					Id = this.Customer.Id
				};
				Provider.Save(dbCustomer);

				transaction.Commit();
			}

			return true;
		}


		public override ValidationResult Validate()
		{
			var errors = base.Validate().Errors;

			return new ValidationResult(errors, "Customer saved", "Could not save customer");
		}
	}
}