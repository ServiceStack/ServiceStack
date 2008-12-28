/*
// $Id: GetCustomersAction.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.Validation;

namespace ServiceStack.Sakila.Logic.LogicCommands
{
	public class StoreCustomersLogicCommand : LogicCommandBase<bool>
	{
		public List<Customer> Customers { get; set; }

		public override bool Execute()
		{
			ThrowAnyValidationErrors(Validate());

			using (var transaction = Provider.BeginTransaction())
			{
				//var dbCustomer = Provider.CreateNewCustomer(this.CustomerDetails.CustomerName);
				//dbCustomer.Balance = 0;
				//dbCustomer.GlobalId = Guid.NewGuid().ToByteArray();
				//dbCustomer.CustomerName = this.CustomerDetails.CustomerName;
				//dbCustomer.Email = this.CustomerDetails.Email;
				//dbCustomer.Title = this.CustomerDetails.Title;
				//dbCustomer.FirstName = this.CustomerDetails.FirstName;
				//dbCustomer.LastName = this.CustomerDetails.LastName;
				//dbCustomer.CountryName = this.CustomerDetails.CountryName;
				//dbCustomer.LanguageCode = this.CustomerDetails.LanguageCode;
				//dbCustomer.SaltPassword = passwordHelper.SaltedPassword;
				//dbCustomer.CanNotifyEmailBool = this.CustomerDetails.CanNotifyEmail;
				//dbCustomer.StoreCreditCardBool = this.CustomerDetails.StoreCreditCard;

				//Provider.Store(dbCustomer);

				transaction.Commit();
			}

			return true;
		}


		public override ValidationResult Validate()
		{
			var errors = base.Validate().Errors;
			//var existingCustomer = Provider.GetCustomerByCustomerName(this.CustomerDetails.CustomerName);
			//if (existingCustomer != null)
			//{
			//    errors.Add(new ValidationError(ErrorCodes.CustomerAlreadyExists.ToString(), "CustomerName"));
			//}

			return new ValidationResult(errors, MessageCodes.NewCustomerCreated.ToString(),
				MessageCodes.CouldNotRegisterNewCustomer.ToString());
		}
	}
}