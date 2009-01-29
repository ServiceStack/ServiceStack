namespace ServiceStack.Translators.Generator.Tests.Support
{
	[TranslateExtension(typeof(DataContract.Address),     "ToDto", typeof(Model.Address),     "ToDomainModel")]
	[TranslateExtension(typeof(DataContract.Customer),    "ToDto", typeof(Model.Customer),    "ToDomainModel")]
	[TranslateExtension(typeof(DataContract.PhoneNumber), "ToDto", typeof(Model.PhoneNumber), "ToDomainModel")]
	public static class ExplicitTranslator
	{}
}