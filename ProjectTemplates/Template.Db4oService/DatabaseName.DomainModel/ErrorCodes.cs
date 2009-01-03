namespace @DomainModelNamespace@
{
	public enum ErrorCodes
	{
		FieldIsRequired,
		InvalidOrExpiredSession,
		@ModelName@AlreadyExists,
		EmailAddressIsNotValid,
	}

	public enum MessageCodes
	{
		New@ModelName@Created,
		CouldNotRegisterNew@ModelName@,
	}
}