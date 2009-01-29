using DtoTypes = ServiceStack.Translators.Generator.Tests.Support.DataContract;
using DomainModel = ServiceStack.Translators.Generator.Tests.Support.Model;

namespace ServiceStack.Translators.Generator.Tests.Support
{
	[TranslateExtension(typeof(DtoTypes.Address),     typeof(DomainModel.Address))]
	[TranslateExtension(typeof(DtoTypes.Customer),    typeof(DomainModel.Customer))]
	[TranslateExtension(typeof(DtoTypes.PhoneNumber), typeof(DomainModel.PhoneNumber))]
	public static partial class ServiceModelTranslator
	{
	}
}