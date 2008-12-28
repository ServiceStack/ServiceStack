namespace Sakila.DomainModel
{
	public enum ErrorCodes
	{
		FieldIsRequired,
		InvalidOrExpiredSession,
		CustomerAlreadyExists,
		EmailAddressIsNotValid,
	}

	public enum MessageCodes
	{
		NewCustomerCreated,
		CouldNotRegisterNewCustomer,
	}
}