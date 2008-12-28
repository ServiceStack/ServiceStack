/*
// $Id: Get@ModelName@sAction.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System;
using System.Collections.Generic;
using System.Net.Mail;
using @DomainModelNamespace@.@ServiceName@;
using @DomainModelNamespace@.Validation;
using @ServiceNamespace@.Logic.Support;

namespace @ServiceNamespace@.Logic.LogicCommands
{
	public class StoreNew@ModelName@LogicCommand : LogicCommandBase<bool>
	{
		public @ModelName@Details @ModelName@Details { get; set; }
		public CreditCardInfo CardInfo { get; set; }
		public string Base64EncryptedPassword { get; set; }
		public string Base64EncryptedConfirmPassword { get; set; }

		public override bool Execute()
		{

			var passwordHelper = PasswordHelper.Create(AppContext.ServerPrivateKey,
			                                           this.Base64EncryptedPassword, this.Base64EncryptedConfirmPassword);

			ThrowAnyValidationErrors(Validate(passwordHelper));

			using (var transaction = Provider.BeginTransaction())
			{
				var db@ModelName@ = Provider.CreateNew@ModelName@(this.@ModelName@Details.@ModelName@Name);
				db@ModelName@.Balance = 0;
				db@ModelName@.GlobalId = Guid.NewGuid().ToByteArray();
				db@ModelName@.@ModelName@Name = this.@ModelName@Details.@ModelName@Name;
				db@ModelName@.Email = this.@ModelName@Details.Email;
				db@ModelName@.Title = this.@ModelName@Details.Title;
				db@ModelName@.FirstName = this.@ModelName@Details.FirstName;
				db@ModelName@.LastName = this.@ModelName@Details.LastName;
				db@ModelName@.Country = this.@ModelName@Details.Country;
				db@ModelName@.LanguageCode = this.@ModelName@Details.LanguageCode;
				db@ModelName@.SaltPassword = passwordHelper.SaltedPassword;
				db@ModelName@.CanNotifyEmailBool = this.@ModelName@Details.CanNotifyEmail;
				db@ModelName@.StoreCreditCardBool = this.@ModelName@Details.StoreCreditCard;

				Provider.Store(db@ModelName@);

				if (this.@ModelName@Details.StoreCreditCard)
				{
					db@ModelName@.SingleClickBuyEnabledBool = this.@ModelName@Details.SingleClickBuyEnabled;
					var cardInfo = new DataAccess.DataModel.CreditCardInfo {
					                                                       		CardType = this.CardInfo.CardType.ToString(),
					                                                       		CardNumber = this.CardInfo.CardNumber,
					                                                       		CardHolderName = this.CardInfo.CardHolderName,
					                                                       		CardCvv = this.CardInfo.CardCvv,
					                                                       		CardExpiryDate = this.CardInfo.ExpiryDate,
					                                                       		BillingAddressLine1 = this.CardInfo.BillingAddressLine1,
					                                                       		BillingAddressLine2 = this.CardInfo.BillingAddressLine2,
					                                                       		BillingAddressTown = this.CardInfo.BillingAddressTown,
					                                                       		BillingAddressCounty = this.CardInfo.BillingAddressCounty,
					                                                       		BillingAddressPostCode = this.CardInfo.BillingAddressPostCode,
					                                                       		IsActiveBool = true,
					                                                       };
					db@ModelName@.PrimaryCreditCard = cardInfo;
					Provider.Store(cardInfo);
				}

				transaction.Commit();
			}

			SendWelcomeMessage(this.@ModelName@Details.Title, this.@ModelName@Details.LastName, this.@ModelName@Details.Email);

			return true;
		}

		private static void SendWelcomeMessage(string title, string lastName, string email)
		{
			var message = new MailMessage {
			                              		From = new MailAddress("donotreply@ddnglobal.com"),
			                              		Subject = "Welcome to PoToPe",
			                              		Body = string.Format(@"Welcome to Potope!

Dear {0} {1},

Thanks for registering with PoToPe


You have registered with the following email address:
{2}	

Please click the link below to activate your account:
http://www.potope.com/validateEmail.ashx?key=hVNwLhnMK5zxaGKBh4KZ

Should you forget your password, don't worry; simply go to potope.com  and click on 'My Account' then click
the 'Forgotten your password?' link for instructions on how to reset your password.

If you need to contact us for any further information, please click
on the link below:
http://www.poptope.com/contact

We hope you enjoy shopping with us.

Kind regards,
PoToPe Customer Services", title, lastName, email),
			                              };
			message.To.Add(new MailAddress(email));
			var client = new SmtpClient();
			client.Send(message);
		}

		public override ValidationResult Validate()
		{
			var passwordHelper = PasswordHelper.Create(AppContext.ServerPrivateKey,
			                                           this.Base64EncryptedPassword, this.Base64EncryptedConfirmPassword);
			return Validate(passwordHelper);
		}

		private ValidationResult Validate(PasswordHelper passwordHelper)
		{
			var errors = new List<ValidationError>();
			errors.AddRange(this.@ModelName@Details.Validate().Errors);

			if (this.@ModelName@Details.StoreCreditCard)
			{
				if (this.CardInfo == null)
				{
					errors.Add(new ValidationError(ErrorCodes.CreditInfoIsRequiredIfStoreCreditCard.ToString(), 
					                               "StoreCreditCard"));
				}
				else
				{
					errors.AddRange(this.CardInfo.Validate().Errors);
				}
			}

			if (string.IsNullOrEmpty(passwordHelper.Password))
			{
				errors.Add(new ValidationError(ErrorCodes.FieldIsRequired.ToString(), "Password"));
			}

			if (!passwordHelper.PasswordsAreEqual)
			{
				errors.Add(new ValidationError(ErrorCodes.PasswordsAreNotEqual.ToString(), "Password"));
			}

			var existing@ModelName@ = Provider.Get@ModelName@By@ModelName@Name(this.@ModelName@Details.@ModelName@Name);
			if (existing@ModelName@ != null)
			{
				errors.Add(new ValidationError(ErrorCodes.@ModelName@AlreadyExists.ToString(), "@ModelName@Name"));
			}

			return new ValidationResult(errors, 
			                            MessageCodes.New@ModelName@Created.ToString(),
			                            MessageCodes.CouldNotRegisterNew@ModelName@.ToString());
		}
	}
}