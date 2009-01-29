using DtoTypes = ServiceStack.Translators.Generator.Tests.Support.DataContract;
using DomainModel = ServiceStack.Translators.Generator.Tests.Support.Model;

namespace ServiceStack.Translators.Generator.Tests.Support
{
	[TranslateModelExtensionAttribute(typeof(DtoTypes.Address), typeof(DomainModel.Address))]
	[TranslateModelExtensionAttribute(typeof(DtoTypes.Customer), typeof(DomainModel.Customer))]
	[TranslateModelExtensionAttribute(typeof(DtoTypes.PhoneNumber), typeof(DomainModel.PhoneNumber))]
	public static partial class ServiceModelTranslator
	{
	}
}