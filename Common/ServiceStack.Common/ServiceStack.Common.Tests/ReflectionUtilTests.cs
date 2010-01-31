using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Tests.Support;
using ServiceStack.Common.Utils;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests
{
	[TestFixture]
	public class ReflectionUtilTests
	{
		public class TestClass
		{
			[Required]
			public string Member1 { get; set; }

			public string Member2 { get; set; }

			[Required]
			public string Member3 { get; set; }

			[StringLength(1)]
			public string Member4 { get; set; }
		}

		[Test]
		public void GetTest()
		{
			var propertyAttributes = ReflectionUtils.GetPropertyAttributes<RequiredAttribute>(typeof(TestClass));
			var propertyNames = propertyAttributes.ToList().ConvertAll(x => x.Key.Name);
			Assert.That(propertyNames, Is.EquivalentTo(new[] { "Member1", "Member3" }));
		}

		[Test]
		public void Populate_Same_Objects()
		{
			var toObj = ModelWithFieldsOfDifferentTypes.Create(1);
			var fromObj = ModelWithFieldsOfDifferentTypes.Create(2);

			var obj3 = (ModelWithFieldsOfDifferentTypes)ReflectionUtils.PopulateObject(toObj, fromObj);

			Assert.IsTrue(obj3 == toObj);
			Assert.That(obj3.Bool, Is.EqualTo(fromObj.Bool));
			Assert.That(obj3.DateTime, Is.EqualTo(fromObj.DateTime));
			Assert.That(obj3.Double, Is.EqualTo(fromObj.Double));
			Assert.That(obj3.Guid, Is.EqualTo(fromObj.Guid));
			Assert.That(obj3.Id, Is.EqualTo(fromObj.Id));
			Assert.That(obj3.LongId, Is.EqualTo(fromObj.LongId));
			Assert.That(obj3.Name, Is.EqualTo(fromObj.Name));
		}

		[Test]
		public void Populate_Different_Objects_with_different_property_types()
		{
			var toObj = ModelWithFieldsOfDifferentTypes.Create(1);
			var fromObj = ModelWithOnlyStringFields.Create("2");

			var obj3 = (ModelWithFieldsOfDifferentTypes)
				ReflectionUtils.PopulateObject(toObj, fromObj);

			Assert.IsTrue(obj3 == toObj);
			Assert.That(obj3.Id, Is.EqualTo(2));
			Assert.That(obj3.Name, Is.EqualTo(fromObj.Name));
		}

		[Test]
		public void Populate_From_Properties_With_Attribute()
		{
			var originalToObj = ModelWithOnlyStringFields.Create("id-1");
			var toObj = ModelWithOnlyStringFields.Create("id-1");
			var fromObj = ModelWithOnlyStringFields.Create("id-2");

			ReflectionUtils.PopulateFromPropertiesWithAttribute(toObj, fromObj,
				typeof(IndexAttribute));

			Assert.That(toObj.Id, Is.EqualTo(originalToObj.Id));
			Assert.That(toObj.AlbumId, Is.EqualTo(originalToObj.AlbumId));

			//Properties with IndexAttribute
			Assert.That(toObj.Name, Is.EqualTo(fromObj.Name));
			Assert.That(toObj.AlbumName, Is.EqualTo(fromObj.AlbumName));
		}

		[Test]
		public void Populate_From_Properties_With_Non_Default_Values()
		{
			var toObj = ModelWithFieldsOfDifferentTypes.Create(1);
			var fromObj = ModelWithFieldsOfDifferentTypes.Create(2);

			var originalToObj = ModelWithFieldsOfDifferentTypes.Create(1);
			var originalGuid = toObj.Guid;

			fromObj.Name = null;
			fromObj.Double = default(double);
			fromObj.Guid = default(Guid);

			ReflectionUtils.PopulateWithNonDefaultValues(toObj, fromObj);

			Assert.That(toObj.Name, Is.EqualTo(originalToObj.Name));
			Assert.That(toObj.Double, Is.EqualTo(originalToObj.Double));
			Assert.That(toObj.Guid, Is.EqualTo(originalGuid));

			Assert.That(toObj.Id, Is.EqualTo(fromObj.Id));
			Assert.That(toObj.LongId, Is.EqualTo(fromObj.LongId));
			Assert.That(toObj.Bool, Is.EqualTo(fromObj.Bool));
			Assert.That(toObj.DateTime, Is.EqualTo(fromObj.DateTime));
		}
	}
}
