/*
// $Id$
//
// Revision      : $Revision$
// Modified Date : $LastChangedDate$
// Modified By   : $LastChangedBy$
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

namespace @DomainModelNamespace@
{
	/// <summary>
	/// This is called MessageCodes to be consistent with ErrorCodes
	/// </summary>
	public enum MessageCodes
	{
		NewUserCreated,
		LoginWasSuccessful,
		CouldNotRegisterNewUser,
		CreditCardDetailsAreInvalid,
	}

	/// <summary>
	/// This enum is called ErrorCodes rather that ErrorCode to avoid naming conficts with 
	/// the ValidationAttributeBase.ErrorCode field. If you can think of better names refactor!
	/// </summary>
	public enum ErrorCodes 
	{
		Error, //General unspecified error
		InvalidUserOrPassword,
		UserAlreadyExists,
		InvalidOrExpiredSession,

		//Valdiation errors:
		FieldIsRequired,
		FieldsAreNotEqual,
		PasswordsAreNotEqual,
		EmailAddressIsNotValid,
		
		//Credit card validation errors:		
		CreditCardNumberIsInvalid,
		CreditCardTypeIsInvalid,
		CreditCardHasExpired,
		CreditCardCvvIsInvalid,
		CreditCardHolderNameIsInvalid,

		//Business rules:
		CreditInfoIsRequiredIfStoreCreditCard,
	}
}