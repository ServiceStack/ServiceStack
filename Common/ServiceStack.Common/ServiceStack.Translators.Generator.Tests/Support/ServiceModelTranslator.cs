using DtoTypes = ServiceStack.Translators.Generator.Tests.Support.DataContract;
using DomainModel = ServiceStack.Translators.Generator.Tests.Support.Model;

namespace ServiceStack.Translators.Generator.Tests.Support
{
	[TranslateModelExtention(typeof(DtoTypes.Address), typeof(DomainModel.Address))]
	[TranslateModelExtention(typeof(DtoTypes.Customer), typeof(DomainModel.Customer))]
	[TranslateModelExtention(typeof(DtoTypes.PhoneNumber), typeof(DomainModel.PhoneNumber))]
	public static partial class ServiceModelTranslator
	{
	}
}