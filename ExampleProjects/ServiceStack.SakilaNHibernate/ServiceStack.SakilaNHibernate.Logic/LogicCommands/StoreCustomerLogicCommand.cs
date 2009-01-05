using System;
using System.Collections.Generic;
using ServiceStack.Validation;
using Sakila.DomainModel;

namespace ServiceStack.SakilaNHibernate.Logic.LogicCommands
{
	public class StoreCustomerLogicCommand : LogicCommandBase<bool>
	{
		public Customer Customer { get; set; }

		public override bool Execute()
		{

			using (var transaction = Provider.BeginTransaction())
			{
				var dbCustomer = new DataAccess.DataModel.Customer {
					Id = (ushort)this.Customer.Id,
				};
				Provider.Store(dbCustomer);

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