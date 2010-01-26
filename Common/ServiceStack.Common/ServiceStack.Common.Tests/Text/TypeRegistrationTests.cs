using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Common.Text;


namespace ServiceStack.Common.Tests.Text
{

#if STATIC_ONLY

	[TestFixture]
	public class TypeRegistrationTests
	{

		[Test]
		public void RegisterTypes_register_all_types_required_for_a_model()
		{
			//Hard to test without debugging

			TypeRegistration.RegisterType<ModelWithFieldsOfDifferentTypes>();

			var dto = ModelWithFieldsOfDifferentTypes.Create(1);

			var dtoString = StringSerializer.SerializeToString(dto);
			var toDto = StringSerializer.DeserializeFromString<ModelWithFieldsOfDifferentTypes>(dtoString);

			Assert.That(toDto, Is.Not.Null);
		}

	}

#endif

}